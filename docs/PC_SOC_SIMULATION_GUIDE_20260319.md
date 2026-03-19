# PC SOC 仿真指南

## 目标

这份文档说明当前仓库里第一版 `SOC` 主机侧回放链路如何使用。

目标不是一比一复刻量产 `SOC` 全逻辑，而是先给 Codex 和你自己一条稳定的策略级入口，用于验证：

- 充电积分趋势
- 放电积分趋势
- 静置后的 `OCV` 修正趋势
- 充放切换后的 `SOC` 连续性
- `SOC` 边界钳位行为

## 当前入口

### 单场景

```bash
task sim-soc
task sim-soc-charge
task sim-soc-discharge
task sim-soc-idle
task sim-soc-mixed
task sim-soc-restore
task sim-soc-long
task sim-soc-cycle
```

### 套件与摘要

```bash
task sim-soc-suite
task sim-soc-suite-summary
```

## 当前场景

- `soc_charge.csv`
  充电积分与后段静置
- `soc_discharge.csv`
  放电积分与放空边界趋势
- `soc_idle_ocv.csv`
  低电流静置后的 `OCV` 修正
- `soc_mixed_cycle.csv`
  充电、放电、静置混合切换
- `soc_power_restore.csv`
  掉电恢复与 EEPROM 重载连续性
- `soc_long_drift.csv`
  用小电流与静置反复回放，观察长时间漂移趋势
- `soc_cycle_counter.csv`
  用重放电场景验证 `DSG_SOC_Int / Cycle_times` 循环统计
  这个场景会单独把回放初始 `SOC` 设为 `1000`，避免默认 `80%` 基线无法打满一圈

## 产物

套件摘要默认输出：

- [soc-suite-summary.json](/Users/cs/Downloads/work/todo/030-309-template/artifacts/host-sim/soc-suite-summary.json)
- [soc-suite-summary.md](/Users/cs/Downloads/work/todo/030-309-template/artifacts/host-sim/soc-suite-summary.md)

单场景会输出：

- `artifacts/host-sim/soc-*.log`
- `artifacts/host-sim/soc-*.jsonl`

## 当前输出字段

当前 `jsonl` 已包含：

- `soc_est_pct_x10`
- `soc_ocv_pct_x10`
- `soc_error_pct_x10`
- `soc_dsg_cycle_acc_pct_x10`
- `soc_cycle_times_x100`
- `soc_state_flags`

其中 `soc_state_flags` 当前表示：

- `charge`
- `discharge`
- `ocv_corrected`
- `clamped_empty`
- `clamped_full`
- `power_restore`
- `eeprom_restored`
- `terminal_corrected`
- `cycle_incremented`

摘要还会额外给出：

- `soc_drift_span_pct_x10`
- `final_error_pct_x10`
- `terminal_corrected_steps`
- `restore_steps`
- `cycle_increment_steps`
- `final_cycle_times_x100`

## 当前边界

这条链路当前是第一版策略骨架，边界很明确：

- 已有第一版 `EEPROM` 恢复骨架，但还没有一比一映射真实存储协议与磨损策略
- 已接入第一版 `DSG_SOC_Int / Cycle_times` 循环统计骨架，但还没有接入工厂容量衰减与 `SOH` 模型
- 还没有接入温度补偿
- 还没有一比一映射 [SocEnhance.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/SocEnhance.c)

所以它目前更适合做：

- 趋势检查
- 回归入口
- Codex 自动分析输入

而不是直接当作量产 `SOC` 结论模型。

## 后续建议

下一阶段优先做：

1. 继续从 [SocEnhance.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/SocEnhance.c) 提炼更多真实校正与持久化条件
2. 补“容量衰减 / `SOH` / 循环寿命”模型
3. 把 `SOC` 摘要进一步和保护、构建摘要串成统一接管工作流
