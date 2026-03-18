# 阶段 B 固件 CMake 构建骨架说明

本阶段目标是为当前 STM32F030 下位机工程补齐最小可用的 `CMake + arm-none-eabi-gcc` 构建入口，同时保留原有 Keil 工程不变。

## 本阶段新增内容

- [`firmware/CMakeLists.txt`](E:/TODO/030%20+%20309/firmware/CMakeLists.txt)
- [`firmware/linker/stm32f030c8_app.ld`](E:/TODO/030%20+%20309/firmware/linker/stm32f030c8_app.ld)
- [`firmware/startup/startup_stm32f0xx_gcc.S`](E:/TODO/030%20+%20309/firmware/startup/startup_stm32f0xx_gcc.S)
- [`toolchains/arm-none-eabi-gcc.cmake`](E:/TODO/030%20+%20309/toolchains/arm-none-eabi-gcc.cmake)

## 设计原则

- 不迁移现有业务源码目录。
- 不替换现有 `Keil` 工程，只新增一条 GCC 构建链。
- 复用当前 APP 起始地址 `0x08001C00` 和 RAM 布局 `0x200000C0 + 0x1F40`。
- 明确保留 `0x20000000 ~ 0x200000BF` 给 IAP 的 SRAM 向量表重映射区。

## 当前能力边界

- `Taskfile.yml` 里的 `build` 已切换为真实 `cmake --preset` 入口。
- 由于当前机器未安装 `cmake`、`ninja`、`arm-none-eabi-gcc`，本阶段无法做本机构建验真。
- 当前 CMake 源文件清单按现有 `uvprojx` 手工同步，后续可以再做自动提取或分模块整理。

## 后续建议

1. 在 Windows 和 macOS 上统一安装 `cmake`、`ninja`、`arm-none-eabi-gcc`。
2. 执行 `task build` 验证 GCC 首次构建。
3. 根据 GCC 编译报错补齐少量编译器兼容宏。
4. 把 `task flash` 接到 `JLinkExe` 或 `OpenOCD` 无交互脚本。
5. 把产物导出到 `artifacts/firmware/`，并加入版本信息和 map 摘要。
