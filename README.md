# STM32 BMS Toolchain Migration Workspace

本仓库正在从 `Keil + bat + 手工流程` 迁移到 `CMake + Taskfile + Python + Codex` 的跨平台自动化模板。

## 你现在可以直接用的入口

- `task doctor`：检查当前机器缺哪些工具
- `task init`：初始化 Python 工具环境
- `task build`：走 CMake Preset 构建固件
- `task map`：生成 map 摘要
- `task flash`：生成烧录命令计划
- `task debug`：生成调试命令计划
- `task new`：基于模板生成新项目
- `task patch`：对现有项目执行占位符替换

## 关键文档

- [`docs/TOOLCHAIN_MIGRATION_BLUEPRINT_20260318.md`](E:/TODO/030%20+%20309/docs/TOOLCHAIN_MIGRATION_BLUEPRINT_20260318.md)
- [`docs/CROSS_PLATFORM_SETUP_GUIDE_20260318.md`](E:/TODO/030%20+%20309/docs/CROSS_PLATFORM_SETUP_GUIDE_20260318.md)
- [`docs/CODEX_AUTOMATION_HANDBOOK_20260318.md`](E:/TODO/030%20+%20309/docs/CODEX_AUTOMATION_HANDBOOK_20260318.md)
- [`docs/PC_SIMULATION_GUIDE_20260318.md`](E:/TODO/030%20+%20309/docs/PC_SIMULATION_GUIDE_20260318.md)
- [`docs/SAME_CODE_SIMULATION_STRATEGY_20260318.md`](E:/TODO/030%20+%20309/docs/SAME_CODE_SIMULATION_STRATEGY_20260318.md)
- [`docs/LOGGING_AND_MONITORING_PLAN_20260318.md`](E:/TODO/030%20+%20309/docs/LOGGING_AND_MONITORING_PLAN_20260318.md)

## 当前状态

- Keil 工程仍保留兼容性
- GCC/CMake 构建链已搭出骨架
- 模板生成与占位符替换已具备基础能力
- 调试/烧录已具备 J-Link/OpenOCD 命令化入口

## 建议使用顺序

1. 先看跨平台安装文档
2. 运行 `task doctor`
3. 运行 `task init`
4. 运行 `task test`
5. 工具齐全后运行 `task build`
