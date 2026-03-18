# Codex 自动化接管手册

## 1. 这次改造解决了什么

这轮改造把当前仓库从“以 IDE 为中心的工程”推进成“以命令为中心的自动化模板骨架”。

你后续在 Windows 和 macOS 上都应优先通过统一命令入口工作，而不是依赖 Keil、Visual Studio 或手工点击流程。

## 2. 现在仓库里关键文件分别做什么

### 根目录

- `AGENTS.md`
  约束 Codex 的工作方式、分支策略、提交规则和文档要求。
- `Taskfile.yml`
  所有自动化命令的统一入口。
- `CMakePresets.json`
  固件构建 preset 定义。
- `pyproject.toml`
  Python 自动化脚本依赖入口。
- `mise.toml`
  跨平台工具版本固定入口。
- `README.md`
  仓库入口说明。

### `docs/`

- `TOOLCHAIN_MIGRATION_BLUEPRINT_20260318.md`
  总蓝图。
- `STAGE_A_AUTOMATION_BOOTSTRAP_20260318.md`
  自动化骨架说明。
- `STAGE_B_FIRMWARE_CMAKE_BOOTSTRAP_20260318.md`
  固件 CMake 骨架说明。
- `CROSS_PLATFORM_SETUP_GUIDE_20260318.md`
  Win/Mac 环境安装指南。
- `CODEX_AUTOMATION_HANDBOOK_20260318.md`
  本手册，面向日后使用与学习。

### `scripts/`

- `check_toolchain.py`
  诊断工具链是否安装齐全。
- `new_project.py`
  从 `templates/` 读取模板并生成新项目。
- `patch_project.py`
  按 JSON 变量表对现有项目执行占位符替换。
- `analyze_map.py`
  分析 map 文件。
- `export_artifacts.py`
  清理或导出自动化产物目录。
- `debug_capture.py`
  生成或执行 J-Link/OpenOCD 命令。

### `firmware/`

- `CMakeLists.txt`
  当前 STM32F030 固件的 GCC 构建入口。
- `linker/stm32f030c8_app.ld`
  APP 分区链接脚本。
- `startup/startup_stm32f0xx_gcc.S`
  GCC 启动文件。

### `toolchains/`

- `arm-none-eabi-gcc.cmake`
  GCC 交叉编译工具链定义。

### `templates/`

- `firmware-stm32f0/`
  新项目模板示例。
- `vars.sample.json`
  占位符替换示例变量表。

## 3. 你以后最常用的命令

### 环境检查

```powershell
task doctor
```

### 初始化

```powershell
task init
```

### 固件构建

```powershell
task build
```

### 生成新项目

```powershell
task new PROJECT_NAME=my-bms DEVICE_CODE=STM32F030C8 PROTOCOL_VARIANT=modbus CUSTOMER_CODE=acme
```

### 对现有项目做批量替换

```powershell
task patch VARS_FILE=templates/vars.sample.json
```

### 生成烧录/调试计划

```powershell
task flash
task debug
```

## 4. 模板生成机制

`new_project.py` 会从 `templates/<template-name>/template.json` 读取模板说明，然后复制模板目录里的文件到目标目录，并替换以下占位符：

- `__PROJECT_SLUG__`
- `__DEVICE_CODE__`
- `__PROTOCOL_VARIANT__`
- `__CUSTOMER_CODE__`

这意味着你以后新增模板时，只要在模板文件和文件名里使用这些占位符，Codex 就能自动生成新工程。

## 5. 批量替换机制

`patch_project.py` 通过 `--vars-file` 读取 JSON 键值对，例如：

```json
{
  "__PROJECT_SLUG__": "my-bms",
  "__DEVICE_CODE__": "STM32F030C8"
}
```

然后对项目树内的文本文件执行替换。先用 `--check-only` 预览，再决定是否实际执行。

## 6. 你这次让我做了哪些工作

本轮我完成了以下几件事：

1. 新建独立分支 `codex/toolchain-migration-blueprint`，避免污染你现有工作区。
2. 补齐工具链迁移蓝图与分阶段文档。
3. 增加统一任务入口 `Taskfile.yml`。
4. 增加 Python 自动化脚本骨架。
5. 增加 Win/Mac 跨平台环境安装文档。
6. 增加 STM32F030 的 GCC/CMake 构建骨架。
7. 增加 J-Link/OpenOCD 调试与烧录命令化入口。
8. 增加模板生成与占位符替换能力。

## 7. 现在还没完成到什么程度

已经完成：

- 模板仓库骨架
- 自动化入口
- 文档与学习材料
- 基础模板生成能力
- 基础调试/烧录命令化能力

还未完成：

- 在当前机器上真实跑通 GCC 首次编译
- 把 `flash/debug` 直接接到你本机已安装的实际调试器
- 把上位机迁到 `.NET 8`
- 把模板生成器接到更复杂的工程改名、协议裁剪和客户配置注入逻辑

## 8. 下一步你最该做什么

1. 按 `CROSS_PLATFORM_SETUP_GUIDE_20260318.md` 在 Win/Mac 装好工具。
2. 先跑 `task doctor`。
3. 工具齐全后让我继续推进，直接追到 GCC 首次编译通过。
4. 编译跑通后，再让我把 `task flash/debug` 绑定到你的实际 J-Link 或 OpenOCD 环境。
