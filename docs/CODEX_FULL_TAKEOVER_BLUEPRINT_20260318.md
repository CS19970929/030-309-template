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

## 当前优先级调整

基于当前目标，接下来一段时间**暂不以硬件闭环为主线**，而是优先把下面这条链路做强：

- `PC` 仿真
- `PC` 调试
- `PC` 日志回放
- `PC` 同码化抽象

原因很直接：

- 这部分不依赖板子是否在线
- Win/Mac 两端都可以高频执行
- 更适合 Codex 持续接管
- 可以先把大量协议、状态机、数据适配问题在无板条件下提前收敛

因此当前建议把“真板下载/调试”降为后续阶段，而把“主机侧能力扩张”提到第一优先级。

## 当前工具链的优点

这里的“当前工具链”指的是：

- `Taskfile.yml`
- `CMake + Ninja`
- `arm-none-eabi-gcc`
- `Python + uv`
- `OpenOCD / J-Link`
- `VS Code + Cortex-Debug`

相对于原来的 `Keil + bat + 人工点击`，它的优点不是“更潮”，而是更适合长期被 Codex 接管。

### 优点 1：统一命令契约

旧流程的问题是：

- 同一个动作可能有 IDE、批处理、口头约定三种做法
- Codex 每次都需要重新猜“你现在想走哪一条”

当前工具链的优点是：

- `task doctor`
- `task init`
- `task sim-modbus`
- `task sim-replay`
- `task build`
- `task flash-stlink`

这些命令就是明确契约。

对 Codex 来说，契约越稳定，接管成本越低。

### 优点 2：Win/Mac 可复用

旧流程的主要瓶颈是：

- `Keil`
- `bat`
- Windows 路径依赖

这些天然限制了 `macOS`。

当前工具链的优点是：

- `Taskfile` 可以跨平台
- `CMake + Ninja` 可以跨平台
- `arm-none-eabi-gcc` 可以跨平台
- `Python` 脚本可以跨平台
- `OpenOCD` 可以跨平台

这意味着同一份仓库可以在公司 Windows 和家里 Mac 之间切换，而不是维护两套脑内流程。

### 优点 3：可脚本化、可批处理、可回放

旧流程更像“手工操作现场”。

当前工具链的优点是：

- 可以批量执行
- 可以被 Codex 直接调用
- 可以把日志和产物落到 `artifacts/`
- 可以把失败过程重新分析

这对你后面做：

- 自动化改造
- 日志回放
- 长期监控
- 模板化生成

都有直接收益。

### 优点 4：环境问题与源码问题可以分层定位

旧流程下最容易混淆的是：

- 机器没装好
- 路径没配好
- 工具是残缺安装
- 真正的源码兼容问题

当前工具链的优点是：

- `task doctor` 先看环境
- `task sim-*` 先看 `PC` 逻辑
- `task build` 再看 `MCU` 构建
- `flash/debug` 最后看板级链路

这样问题会按层收敛，而不是一上来就陷入“为什么板子不跑”。

### 优点 5：更适合知识资产沉淀

旧流程里很多知识只存在于：

- 你脑子里
- 某个临时终端
- 某次 IDE 点击顺序

当前工具链的优点是：

- 入口文件在仓库里
- 脚本在仓库里
- 文档在仓库里
- 产物位置固定

这才符合“可长期保存、可复用、可被 Codex 持续学习”的目标。

### 优点 6：兼容旧链路，但不再被旧链路绑定

这个点很关键。

当前策略不是“推翻 Keil”，而是：

- 保留 `Keil` 作为 Windows 兼容出口
- 用 GCC/CMake 建立双端主路径
- 用统一命令层收敛流程

这样做的优点是：

- 风险小
- 可渐进迁移
- 出现问题时还能回到旧链路做对照

这比一次性激进替换更适合当前项目。

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
- 在当前阶段，这条路线不是主线，只保留为后续扩展能力。

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

## 当前主线任务

当前最值得投入、也最适合 Codex 接管的主线不是硬件，而是下面四类能力：

### 主线 1：PC 仿真能力扩张

目标：

- 让更多业务逻辑在 `PC` 上运行
- 让 Win/Mac 双端都能快速复现问题

