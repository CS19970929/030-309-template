# 真板状态终端监控说明

## 目标

这条链路的目标是：

- 不占业务串口
- 不依赖额外 USB 转串口
- 直接通过 `ST-Link + OpenOCD` 读取板上运行时快照
- 在终端持续输出 `BMS` 状态

## 原理

监控版固件会把运行时快照编码成 JSON 文本，放在 RAM 中。

PC 侧脚本通过：

- `ST-Link`
- `OpenOCD`
- `nm`

解析出这几个符号地址并轮询：

- `g_app_runtime_monitor_json`
- `g_app_runtime_monitor_json_length`
- `g_app_runtime_monitor_update_count`

脚本检测到快照更新后，会把 JSON 打到终端，并同时写入文件。

## 使用步骤

### 1. 编译监控版固件

```powershell
task build-monitor
```

### 2. 烧录监控版固件

```powershell
task flash-stlink-monitor
```

### 3. 开始终端监控

```powershell
task watch-board-stlink
```

默认输出文件：

- [board-status-watch.jsonl](E:/TODO/030%20+%20309/artifacts/board-status-watch.jsonl)

## VS Code 入口

### Run Task

- `mcu: build gcc monitor`
- `mcu: flash stlink monitor`
- `mcu: watch board status`

## 适用场景

- 真板在线观察 SOC、电压、电流、温度状态
- 不想抢占业务串口
- 想让 Codex 直接分析真板运行快照
- 想把板上运行状态长期落盘

## 边界

这条链路不是硬实时串口流。

它的特点是：

- 对现有通讯链路侵入小
- 不需要额外接线
- 更适合调试和分析

但它不是：

- 高频波形采集
- 硬实时示波器替代品
- 真实串口裸流

## 推荐用法

如果你只是想“在终端看真板状态”，优先用这条链路。

如果你后面要做：

- 产测
- 上位机联调
- 长期开机日志采集

再考虑补一条真实串口日志链路。
