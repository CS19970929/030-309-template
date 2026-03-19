# PC 低功耗策略仿真指南

## 目标

这份文档说明当前仓库里第一版低功耗主机侧回放链路如何使用。

目标不是一比一复刻板级 `SleepDeal.c` 的全部硬件动作，而是先给 Codex 和你自己一条稳定的策略级入口，用于验证：

- 正常静置是否会进入 `L2`
- 低压静置是否会进入 `L3`
- 唤醒事件是否会打断休眠计时
- 极低电压下是否会进入深度休眠

## 当前入口

### 单场景

```bash
task sim-low-power
task sim-low-power-normal
task sim-low-power-low-v
task sim-low-power-wake
task sim-low-power-deep
```

### 套件与摘要

```bash
task sim-low-power-suite
task sim-low-power-suite-summary
```

## 当前场景

- `low_power_normal.csv`
  正常静置后进入 `L2`
- `low_power_low_v.csv`
  低压静置后进入 `L3`
- `low_power_wake.csv`
  已接近休眠时收到唤醒事件并重置计时
- `low_power_deep.csv`
  极低电压长时间静置后进入深度休眠

## 产物

套件摘要默认输出：

- [low-power-suite-summary.json](/Users/cs/Downloads/work/todo/030-309-template/artifacts/host-sim/low-power-suite-summary.json)
- [low-power-suite-summary.md](/Users/cs/Downloads/work/todo/030-309-template/artifacts/host-sim/low-power-suite-summary.md)

单场景会输出：

- `artifacts/host-sim/low-power-*.log`
- `artifacts/host-sim/low-power-*.jsonl`

## 当前输出字段

当前 `jsonl` 已包含：

- `cell_min_mv`
- `charge_current_ma`
- `discharge_current_ma`
- `ext_wake_event`
- `force_sleep_level`
- `power_mode`
- `power_state_flags`

其中 `power_mode` 当前表示：

- `0`：active
- `1`：normal_l2
- `2`：normal_l3
- `3`：deep

其中 `power_state_flags` 当前表示：

- `eligible_normal`
- `eligible_low_v`
- `to_sleep`
- `wake_event`
- `deep_sleep`
- `force_request`
- `reset_by_current`
- `reset_by_wake`

摘要还会额外给出：

- `to_sleep_steps`
- `wake_steps`
- `deep_sleep_steps`
- `normal_l2_steps`
- `normal_l3_steps`

## 当前边界

这条链路当前是第一版策略骨架，边界很明确：

- 已经覆盖 `SleepDeal_Normal_Select / Normal_L2 / Normal_L3 / App_SleepDeal` 的核心判定意图
- 还没有接入真实 `RTC_ExtComCnt`、中断、GPIO、AFE 动作
- 还没有接入 `SleepDeal_Continue()` 的底层硬件行为
- 还没有一比一映射全部 `Sleep_Mode` 位

所以它目前更适合做：

- 状态机趋势检查
- 唤醒/休眠回归入口
- Codex 自动分析输入

而不是直接当作真板低功耗结论。

## 后续建议

下一阶段优先做：

1. 从 [SleepDeal.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/SleepDeal.c) 提炼更多 `Sleep_Mode` 位和 `RTC_ExtComCnt` 行为
2. 把低功耗摘要进一步和保护、`SOC`、构建摘要串成统一接管工作流
