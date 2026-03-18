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

安装后执行：

```bash
python3 scripts/check_toolchain.py
python3 -m pip install uv
uv sync
task doctor
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
