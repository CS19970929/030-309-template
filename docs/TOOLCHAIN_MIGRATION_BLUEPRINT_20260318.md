# 项目工具链迁移蓝图（面向 Codex 自动化与 Win/Mac 跨平台）

## 1. 目标

本蓝图用于把当前仓库从“IDE 驱动、平台绑定、人工操作较多”的工程，迁移为“命令驱动、跨平台、适合 Codex 自动化接管”的模板仓库。

迁移后的目标能力：

- 新项目可基于模板自动生成。
- Codex 可稳定执行构建、静态检查、打包、烧录、日志采集与常见改造。
- 公司 Windows 与家里 macOS 使用相同命令入口。
- 下位机、上位机、脚本、文档采用统一工程约定。
- 保留 Keil 兼容出口，但不再把 Keil 作为唯一构建入口。

## 2. 当前项目现状

当前仓库可识别出两类工程：

- 下位机：Keil uVision + ARMCC5 + STM32F030 工程
- 上位机：WinForms + .NET Framework 4.8 工程

当前主要问题：

- 下位机构建依赖 `*.uvprojx`，难以在 macOS 上复用。
- 自动化入口分散，缺少统一命令层。
- `bat` 只能在 Windows 侧自然运行，不利于跨平台。
- 上位机绑定 `WinForms + .NET Framework`，不适合作为跨平台长期方案。
- Codex 缺少稳定的“任务契约”，每次都要重新判断如何构建、调试和改模板。

## 3. 推荐目标工具链

### 3.1 下位机

- 构建系统：`CMake`
- 构建后端：`Ninja`
- 编译器：`arm-none-eabi-gcc`
- 调试/烧录：`J-Link CLI`，可选补充 `OpenOCD`
- 静态检查：`clang-tidy`、`cppcheck`
- 格式化：`clang-format`

说明：

- `CMake + Ninja` 是跨平台自动化的核心。
- `arm-none-eabi-gcc` 可在 Windows/macOS 统一安装与调用。
- Keil 工程保留作为兼容出口，不作为主入口。

### 3.2 上位机

- 首选：`.NET 8`
- 若需要 GUI：`Avalonia`
- 若以产测/配置/日志工具为主：优先做 `.NET 8 CLI`

说明：

- 你当前的 WinForms 工程可先维持现状，不建议马上并入大迁移。
- 新模板建议优先构建 CLI，再决定是否补 GUI。
- CLI 对 Codex 接管最友好，GUI 自动化成本明显更高。

### 3.3 脚本与项目生成

- 脚本语言：`Python`
- Python 环境管理：`uv`
- 任务入口：`Taskfile.yml`

说明：

- Python 适合做模板替换、协议代码生成、map 分析、日志处理、批量改名。
- `uv` 安装快、锁定清晰、跨平台一致性好。
- `Taskfile.yml` 负责把复杂命令收敛为固定入口，便于 Codex 调用。

### 3.4 编辑器与调试体验

- 编辑器：`VS Code`
- C/C++ 语言支持：`clangd` 或 VS Code C/C++ 插件
- 调试插件：`Cortex-Debug`

说明：

- 编辑器只作为人机交互界面，不应承担流程控制职责。
- 真正的自动化入口应是 `task`、`cmake`、`python`、`dotnet`、`JLinkExe`。

### 3.5 环境管理

- 推荐：`mise`
- 可选：`direnv`

说明：

- 用于固定 `python`、`cmake`、`ninja`、`dotnet` 等版本。
- 新机器拉仓库后，环境初始化成本更低。

## 4. 目标仓库结构

建议逐步重构为以下结构：

```text
.
├─ firmware/
│  ├─ app/
│  ├─ bsp/
│  ├─ drivers/
│  ├─ middleware/
│  ├─ linker/
│  ├─ startup/
│  ├─ CMakeLists.txt
│  └─ cmake/
├─ host/
│  ├─ cli/
│  └─ gui/
├─ scripts/
│  ├─ new_project.py
│  ├─ patch_project.py
│  ├─ analyze_map.py
│  ├─ export_artifacts.py
│  └─ debug_capture.py
├─ templates/
│  ├─ firmware-stm32f0/
│  └─ host-cli-dotnet/
├─ tools/
│  ├─ openocd/
│  └─ jlink/
├─ docs/
├─ artifacts/
├─ tests/
├─ Taskfile.yml
├─ pyproject.toml
├─ CMakePresets.json
├─ mise.toml
└─ AGENTS.md
```

说明：

- `firmware/` 是下位机主工程目录。
- `host/` 用于上位机 CLI/GUI。
- `scripts/` 收纳所有可自动执行的工程脚本。
- `templates/` 用于新项目初始化。
- `artifacts/` 收纳构建输出，避免结果文件散落。

## 5. 统一命令入口设计

迁移完成后，应保证所有常见动作都能通过固定命令执行。

建议保留以下标准命令：

- `task init`
- `task build`
- `task rebuild`
- `task lint`
- `task format`
- `task test`
- `task package`
- `task flash`
- `task debug`
- `task map`
- `task new NAME=xxx MCU=stm32f030`

建议的职责边界：

- `task`：对外唯一任务入口
- `cmake`：负责固件工程配置与构建
- `python`：负责脚本化改造与分析
- `dotnet`：负责上位机编译与测试
- `JLinkExe` 或 `OpenOCD`：负责下载与调试动作

Codex 接管时，优先执行这些固定命令，而不是依赖 IDE 点击流程。

## 6. 下位机迁移策略

### 6.1 原则

- 保留当前 `Keil` 工程文件用于兼容和回归对比。
- 新增 `CMake` 构建链，不直接破坏旧流程。
- 在迁移前期允许“双轨制”：`Keil` 与 `CMake` 同时存在。

