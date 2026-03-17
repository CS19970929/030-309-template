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

### 3.1 `0x61` 总压单位

原实现问题：

- 内部 `g_stCellInfoReport.u16VCellTotle` 实际是整包电压 `0.01V`
- 协议截图要求单位为 `V`，精度 `3`
- 也就是协议字段应输出 `mV`

当前修正：

- 协议总压改为内部总压乘以 `10`

公式：

```text
协议总压 = 内部总压(0.01V) * 10 = mV
```

### 3.2 `0x61` 总电流单位/编码

原实现问题：

- 之前曾按示例误判为偏移编码
- 但截图已明确：单位为 `A`，精度 `2`
- 示例 `0x61A8 = 25000 = 250.00A`

当前修正：

- 协议输出改为带符号 `0.01A`
- 编码公式：

```text
协议总电流 = 电流(0.01A)
```

说明：

- 内部充电电流为正，放电电流为负
- 内部量纲为 `A * 10`
- 协议输出时再乘以 `10`

代码：

- `Code/Source/bms_comm_data_adapter.c`

### 3.3 `0x61` 温度单位/编码

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

### 3.4 `0x61` 温度数据源修正

原实现问题：

- 以前 MOS 温度和 BMS 温度直接复用电芯温度
- 与截图中的字段含义不一致
- 文档还明确说明：产品不支持的模拟量应返回 `FF`

当前修正：

- 电芯温度：
  - 改为仅统计 `AFE1_TEMP1 ~ AFE2_TEMP3`
  - 不再混入环境温度
- MOS 温度：
  - 改为读取真实 `MOS_TEMP1`
- BMS 温度：
  - 当前产品无独立板载温度源
  - 按文档要求输出 `0xFFFF`

### 3.5 `0x61` 位置字段格式

原实现问题：

- 单体/温度位置字段原先未按协议说明单独处理

当前修正：

- 改为两字节位置编码
- 当前单板按 `0x01xx` 输出
- 电压位置低字节使用真实单体序号
- 电芯温度位置低字节使用真实温度采样序号
- MOS 温度位置当前固定 `0x0101`
- BMS 温度不支持时位置也输出 `0xFFFF`

代码：

- `Code/Source/bms_comm_data_adapter.c`
- `BmsComm_EncodePylonLocation()`

### 3.6 `0x62` 告警/保护状态映射

原实现问题：

- 之前 `0x62` 只是把内部二级/三级故障字直接拆字节输出
- 没有按协议截图逐 bit 映射

当前修正：

- `Alarm1` 映射为：
  - bit7 模块总压高压 -> `b1BatOvp`
  - bit6 模块总压低压 -> `b1BatUvp`
  - bit5 单芯高压 -> `b1CellOvp`
  - bit4 单芯低压 -> `b1CellUvp`
  - bit3 单芯高温 -> `b1CellChgOtp || b1CellDischgOtp`
  - bit2 单芯低温 -> `b1CellChgUtp || b1CellDischgUtp`
  - bit1 MOSFET 高温 -> `b1TmosOtp`
  - bit0 单芯压差过大 -> `b1VcellDeltaBig`
- `Alarm2` 映射为：
  - bit7 温差过大 -> `b1TempDeltaBig`
  - bit6 充电过流告警 -> `b1IchgOcp`
  - bit5 放电过流告警 -> `b1IdischgOcp`
  - bit4 内部通信错误 -> `AFE1` 离线状态
- `Protect1` 按三级故障对应映射
- `Protect2` 映射为：
  - bit7 充电过流保护 -> `b1IchgOcp`
  - bit6 放电过流保护 -> `b1IdischgOcp`
  - bit4 BMS error -> `AFE1` 离线状态

说明：

- 当前单板系统没有从截图中看到更多 `Protect2` 其余位定义，未定义位保持 `0`

代码：

- `Code/Source/bms_comm_data_adapter.c`

### 3.7 `0x63` 充放电建议电压

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

### 3.8 `0x63` 最大充放电电流精度

原实现问题：

- 之前按 `0.01A` 输出
- 但截图明确 `0x63` 限流字段精度为 `1`
- 即单位应为 `0.1A`

当前修正：

- 直接输出内部 `A * 10`

### 3.9 `0x63` 充放电状态

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
- MOS 温度：`u16Temperature[MOS_TEMP1]`

### 4.2 `0x62` 告警/保护

当前已接到内部故障字：

- 告警：`g_stCellInfoReport.unMdlFault_Second`
- 保护：`g_stCellInfoReport.unMdlFault_Third`

当前输出方式：

- `alarm1/alarm2` 已按截图逐 bit 映射 `Second`
- `protect1/protect2` 已按截图逐 bit 映射 `Third`

这意味着：

- 不是写死值
- 已经和 BMS 实际故障状态联动

### 4.3 `0x63` 状态

当前已接到系统真实 MOS 开关状态：

- 充电允许状态：`SystemStatus.bits.b1Status_MOS_CHG`
- 放电允许状态：`SystemStatus.bits.b1Status_MOS_DSG`

## 5. 当前仍存在的限制

### 5.1 BMS 板载温度当前按不支持处理

原因：

- 当前程序没有独立板载温度数据源
- 根据截图说明，不支持的模拟量应返回 `FF`

因此当前输出：

- BMS 平均/最高/最低温度 = `0xFFFF`
- BMS 温度位置 = `0xFFFF`

### 5.2 基础信息仍为工程内现有数据

当前 `0x60` 仍存在以下保守实现：

- 厂家名固定为 `"BMS"`
- 每个电池条码都复用同一个序列号源

这部分未按 PYLON 厂商字段习惯进一步定制。

## 6. 本次修改文件

- `Code/Source/bms_comm_data_adapter.c`

## 7. 建议后续动作

若后续能够拿到 `0x62` 的位定义页，可继续完成：

1. 若协议对 `Protect2` 其余保留位还有要求，再补全对应来源
2. 若后续增加板载温度采样，再替换当前 `0xFFFF`
3. 按项目需求完善 `0x60` 厂家名、设备名、条码输出规则

## 8. 当前结论

当前协议适配状态可归纳为：

- `0x61`：核心模拟量已按截图定义修正单位、精度与温度来源
- `0x62`：已按截图位表完成报警/保护逐 bit 映射
- `0x63`：限压、限流精度、充放电状态已按截图定义修正
