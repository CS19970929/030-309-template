# 存储与 IAP 梳理文档

## 1. 目的

本文梳理当前工程中所有与存储相关的地址、数据流、阈值持久化逻辑、睡眠标志、IAP 交互链路，并给出问题判断与优化建议。

结论先行：

- 当前 `APP` 代码区与内部 `Flash` 保留页未发现直接地址重叠。
- 当前版本已经完成两项迁移：
  - 睡眠启动标志已从内部 `Flash` 迁移到 `RTC backup register`
  - 电流 offset 已从内部 `Flash` 迁移到外部 `EEPROM`
- 当前仍保留一项旧协议：
  - `APP -> IAP` 升级请求仍通过内部 `Flash` 地址 `0x0800F800` 传递
- 当前主要风险不在“地址冲突”，而在“异常值加载后继续运行”“参数换算除零”“首次 AFE 初始化无限重试”“Flash 擦页无超时”

## 2. 存储介质与职责划分

### 2.1 内部 Flash

用途分为两类：

- 程序代码区
- 少量保留页，用于历史兼容和 IAP 标志

地址定义见 [Code/Source/Flash.h](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Flash.h)。

### 2.2 外部 EEPROM

用途：

- 保护阈值参数
- OtherElement / HeatCool 参数
- 校准系数
- 事件记录
- 电流 offset
- 版本/首启标志

地址定义见 [Code/Source/EEPROM.h](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.h)。

### 2.3 RTC Backup Register

用途：

- 保存睡眠模式启动标志

实现见 [Code/Source/Flash.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Flash.c) 中 `BootFlag_Write/Read/Clear()`。

## 3. 内部 Flash 地址梳理

### 3.1 分区定义

- `FLASH_ADDR_IAP_START = 0x08000000`
- `FLASH_ADDR_APP_START = 0x08001C00`
- `FLASH_ADDR_SH367309_VALUE = 0x0800F000`
- `FLASH_ADDR_SH367309_FLAG = 0x0800F400`
- `FLASH_ADDR_UPDATE_FLAG = 0x0800F800`
- `FLASH_ADDR_SLEEP_FLAG = 0x0800FC00`

定义来源：

