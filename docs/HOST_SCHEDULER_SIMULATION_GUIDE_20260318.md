# 主机侧调度器仿真说明

## 目标

这条仿真链的目标不是替代真实板子，而是把无板调试从“只看协议解析”推进到“看任务调度节奏和长期运行快照”。

当前 PC 仿真已经有两层：

### 协议层

- `task sim-modbus`
- `task sim-replay`
- `task sim-replay-long`

### 调度层

- `task sim-scheduler`
- `task sim-scheduler-long`

其中调度层复用了共享的：

- [Time_Triggered.c](E:/TODO/030%20+%20309/Code/Source/BSP/Time_Triggered.c)
- [app_state_snapshot.c](E:/TODO/030%20+%20309/Code/Source/BSP/app_state_snapshot.c)

## 现在能模拟什么

当前主机侧调度器仿真会在 PC 上按任务周期执行这些任务槽位：

- `App_AFEGet`
- `App_WarnCtrl`
- `App_AnlogCal`
- `App_SOC`
- `App_LogRecord`
- `AppRuntimeMonitor_RunTask`

它使用和当前固件主循环一致的周期参数：

- `0 / 200`
- `8 / 10`
- `2 / 10`
- `5 / 100`
- `6 / 1000`
- `11 / 1000`

说明：
- 前一个数字是启动延时
- 后一个数字是任务周期
- 当前仿真 tick 只是逻辑调度 tick，不等于真实 MCU 中断精度

## 如何运行

### 短跑

```powershell
task sim-scheduler
```

产物：

- [scheduler-replay.log](E:/TODO/030%20+%20309/artifacts/host-sim/scheduler-replay.log)
- [scheduler-replay.jsonl](E:/TODO/030%20+%20309/artifacts/host-sim/scheduler-replay.jsonl)

### 长跑

```powershell
task sim-scheduler-long
```

产物：

- [scheduler-replay-long.log](E:/TODO/030%20+%20309/artifacts/host-sim/scheduler-replay-long.log)
- [scheduler-replay-long.jsonl](E:/TODO/030%20+%20309/artifacts/host-sim/scheduler-replay-long.jsonl)

## VS Code 一键入口

### Run Task

- `pc: scheduler replay`
- `pc: scheduler replay long`

### Run and Debug

- `Host Replay: Scheduler`
- `Host Replay: Scheduler (Long)`

## 适合拿它做什么

- 验证任务周期是否符合预期
- 观察长期运行日志是否稳定增长
- 生成 JSONL 快照给 Codex 自动分析
- 做“任务调度级别”的回归验证

## 不适合拿它做什么

- 验证真实 ADC 采样
- 验证 GPIO 电平
- 验证 AFE 真实硬件通信
- 替代板上时序测试

## 后续演进方向

下一步如果继续推进，同码化应该按这个顺序扩：

1. 协议层：共享 `modbus_service`
2. 调度层：进一步把主循环任务配置抽成共享描述
3. 平台层：逐步抽离时间、串口、EEPROM、AFE 接口

这样 PC 才能越来越接近 MCU 的正式运行代码。
