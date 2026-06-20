# IAP 与 APP Map 文件分析 (2026-06-20)

基于以下两个 map 文件的分析报告：
- `030-iap/Users/Listings/IAP_030.map`（ARM Compiler 5.06 update 7）
- `030-309-template/CommomSH367309_16series_030C8T6_C.map`

---

## 一、总体大小对比

| 指标 | IAP 启动加载器 | APP 主固件 |
|---|---|---|
| **ROM 总计** | **5.25 kB** | **52.32 kB** |
| Code | 5,076 B | 50,280 B |
| RO Data（常量） | 224 B | 2,820 B |
| RW Data（已初始化变量） | 76 B | 1,252 B |
| **ZI Data（零初始化）** | **3,452 B** | **6,780 B** |
| **Debug Info** | 261,934 B | 635,172 B |
| **MCU Flash 占用率** | 5% | **82%** |
| **MCU RAM 占用率** | 43% | **98%** |

> STM32F030C8：Flash 64 kB，RAM 8 kB。
> APP Flash 剩余 ~12 kB，RAM 仅余 ~16 字节（8,192 - 8,032），RAM 极度紧张。

---

## 二、IAP 代码分布（共 5.25 kB ROM）

IAP 是全功能 bootloader：SCI 协议、Flash 擦写、EEPROM 读写、定时器时基。

### Object 大小排名

| 模块 | Code | RO Data | RW Data | ZI Data | 说明 |
|---|---|---|---|---|---|
| `sci.o` | 2,330 B | 0 B | 10 B | 2,428 B | IAP 升级协议（帧解析 + Flash 写入 + 命令分发） |
| `main.o` | 900 B | 0 B | 25 B | 0 B | 入口、IO/Timer/USART 初始化、跳转逻辑 |
| `stm32f0xx_rcc.o` | 448 B | 0 B | 16 B | 0 B | RCC 时钟配置 |
| `stm32f0xx_usart.o` | 286 B | 0 B | 0 B | 0 B | UART 驱动 |
| `system_stm32f0xx.o` | 260 B | 0 B | 20 B | 0 B | SystemInit + SetSysClock |
| `stm32f0xx_flash.o` | 242 B | 0 B | 0 B | 0 B | Flash 擦写 |
| `stm32f0xx_tim.o` | 182 B | 0 B | 0 B | 0 B | TIM17 定时器 |
| `stm32f0xx_gpio.o` | 156 B | 0 B | 0 B | 0 B | GPIO 初始化 |
| `stm32f0xx_misc.o` | 80 B | 0 B | 0 B | 0 B | NVIC 中断配置 |
| `startup_stm32f0xx.o` | 28 B | 192 B | 0 B | 1,024 B | 启动文件 + 栈空间 |
| `stm32f0xx_it.o` | 10 B | 0 B | 0 B | 0 B | 弱中断处理函数 |

### 被链接器移除的部分

```
Removing i2c.o(i.EEPROM_Read)                   224 bytes
Removing i2c.o(i.EEPROM_Write)                  260 bytes
Removing i2c.o(i.I2C_Configuration)             104 bytes
Removing i2c.o(i.I2C_EE_Init)                    20 bytes
Removing i2c.o(i.ReadEEPROM_Word_NoZone)         26 bytes
Removing i2c.o(i.WriteEEPROM_Word_NoZone)        92 bytes
Removing i2c.o(i.sEE_TIMEOUT_UserCallback)       16 bytes
Removing i2c.o(i.sEE_WaitEepromStandbyState)    108 bytes
Removing i2c.o(.data)                             8 bytes
Removing i2c.o(.data)                             1 bytes
Removing i2c.o(.data)                             2 bytes
Total I2C removed:                              ~861 bytes
```

**关键发现**：IAP 中编译了 I2C 驱动代码，但**主循环未调用任何 EEPROM 函数**，链接器自动移除了全部 861 B。如果在 IAP 中编译时去掉 `i2c.c` 源文件，可节省这部分空间并减少编译时间。

---

## 三、APP 代码分布（共 52.32 kB ROM，已用 Flash 的 82%）

### Top 12 模块（占总 Code 的 70%）

