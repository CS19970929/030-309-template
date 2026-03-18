# 环境打通状态

## 当前结论

截至 2026-03-18，这个仓库的运行链路已经打通到下面这个程度：

- Windows 上的 `Keil` 命令行构建可用
- Windows 上的 `Python` 自动化脚本可用
- Windows 上的 `host_sim` 主机侧仿真可用
- Windows 上的 `host replay` 日志回放可用
- Windows 上的 `GCC + CMake Release` 固件构建可用
- `VS Code` 已提供常用任务入口
- `J-Link` / `OpenOCD` 工具已安装，但真实连板调试还需要硬件接入
- `GCC + CMake` 固件构建已进入源码兼容性修复阶段，不再是环境问题

## 当前机器已经安装并验证的工具

- `Python 3.12`
- `uv`
- `cmake`
- `ninja`
- `arm-none-eabi-gcc`
- `task`
- `J-Link`
- `openocd`
- `Keil MDK`

说明：
- 这些工具已经在当前 Windows 机器上安装完成。
- 当前 Codex 线程里的旧终端没有自动继承新的用户 `PATH`，所以新开的终端或重启后的 `VS Code` 体验会更稳定。
- 仓库里的 [`Taskfile.yml`](E:/TODO/030%20+%20309/Taskfile.yml) 和 [tasks.json](E:/TODO/030%20+%20309/.vscode/tasks.json) 已经优先使用稳定路径或 `py -3.12`，尽量降低这个影响。

## 已实际跑通的链路

### 1. MCU: Keil 构建

已执行：

```powershell
C:\Keil_v5\UV4\UV4.exe -b CommomSH367309_16series_030C8T6_C.uvprojx -j0
```

结果：

- `0 errors`
- `36 warnings`
- `Code=50716`
- `RO-data=2876`
- `RW-data=1252`
- `ZI-data=6284`
- `Build Time Elapsed=00:00:04`

日志与产物：

- [build_log.htm](E:/TODO/030%20+%20309/Objects/CommomSH367309_16series_030C8T6_C.build_log.htm)
- [CommomSH367309_16series_030C8T6_C.axf](E:/TODO/030%20+%20309/Objects/CommomSH367309_16series_030C8T6_C.axf)
- [CommomSH367309_16series_030C8T6_C.bin](E:/TODO/030%20+%20309/Objects/CommomSH367309_16series_030C8T6_C.bin)

### 2. PC: 自动化脚本

已执行成功：

```powershell
py -3.12 scripts/check_toolchain.py
py -3.12 scripts/new_project.py --template firmware-stm32f0 --project-name demo-bms --device-code STM32F030C8 --protocol-variant modbus --customer-code common --output templates/generated/demo-bms
py -3.12 scripts/patch_project.py --project-root templates/firmware-stm32f0 --vars-file templates/vars.sample.json --check-only
py -3.12 scripts/analyze_map.py --map CommomSH367309_16series_030C8T6_C.map --top 20 --json artifacts/map-summary.json
py -3.12 scripts/debug_capture.py flash --probe jlink --artifact Objects/output.hex --output artifacts/flash-plan.md
py -3.12 scripts/debug_capture.py debug --probe openocd --artifact Objects/output.hex --output artifacts/debug-plan.md
```

产物：

- [map-summary.json](E:/TODO/030%20+%20309/artifacts/map-summary.json)
- [flash-plan.md](E:/TODO/030%20+%20309/artifacts/flash-plan.md)
- [debug-plan.md](E:/TODO/030%20+%20309/artifacts/debug-plan.md)
- [project.json](E:/TODO/030%20+%20309/templates/generated/demo-bms/project.json)

### 3. PC: 主机侧仿真

已执行成功：

```powershell
"C:\Program Files\CMake\bin\cmake.exe" --preset host-sim-debug
"C:\Program Files\CMake\bin\cmake.exe" --build --preset build-host-sim-debug
artifacts\cmake\host-sim-debug\host_sim\modbus_parser_host_test.exe
```

实际输出：

```text
[PASS] read holding registers
[PASS] write single register
[PASS] write multiple registers
[PASS] invalid slave address
[PASS] invalid function code
All host-side Modbus parser checks passed.
```

可执行文件：

- [modbus_parser_host_test.exe](E:/TODO/030%20+%20309/artifacts/cmake/host-sim-debug/host_sim/modbus_parser_host_test.exe)

### 4. PC: 日志回放与快照

已执行成功：

```powershell
task sim-replay
```

产物：