当前基础：

- `task sim-modbus`
- `task sim-replay`
- `task sim-scheduler`

下一步应扩张到：

- `ascii_slave` 解析路径
- `bms_comm_data_adapter`
- 更多状态机与数据封装逻辑

### 主线 2：PC 调试体验稳定化

目标：

- 在 Win/Mac 上都能直接从 VS Code 启动 `host_sim`
- 让断点、单步、日志观察成为默认工作方式

当前基础：

- `.vscode/launch.json` 已兼容双端
- 主机侧 replay/scheduler 已有独立入口

下一步应补强：

- 明确 `cppdbg` 在 Win/Mac 的推荐调试器
- 补一份主机侧调试操作手册
- 统一常用 launch 配置命名

### 主线 3：PC 可观测性增强

目标：

- 让 Codex 不只是“跑程序”，还能“读日志、看快照、判断异常”

当前基础：

- `artifacts/host-sim/*.log`
- `artifacts/host-sim/*.jsonl`

下一步应补强：

- 错误分类
- 回放摘要
- 周期任务状态快照
- 更稳定的日志字段契约

### 主线 4：同码化改造

目标：

- 逐步把项目从“板级强耦合”改成“PC/MCU 共用业务层”

当前方向已明确：

- `app_core`
- `platform_api`
- `platform_impl`

下一步应优先拆的模块：

- `ascii_slave`
- `bms_comm_data_adapter`
- 纯数据变换逻辑
- 调度器相关逻辑

## 还未完全闭环的部分

下面这些能力当前已经有入口，但在当前阶段先不作为第一优先级：

- macOS 下真实 `ST-Link` 烧录实测
- macOS 下 `Cortex-Debug` 断点与单步实测
- `J-Link` 在 Win/Mac 的统一探测与启动路径
- 真板状态采样链路的长期运行验证

## 下一阶段规划（PC 优先）

这里的规划目标不是“继续加功能”，而是把“Codex 可以部分接管”推进到“Codex 可以稳定接管日常主流程”。

### 阶段 A：PC 仿真扩张

目标：

- 把 `PC` 能覆盖的逻辑面继续扩大

完成标准：

- `task sim-modbus`
- `task sim-replay`
- `task sim-scheduler`

三条路径都保持稳定。

- 至少再新增一类业务逻辑进入 `host_sim`

产出：

- 更宽的无板验证面
- 更高频的 Win/Mac 共用工作流

### 阶段 B：PC 调试与日志闭环

目标：

- 让主机侧调试变成第一默认路径

完成标准：

- Win/Mac 下 VS Code 主机侧 launch 都能直接使用
- Codex 可稳定读取 replay / scheduler 日志与快照
- 出错时能快速定位到输入帧、状态步、输出快照

产出：

- 主机侧调试手册
- 日志字段约定
- 常见问题定位模板

### 阶段 C：同码化拆分

目标：

- 把更多业务代码从板级依赖中抽离出来

完成标准：

- 至少形成一块可复用的 `app_core` 雏形
- 平台差异开始集中到 `platform_api/platform_impl`

产出：

- 结构化改造方案
- 第一批可共用模块

### 阶段 D：真板闭环

目标：

- 在前面三阶段稳定后，再补真实硬件闭环

完成标准：

- Windows 上完成一次 `build -> flash-stlink -> halt`
- macOS 上完成一次 `build -> flash-stlink -> halt`

产出：

- 双端 ST-Link 排障文档
- 调试链路实机验真记录
### 阶段 E：命令收敛与模板定型

目标：

- 把仓库主命令收敛为稳定主路径
- 把“当前项目实践”提升为“后续项目模板”

完成标准：

- 常用命令稳定在 `8~12` 个
- 文档与脚本边界清晰
- 新项目复制后可直接进入接管流程

产出：

- 模板仓库规范
- 项目启动清单
- 交接与协作约定

## 推荐执行顺序

如果你的目标是“尽快让 Codex 真正接管日常工作”，建议按下面顺序推进：

1. 先扩张 `PC` 仿真覆盖面
2. 再把 `PC` 调试和日志闭环做强
3. 再推进同码化拆分
4. 最后再做真板下载、调试和观测闭环

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