| Rank | 模块 | Code | RO Data | RW Data | ZI Data | 累计占比 | 说明 |
|---|---|---|---|---|---|---|---|
| 1 | `fault.o` | **6,682 B** | 0 B | 64 B | 370 B | 13% | 三级故障保护逻辑（13 类 × 3 级 × 5 字段） |
| 2 | `sci_upper.o` | **6,124 B** | 244 B | 8 B | 378 B | 25% | Modbus 寄存器命令路由（上百条命令 + 校验 + 响应） |
| 3 | `eeprom.o` | **3,442 B** | **1,172 B** | 52 B | 0 B | 32% | EEPROM 驱动 + 参数增量写 + 地址表 |
| 4 | `socenhance.o` | **2,938 B** | 252 B | 37 B | 244 B | 38% | SOC 增强算法（安时积分 + 电压修正 + 动态补偿） |
| 5 | `ascii_slave.o` | **2,348 B** | 0 B | 0 B | 0 B | 42% | ASCII 从站协议帧解析 + 响应组包 |
| 6 | `i2c_afe1.o` | **2,324 B** | 368 B | 0 B | 92 B | 47% | 软件 I2C 时序 + AFE SH367309 寄存器读写 |
| 7 | `sleepdeal.o` | **2,218 B** | 0 B | 91 B | 0 B | 51% | 休眠状态机（HICCUP 多种触发原因 + 唤醒） |
| 8 | `comm.o` | **1,890 B** | 0 B | 0 B | 2,054 B | 55% | RS-485 环形缓冲 + 双协议自动检测 |
| 9 | `datadeal.o` | **1,844 B** | 0 B | 30 B | 316 B | 59% | 数据采集 + 校准系数 + 串数映射 |
| 10 | `sh367309_func.o` | **1,376 B** | 0 B | 339 B | 12 B | 61% | AFE 驱动（MOS 控制 + 保护寄存器配置） |
| 11 | `iodrivers.o` | **1,372 B** | 0 B | 264 B | 80 B | 64% | IO 驱动层 |
| 12 | `bms_comm_data_adapter.o` | **1,388 B** | 0 B | 0 B | 0 B | 67% | BMS 数据适配器（PYLON 上行载荷） |

### 其余模块

| 模块 | Code | RO Data | RW Data | ZI Data | 说明 |
|---|---|---|---|---|---|
| `system_monitor.o` | 1,448 B | 0 B | 16 B | 24 B | 系统功能开关 + 错误回调 |
| `io_control.o` | 1,034 B | 0 B | 12 B | 0 B | MOS/Relay 控制 |
| `logrecord.o` | 1,172 B | 0 B | 9 B | 221 B | 日志记录 |
| `soc.o` | 340 B | 0 B | 0 B | 84 B | SOC 主接口 |
| `pubfunc.o` | 794 B | 0 B | 3 B | 0 B | CRC16/CRC8/查表工具 |
| `rtc.o` | 596 B | 0 B | 8 B | 52 B | RTC 时钟 |
| `chargerloadfunc.o` | 474 B | 0 B | 6 B | 0 B | 充电器负载检测 |
| `main.o` | 400 B | 256 B | 1 B | 0 B | 入口 + 主循环 |
| `sh367309_datadeal.o` | 860 B | 192 B | 200 B | 26 B | AFE 参数 EEPROM 读写 |
| `productionid.o` | 524 B | 0 B | 1 B | 112 B | 产品 ID 管理 |
| `cell_balance.o` | （未单独统计） | — | — | — | 电池均衡 |
| `flash.o` | 298 B | 0 B | 0 B | 192 B | Flash 读写 |
| `adc.o` | 560 B | 112 B | 13 B | 24 B | ADC 采样 |
| `shortfunc.o` | 220 B | 0 B | 0 B | 0 B | 短路保护 |
| `heat_cool.o` | 298 B | 0 B | 0 B | 48 B | 加热/散热 |
| `bsp.o` | 28 B | 0 B | 0 B | 0 B | 板级支持 |
| `bsp_timer.o` | 678 B | 0 B | 36 B | 120 B | BSP 定时器 |
| `time_triggered.o` | 348 B | 0 B | 0 B | 160 B | 调度器核心 |
| `modbus_rtu_parser.o` | 214 B | 0 B | 0 B | 0 B | RTU 帧解析 |
| `modbus_service.o` | 150 B | 0 B | 0 B | 0 B | Modbus 服务桥 |
| `conf.o` | 0 B | 0 B | 0 B | 112 B | 调试计数器（无代码） |

### Standard Peripheral 库占用

| 模块 | Code | 说明 |
|---|---|---|
| `stm32f0xx_adc.o` | 326 B | ADC 驱动 |
| `stm32f0xx_dma.o` | 474 B | DMA 驱动 |
| `stm32f0xx_exti.o` | 176 B | 外部中断 |
| `stm32f0xx_flash.o` | 312 B | Flash 读/写状态 |
| `stm32f0xx_gpio.o` | 232 B | GPIO 驱动 |
| `stm32f0xx_it.o` | 254 B | 中断向量（弱函数） |
| `stm32f0xx_iwdg.o` | 68 B | 独立看门狗 |
| `stm32f0xx_misc.o` | 148 B | NVIC + SysTick |
| `stm32f0xx_pwr.o` | 140 B | 电源管理 |
| `stm32f0xx_rcc.o` | 916 B | RCC 时钟配置 |
| `stm32f0xx_rtc.o` | 1,322 B | RTC 驱动 |
| `stm32f0xx_syscfg.o` | 92 B | SYSCFG |
| `stm32f0xx_tim.o` | 172 B | TIM 驱动 |
| `stm32f0xx_usart.o` | 420 B | USART 驱动 |