### 6.2 建议顺序

第一阶段：

- 抽取源码目录与 include 目录。
- 建立 `firmware/CMakeLists.txt`。
- 固定 GCC toolchain 文件。
- 跑通 `elf`、`bin`、`map` 输出。

第二阶段：

- 补充 `clang-format` 和 `clang-tidy`。
- 把 map 分析、产物导出、版本号注入脚本化。
- 用 `task build` 代替手工 IDE 构建。

第三阶段：

- 把下载和日志采集纳入 `task flash`、`task debug`。
- 把新项目重命名和参数替换纳入 `scripts/new_project.py`。
- 将 Keil 从“主流程”降级为“兼容流程”。

## 7. 上位机迁移策略

### 7.1 对现有项目的判断

现有上位机为 WinForms + .NET Framework 4.8，仅适合继续在 Windows 侧维护，不适合作为新的跨平台模板母版。

### 7.2 目标建议

按优先级推荐：

1. 新建 `host/cli`，采用 `.NET 8`。
2. 如果确实需要跨平台 GUI，再新建 `host/gui`，采用 `Avalonia`。
3. 老 WinForms 工程保留在 `legacy/` 或原位冻结维护。

### 7.3 原因

- CLI 更易自动化、测试、集成和远程调用。
- 绝大多数参数配置、日志抓取、升级分发、产测流程都可以先 CLI 化。
- GUI 应建立在稳定 CLI 能力之上，而不是反过来。

## 8. 面向 Codex 的模板化设计

后续若希望 Codex 自动接管新项目，模板仓库应保证以下内容标准化。

### 8.1 命名约定

- 项目标识：`project_slug`
- 设备标识：`device_code`
- 协议标识：`protocol_variant`
- 客户标识：`customer_code`

模板中统一使用占位符，例如：

- `__PROJECT_SLUG__`
- `__DEVICE_CODE__`
- `__PROTOCOL_VARIANT__`
- `__CUSTOMER_CODE__`

### 8.2 Codex 可自动处理的任务

- 新项目初始化
- 工程改名
- include 路径替换
- 版本号更新
- 芯片型号与存储布局替换
- 协议字段增删
- map 文件分析
- 固件产物导出
- 日志与调试信息归档

### 8.3 必备约束

- 构建入口必须固定且无交互。
- 调试命令必须可脚本化。
- 构建输出路径必须固定。
- 所有脚本应支持 Windows/macOS。
- 配置应尽量声明式，不把关键逻辑埋进 IDE 文件。

## 9. 建议新增的关键文件

### 9.1 `Taskfile.yml`

职责：

- 统一任务命令
- 封装平台差异
- 为 Codex 提供稳定入口

### 9.2 `pyproject.toml`

职责：

- 统一 Python 依赖
- 承载脚本工具依赖
- 便于 `uv sync`

### 9.3 `CMakePresets.json`

职责：

- 固定常用构建配置
- 避免手敲长命令
- 让 Windows/macOS 命令一致

### 9.4 `mise.toml`

职责：

- 固定工具版本
- 降低换机成本

### 9.5 `AGENTS.md`

职责：

- 约束 Codex 工作方式
- 声明标准命令入口
- 声明分支、提交、文档、测试约定

## 10. 推荐的 AGENTS 约束方向

建议在模板仓库的 `AGENTS.md` 中明确写入以下要求：

- 默认使用中文输出
- 进行大改动前必须新开 `codex/` 分支
- 不允许直接依赖 IDE 点击流程完成任务
- 优先使用 `task build`、`task test`、`task flash`
- 大改动必须补充 `docs/` 文档
- 大改动必须生成结构清晰的 git 提交
- 未经用户确认，不改动量产参数、存储布局、保护阈值
- 对嵌入式安全相关逻辑优先做 review，再做修改

## 11. 分阶段落地计划

### 阶段 A：建立自动化骨架

目标：

- 不迁移业务代码
- 先增加自动化骨架和标准入口

交付物：

- `docs/` 迁移文档
- `Taskfile.yml`
- `pyproject.toml`
- `mise.toml`
- `CMakePresets.json`
- `scripts/` 初始脚本骨架

### 阶段 B：下位机构建双轨化

目标：

- 保留 Keil
- 跑通 GCC + CMake 构建

交付物：

- `firmware/CMakeLists.txt`
- `toolchains/arm-none-eabi-gcc.cmake`
- `artifacts/firmware/` 统一输出

### 阶段 C：新项目模板化

目标：

- 从“项目仓库”升级为“项目母版”

交付物：

- `templates/firmware-stm32f0/`
- `scripts/new_project.py`
- `scripts/patch_project.py`

### 阶段 D：上位机跨平台化

目标：

- 逐步替换 WinForms 绑定

交付物：

- `host/cli` 的 `.NET 8` 工具
- 视需要新增 `host/gui` 的 `Avalonia` 应用

## 12. 你当前仓库的建议执行顺序

结合当前仓库现状，建议实际顺序如下：

1. 先不碰现有业务代码。
2. 先新增自动化骨架文件与迁移文档。
3. 再为当前下位机工程补一个最小可用 `CMake` 构建。
4. 再把下载、map 分析、导出产物脚本化。
5. 最后再决定上位机是保留旧工程，还是拆出新的 `.NET 8 CLI`。

这是成本最低、风险最小、最适合你当前项目节奏的方案。

## 13. 下一步建议

如果要继续推进，下一轮建议直接做“阶段 A”：

- 新建 `Taskfile.yml`
- 新建 `pyproject.toml`
- 新建 `mise.toml`
- 新建 `CMakePresets.json`
- 新建 `scripts/` 骨架
- 新建模板级 `AGENTS.md` 约束

这样 Codex 就可以开始通过统一入口接管项目，而不是停留在方案层面。
