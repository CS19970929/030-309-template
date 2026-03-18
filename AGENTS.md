# AGENTS.md

## 语言与协作

- 全部使用中文回答。
- 涉及较大改动时，先新建 `codex/` 前缀分支再动手。
- 进行较大改动时，必须同时补充 `docs/` 文档，并生成描述清晰的 git 提交。

## 工作原则

- 不把 IDE 点击流程当作主流程。
- 优先使用统一命令入口：`task init`、`task build`、`task test`、`task flash`、`task debug`。
- 未经确认，不直接修改量产参数、保护阈值、Flash 布局、校准常量。
- 涉及安全、存储、升级、保护策略时，先 review 再修改。

## 当前模板目标

- 当前仓库要逐步演进为“可被 Codex 接管的模板仓库”。
- 自动化入口由 `Taskfile.yml`、`scripts/`、`CMakePresets.json`、`pyproject.toml` 提供。
- Keil 工程保留兼容性，但不再作为唯一构建入口。

## 分支与提交

- 需要新建工作分支时，使用 `codex/<topic>`。
- 提交信息尽量遵循如下前缀：`docs:`、`build:`、`tooling:`、`scripts:`、`refactor:`、`fix:`。
- 不要把用户现有的未提交改动混入本次提交。

## 文件约定

- 自动化脚本放在 `scripts/`。
- 迁移方案、设计说明、操作手册放在 `docs/`。
- 构建产物统一导出到 `artifacts/`。
- 新项目模板放在 `templates/`。

## 面向 Codex 的执行要求

- 构建前先检查工作区状态，避免误提交用户改动。
- 修改模板或工具链时，优先保证 Windows/macOS 命令一致。
- 优先实现可脚本化、无交互的流程。
- 如需新增自动化命令，优先收敛到 `Taskfile.yml`，而不是分散成多个平台专用脚本。
