# PC 保护策略仿真指南

## 目标

这份文档对应第一版 `PC` 保护策略仿真骨架，目标是先把以下三件事打通：

- 统一输入快照
- 统一输出快照
- 统一 `Taskfile` 命令入口

这一版不是对真实量产阈值的最终等价复刻，而是 `host baseline` 骨架，用来承接后续从 [Fault.c](/Users/cs/Downloads/work/todo/030-309-template/Code/Source/Fault.c) 继续抽取的工作。

当前已经对齐了一部分 `Fault.c` 的结构语义：

- 每类保护区分 `Second` / `Third` 两级锁存
- `Second` 级按 `Second threshold -> First threshold` 做恢复
- `Third` 级按 `Third threshold -> Recover threshold` 做恢复
- 结果输出为 `fault_first / fault_second / fault_third` 三层摘要，其中：
  - `fault_first` 表示该步存在任一级故障
  - `fault_second` 表示该步存在 `Second` 级或更高故障
  - `fault_third` 表示该步存在 `Third` 级故障

## 当前入口

使用以下命令：

```bash
task sim-protection
```

持续在终端观察：

```bash
task sim-protection-watch
```

输出文件：

- `artifacts/host-sim/protection-replay.log`
- `artifacts/host-sim/protection-replay.jsonl`

日志末尾会额外输出摘要：

- `fault_first_steps`
- `fault_second_steps`
- `fault_third_steps`
- `chg_mos_off_steps`
- `dsg_mos_off_steps`
- `transitions`

## 当前结构

第一版新增文件：

- [app_sim_snapshot.h](/Users/cs/Downloads/work/todo/030-309-template/host_sim/include/app_sim_snapshot.h)
- [protection_sim.h](/Users/cs/Downloads/work/todo/030-309-template/host_sim/include/protection_sim.h)
- [app_sim_snapshot.c](/Users/cs/Downloads/work/todo/030-309-template/host_sim/support/app_sim_snapshot.c)
- [protection_sim.c](/Users/cs/Downloads/work/todo/030-309-template/host_sim/support/protection_sim.c)
- [protection_host_replay.c](/Users/cs/Downloads/work/todo/030-309-template/host_sim/tools/protection_host_replay.c)
- [protection_basic.csv](/Users/cs/Downloads/work/todo/030-309-template/host_sim/scenarios/protection/protection_basic.csv)

## 当前覆盖范围

第一版场景覆盖以下保护类型：

- `Cell OVP`
- `Cell UVP`
- `Charge OCP`
- `Discharge OCP`
- `Charge OTP`
- `Discharge UTP`
- `MOS OTP`

输出包括：

- `fault_first`
- `fault_second`
- `fault_third`
- `charge_mos_off`
- `discharge_mos_off`

示例场景已经覆盖：

- `OVP second/third` 触发与恢复
- `UVP second/third` 触发与恢复
- `Charge OCP second/third`
- `Discharge OCP second/third`
- `Charge OTP second/third`
- `Discharge UTP second/third`
- `MOS OTP second/third`

## 当前边界

这一版故意保持克制：

- 不直接改量产参数
- 不直接把 `Fault.c` 整体搬进 `host_sim`
- 不追求和真板一比一等价

它的定位是：

- 先验证 `PC` 策略仿真链路能稳定工作
- 先把场景、日志、快照、任务入口固定下来
- 为下一步抽取真实 `Fault` 判定逻辑做承接

## 下一步

建议按这个顺序继续收敛：

1. 把 `Fault.c` 的第一批判定函数继续映射到 `ProtectionSim`，优先补 `Bat OVP/UVP`、`Vdelta`、`SOC low`。
2. 把场景从单一 `csv` 扩成可分类的 `OVP/UVP/OCP/OTP` 用例集。
3. 增加更细的摘要统计，让 Codex 自动总结触发点、恢复点和 MOS 行为。
