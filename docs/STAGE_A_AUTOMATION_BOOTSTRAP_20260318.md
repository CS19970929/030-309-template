# 阶段 A 自动化骨架说明

本文件对应“工具链迁移蓝图”的阶段 A，目标是先补齐跨平台自动化骨架，不直接改动当前业务源码。

## 本阶段新增内容

- 根级 `AGENTS.md`
- 根级 `Taskfile.yml`
- 根级 `pyproject.toml`
- 根级 `mise.toml`
- 根级 `CMakePresets.json`
- `scripts/` 目录下的基础脚本骨架

## 本阶段解决的问题

- 为 Codex 提供统一命令入口。
- 为 Codex 提供统一环境诊断入口 `task doctor`。
- 为未来的 `CMake + GCC` 固件构建预留配置位置。
- 为模板化生成、map 分析、调试采集建立脚本骨架。
- 为 Windows/macOS 双端协作建立统一约束。

## 当前阶段的边界

- 还没有迁移现有 Keil 工程到 `CMake`。
- 还没有把上位机迁移到 `.NET 8`。
- `flash` 与 `debug` 任务当前输出的是操作计划，不直接调用下载器。
- `new` 任务当前生成的是基础模板骨架，用于验证模板工作流。

## 建议的下一步

1. 建立 `firmware/` 目录，并迁入最小可编译的 STM32F0 工程。
2. 增加 `toolchains/arm-none-eabi-gcc.cmake`。
3. 让 `task build` 真正调用 `cmake --preset firmware-debug`。
4. 补齐 `J-Link` 和 `OpenOCD` 的无交互下载脚本。
5. 把当前项目重命名、参数替换逻辑逐步沉淀到 `scripts/new_project.py` 和 `scripts/patch_project.py`。
