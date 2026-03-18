# macOS 本机打通记录

## 目标

这份文档用于说明当前仓库在 `macOS` 上的实际打通情况，以及你换到家里 Mac 后应该怎么走最稳。

## 2026-03-18 现场验证结果

这次在 macOS 上实际验证到的结论如下：

- `python3` 可用
- `cmake` 可用
- `ninja` 可用
- `arm-none-eabi-gcc` 可执行文件可找到
- `host_sim` 的 `configure + build + run` 已实际通过
- 原始 `Taskfile.yml` 在 Unix 下执行 `sim-*` 时缺少 `./` 前缀，已修复
- 固件 `firmware-release` 的 `configure` 可通过
- 固件 `build` 当前卡在 `arm-none-eabi-gcc` 缺少标准头文件，不是业务源码错误

## 已实际跑通的命令

```bash
python3 scripts/check_toolchain.py
cmake --preset host-sim-debug
cmake --build --preset build-host-sim-debug
./artifacts/cmake/host-sim-debug/host_sim/modbus_parser_host_test
cmake --preset firmware-release
```

其中 `modbus_parser_host_test` 的结果为全部 `PASS`。

## 当前阻塞点

当前 macOS 的主要阻塞不是仓库代码，而是 ARM 工具链安装形态。

现场检查到：

- `arm-none-eabi-gcc -xc -E -v /dev/null` 的搜索路径里没有 `arm-none-eabi/include`
- Homebrew 当前这套 `arm-none-eabi-gcc` 是 `without-headers` 构建
- 真正编译固件时会报：

```text
fatal error: stdint.h: No such file or directory
```

所以当前状态应理解为：

- `host_sim` 已跑通
- `firmware build` 还差一套完整可用的 ARM 标准库/头文件环境

## 推荐安装顺序

### 先打通主机侧

```bash
brew install python uv cmake ninja go-task open-ocd
task doctor
task init
task sim-modbus
task sim-replay
```

说明：

- `task init` 现在会优先复用已经装好的 `uv`，避免 Homebrew Python 的 `externally-managed-environment` 报错。
- `task init` 同时改为 `uv sync --no-install-project`，避免 `hatchling` 试图把当前仓库按 Python 包安装而失败。

### 再打通固件侧

安装一套完整的 `Arm GNU Toolchain`，确保下面命令返回的不是 Homebrew 的 `without-headers` 版本：

```bash
which arm-none-eabi-gcc
arm-none-eabi-gcc -xc -E - <<'EOF'
#include <stdint.h>
EOF
```

只有这一步通过后，再执行：

```bash
task build
```

## 判断标准

### 主机侧通过标准

- `task sim-modbus` 结束码为 `0`
- `task sim-replay` 能生成：
  - `artifacts/host-sim/modbus-replay.log`
  - `artifacts/host-sim/modbus-replay.jsonl`

### 固件侧通过标准

- `task build` 成功生成：
  - `artifacts/cmake/firmware-release/firmware/CommomSH367309_16series_030C8T6_C.elf`
  - `artifacts/cmake/firmware-release/CommomSH367309_16series_030C8T6_C.bin`
  - `artifacts/cmake/firmware-release/CommomSH367309_16series_030C8T6_C.hex`

## 结论

当前仓库在 macOS 上已经可以稳定跑：

- 脚本链路
- 主机侧仿真链路

当前还不能直接跑通的只有：

- 基于 Homebrew `arm-none-eabi-gcc` 的固件构建链路

这部分需要你把 ARM 工具链替换成完整发行版后再继续。