> 其他标准外设（CAN、SPI、I2S、CEC、COMP、CRC、CRS、DAC、DBGMCU、DMA、I2C、WWDG）被链接器完全移除。

---

## 四、IAP vs APP 关键差异

| 方面 | IAP（5.25 kB） | APP（52.32 kB） |
|---|---|---|
| **协议** | 简单帧 + CRC 校验 | Modbus RTU + ASCII + 上百寄存器映射 |
| **存储** | 仅内部 Flash 擦写 | 外部 EEPROM 分区 + 写标志位 + 延迟批量写入 |
| **外设** | UART + TIM + GPIO | UART×2 + 软件 I2C + ADC + RTC + IWDG |
| **状态机** | 简单升级超时监控 | 三级故障 + 休眠 HICCUP + 均衡 + 充放电 |
| **数学** | 无 | SOC 查表 + 安时积分 + 电流校准 + 铜损补偿 |
| **调度** | 定时器轮询 | 时间触发调度器（10 个任务） |
| **RAM 大头** | sci.o ZI=2,428 B（通信缓冲） | comm.o ZI=2,054 B + iodrivers.o ZI=264 B + logrecord.o ZI=221 B + socenhance.o ZI=244 B |

---

## 五、RAM 使用明细（APP）

| 段 | 大小 | 说明 |
|---|---|---|
| RW Data（已初始化） | 1,252 B | 全局变量初值 |
| ZI Data（零初始化） | 6,780 B | `.bss` 段 |
| 栈（STACK） | 2,048 B | startup 定义 |
| 堆（HEAP） | 512 B | (被链接器移除) |
| **总计** | **≈ 8,032 B** | 8 kB RAM 的 98% |

ZI Data 大头分布：
- `comm.o` — 2,054 B：两个 `CommPortContext` 的环形缓冲（260×2 = 520 B）+ 帧缓冲 + 协议解析状态
- `sci_upper.o` — 378 B：`RS485MSG` 结构体 + 响应缓冲
- `fault.o` — 370 B：故障记录数组（三级 × 10 条 × 2 组）
- `datadeal.o` — 316 B：校准系数 + 铜损补偿数组
- `iodrivers.o` — 264 B：IO 驱动状态
- `socenhance.o` — 244 B：SOC 增强算法中间变量
- `logrecord.o` — 221 B：日志 FIFO
- `flash.o` — 192 B：Flash 操作缓冲
- `time_triggered.o` — 160 B：任务表
- `bsp_timer.o` — 120 B：定时器状态
- `productionid.o` — 112 B：产品 ID 缓冲
- `conf.o` — 112 B：调试计数器

---

## 六、优化建议

### 高 ROI（风险低、收益大）

1. **`sci_upper.o`（6.1 kB）** — 寄存器命令枚举庞大。可用 `命令 → 处理函数` 跳转表线性遍历替代当前的多层 `if-else` / 枚举展开，预期节省 **1~2 kB**

2. **`fault.o`（6.7 kB）** — 三级保护代码结构高度重复（First/Second/Third 的判据完全相同，仅阈值不同）。可用查表 + 循环泛化，预期节省 **2~3 kB**

3. **`ascii_slave.o`（2.3 kB）** — 如果客户只用了 Modbus RTU，移除 ASCII 协议可直接释放 2.3 kB 空间 + 降低通信复杂度

### 中 ROI（需谨慎修改）

4. **`eeprom.o` 的 RO Data（1.2 kB）** — EEPROM 地址定义是大量的 `#define` 常量，可以使用 `const uint16_t[]` 数组 + 枚举偏移替代，减少个别常量的冗余

5. **`sleepdeal.o`（2.2 kB）** — 如果不可能进入 HICCUP 的某些子状态（如 `SLEEP_HICCUP_CBC`），可剪枝对应分支

6. **`comm.o` 的 ZI Data（2 kB）** — 两路独立的环形缓冲区各 260 B（`COMM_RX_RING_SIZE`），可评估是否单向缩减到 128 B

### 已操作项

7. **IAP `i2c.o`（861 B）** — 已被链接器自动移除。从编译中排除 `i2c.c` 可减少编译时间，对固件大小无额外影响

---

## 七、资源余量总结

| 资源 | 总量 | 已用 | 剩余 | 紧张程度 |
|---|---|---|---|---|
| Flash（APP） | 64 kB | 52.32 kB | **≈ 12 kB** | ⚠️ 中等 |
| RAM（APP） | 8 kB | 7.84 kB | **≈ 160 B** | 🔴 极度紧张 |
| Flash（IAP） | 64 kB | 5.25 kB | **≈ 59 kB** | ✅ 充裕 |