- [modbus-replay.log](E:/TODO/030%20+%20309/artifacts/host-sim/modbus-replay.log)
- [modbus-replay.jsonl](E:/TODO/030%20+%20309/artifacts/host-sim/modbus-replay.jsonl)
- [modbus_frames.txt](E:/TODO/030%20+%20309/host_sim/scenarios/modbus_frames.txt)
- [modbus_invalid_frames.txt](E:/TODO/030%20+%20309/host_sim/scenarios/modbus_invalid_frames.txt)

### 5. MCU: GCC + CMake Release 构建

已执行成功：

```powershell
"C:\Program Files\CMake\bin\cmake.exe" --preset firmware-release
"C:\Program Files\CMake\bin\cmake.exe" --build --preset build-firmware-release
```

实际结果：

```text
text=36848
data=60
bss=6720
dec=43628
hex=aa6c
```

说明：

- 当前 `Release` 已经能落进现有应用区。
- 当前 `Debug` 仍然会超出 `FLASH`，这是正常的尺寸差异，不影响你先把自动化链路跑通。

产物：

- [CommomSH367309_16series_030C8T6_C.elf](E:/TODO/030%20+%20309/artifacts/cmake/firmware-release/firmware/CommomSH367309_16series_030C8T6_C.elf)
- [CommomSH367309_16series_030C8T6_C.hex](E:/TODO/030%20+%20309/artifacts/cmake/firmware-release/CommomSH367309_16series_030C8T6_C.hex)
- [CommomSH367309_16series_030C8T6_C.bin](E:/TODO/030%20+%20309/artifacts/cmake/firmware-release/CommomSH367309_16series_030C8T6_C.bin)

## 现在怎么运行

### Windows

建议在仓库根目录执行：

```powershell
py -3.12 scripts/check_toolchain.py
task doctor
task sim-modbus
task sim-replay
task build
C:\Keil_v5\UV4\UV4.exe -b CommomSH367309_16series_030C8T6_C.uvprojx -j0
```

如果当前终端还没继承 `task` 的新路径，先重开终端；或者直接调用：

```powershell
"C:\Program Files\CMake\bin\cmake.exe" --preset host-sim-debug
"C:\Program Files\CMake\bin\cmake.exe" --build --preset build-host-sim-debug
artifacts\cmake\host-sim-debug\host_sim\modbus_parser_host_test.exe
```

### macOS

macOS 不走 `Keil`，走 `Python + CMake + GCC + OpenOCD/J-Link`：

```bash
python3 scripts/check_toolchain.py
python3 -m pip install uv
uv sync
cmake --preset host-sim-debug
cmake --build --preset build-host-sim-debug
./artifacts/cmake/host-sim-debug/host_sim/modbus_parser_host_test
```

未来固件 GCC 构建入口：

```bash
cmake --preset firmware-debug
cmake --build --preset build-firmware-debug
```

## VS Code 里怎么跑

### Tasks

路径：
- [tasks.json](E:/TODO/030%20+%20309/.vscode/tasks.json)

建议使用：

- `pc: doctor`
- `pc: new project`
- `pc: patch preview`
- `pc: analyze map`
- `pc: flash plan`
- `pc: debug plan`
- `pc: host sim (cmake)`
- `pc: host replay`
- `mcu: build gcc release`
- `mcu: build keil`
- `mcu: open keil build log`

### Debug

路径：
- [launch.json](E:/TODO/030%20+%20309/.vscode/launch.json)

当前可直接用的：

- `Python: Toolchain Doctor`
- `Python: New Project`
- `Python: Analyze Map`

连板后可用的：

- `Cortex-Debug: STM32F030 (J-Link)`

## 当前还没完全打通的部分

### 1. 真正连板调试

工具已经装好，但还需要：

- 板子
- 调试探针
- 目标连接参数确认

### 2. GCC 固件构建

当前状态：

- `Release` 已跑通
- `Debug` 仍然超出应用区大小

后续可以继续优化的点：

- 继续清理 `Debug` 体积
- 补齐旧头文件声明，减少兼容性警告
- 继续对齐 `CMake` 源文件清单和 `uvprojx`

## 建议你现在先熟悉的路径

1. 先跑 `task doctor`
2. 再跑 `task sim-modbus`
3. 再跑 `mcu: build keil`
4. 看 [README.md](E:/TODO/030%20+%20309/README.md)
5. 看 [CODEX_AUTOMATION_HANDBOOK_20260318.md](E:/TODO/030%20+%20309/docs/CODEX_AUTOMATION_HANDBOOK_20260318.md)
6. 再看这份状态文档

这样你会比较快理解“现在已经能跑什么、接下来还差什么”。
