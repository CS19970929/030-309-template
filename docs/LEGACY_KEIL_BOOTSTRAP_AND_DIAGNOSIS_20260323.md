# 旧项目一键迁移与启动诊断

这次把两类能力正式收敛成脚本：

- `scripts/bootstrap_legacy_keil.py`
- `scripts/diagnose_legacy_boot.py`

## 一键迁移

适用场景：

- 老项目主入口仍是 `Keil .uvprojx`
- 芯片仍是 `STM32F0/STM32F030` 一类 Cortex-M0 项目
- 需要保留 Keil，同时补齐 `GCC/CMake/Task/VS Code`

命令：

```powershell
task bootstrap-legacy LEGACY_ROOT=E:/TODO/某旧项目
```

等价直接运行：

```powershell
py -3.12 scripts/bootstrap_legacy_keil.py --legacy-root E:/TODO/某旧项目
```

脚本会自动完成：

- 解析老项目根目录下的 `uvprojx`
- 抽取 `OutputName / Device / Source / Include / Define`
- 生成根级 `CMakeLists.txt`
- 生成 `CMakePresets.json`
- 生成 `Taskfile.yml`
- 生成 `firmware/CMakeLists.txt`
- 生成 `firmware/generated/legacy_keil_manifest.cmake`
- 生成 `firmware/linker/legacy_stm32_app.ld`
- 生成 `firmware/startup/startup_stm32f0xx_gcc.S`
- 生成 `.vscode/tasks.json`
- 生成 `.vscode/launch.json`
- 生成 `.vscode/extensions.json`
- 复制必要辅助脚本和 `toolchains/arm-none-eabi-gcc.cmake`
- 生成 `docs/LEGACY_TOOLCHAIN_BOOTSTRAP.md`

默认地址参数：

- `APP_ORIGIN=0x08001C00`
- `RESERVED_FLASH_START=0x0800F000`
- `STACK_TOP=0x20001B80`

如果老项目地址不同，可以覆盖：

```powershell
py -3.12 scripts/bootstrap_legacy_keil.py ^
  --legacy-root E:/TODO/某旧项目 ^
  --app-origin 0x08002000 ^
  --reserved-flash-start 0x0800F800 ^
  --stack-top 0x20001C00
```

首次迁移后必须人工确认：

- `firmware/generated/legacy_keil_manifest.cmake` 的源文件和宏是否完整
- `firmware/linker/legacy_stm32_app.ld` 的 Flash/RAM 布局是否符合旧项目
- `system_stm32f0xx.c` 时钟配置是否与硬件一致
- 若项目带 bootloader，`main.c/Flash.c` 是否有额外启动约束

## 启动诊断

适用场景：

- `Keil bin` 能跑，`GCC bin` 不能跑
- 怀疑是 `MSP / Reset_Handler / HardFault / bin 尺寸 / ELF 段布局` 差异

命令：

```powershell
task diagnose-legacy ^
  KEIL_BIN=Objects/xxx.bin ^
  GCC_BIN=artifacts/cmake/firmware-release/xxx.bin ^
  GCC_ELF=artifacts/cmake/firmware-release/firmware/xxx.elf
```

等价直接运行：

```powershell
py -3.12 scripts/diagnose_legacy_boot.py ^
  --keil-bin Objects/xxx.bin ^
  --gcc-bin artifacts/cmake/firmware-release/xxx.bin ^
  --gcc-elf artifacts/cmake/firmware-release/firmware/xxx.elf
```

输出：

- `artifacts/legacy-boot-diagnosis.json`
- `artifacts/legacy-boot-diagnosis.md`

当前这套诊断会自动检查：

- 首向量 `MSP`
- `Reset_Handler`
- `NMI/HardFault` 向量
- 镜像尺寸
- 是否越过保留区
- `ELF PT_LOAD` 是否从应用起始地址开始

## 本次经验沉淀

在 `030 + TI` 这类带 bootloader 的 `STM32F030` 项目里，最需要优先排查的是：

- `ELF PT_LOAD` 是否错误地从 `0x08001000` 之类更早地址开始
- `bin` 是否越界踩到参数保留区
- 首栈值是否满足 bootloader 的 SRAM 合法性判断
- 应用是否在第一次中断发生前，已经完成 SRAM 向量表复制和 remap

后一条尤其关键，因为 `STM32F030` 没有 `VTOR`，如果 bootloader 跳入 app 后过早触发异常，PC 很可能会落回 bootloader 的 `HardFault`，从表面看像是“bootloader 没跳转”。
