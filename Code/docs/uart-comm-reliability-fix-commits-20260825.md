# UART / RS485 通信可靠性修复提交说明

日期：2026-08-25  
修复分支：`fix/uart-comm-reliability`  
基线分支：`mot-dev`  
基线提交：`ab58f0d7ae0fe29650fae03dfd2340d5cc01e779`

## 1. 背景

本分支针对 STM32F030 + USART1 RS485 / USART2 UART 通信模块的专项审核结果进行修复。

现场通信日志表现为：通信整体不会连续停摆数秒，但存在偶发单次轮询事务缺失，下一轮又可恢复。代码审核同时发现 RS485 TX→RX 切换、非本机地址响应、UART 错误恢复、RX/TX 缓冲复用、临界区、普通 UART 全双工、Modbus 广播处理以及寄存器魔数等可靠性问题。

本分支遵循“一类问题一个 commit”的原则，便于逐项 review、cherry-pick、回退和现场 A/B 验证。

## 2. Commit 说明

| # | Commit | 提交信息 | 修复内容 |
|---|---|---|---|
| 1 | `e29a44e` | `fix(comm): wait for USART transmission complete` | 修复 `TRANS_485_WAIT_COMPLETE()` 错把 `TXE(bit7)` 当作发送完成的问题，改为等待 `USART_ISR_TC`，避免最后一个字节尚未真正发送完成就切换 RS485 方向。 |
| 2 | `48f364f` | `fix(comm): restore receive mode immediately on TC` | USART TC 中断到达后立即将 RS485 切回接收、清除 `tx_active` 并重新使能 RX；主循环只保留发送完成后的业务通知，消除“物理发送已结束但等待下一次 `Comm_PollAll()` 才恢复接收”的盲区。 |
| 3 | `0702146` | `fix(comm): ignore ASCII frames for other slave addresses` | ASCII/Pylon 类协议收到其他从机地址时静默丢弃，不再回复 `0x90` 地址错误，避免多 BMS 共 RS485 总线时多个从机同时应答导致总线冲突。非法地址编码仍交由协议层返回格式错误。 |
| 4 | `70aebdf` | `fix(comm): separate RX ring from TX buffer` | 将 RX ring 与 TX buffer 从共用 `union` 拆成独立缓冲区，防止发送回复时覆盖已经缓存的后续接收数据；同时为 USART2 全双工接收奠定基础。 |
| 5 | `d464c28` | `fix(comm): preserve interrupt state in critical sections` | 临界区由无条件 `DISABLE_INT()/ENABLE_INT()` 改为保存并恢复 `PRIMASK`，避免调用者原本处于关中断状态时被通信模块错误提前开中断。 |
| 6 | `0ab0494` | `fix(comm): recover cleanly from UART receive errors` | 重构 FE/NE/ORE/PE 处理：带硬件错误的 RX 字节读取后直接丢弃，不进入 ring；统一清 ICR；当前损坏帧 reset+flush；普通 parser 格式错误只重置 parser、不清掉 ring 中后续数据，提高自动重新同步能力。 |
| 7 | `62fa0c3` | `fix(comm): keep non-RS485 UART receive path active during TX` | USART2 为普通 UART 时发送期间不再关闭 RE/RXNE，可继续把同时到达的数据存入独立 RX ring，发送结束后再解析；USART1 RS485 仍保持半双工策略。 |
| 8 | `9f4bf8e` | `fix(comm): suppress Modbus broadcast responses` | Modbus `0x00` 广播地址：读命令静默忽略；写单寄存器/写多寄存器仍执行，但不产生响应，避免广播场景多从机同时回复。 |
| 9 | `694226c` | `refactor(comm): use STM32 USART register bit definitions` | 将 `CR1/CR3` 中 `(1 << n)` 魔数替换为 `USART_CR1_RE`、`USART_CR3_EIE`、`USART_CR3_OVRDIS` 等 STM32 CMSIS 位定义，降低寄存器位误用风险。 |
| 10 | `421a380` | `refactor(comm): clarify TX completion state naming` | 将旧 `tx_switchback_pending` / `Comm_PortProcessTxSwitchback` 重命名为真正含义的 `tx_complete_pending` / `Comm_PortProcessTxComplete`；将延时宏改名为 `COMM_RS485_TX_ENABLE_DELAY_US`，避免后续维护人员误以为 RX 切换仍在主循环执行。 |
| 11 | `6fc5e76` | `feat(comm): add runtime communication diagnostics` | 增加 RX 字节数、完整帧数、TX 帧数、ORE/FE/NE/PE、ring overflow、parser error、接收超时、非本机地址忽略、错误字节丢弃等运行计数器，为现场偶发断讯定责提供数据。 |