- [Code/Source/Flash.h](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Flash.h#L6)
- [Code/Source/iap/main.h](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/iap/main.h#L16)

### 3.2 APP 代码区占用

根据当前 map 文件：

- `RESET` 位于 `0x08001C00`
- `Total ROM Size = 47356 bytes`

折算后当前 APP ROM 末尾约为：

- `0x0800D4FB`

与保留页起始地址 `0x0800F000` 之间仍有余量，因此当前未看到 APP 代码覆盖保留页的问题。

依据：

- [CommomSH367309_16series_030C8T6_C.map](/Users/cs/Downloads/work/todo/030-309-template/CommomSH367309_16series_030C8T6_C.map)

### 3.3 当前各保留页状态

- `0x0800F000`
  - 历史上用于保存电流 offset
  - 当前仅作为 EEPROM 迁移失败时的一次性回读兼容入口
  - 读取见 [Code/Source/EEPROM.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.c#L656)
- `0x0800F400`
  - 当前代码中仅定义，未见实际使用
- `0x0800F800`
  - 当前仍用于 APP 与 IAP 的升级请求握手
- `0x0800FC00`
  - 历史 sleep flag 地址，当前已不再使用

## 4. 外部 EEPROM 地址梳理

### 4.1 顶部保留区

- `0x3FF6` `EEPROM_ADDR_CURRENT_OFFSET_INV`
- `0x3FF8` `EEPROM_ADDR_CURRENT_OFFSET`
- `0x3FFA` `EEPROM_ADDR_SLEEP`
- `0x3FFC` `EEPROM_ADDR_PASS`
- `0x3FFE` `EEPROM_ADDR_FLASHUPDATE`

说明：

- 当前真正仍在使用的是：
  - `CURRENT_OFFSET`
  - `CURRENT_OFFSET_INV`
  - `PASS`
- `SLEEP` 与 `FLASHUPDATE` 只剩历史定义，当前业务路径未见实际使用

依据：

- [Code/Source/EEPROM.h](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.h#L38)
- [Code/Source/EEPROM.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.c#L18)

### 4.2 参数区

当前主要参数区如下：

- Protect 参数：起始散布地址 `0 ~ 128`
- RTC 参数：`130 ~ 152`
- 校准 K：`154 ~ 246`
- 校准 B：`248 ~ 340`
- SOC 表：`342 ~ 424`
- 铜损表：`426 ~ 489`
- Fault Record：`490 ~ 675`
- OtherElement：`676 ~ 738`
- HeatCool：`740 ~ 788`
- Enhance SOC：`790`
- Serial / Hardware / Software Version：`830 / 870 / 910`
- Event Record：`1000 ~ 1200`

依据：

- [Code/Source/EEPROM.h](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.h#L250)

## 5. 启动加载与持久化流程

### 5.1 EEPROM 启动入口

启动链路：

1. `main -> InitDevice()`
2. `InitE2PROM()`
3. `InitData_E2prom()`

依据：

- [Code/Source/main.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/main.c#L114)
- [Code/Source/EEPROM.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.c#L575)

### 5.2 正常上电路径

若 `EEPROM_ADDR_PASS` 中保存的是 `EEPROM_VALUE_BEGIN_FLAG`，则走正常加载：

- `ReadEEPROM_ByteData_StartUp()`
- 加载 `OtherElement` 后计算 `g_u32CS_Res_AFE`
- 读取 EEPROM 中保存的 offset 及其反码
- 若 EEPROM offset 无效，则回退读取旧 Flash 地址 `0x0800F000`

依据：

- [Code/Source/EEPROM.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.c#L649)
- [Code/Source/EEPROM.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.c#L682)

### 5.3 首次上电路径

若 `EEPROM_ADDR_PASS` 不匹配：

- RAM 参数先恢复默认值
- 按标志位分批写 EEPROM
- 初始化生产信息与 SOC 工厂参数
- 初始化 AFE 参数
- 采样得到 offset 并写入 EEPROM
- 最后写入 `EEPROM_ADDR_PASS`

依据：

- [Code/Source/EEPROM.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.c#L690)

### 5.4 周期性 EEPROM 写回策略

运行时不是整块重写，而是按 `WriteFlag` 每次只写一项：

- `u8E2P_KB_WriteFlag`
- `u32E2P_Pro_VolCur_WriteFlag`
- `u32E2P_Pro_Temp_WriteFlag`
- `u32E2P_Pro_Other_WriteFlag`
- `u32E2P_OtherElement1_WriteFlag`
- `u32E2P_HeatCool_WriteFlag`
- `gu8_Reset_EventRecord`

执行入口：

- [Code/Source/EEPROM.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.c#L454)
- [Code/Source/EEPROM.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.c#L723)

该策略的优点：

- 减少单次写入时长
- 降低 EEPROM 写放大

该策略的限制：

- 多字段参数更新期间不是事务性提交
- 掉电时可能出现“部分新值 + 部分旧值”组合

## 6. 阈值、均衡、AFE 参数联动

### 6.1 Protect / Balance / OtherElement

串口写入后主要动作是：

- 先更新 RAM 参数
- 再拉起 EEPROM 写标志
- 对影响 AFE 的参数，拉起 `AFE_PARAM_WRITE_Flag`

代表路径：

- 均衡阈值写入：
  - [Code/Source/Sci_Upper.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Sci_Upper.c#L1012)
- 保护参数复位：
  - [Code/Source/Sci_Upper.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Sci_Upper.c#L1380)
- OtherElement 复位：
  - [Code/Source/Sci_Upper.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Sci_Upper.c#L1404)

### 6.2 AFE 配置下发闭环

`AFE_PARAM_WRITE_Flag` 最终由 `SH367309_UpdataAfeConfig()` 消费：

- 若标志置位，则刷新参数、写入 AFE、复位 AFE、重新使能功能

依据：

- [Code/Source/SH367309_DataDeal.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/SH367309_DataDeal.c#L162)
- [Code/Source/SH367309_Func.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/SH367309_Func.c#L630)

说明：

- 当前 AFE 参数更新闭环是存在的
- 但 AFE 参数自身的 EEPROM 存储接口当前大段被 `#if 0` 屏蔽，说明“AFE 参数单独落 EEPROM”这条旧设计目前并未启用

## 7. 睡眠标志与备份域

### 7.1 当前实现

睡眠前：

- 根据目标模式写 `RTC->BKP1R/BKP2R`
- 然后 `AFE_Sleep()`
- 再 `MCU_RESET()`

启动后：

- `IsSleepStartUp()` 读取 backup flag
- 根据不同 sleep 模式进入不同唤醒处理
- 完成后清除 backup flag

依据：

- [Code/Source/Flash.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Flash.c#L42)
- [Code/Source/SleepDeal.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/SleepDeal.c#L364)
- [Code/Source/SleepDeal.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/SleepDeal.c#L936)

### 7.2 评价

这部分迁移方向是正确的：

- 避免为 sleep flag 擦写整页 Flash
- 避免频繁擦写影响代码区安全性
- 使用正反码提升掉电一致性判断

## 8. IAP 链路梳理

### 8.1 APP 侧

收到升级连接命令后：

1. 调用 `IapRequest_ArmLegacyFlag()`
2. 向 `FLASH_ADDR_UPDATE_FLAG(0x0800F800)` 写入 `FLASH_TO_IAP_VALUE(0x00AB)`
3. 发送应答完成后置位 `u8FlashUpdateFlag`
4. 主循环中触发 `MCU_RESET()`

依据：

- [Code/Source/Sci_Upper.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Sci_Upper.c#L1200)
- [Code/Source/Comm.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Comm.c#L521)
- [Code/Source/Flash.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Flash.c#L70)

### 8.2 IAP 侧

IAP 启动后：

- 若 `FLASH_ADDR_UPDATE_FLAG == FLASH_TO_APP_VALUE(0xFFFF)`，则直接跳转 APP
- 否则留在 IAP

升级完成后：

- IAP 把 `FLASH_ADDR_UPDATE_FLAG` 再写回 `0xFFFF`
- 应答发送完成后执行 reset

依据：

- [Code/Source/iap/main.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/iap/main.c#L47)
- [Code/Source/iap/Sci.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/iap/Sci.c#L260)

### 8.3 评价

当前 IAP 协议在“兼容旧 Bootloader”前提下可工作，但属于遗留协议：

- 仅靠单一 Flash 半字表示状态
- 写入前需要整页擦除
- 无版本、无序列号、无 CRC、无双向确认
- 掉电恢复能力弱

## 9. 当前确认到的问题

### 9.1 不是地址冲突问题，而是异常值处理问题

`EEPROM` 启动加载时，多数参数是：

- 先读出并写入 RAM
- 再做范围判断
- 异常时仅记 `ERROR_EEPROM_STORE`

但不会：

- 回退默认值
- 拉写回标志
- 阻止坏值继续参与运行时换算

代表代码：

- [Code/Source/EEPROM.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.c#L313)

这意味着：

- EEPROM 若出现单点损坏，系统可能带着坏阈值继续运行

### 9.2 `CS resistor` 相关参数存在除零风险

当前多处直接使用：

- `g_u32CS_Res_AFE = (...) / OtherElement.u16Sys_CS_Res`

见：

- [Code/Source/EEPROM.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.c#L655)
- [Code/Source/main.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/main.c#L170)
- [Code/Source/SH367309_DataDeal.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/SH367309_DataDeal.c#L59)
- [Code/Source/ShortFunc.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/ShortFunc.c#L43)

若 `u16Sys_CS_Res == 0` 或 `u16Sys_CS_Res_Num == 0`：

- 会直接导致换算错误
- 严重时会触发启动异常

### 9.3 首次 AFE 初始化为无限重试

当前首次初始化：

- `do { ... ret = SH367309_UpdataAfeConfig(); } while (ret == false);`

见：

- [Code/Source/EEPROM.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/EEPROM.c#L704)

风险：

- AFE I2C 或上电时序异常时，系统可能永久卡死在启动阶段

### 9.4 Flash 擦页无超时

APP 与 IAP 两侧都存在：

- `while (FLASH_ErasePage(...) != FLASH_COMPLETE);`

见：

- [Code/Source/Flash.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Flash.c#L92)
- [Code/Source/iap/Sci.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/iap/Sci.c#L41)

风险：

- Flash 异常时可能死等

### 9.5 遗留地址定义仍较多

当前仍保留多组历史地址和标志定义，但部分已迁移不用：

- `EEPROM_ADDR_SLEEP`
- `EEPROM_ADDR_FLASHUPDATE`
- `FLASH_ADDR_SLEEP_FLAG`
- `FLASH_ADDR_SH367309_FLAG`

风险：

- 后续维护时容易误以为仍在使用

## 10. 优化建议

### 10.1 建议优先级 P1

- 建立统一的参数校验与回退接口
- 对 EEPROM 启动加载实行：
  - 读出
  - 校验
  - 非法则回退默认
  - 置写回标志
  - 上报错误

### 10.2 建议优先级 P1

- 将 `CS resistor` 相关计算统一封装
- 所有换算前先检查：
  - `u16Sys_CS_Res != 0`
  - `u16Sys_CS_Res_Num != 0`

### 10.3 建议优先级 P1

- 首次 AFE 初始化改为有限重试
- 建议：
  - 固定重试次数
  - 失败后进入故障态或降级态
  - 避免无限卡死

### 10.4 建议优先级 P2

- 为 `FlashWriteOneHalfWord()` 增加超时与错误返回
- APP/IAP 两边保持一致处理

### 10.5 建议优先级 P2

- 将“当前在用地址”和“legacy 地址”分组定义
- 在头文件中明确标注：
  - active
  - compatibility only
  - deprecated

### 10.6 建议优先级 P3

- 中长期将 IAP 请求协议从 Flash 单 flag 迁移到 RTC backup flag 或更完整的升级 mailbox 协议
- 前提是 Bootloader 与 APP 同步升级

## 11. 总结

当前工程在存储地址布局上整体可用，未发现 APP 代码覆盖保留页的问题。已完成的两项迁移方向是正确的：睡眠标志迁到 backup 域，offset 迁到 EEPROM。当前最需要关注的不是地址，而是异常值处理与鲁棒性边界，尤其是：

- EEPROM 异常值未回退
- `CS resistor` 参数可能除零
- 首次 AFE 初始化无限重试
- Flash 擦页无超时

如果后续要继续演进，建议按“参数校验统一化 -> 关键换算防御化 -> 启动流程有限重试 -> IAP 协议去 Flash 化”的顺序推进。
