# 当前仓库运行指南

## 1. 我已经实际跑过什么

### 当前 PC 上已实际执行

已执行并成功：

- `python scripts/check_toolchain.py`
- `python scripts/new_project.py --template firmware-stm32f0 --project-name demo-bms --device-code STM32F030C8 --protocol-variant modbus --customer-code common --output templates/generated/demo-bms`
- `python scripts/patch_project.py --project-root templates/firmware-stm32f0 --vars-file templates/vars.sample.json --check-only`
- `python scripts/analyze_map.py --map CommomSH367309_16series_030C8T6_C.map --top 20 --json artifacts/map-summary.json`
- `python scripts/debug_capture.py flash --probe jlink --artifact Objects/output.hex --output artifacts/flash-plan.md`
- `python scripts/debug_capture.py debug --probe openocd --artifact Objects/output.hex --output artifacts/debug-plan.md`

当前产物位置：

- [`artifacts/map-summary.json`](E:/TODO/030%20+%20309/artifacts/map-summary.json)
- [`artifacts/flash-plan.md`](E:/TODO/030%20+%20309/artifacts/flash-plan.md)
- [`artifacts/debug-plan.md`](E:/TODO/030%20+%20309/artifacts/debug-plan.md)
- [`templates/generated/demo-bms/project.json`](E:/TODO/030%20+%20309/templates/generated/demo-bms/project.json)

### 当前 MCU 工程上已实际执行

已执行并成功：

- `C:\Keil_v5\UV4\UV4.exe -b CommomSH367309_16series_030C8T6_C.uvprojx -j0`

本次构建结果：

- 0 errors
- 36 warnings
- Program Size:
  - Code = 50716
  - RO-data = 2876
  - RW-data = 1252
  - ZI-data = 6284
- Build Time Elapsed = 00:00:04

构建日志文件：

- [`Objects/CommomSH367309_16series_030C8T6_C.build_log.htm`](E:/TODO/030%20+%20309/Objects/CommomSH367309_16series_030C8T6_C.build_log.htm)

构建产物：

- [`Objects/CommomSH367309_16series_030C8T6_C.axf`](E:/TODO/030%20+%20309/Objects/CommomSH367309_16series_030C8T6_C.axf)
- [`Objects/CommomSH367309_16series_030C8T6_C.bin`](E:/TODO/030%20+%20309/Objects/CommomSH367309_16series_030C8T6_C.bin)

## 2. 当前这台机器缺什么

`python scripts/check_toolchain.py` 的结果显示当前机器缺：

- `uv`
- `cmake`
- `ninja`
- `arm-none-eabi-gcc`
- `JLinkExe`
- `openocd`

所以当前状态是：

- PC 脚本链路可以跑
- Keil 命令行构建可以跑
- GCC/CMake 固件构建不能跑
- 主机侧 `host_sim` 不能跑
- 真正 J-Link / OpenOCD 调试不能跑

## 3. 在 Windows 上怎么用

### 先做环境检查

```powershell
python scripts/check_toolchain.py
```

### 先跑当前可用的 PC 流程

```powershell
python scripts/new_project.py --template firmware-stm32f0 --project-name demo-bms --device-code STM32F030C8 --protocol-variant modbus --customer-code common --output templates/generated/demo-bms
python scripts/patch_project.py --project-root templates/firmware-stm32f0 --vars-file templates/vars.sample.json --check-only
python scripts/analyze_map.py --map CommomSH367309_16series_030C8T6_C.map --top 20 --json artifacts/map-summary.json
python scripts/debug_capture.py flash --probe jlink --artifact Objects/output.hex --output artifacts/flash-plan.md
python scripts/debug_capture.py debug --probe openocd --artifact Objects/output.hex --output artifacts/debug-plan.md
```

### 当前机器可用的 MCU 构建

```powershell
C:\Keil_v5\UV4\UV4.exe -b CommomSH367309_16series_030C8T6_C.uvprojx -j0
```

## 4. 在 macOS 上怎么用

macOS 上不能用当前的 `Keil UV4` 路线。

macOS 侧应走：

- `python3`
- `uv`
- `cmake`
- `ninja`
- `arm-none-eabi-gcc`
- `openocd` 或 J-Link

典型流程：

```bash
python3 scripts/check_toolchain.py
python3 -m pip install uv
uv sync
cmake --preset host-sim-debug
cmake --build --preset build-host-sim-debug
```

未来固件构建应走：

```bash
cmake --preset firmware-debug
cmake --build --preset build-firmware-debug
```

## 5. VS Code 里怎么运行

我已经新增：

- [`/.vscode/tasks.json`](E:/TODO/030%20+%20309/.vscode/tasks.json)
- [`/.vscode/launch.json`](E:/TODO/030%20+%20309/.vscode/launch.json)

### Tasks 面板

在 VS Code 中：

1. `Terminal` -> `Run Task`
2. 选择以下任务之一：

- `pc: doctor`
- `pc: new project`
- `pc: patch preview`
- `pc: analyze map`
- `pc: flash plan`
- `pc: debug plan`
- `pc: host sim (cmake)`
- `mcu: build keil`
- `mcu: open keil build log`

### Debug 面板

在 VS Code 中：

1. 打开 `Run and Debug`
2. 可选配置：

- `Python: Toolchain Doctor`
- `Python: New Project`
- `Python: Analyze Map`
- `Cortex-Debug: STM32F030 (J-Link)`

说明：

- 前三个当前机器就能用
- `Cortex-Debug: STM32F030 (J-Link)` 需要你安装 `Cortex-Debug` 扩展、J-Link 工具，并且有板子连接

## 6. 当前不能直接跑的部分

### PC 主机侧仿真

仓库已经有：

- [`host_sim/CMakeLists.txt`](E:/TODO/030%20+%20309/host_sim/CMakeLists.txt)

但当前机器没有 `cmake/ninja`，所以还不能执行。

### MCU 真正调试

当前机器没有：

- `JLinkExe`
- `openocd`

而且当前线程里也没有板子可连，所以我现在不能替你把硬件调试真正跑起来。

## 7. 你接下来怎么熟悉最合适

建议顺序：

1. 先在 VS Code 里跑 `pc: doctor`
2. 再跑 `pc: new project`
3. 再跑 `pc: analyze map`
4. 再跑 `mcu: build keil`
5. 打开 `mcu: open keil build log`
6. 熟悉这些以后，再安装 GCC/J-Link/OpenOCD，我继续把 PC 仿真和 MCU 调试打通