## 3. 最终通信架构

最终数据路径：

```text
USART IRQ
  ├─ RXNE → 检查 UART error → 合法字节进入 RX ring
  ├─ TXE  → 从独立 TX buffer 向 TDR 发送
  └─ TC   → 立即恢复 RS485 RX / 清 tx_active
                ↓
           Comm_PollAll()
                ↓
        RX ring → Parser
                ↓
       ASCII / Modbus Service
                ↓
          Comm_PortStartTx()
```

关键原则：

- ISR 负责确定性的硬件收发和方向切换，不在 ISR 做协议业务。
- 协议解析、寄存器访问、回复构造继续放在主循环。
- USART1 按 RS485 半双工处理；USART2 按普通 UART 全双工处理。
- 物理层错误只破坏当前帧，并尽快重新同步，不让错误字节污染下一帧。
- 多从机总线中，非本机 ASCII 帧和 Modbus 广播响应均采用正确的静默策略。

## 4. RAM 影响

现有基线 `.map` 显示 `Total RW Size = 6896 B`，STM32F030C8T6 SRAM 为 8 KB，基线余量约 1296 B。

本次主要新增 RAM：

- RX/TX 缓冲分离：两路端口合计约 `+520 B`。
- 新增诊断计数器：两路端口合计约 `+40 B`。

因此按结构字段静态估算，新的 RW 使用量约 `7456 B`，预计余量约 `736 B`。该值只是基于原 `.map` 的估算，最终必须以本分支重新 Keil ARMCC5 链接生成的 `.map` 为准。

## 5. 验证状态

Git 分支差异已核对：本分支相对 `mot-dev` 为 **ahead 11 / behind 0**，11 个修复 commit 为线性提交；代码修改范围仅涉及：

- `Code/Source/Comm.c`
- `Code/Source/Comm.h`
- `Code/Source/conf/conf_gpio.h`
- `Code/Source/modbus_service.c`

当前 GitHub 仓库没有可用的 CI/status check；本次通过 GitHub 侧完成源码审核、逐提交 diff 检查和分支一致性检查，但无法替代本地 Keil ARMCC5 的最终编译/链接及硬件长稳测试。

## 6. 建议验收

建议在合并到 `mot-dev` 前至少完成以下验证：

1. Keil ARMCC5 clean rebuild，要求 0 error；检查新的 `.map`，确认 SRAM/Flash 余量满足项目要求。
2. USART1/RS485 9600 连续轮询至少 10 万次，统计请求数、响应数和上位机超时数，目标为 0 次无故漏响应。
3. 同时观察 `overrun_count`、`frame_error_count`、`noise_error_count`、`parity_error_count`、`parser_error_count`、`rx_timeout_count`、`ring_overflow_count`；正常环境下不应持续增长。
4. 多地址 RS485 测试：发送给 `0x22/0x32/...` 时 `0x12` 节点必须保持静默。
5. Modbus `0x00` 广播测试：写命令可以执行但不得回复；广播读命令不得回复。
6. USART2 19200 全双工测试：TX 期间持续注入 RX 数据，确认数据不丢且 TX 完成后能正常解析。
7. 用逻辑分析仪同步抓 RS485 A/B（或 RO/DI）、PA9 TX、PA10 RX、PB13 DE；出现一次超时时即可区分 MCU 未收、MCU 未发、DE 时序、485 收发器或 PC/USB-485 采集端问题。

## 7. 关于现场偶发断讯

本分支已修复审核中能够从源码明确确认的通信软件风险，并补足现场诊断能力。原日志中还出现过非 ASCII 异常字节，因此物理层噪声、RS485 收发器、线束/终端匹配以及 USB-485/日志采集端仍属于需要通过实机波形和上述计数器验证的外部因素；这些因素不能仅通过源码修改直接判定已经消失。
