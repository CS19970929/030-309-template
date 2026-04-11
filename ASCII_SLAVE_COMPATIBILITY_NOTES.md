# ASCII Slave 兼容接入说明

## 目标

在不参考历史提交和其他分支的前提下，把客户提供的 `ascii_slave.c/.h` 协议模块兼容接入当前项目，并尽量少改 `ascii_slave` 模块原有命令处理结构。

## 本次方案

1. 保留客户 `ascii_slave` 的协议常量、数据结构和命令处理函数。
2. 删除当前工程不存在的头文件依赖和 DMA/LED 发送依赖，改为使用项目现有 RS485 方向控制和串口阻塞发送。
3. 在 `ascii_slave` 内补充最小接收状态机：
   - `Ascii_Slave_ConsumeByte()`
   - `Ascii_Slave_ResetRx()`
4. 在 `Sci_Upper.c` 的 `USART1` 接收路径中优先分流 ASCII 帧，未命中的字节继续走原 RTU 逻辑。
5. 在 `App_CommonUpper()` 中追加 `Frame_Parse_Process()`，避免改动 `main.c`。
6. 在 `ascii_slave` 内增加实时数据刷新，把当前项目运行时数据映射到客户协议结构体 `g_battery_data`。
7. 在 Keil 工程文件中补入 `Code/Source/ascii_slave.c`。

## 数据映射

- 总压：`g_stCellInfoReport.u16VCellTotle`
- 单体电压：`g_stCellInfoReport.u16VCell[]`
- 充放电电流：`g_stCellInfoReport.u16Ichg / u16IDischg`
- SOC/SOH/容量/循环次数：`g_stCellInfoReport.SocElement`
- 温度：`g_stCellInfoReport.u16Temperature[]`
- 告警/保护：`g_stCellInfoReport.unMdlFault_Second / unMdlFault_Third`
- 充放电限值：`PRT_E2ROMParas.u16VbusOvp_Third / u16VbusUvp_Third`
- 最大充放电流：`OtherElement.u16CS_Cur_CHGmax / u16CS_Cur_DSGmax`
- 充放电状态：`SystemStatus.bits.b1Status_MOS_CHG / b1Status_MOS_DSG`

## 改动文件

- `Code/Source/ascii_slave.c`
- `Code/Source/ascii_slave.h`
- `Code/Source/Sci_Upper.c`
- `CommomSH367309_16series_030C8T6_C.uvprojx`

## 稳定性处理

- 不再修改 `g_battery_data.charge_dis_info` 内的原始字段，避免重复乘法污染全局数据。
- ASCII 帧先按 SOI、LENGTH、EOI 做边界约束，再进入命令分发。
- ASCII 发送统一走阻塞方式，避免当前项目缺失 DMA 资源定义导致构建失败。
- `main.c` 未改动，减少对现有调度路径的影响。

## 还需要的实际联调

1. 用客户上位机验证 `0x61 / 0x42 / 0x62 / 0x63` 四类命令回包。
2. 确认客户对电流和温度字段单位的最终解释是否与当前映射一致。
3. 连续背靠背发送 ASCII 帧时，确认主循环周期满足解析时序要求。
