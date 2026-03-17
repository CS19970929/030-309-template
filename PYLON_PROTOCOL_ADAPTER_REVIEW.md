# PYLON RS485 协议对接检查与修正说明

## 1. 范围

本次只处理当前程序已经实现的 PYLON ASCII 命令：

- `0x60` 基础信息
- `0x61` 模拟量信息
- `0x62` 告警/保护状态
- `0x63` 充放电管理信息

未新增文档中其他未使用命令。

## 2. 代码位置

当前 PYLON 协议实现主要位于：

- `Code/Source/ascii_slave.c`
- `Code/Source/ascii_slave.h`
- `Code/Source/bms_comm_data_adapter.c`
- `Code/Source/bms_comm_data_adapter.h`

其中：

- `ascii_slave.c` 负责报文解析与组帧
- `bms_comm_data_adapter.c` 负责把内部 BMS 数据转换成 PYLON 协议字段

## 3. 本次已修正内容

### 3.1 `0x61` 总电流单位/编码

原实现问题：

- 直接把内部 `A * 10` 的带符号值作为 16 位数输出
- 与 PYLON 示例报文不一致

当前修正：

- 协议输出改为偏移编码
- 编码公式：

```text
协议总电流 = 30000 + 电流(0.01A)
```

说明：

- 内部充电电流为正，放电电流为负
- 输出字段已按 PYLON 协议侧格式转换

代码：

- `Code/Source/bms_comm_data_adapter.c`
- `BmsComm_EncodePylonCurrent()`

### 3.2 `0x61` 温度单位/编码

原实现问题：

- 直接输出内部温度换算后的摄氏值
- 内部原始温度格式是 `(+40°C) * 10`
- 与示例报文中的绝对温度编码不一致

当前修正：

- 协议输出改为绝对温度 `0.1K`
- 编码公式：

```text
协议温度 = 摄氏温度(0.1°C) + 2730
```

说明：

- 例如 `25.0°C` 输出为 `2980`

代码：

- `Code/Source/bms_comm_data_adapter.c`
- `BmsComm_EncodePylonTemperature()`

### 3.3 `0x61` 位置字段格式

原实现问题：

- 单体最高/最低电压位置直接输出 `1..16`

当前修正：

- 改为两字节位置编码
- 当前按单模块场景输出为 `0x01xx`

说明：

- 低字节表示点位
- 高字节当前固定为模块 `0x01`
- 对当前 16 串单板程序是保守可用实现

代码：

- `Code/Source/bms_comm_data_adapter.c`
- `BmsComm_EncodePylonLocation()`

### 3.4 `0x63` 充放电建议电压

原实现问题：

- 直接输出 `OtherElement.u16Soc_V_100` 和 `OtherElement.u16Soc_V_0`
- 这两个值本质是单串电压，不是整包电压

当前修正：

- 改为乘以串数后输出整包电压
- 单位仍为 `mV`

公式：

```text
充电限压 = 单串满电电压 * SeriesNum
放电限压 = 单串空电电压 * SeriesNum
```

代码：

- `Code/Source/bms_comm_data_adapter.c`

### 3.5 `0x63` 充放电状态

原实现问题：

- 以前用“当前是否存在充电/放电电流”来决定状态位
- 这更像运行方向，不是使能状态

当前修正：

- 改为直接读取系统 MOS 状态位
- `CHG MOS` 打开则置 `0x80`
- `DSG MOS` 打开则置 `0x40`
- 两者都开则为 `0xC0`

状态来源：

- `SystemStatus.bits.b1Status_MOS_CHG`
- `SystemStatus.bits.b1Status_MOS_DSG`

代码：

- `Code/Source/bms_comm_data_adapter.c`
- `BmsComm_GetChargeDischargeStatus()`

## 4. 当前已对接到 BMS 实时数据的字段

### 4.1 `0x61` 模拟量

已经接到内部实时数据的字段：

- 总压：`g_stCellInfoReport.u16VCellTotle`
- 总流：`g_stCellInfoReport.u16Ichg / u16IDischg`
- SOC：`g_stCellInfoReport.SocElement.u16Soc`
- 循环次数：`g_stCellInfoReport.SocElement.u16Cycle_times`
- SOH：`g_stCellInfoReport.SocElement.u16Soh`
- 单体最高/最低电压：`u16VCellMax/u16VCellMin`
- 单体最高/最低电压位置：`u16VCellMaxPosition/u16VCellMinPosition`
- 温度基值：`u16TempMax/u16TempMin`

### 4.2 `0x62` 告警/保护

当前已接到内部故障字：

- 告警：`g_stCellInfoReport.unMdlFault_Second`
- 保护：`g_stCellInfoReport.unMdlFault_Third`

当前输出方式：

- `alarm1/alarm2` 直接拆 `Second`
- `protect1/protect2` 直接拆 `Third`

这意味着：

- 不是写死值
- 已经和 BMS 实际故障状态联动
- 但还不是按 PYLON 文档逐 bit 重新排布后的严格版本

### 4.3 `0x63` 状态

当前已接到系统真实 MOS 开关状态：

- 充电允许状态：`SystemStatus.bits.b1Status_MOS_CHG`
- 放电允许状态：`SystemStatus.bits.b1Status_MOS_DSG`

## 5. 当前仍存在的限制

### 5.1 `0x62` 位定义尚未完成逐 bit 校正

原因：

- 本次拿到的 PDF 无法在当前环境可靠提取位表文本
- 本机 Word 版本不支持直接打开该 PDF
- 现有辅助文本只有指令和示例报文，没有 `0x62` 的位定义表

因此当前 `0x62` 结论是：

- “状态已接上”
- 但“位语义尚未确认到 PYLON 文档级别完全一致”

### 5.2 MOS 温度/BMS 温度仍为复用值

当前实现中：

- MOS 温度复用了电芯温度结果
- BMS 板载温度也复用了电芯温度结果

这意味着：

- 字段有值
- 但不是独立传感器真实温度

### 5.3 温度位置仍非真实物理位置

当前温度位置字段统一按模块 `1` 输出，属于占位式兼容处理。

### 5.4 基础信息仍为工程内现有数据

当前 `0x60` 仍存在以下保守实现：

- 厂家名固定为 `"BMS"`
- 每个电池条码都复用同一个序列号源

这部分未按 PYLON 厂商字段习惯进一步定制。

## 6. 本次修改文件

- `Code/Source/bms_comm_data_adapter.c`

## 7. 建议后续动作

若后续能够拿到 `0x62` 的位定义页，可继续完成：

1. 将 `alarm1/alarm2/protect1/protect2` 改为按 PYLON 文档逐 bit 映射
2. 将 MOS 温度和板载温度切换为真实独立数据源
3. 将温度位置字段改成真实传感器位置
4. 按项目需求完善 `0x60` 厂家名、设备名、条码输出规则

## 8. 当前结论

当前协议适配状态可归纳为：

- `0x61`：核心模拟量已对接，单位/精度关键问题已修正
- `0x63`：限压和充放电状态已对接到更合理的数据源
- `0x62`：已经联动内部故障状态，但仍需文档位表做最终逐 bit 校正
