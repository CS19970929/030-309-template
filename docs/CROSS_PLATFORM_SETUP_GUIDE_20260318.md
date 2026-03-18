# Win/Mac 跨平台环境安装指南

本指南用于让当前仓库在 Windows 与 macOS 上保持一致的自动化入口，便于 Codex 接管构建、调试、模板生成与脚本执行。

## 目标工具

- Python 3.10 及以上
- `uv`
- `cmake`
- `ninja`
- `arm-none-eabi-gcc`
- `JLinkExe` 或 `openocd`
- 可选：`.NET 8`
- 可选：`Task`

## Windows 建议

推荐使用 `winget`。

建议安装项：

- Python
- CMake
- Ninja
- ARM GNU Toolchain
- SEGGER J-Link
- `go-task`
- `.NET SDK 8`

安装后执行：

```powershell
python scripts/check_toolchain.py
python -m pip install uv
uv sync
task doctor
```

## macOS 建议

推荐使用 `brew`。

建议安装项：

- `python`
- `uv`
- `cmake`
- `ninja`
- `arm-none-eabi-gcc`
- `openocd` 或 J-Link
- `go-task`
- `dotnet-sdk`

建议先安装：

```bash
brew install python uv cmake ninja go-task open-ocd
```

说明：

- `host_sim` 路线只依赖 `python3 + uv + cmake + ninja + task`。
- 固件 `task build` 还依赖一套**完整**的 `Arm GNU Toolchain`。
- 我在 macOS 现场验证时发现，当前 Homebrew 的 `arm-none-eabi-gcc` 是 `without-headers` 形态，`cmake --preset firmware-release` 能过，但真正编译会在 `<stdint.h>` 处失败。
- 因此，macOS 上如果你的目标是跑通固件构建，优先安装 Arm 官方发布的完整工具链，或者其它自带 `newlib` / 标准头的发行版，再确保 `arm-none-eabi-gcc` 在 `PATH` 中指向那套完整工具链。

安装后执行：

```bash
python3 scripts/check_toolchain.py
task init
task doctor
```

补充说明：

- `task init` 在 macOS 下会优先复用已安装的 `uv`。
- 如果你已经通过 `brew install uv` 安装过，就不会再触发 Homebrew Python 的 `PEP 668` 限制。
- 当前仓库的 `task init` 使用 `uv sync --no-install-project`，只同步工具依赖，不要求仓库本身是可发布 Python 包。

如果只是先跑主机侧仿真，推荐顺序：

```bash
task doctor
task init
task sim-modbus
task sim-replay
```

如果要跑固件构建，再执行：

```bash
task build
```

## 推荐验证顺序

1. 先执行 `task doctor`
2. 再执行 `task init`
3. 再执行 `task test`
4. 工具齐全后执行 `task build`
5. 产物生成后执行 `task flash` 或 `task debug`

## 当前仓库的注意事项

- 当前机器若未安装 `cmake`、`ninja`、`arm-none-eabi-gcc`，`task build` 不会成功。
- 当前机器若未安装 `JLinkExe` 或 `openocd`，`task flash` 与 `task debug` 只能生成命令计划文件。
- 现有 `Keil` 工程仍保留，可作为迁移过渡期的兼容出口。
