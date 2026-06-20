# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概览

基于 STM32F030C8 (Cortex-M0) 的 BMS（电池管理系统）固件，使用 **SH367309** 前端模拟芯片（AFE），支持磷酸铁锂（LiFePO₄）和三元锂（Ternary Lithium）两种电芯化学体系。

## 构建与开发

### IDE / 工具链

- **Keil MDK-ARM** — 主开发环境，项目文件：`CommomSH367309_16series_030C8T6_C.uvprojx`
- 工具链路径：`C:/Keil_v5/UV4/UV4.exe`
- 构建命令：`"C:/Keil_v5/UV4/UV4.exe" -r "CommomSH367309_16series_030C8T6_C.uvprojx" -o "build_output.txt"`

### 关键构建配置宏（定义于 `Code/Source/conf/conf.h`）

| 宏 | 作用 |
|---|---|
| `TERNARYLI` / `LIFEPO` | 选择电芯化学体系 |
| `wdog_enable` | 开启独立看门狗 IWDG |
| `__FUNC__HEAT__` | 启用加热控制功能 |
| `LEVEL_CURR` | 电流等级（80A/100A/150A/200A/250A/DEFAULT） |
| `_COMMOM_UPPER_SCI1/2` | 选择串口通信协议类型 |

### 清理构建产物

```bash
./Clean.bat
```

## 代码架构

```
Code/
├── Drivers/              ← CMSIS + STM32F0xx 启动文件
├── Source/
│   ├── main.c/h          ← 入口、设备初始化、主循环
│   ├── conf/             ← conf.h（全局配置）+ conf_gpio.h（引脚宏定义）
│   ├── BSP/              ← 板级支持包（Time_Triggered 调度器等）
│   └── *.{c,h}           ← 核心功能模块
└── STM32F0xx_StdPeriph_Driver/  ← ST 标准外设库
```

### 调度器

时间触发协作式调度器（`Time_Triggered.c`），主循环 `SCH_Dispatch_Tasks()` 中运行 8 个注册任务。
定时器 TIM17 中断（500µs）触发 `SCH_Update()` 更新任务计数器。

### 故障保护体系（三级）

Fault.c 使用数据驱动引擎 `App_FaultCheck_Run()` + `s_faultDesc[26]` 描述符表统一处理 13 种故障 × 2 个软件级。

每个描述符包含：源值指针、阈值指针、计数/滤波时间指针、MDLCHGFAULT_REG 和 Fault_Flag 的 bit 位置、逻辑方向、虚电流门控类型、FaultFlag 枚举值。

## 通信协议

双协议自动检测：Modbus RTU（超时 20ms）或自定义 ASCII（`0x7E` SOI，超时 100ms）。
两个 RS-485 端口独立运行协议检测。

## 内存约束

- STM32F030C8: 64 kB Flash, 8 kB RAM
- IAP Bootloader 占用 5.25 kB Flash + 3.45 kB RAM
- **优化后** APP: ~47.7 kB Flash / ~6.8 kB RAM（79%）
- 当前故障保护引擎使用 44 kB Code + 3.5 kB RO-data
