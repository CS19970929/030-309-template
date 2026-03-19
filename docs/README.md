# 文档导航与当前状态

## 当前工具链回顾

截至 `2026-03-19`，这个仓库已经从“主要依赖 `Keil + bat + 手工点击`”推进到“`Taskfile + CMake + Python + host_sim + Codex`”的可接管状态。

当前已经稳定可用的主路径：

- 环境检查：`task doctor`
- Python/脚本初始化：`task init`
- 固件构建：`task build`
- 构建自动诊断：`task build-summary`
- map/内存摘要：`task map`
- Modbus 主机仿真：`task sim-modbus`
- 回放日志仿真：`task sim-replay`
- 保护策略回归：`task sim-protection-suite-summary`
- SOC 策略回归：`task sim-soc-suite-summary`
- 低功耗策略回归：`task sim-low-power-suite-summary`
- 接管总览：`task codex-overview` / `task codex-overview-refresh`
- 模板化起项目：`task new`

当前仓库已经具备的核心能力：

- Windows/macOS 双端统一命令入口
- `GCC/CMake` 固件构建主链
- `heap/stack` 参数化
- `map` 与 `build` 的结构化摘要
- 保护策略 `PC` 回放与摘要
- SOC 策略 `PC` 回放与摘要
- 适合个人多项目维护的模板骨架
- 适合 Codex 审查与自动分析的输入产物

## 建议阅读顺序

如果你是第一次进入这个仓库，建议按下面顺序看：

1. [README.md](/Users/cs/Downloads/work/todo/030-309-template/README.md)
2. [TOOLCHAIN_MIGRATION_BLUEPRINT_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/TOOLCHAIN_MIGRATION_BLUEPRINT_20260318.md)
3. [CROSS_PLATFORM_SETUP_GUIDE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/CROSS_PLATFORM_SETUP_GUIDE_20260318.md)
4. [CODEX_RELIABILITY_WORKFLOW_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/CODEX_RELIABILITY_WORKFLOW_20260319.md)
5. [INDIVIDUAL_MULTI_PROJECT_MANAGEMENT_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/INDIVIDUAL_MULTI_PROJECT_MANAGEMENT_20260319.md)

## 当前主文档

### 蓝图与总览

- [TOOLCHAIN_MIGRATION_BLUEPRINT_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/TOOLCHAIN_MIGRATION_BLUEPRINT_20260318.md)
- [CODEX_FULL_TAKEOVER_BLUEPRINT_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/CODEX_FULL_TAKEOVER_BLUEPRINT_20260318.md)
- [CODEX_AUTOMATION_HANDBOOK_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/CODEX_AUTOMATION_HANDBOOK_20260318.md)

### 环境与入门

- [CROSS_PLATFORM_SETUP_GUIDE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/CROSS_PLATFORM_SETUP_GUIDE_20260318.md)
- [MACOS_BOOTSTRAP_GUIDE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/MACOS_BOOTSTRAP_GUIDE_20260318.md)
- [WINDOWS_SHELL_DIRECT_USE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/WINDOWS_SHELL_DIRECT_USE_20260318.md)
- [DAILY_WORKFLOW_QUICK_REFERENCE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/DAILY_WORKFLOW_QUICK_REFERENCE_20260318.md)

### 构建、诊断与调试

- [BUILD_MODES_AND_GUARDRAILS_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/BUILD_MODES_AND_GUARDRAILS_20260318.md)
- [BUILD_SUMMARY_AUTOMATION_GUIDE_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/BUILD_SUMMARY_AUTOMATION_GUIDE_20260319.md)
- [MAP_SUMMARY_AUTOMATION_GUIDE_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/MAP_SUMMARY_AUTOMATION_GUIDE_20260319.md)
- [RUNNING_GUIDE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/RUNNING_GUIDE_20260318.md)
- [VSCODE_CORTEX_DEBUG_TROUBLESHOOTING_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/VSCODE_CORTEX_DEBUG_TROUBLESHOOTING_20260318.md)

### PC 仿真与策略验证

