# 当前分支工具链迁移与编译验证 2026-03-23

## 本次目标

将 `codex/toolchain-migration-blueprint` 分支中的 GCC/CMake/Task 工具链能力迁移到当前分支，并在保留当前业务代码改动的前提下完成可编译验证。

## 本次落地内容

- 引入根级 [CMakeLists.txt](/E:/TODO/030%20+%20309/CMakeLists.txt)
- 引入 [CMakePresets.json](/E:/TODO/030%20+%20309/CMakePresets.json)
- 引入 [Taskfile.yml](/E:/TODO/030%20+%20309/Taskfile.yml)
- 引入 `firmware/`、`toolchains/`、`scripts/` 等 GCC 构建骨架
- 使用 `bootstrap_legacy_keil.py` 按当前 `uvprojx` 重新生成 `firmware/generated/legacy_keil_manifest.cmake`
- 保留并复用当前分支已经修改过的业务源码，不回退现有功能改动

## 为适配当前分支额外做的修正

### 1. 以当前 `uvprojx` 为准生成源码清单

直接套用蓝图分支里的旧 `firmware/CMakeLists.txt` 会引用当前分支不存在的文件，例如：

- `Comm.c`
- `bms_comm_data_adapter.c`
- `modbus_rtu_parser.c`
- `modbus_service.c`

因此本次改为基于当前 [CommomSH367309_16series_030C8T6_C.uvprojx](/E:/TODO/030%20+%20309/CommomSH367309_16series_030C8T6_C.uvprojx) 自动生成 `legacy_keil_manifest.cmake`，确保 GCC 源码清单与当前工程一致。

### 2. 修复 `ascii_slave` 的底层外设接口残留

当前分支里的 [ascii_slave.h](/E:/TODO/030%20+%20309/Code/Source/ascii_slave.h) 和 [ascii_slave.c](/E:/TODO/030%20+%20309/Code/Source/ascii_slave.c) 存在两类迁移残留：

- 使用了不存在的 `UART_TypeDef`，当前项目实际类型为 `USART_TypeDef`
- 使用了 STM32 LL 风格 API，但当前工程底层是 StdPeriph

本次已改为：

- `USART_TypeDef`
- `GPIO_SetBits / GPIO_ResetBits`
- `USART_SendData / USART_GetFlagStatus`
- `Delay1ms`

并移除了当前工程中不存在且未实际使用的头文件依赖：

- `uart.h`
- `modbus_host.h`
- `CRC.h`
- `led.h`

### 3. 调整 GCC 默认栈占位

当前引导脚本与链接脚本已经固定：

- `_estack = 0x20001B80`
- 应用 RAM 区为 `0x200000C0 ~ 0x20001B7F`

若继续使用蓝图脚本默认 `APP_STACK_SIZE=0xC00`，会导致 `._user_heap_stack` 超出 RAM。

本次将 [firmware/CMakeLists.txt](/E:/TODO/030%20+%20309/firmware/CMakeLists.txt) 的默认 `APP_STACK_SIZE` 调整为 `0x0`，保证当前分支的 GCC Release 能成功链接。

## 已完成验证

### 1. 环境检查

已通过：

- `py -3.12 scripts/check_toolchain.py`

确认当前机器可用工具包括：

- `cmake`
- `ninja`
- `arm-none-eabi-gcc`
- `task`

### 2. GCC Release 构建

已通过：

```powershell
"C:/Program Files/CMake/bin/cmake.exe" --fresh --preset firmware-release
"C:/Program Files/CMake/bin/cmake.exe" --build --preset build-firmware-release
task build
```

产物：

- [CommomSH367309_16series_030C8T6_C.elf](/E:/TODO/030%20+%20309/artifacts/cmake/firmware-release/firmware/CommomSH367309_16series_030C8T6_C.elf)
- [CommomSH367309_16series_030C8T6_C.bin](/E:/TODO/030%20+%20309/artifacts/cmake/firmware-release/CommomSH367309_16series_030C8T6_C.bin)
- [CommomSH367309_16series_030C8T6_C.hex](/E:/TODO/030%20+%20309/artifacts/cmake/firmware-release/CommomSH367309_16series_030C8T6_C.hex)

最终 `size` 结果：

- `text = 33588`
- `data = 404`
- `bss = 4376`
- `dec = 38368`
- `hex = 95e0`

### 3. Keil/GCC 启动镜像对比

已执行：

```powershell
py -3.12 scripts/diagnose_legacy_boot.py --keil-bin Objects/CommomSH367309_16series_030C8T6_C.bin --gcc-bin artifacts/cmake/firmware-release/CommomSH367309_16series_030C8T6_C.bin --gcc-elf artifacts/cmake/firmware-release/firmware/CommomSH367309_16series_030C8T6_C.elf
```

关键结果：

- GCC 首个 `PT_LOAD` 从 `0x08001C00` 开始
- GCC `flash_end = 0x0800A0C8`
- Keil `flash_end = 0x0800D02C`
- 两者都没有越过 `0x0800F000` 保留区

报告文件：

- [legacy-boot-diagnosis.json](/E:/TODO/030%20+%20309/artifacts/legacy-boot-diagnosis.json)
- [legacy-boot-diagnosis.md](/E:/TODO/030%20+%20309/artifacts/legacy-boot-diagnosis.md)

## 当前仍存在的风险

- 当前只完成了编译与静态镜像诊断，未做真实上板下载运行验证
- GCC 镜像首栈值为 `0x20001B80`，与当前 Keil 产物 `0x20001968` 不一致，这属于启动行为差异点，后续建议做一次实机验证
- 当前构建仍有若干业务代码警告，例如：
  - `SH367309_DataDeal.c` 非 `void` 函数缺少返回值
  - `Sci_Upper.c` 公共函数前置声明位置不理想

这些警告当前不阻塞编译，但建议后续单独清理
