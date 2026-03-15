# BMS 通讯双协议重构说明

## 背景

原项目通信层主要由 `MODBUS RTU` 的 `Sci_Upper.c/.h` 承担，存在以下问题：

- `SCI1` 和 `SCI2` 的接收、发送、初始化逻辑重复。
- 串口中断直接操作协议状态，硬件访问和协议处理耦合较深。
- 新增的 `ascii_slave.c/.h` 与当前工程风格不一致，依赖独立 UART/LL 接口，无法直接并入。
- 协议扩展能力弱，后续再增加新协议会继续复制分支代码。

本次改造目标是让 `SCI1/SCI2` 都支持同口自动识别 `MODBUS RTU + ASCII`，同时保留现有业务变量、寄存器读写和 EEPROM/Flash 流程。

## 重构结果

### 1. 统一通讯内核

新增文件：

- `Code/Source/Comm.h`
- `Code/Source/Comm.c`

核心结构：

- `CommPortContext`
- `ProtocolType`
- `CommRequest`
- `CommResponse`

每个串口对应一个 `CommPortContext`，统一维护：

- 串口实例
- 当前协议类型
- 接收缓冲
- 发送缓冲
- 发送状态
- 错误计数
- `MODBUS` 上下文
- `ASCII` 解析上下文

主循环统一调用：

- `Comm_InitAll()`
- `Comm_PollAll()`

中断统一调用：

- `Comm_PortIrqHandler(&g_comm_port1)`
- `Comm_PortIrqHandler(&g_comm_port2)`

### 2. 协议自动分流

分流规则：

- 首字节 `0x7E` 进入 `ASCII`
- 首字节为 `RS485_SLAVE_ADDR` 或 `0x00` 进入 `MODBUS RTU`
- 其他字节直接丢弃

每路串口在一帧未结束前锁定协议，防止混包。

### 3. MODBUS 拆分

新增文件：

- `Code/Source/modbus_rtu_parser.h`
- `Code/Source/modbus_rtu_parser.c`
- `Code/Source/modbus_service.h`
- `Code/Source/modbus_service.c`

职责划分：

- `modbus_rtu_parser`：逐字节判定帧长，识别 `0x03 / 0x06 / 0x10`
- `modbus_service`：把完整 RTU 帧交给原 `Sci_Upper` 业务逻辑处理
- `Sci_Upper.c`：保留寄存器映射、读写处理、应答生成

当前保留的兼容策略：

- 原 `Sci_Deal_ReadRegs_0x03`
- 原 `Sci_Deal_WrReg_0x06`
- 原 `Sci_Deal_WrRegs_0x10`
- 原 `Sci_ACK_0x03`
- 原 `Sci_ACK_0x06_0x10`

这样可以在不重写寄存器表的前提下，先把“串口驱动”和“协议业务”分开。

### 4. ASCII 改造成协议插件

重写文件：

- `Code/Source/ascii_slave.h`
- `Code/Source/ascii_slave.c`

改造点：

- 删除 `uart.h / modbus_host.h / led.h / LL_UART_* / UART1` 依赖
- 删除私有发送函数和独立 UART 全局缓冲
- 新增 `AsciiParser`
- 新增 `AsciiParser_ConsumeByte()`
- 新增 `Ascii_HandleFrame()`
- 保留 ASCII 协议的 `SOI/EOI/LENGTH/CHKSUM` 规则

### 5. 数据适配层

新增文件：

- `Code/Source/bms_comm_data_adapter.h`
- `Code/Source/bms_comm_data_adapter.c`

适配层作用：

- 从 `g_stCellInfoReport`、`ProductionInfor`、`OtherElement` 等现有变量生成 ASCII 所需数据视图
- 避免 `ASCII` 维护一份长期静态示例数据
- 为后续 CAN/蓝牙/其他协议复用提供入口

当前已映射的数据包括：

- 基础信息
- 模拟量
- 告警/保护状态
- 充放电限值与状态

## 关键行为变化

### 中断层

重构前：

- 中断直接参与 `MODBUS` 状态机推进

重构后：

- 中断只做错误清理、读取 `RDR`、投递字节到统一接收入口

### 发送路径

重构前：

- `SCI1/SCI2` 各自维护发送开关和轮询发送逻辑

重构后：

- 统一由 `Comm_PortStartTx()` 和 `Comm_PortTxPump()` 驱动发送
- 保留 `TRANS_EN_485() / RECV_EN_485()` 的 RS485 方向控制
- `u8FlashUpdateE2PROM` 仍在发送完成后切换为 `u8FlashUpdateFlag`

## 影响文件

主要修改：

- `Code/Drivers/stm32f0xx_it.c`
- `Code/Source/Sci_Upper.c`
- `Code/Source/Sci_Upper.h`
- `Code/Source/main.c`
- `Code/Source/main.h`
- `CommomSH367309_16series_030C8T6_C.uvprojx`

主要新增：

- `Code/Source/Comm.c`
- `Code/Source/Comm.h`
- `Code/Source/ascii_slave.c`
- `Code/Source/ascii_slave.h`
- `Code/Source/bms_comm_data_adapter.c`
- `Code/Source/bms_comm_data_adapter.h`
- `Code/Source/modbus_rtu_parser.c`
- `Code/Source/modbus_rtu_parser.h`
- `Code/Source/modbus_service.c`
- `Code/Source/modbus_service.h`

## 验证建议

建议在 Keil 中进行以下验证：

1. `SCI1` 发送 RTU `0x03 / 0x06 / 0x10`，确认响应与旧版本一致。
2. `SCI2` 发送 RTU `0x03 / 0x06 / 0x10`，确认响应与旧版本一致。
3. `SCI1/SCI2` 分别发送 ASCII `0x60 / 0x61 / 0x62 / 0x63`，确认返回帧格式正确。
4. 同一串口依次发送 `RTU -> ASCII -> RTU`，确认协议可自动切换。
5. 验证地址错误、CRC/CHKSUM 错误、格式错误时是否按预期丢弃或回复错误帧。
6. 验证写 EEPROM/Flash 的命令在应答发完后仍按原流程处理。

## 当前限制

- 这次重构没有重写 `MODBUS` 寄存器表，只是把旧业务逻辑挂到了新的服务层。
- `ASCII` 的数据视图当前优先复用已有全局变量，个别字段仍是近似映射，不是重新定义的一套业务模型。
- 当前默认两路串口共用现有 RS485 方向控制宏；如果现场硬件要求每路独立 DE/RE，可继续把方向脚配置下沉到 `CommPortContext`。