- [PC_SIMULATION_GUIDE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/PC_SIMULATION_GUIDE_20260318.md)
- [PC_STRATEGY_SIMULATION_ARCHITECTURE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/PC_STRATEGY_SIMULATION_ARCHITECTURE_20260318.md)
- [PC_PROTECTION_SIMULATION_GUIDE_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/PC_PROTECTION_SIMULATION_GUIDE_20260319.md)
- [PC_SOC_SIMULATION_GUIDE_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/PC_SOC_SIMULATION_GUIDE_20260319.md)
- [PC_LOW_POWER_SIMULATION_GUIDE_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/PC_LOW_POWER_SIMULATION_GUIDE_20260319.md)
- [HOST_REPLAY_LOGGING_GUIDE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/HOST_REPLAY_LOGGING_GUIDE_20260318.md)
- [HOST_SCHEDULER_SIMULATION_GUIDE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/HOST_SCHEDULER_SIMULATION_GUIDE_20260318.md)
- [SAME_CODE_SIMULATION_STRATEGY_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/SAME_CODE_SIMULATION_STRATEGY_20260318.md)

### Codex 接管与流程

- [CODEX_RELIABILITY_WORKFLOW_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/CODEX_RELIABILITY_WORKFLOW_20260319.md)
- [CODEX_OVERVIEW_GUIDE_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/CODEX_OVERVIEW_GUIDE_20260319.md)
- [CODEX_PR_REVIEW_WORKFLOW_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/CODEX_PR_REVIEW_WORKFLOW_20260319.md)
- [INDIVIDUAL_MULTI_PROJECT_MANAGEMENT_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/INDIVIDUAL_MULTI_PROJECT_MANAGEMENT_20260319.md)

### 模板与项目族

- [TEMPLATE_PROJECT_FAMILY_GUIDE_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/TEMPLATE_PROJECT_FAMILY_GUIDE_20260319.md)

### 板级兼容与旧链路参考

- [BOARD_DEBUG_GUIDE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/BOARD_DEBUG_GUIDE_20260318.md)
- [BOARD_DEBUG_SCENARIOS_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/BOARD_DEBUG_SCENARIOS_20260318.md)
- [BOARD_STATUS_WATCH_GUIDE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/BOARD_STATUS_WATCH_GUIDE_20260318.md)
- [GCC_KEIL_AFE_ALIGNMENT_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/GCC_KEIL_AFE_ALIGNMENT_20260318.md)
- [AFE_GCC_KEIL_RUNTIME_INCIDENT_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/AFE_GCC_KEIL_RUNTIME_INCIDENT_20260318.md)

## 已归档文档

下面这些属于阶段性记录或现场结论，保留用于回溯，但不再作为主入口：

- [STAGE_A_AUTOMATION_BOOTSTRAP_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/archive/20260318/STAGE_A_AUTOMATION_BOOTSTRAP_20260318.md)
- [STAGE_B_FIRMWARE_CMAKE_BOOTSTRAP_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/archive/20260318/STAGE_B_FIRMWARE_CMAKE_BOOTSTRAP_20260318.md)
- [ENVIRONMENT_BOOTSTRAP_STATUS_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/archive/20260318/ENVIRONMENT_BOOTSTRAP_STATUS_20260318.md)
- [BOARD_RUNTIME_DEBUG_FINDINGS_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/archive/20260318/BOARD_RUNTIME_DEBUG_FINDINGS_20260318.md)

## 维护约定

后续新增文档建议按这个规则放置：

- 当前主路径、长期指南：继续放 `docs/` 根目录
- 阶段性记录、现场排障、一次性状态快照：放 `docs/archive/<date>/`
- 所有新的主入口文档，都要在本文件登记

## 当前已知边界

- 还有一份 [PROJECT_ANALYSIS_REPORT_20260319.md](/Users/cs/Downloads/work/todo/030-309-template/docs/PROJECT_ANALYSIS_REPORT_20260319.md) 属于工作区内的临时分析文件，当前未纳入正式文档导航。
