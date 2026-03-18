# Codex 全接管蓝图（Win/Mac 双端）

## 目标

本蓝图的目标不是“让项目能编一次”，而是让 `Codex App` 在 `Windows` 和 `macOS` 上都能稳定接管日常工程工作。

接管的目标边界包括：

- 环境检查
- Python 工具初始化
- `PC` 主机侧仿真
- `MCU` 固件构建
- 烧录与调试入口
- VS Code 任务与调试配置
- 文档与操作知识资产沉淀

不包括：

- 用户未接入的真实硬件链路结果
- 未安装的闭源工具自动下载
- 量产参数、保护阈值、Flash 布局的未经确认修改

## 当前阶段结论

截至 2026-03-18，仓库已经达到下面这个状态：

- `task doctor` 在 `Win/Mac` 都可作为统一入口
- `task init` 在 `Win/Mac` 都可执行
- `task sim-modbus` / `task sim-replay` 在 `Win/Mac` 都可作为 `PC` 主路径
- `task build` 在 `Win/Mac` 都可作为 `MCU` GCC 构建主路径
- `ST-Link/OpenOCD` 已纳入统一命令入口
- VS Code 的 `tasks.json` / `launch.json` 已从“Windows 写死配置”调整为 “Win/Mac 双端兼容”

## 接管分层

### 第 1 层：命令契约

Codex 不应依赖 IDE 点击路径，而应依赖固定命令契约。

当前建议的主命令集：

- `task doctor`
- `task init`
- `task sim-modbus`
- `task sim-replay`
- `task build`
- `task build-optimized`
- `task build-monitor`
- `task flash-stlink`
- `task watch-board-stlink`

### 第 2 层：平台适配

平台差异应收敛在少数文件中，而不是散落在业务代码里。

当前平台适配点：

- [Taskfile.yml](/Users/cs/Downloads/work/todo/030-309-template/Taskfile.yml)
- [toolchains/arm-none-eabi-gcc.cmake](/Users/cs/Downloads/work/todo/030-309-template/toolchains/arm-none-eabi-gcc.cmake)
- [.vscode/tasks.json](/Users/cs/Downloads/work/todo/030-309-template/.vscode/tasks.json)
- [.vscode/launch.json](/Users/cs/Downloads/work/todo/030-309-template/.vscode/launch.json)
- [scripts/check_toolchain.py](/Users/cs/Downloads/work/todo/030-309-template/scripts/check_toolchain.py)

### 第 3 层：执行反馈

Codex 要能判断执行结果，而不是只会发命令。

当前反馈产物：

- `artifacts/host-sim/*.log`
- `artifacts/host-sim/*.jsonl`
- `artifacts/cmake/firmware-release/*.bin`
- `artifacts/cmake/firmware-release/*.hex`
- `artifacts/cmake/firmware-release/firmware/*.elf`

### 第 4 层：知识资产

每次平台适配和流程收敛都必须沉淀进 `docs/`，否则 Codex 下一次仍然需要重新猜。

当前关键文档建议阅读顺序：

1. [README.md](/Users/cs/Downloads/work/todo/030-309-template/README.md)
2. [CROSS_PLATFORM_SETUP_GUIDE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/CROSS_PLATFORM_SETUP_GUIDE_20260318.md)
3. [MACOS_BOOTSTRAP_GUIDE_20260318.md](/Users/cs/Downloads/work/todo/030-309-template/docs/MACOS_BOOTSTRAP_GUIDE_20260318.md)
4. 本文档

## Win/Mac 能力矩阵

### PC 路线

- Windows：可用
- macOS：可用

统一入口：

```bash
task sim-modbus
task sim-replay
```

### MCU 构建路线

- Windows：可用
- macOS：可用

统一入口：

```bash
task build
```

### MCU 下载与调试路线

- Windows：已具备命令与 VS Code 入口
- macOS：已具备命令与 VS Code 入口

统一入口：

```bash
task flash-stlink
```

VS Code 入口：

- `Cortex-Debug: STM32F030 (ST-Link/OpenOCD)`
- `Cortex-Debug: STM32F030 (ST-Link/OpenOCD, Optimized)`

说明：

- “入口具备”不等价于“当前线程已完成实机验证”。
- 真正是否成功，还依赖你的 `ST-Link`、线缆、板子上电状态和本机 USB 连接。

## 设计原则

### 原则 1：Win/Mac 共享同一任务名

用户和 Codex 都不应该记忆两套命令。

### 原则 2：IDE 只做界面，不做流程控制

真正的流程控制应下沉到：

- `Taskfile.yml`
- `scripts/`
- `CMakePresets.json`

### 原则 3：平台差异只允许出现在入口层

业务源码不应该因为 `Windows` / `macOS` 差异而分叉。

本轮修复的 `#include "BSP\\bsp.h"` 就属于违反这条原则的典型例子。

### 原则 4：诊断必须比执行更可靠

`task doctor` 的价值高于“盲目尝试 build”。

因此工具检测必须能区分：

- 工具不存在
- 工具存在但不可用
- 工具可用但板级链路未接

## 推荐日常工作流

### 新机器上手

```bash
task doctor
task init
task sim-modbus
task build
```

### 日常协议或脚本开发

```bash
task sim-modbus
task sim-replay
```

### 日常 MCU 开发

```bash
task build
task flash-stlink
```

### 监控与状态采样

```bash
task build-monitor
task flash-stlink-monitor
task watch-board-stlink
```

## 还未完全闭环的部分

下面这些能力当前已经有入口，但还应继续做“双端实机确认”：

- macOS 下真实 `ST-Link` 烧录实测
- macOS 下 `Cortex-Debug` 断点与单步实测
- `J-Link` 在 Win/Mac 的统一探测与启动路径
- 真板状态采样链路的长期运行验证

## 下一阶段建议

### 阶段 1

把 `ST-Link` 真板下载与调试在 `Win/Mac` 各做一次实机验真，并把错误模式沉淀到脚本和文档里。

### 阶段 2

把 `Taskfile.yml` 继续收敛成 “8 到 12 个稳定主命令”，减少同类入口。

### 阶段 3

把板级日志、烧录日志、调试会话日志统一导出到 `artifacts/`，让 Codex 能直接分析。

### 阶段 4

把当前仓库正式定型为“Codex 接管模板仓库”，用于后续 `STM32/BMS` 新项目复用。

## 结论

当前仓库已经从“只在某一台 Windows 上可人工操作”推进到了“Win/Mac 双端都有统一接管入口”的阶段。

下一步的重点不再是补更多零散脚本，而是把：

- 真板下载
- 真板调试
- 长期日志
- 模板参数化

继续做成 `Codex App` 可稳定重复执行的标准流程。
