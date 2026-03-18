# 真板状态终端监控说明

## 目标

这条链路用于在不占用业务串口的前提下，直接通过 `ST-Link + OpenOCD` 从板上读取运行时状态快照，并在终端持续输出。

它适合：
- 联机观察 `SOC`、电压、电流、温度、状态字
- 长时间落盘运行快照，方便 Codex 后处理
- 调试板级行为时快速确认主循环是否还在跑

它不适合：
- 高频波形采集
- 硬实时串口日志替代
- 精确定时分析

## 原理

监控版固件会把运行时快照编码成一段紧凑 JSON 文本，放在 RAM 中。PC 侧脚本通过：

- `ST-Link`
- `OpenOCD`
- `arm-none-eabi-nm`

定位以下符号地址并轮询：

- `g_app_runtime_monitor_json`
- `g_app_runtime_monitor_json_length`
- `g_app_runtime_monitor_update_count`

当更新计数变化时，脚本会读取 JSON 文本，在终端打印，并同时写入 `jsonl` 文件。

## 使用步骤

### 1. 编译监控版固件

```powershell
task build-monitor
```

### 2. 烧录监控版固件

```powershell
task flash-stlink-monitor
```

说明：
- 监控版默认走 `STM32CubeProgrammer`
- 如果当前有 `OpenOCD` 正在占用探针，脚本会先尝试自动关闭已有会话
- 这条链路比 `OpenOCD program bin ... verify` 更稳定

### 3. 开始终端监控

```powershell
task watch-board-stlink
```

默认输出文件：
- [artifacts/board-status-watch.jsonl](E:/TODO/030%20+%20309/artifacts/board-status-watch.jsonl)

终端输出示例：

```json
{"src":"mcu","step":2,"reason":"periodic","v":26250,"i":1400,"tmax":250,"tmin":250,"soc":0,"soh":0,"fault":0,"status":1036}
```

## VS Code 入口

在 `Terminal -> Run Task` 中使用：

- `mcu: build gcc monitor`
- `mcu: flash stlink monitor`
- `mcu: watch board status`

## 适用场景

- 真板在线观察 `SOC`、电压、电流、温度状态
- 不想占用业务串口
- 想让 Codex 直接分析真板运行快照
- 想把板上运行状态长期落盘

## 边界

这条链路本质上是“调试器采样”，不是“板子主动串口上报”。

它的优点：
- 不改业务串口协议
- 不需要额外接线
- 适合调试和状态分析

它的限制：
- 采样有侵入性，默认会 `halt/resume`
- 刷新频率不适合做高频数据分析
- 更适合看状态，不适合看严格时序

## 推荐用法

如果你只是想“在终端看真板状态”，优先用这条链路。

如果后面你要做：
- 产测
- 上位机联调
- 长期开机日志采集

再补一条真实串口日志链会更合适。
