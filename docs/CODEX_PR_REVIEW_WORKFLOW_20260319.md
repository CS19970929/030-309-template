# Codex PR 审查工作流

## 目标

这份文档用于把当前仓库的 `PR` 审查流程收敛成 Codex 可接管的标准动作。

重点不是“让 Codex 看一眼 diff”，而是让它结合：

- `PR` 描述
- 回归任务
- 结构化摘要
- 风险边界

去做第一轮工程审查。

## 当前仓库已补的基础设施

### 1. PR 模板

文件：

- [.github/pull_request_template.md](/Users/cs/Downloads/work/todo/030-309-template/.github/pull_request_template.md)

作用：

- 强制补充变更摘要
- 明确风险类型
- 记录已执行的本地验证
- 在 `PR` 中固定提醒 `@codex review`

### 2. PR Host Sim 工作流

文件：

- [.github/workflows/pr-host-sim.yml](/Users/cs/Downloads/work/todo/030-309-template/.github/workflows/pr-host-sim.yml)

作用：

- 在 `PR` 上自动跑主机侧仿真
- 自动生成保护、`SOC`、低功耗回归摘要
- 自动生成 `Codex` 接管总览
- 上传日志、`jsonl` 和摘要文件

这意味着 Codex 在审查 `PR` 时，不只看到代码 diff，还能看到：

- `protection-*.log`
- `protection-*.jsonl`
- `protection-suite-summary.json`
- `protection-suite-summary.md`
- `soc-*.log`
- `soc-*.jsonl`
- `soc-suite-summary.json`
- `soc-suite-summary.md`
- `low-power-*.log`
- `low-power-*.jsonl`
- `low-power-suite-summary.json`
- `low-power-suite-summary.md`
- `codex-overview.json`
- `codex-overview.md`

## 建议的 PR 审查流程

### 普通改动

1. 开发者本地执行：
   - `task build-summary`
   - `task map`
   - `task sim-protection-suite-summary`
   - `task sim-soc-suite-summary`
   - `task sim-low-power-suite-summary`
   - `task codex-overview`
2. 发起 `PR`
3. 等待 `PR Host Sim Review` 工作流完成
4. 在 `PR` 评论里执行：

```text
@codex review
```

### 涉及保护/SOC/低功耗

建议把请求写得更具体：

```text
@codex review the protection logic, host simulation coverage, and cross-platform impact
```

### 涉及存储/升级/校准

建议这样请求：

```text
@codex review for storage layout, flash safety, and calibration risks
```

## Codex 在当前仓库中应重点审查什么

当前最值得 Codex 优先关注的不是格式问题，而是：

- 是否破坏 `Taskfile` 命令入口
- 是否破坏 Win/Mac 一致性
- 是否影响保护回归
- 是否影响构建产物、linker、map
- 是否误碰量产参数、保护阈值、Flash 布局、校准常量

## 当前已形成的审查输入

Codex 现在已经可以稳定读取这些审查输入：

- [Taskfile.yml](/Users/cs/Downloads/work/todo/030-309-template/Taskfile.yml)
- [CMakePresets.json](/Users/cs/Downloads/work/todo/030-309-template/CMakePresets.json)
- [PC_PROTECTION_SIMULATION_GUIDE_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/PC_PROTECTION_SIMULATION_GUIDE_20260319.md)
- [PC_SOC_SIMULATION_GUIDE_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/PC_SOC_SIMULATION_GUIDE_20260319.md)
- [PC_LOW_POWER_SIMULATION_GUIDE_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/PC_LOW_POWER_SIMULATION_GUIDE_20260319.md)
- [CODEX_RELIABILITY_WORKFLOW_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/CODEX_RELIABILITY_WORKFLOW_20260319.md)
- `artifacts/host-sim/protection-suite-summary.json`
- `artifacts/host-sim/protection-suite-summary.md`
- `artifacts/host-sim/soc-suite-summary.json`
- `artifacts/host-sim/soc-suite-summary.md`
- `artifacts/host-sim/low-power-suite-summary.json`
- `artifacts/host-sim/low-power-suite-summary.md`
- `artifacts/codex-overview.json`
- `artifacts/codex-overview.md`

## 下一步建议

要让 Codex 在 `PR` 上更强，下一阶段建议补两件事：

1. 把 `build-summary` / `map` 也接进 GitHub PR 自动产物
2. 增加更细的规则门禁，例如 RAM 超阈值或 `SOC` 误差超阈值时直接标红
