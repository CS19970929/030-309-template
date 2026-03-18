# ASCII Slave 协议复查记录

## 范围

本次只复查当前程序里当前需要联调的 PYLON ASCII 从机协议：

- `0x61` 模拟量
- `0x62` 告警/保护
- `0x63` 充放电管理

对照文件：

- 原版 [ascii_slave.c](/E:/TODO/030%20+%20309/ascii_slave.c)
- 原版 [ascii_slave.h](/E:/TODO/030%20+%20309/ascii_slave.h)
- 当前 [ascii_slave.c](/E:/TODO/030%20+%20309/Code/Source/ascii_slave.c)
- 当前 [ascii_slave.h](/E:/TODO/030%20+%20309/Code/Source/ascii_slave.h)
- 当前 [bms_comm_data_adapter.c](/E:/TODO/030%20+%20309/Code/Source/bms_comm_data_adapter.c)

## 本轮确认的问题

### 1. `0x61` 字段顺序被插错，后续数据整体错位

当前 [bms_comm_data_adapter.c](/E:/TODO/030%20+%20309/Code/Source/bms_comm_data_adapter.c) 在循环次数字段之后，又额外追加了一次 `u16Cycle_times`。

这会导致：

- `SOH` 起始位置后移 2 字节
- 后面的单体电压、温度、MOS 温度、BMS 温度全部错位
- `LENID` 也会被一并拉长

而原版 [ascii_slave.c](/E:/TODO/030%20+%20309/ascii_slave.c) 的 `CMD_GET_ANALOG_DATA` 并没有这 2 字节。

### 2. `0x61` 位置字段编码方向写反

当前 [bms_comm_data_adapter.c](/E:/TODO/030%20+%20309/Code/Source/bms_comm_data_adapter.c) 之前用的是：

- `0x0100 | index`

但从原版响应和你给的示例可确认，位置字段实际应为：

- 高字节：点位索引
- 低字节：类型码

典型示例：

- `0x0304`：第 3 个电压点，类型 `0x04`
- `0x0105`：第 1 个温度点，类型 `0x05`
- `0x0106`：第 1 个 MOS 温度点，类型 `0x06`

所以之前实现会把位置报成方向相反的错误格式。

## 本轮修正

### `bms_comm_data_adapter.c`

- 删除 `0x61` 里多插入的那一个 `u16Cycle_times`
- `BmsComm_EncodePylonLocation()` 改为：
  - 高字节 = 点位索引
  - 低字节 = 类型码
- `0x61` 各位置字段改成正确类型码：
  - 电压位置：`0x04`
  - 电芯温度位置：`0x05`
  - MOS 温度位置：`0x06`

## 仍需以文档为准的点

以下项原版示例值和文档注释本身存在矛盾，这轮没有仅凭一条示例报文硬改：

- `0x61` 总电流字段的最终缩放方式
- `0x63` 最大充放电电流精度是否要完全贴原版示例值
- `0x61` BMS 温度是否继续保持 `0xFFFF`

这些项应继续以协议表定义和联调结果共同确认。
