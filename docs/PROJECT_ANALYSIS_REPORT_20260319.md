# BMS 项目完整分析报告

> 日期: 2026-03-19
> 目标: 梳理 APP、IAP、上位机整个项目架构

---

## 一、项目概述

### 1.1 项目背景

本项目为基于 STM32F030C8T6 的 BMS (Battery Management System) 解决方案，采用 SH367309 AFE 芯片进行电池监控。

### 1.2 技术栈

| 层级 | 技术 |
|------|------|
| 主控 | STM32F030C8T6 (Cortex-M0) |
| AFE | SH367309 |
| 固件开发 | Keil + GCC/CMake |
| 上位机 | C# WinForms (.NET Framework) |
| 构建自动化 | Taskfile + Python + Codex |

### 1.3 代码规模

```
Code/Source/        - 约 40+ 源文件
  ├── main.c        - APP 入口
  ├── iap/main.c    - IAP 入口
  ├── *.c           - 功能模块
Code/STM32F0xx_*    - ST 标准库
BMS upper/          - C# 上位机
host_sim/           - PC 仿真代码
```

---

## 二、系统架构

### 2.1 整体架构图

```
┌─────────────────────────────────────────────────────────────────────┐
│                        BMS 系统架构                                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────────┐                                              │
│  │  上位机 (C#)      │  CommomUpper_32Series                       │
│  │  - 参数配置       │  - Form1.cs (主界面)                         │
│  │  - 固件升级       │  - SerialPort.cs (串口通信)                   │
│  │  - 实时监控       │  - AFE_Parametes.cs (参数管理)               │
│  └────────┬─────────┘                                              │
│           │ RS485 (ASCII/Modbus)                                   │
│           ▼                                                          │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                     APP 应用层 (16KB Flash)                 │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │   │
│  │  │ 任务调度     │  │ 通信协议    │  │ 电池管理           │  │   │
│  │  │ SCH_Dispatch│  │ ASCII/Modbus│  │ SOC/Balance/Heat   │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────────────┘  │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │   │
│  │  │ 保护策略     │  │ 数据采集    │  │ 存储/日志           │  │   │
│  │  │ Fault.c     │  │ SH367309    │  │ EEPROM/Flash       │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────────────┘  │   │
│  └──────────────────────────────────────────────────────────────┘   │
│           ▲                                                          │
│           │ 升级跳转                                                 │
│           ▼                                                          │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                  IAP 引导程序 (16KB Flash)                  │   │
│  │  - 固件接收                                                    │   │
│  │  - Flash 写入                                                  │   │
│  │  - APP 跳转                                                    │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                     硬件层                                   │   │
│  │  SH367309 (AFE)  │  MOSFET  │  继电器  │  传感器            │   │
│  └──────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 Flash 布局

```
Flash 空间分配 (64KB):
┌────────────────┬────────────────┬────────────────┬────────────────┐
│ 0x08000000    │ 0x08004000    │ 0x08008000    │ 0x0800C000    │
├────────────────┼────────────────┼────────────────┼────────────────┤
│ IAP (16KB)     │ APP (16KB)     │ APP 备份(16KB) │ CONFIG (16KB) │
│ 0x0000-0x3FFF │ 0x4000-0x7FFF │ 0x8000-0xBFFF │ 0xC000-0xFFF  │
└────────────────┴────────────────┴────────────────┴────────────────┘
```

---

## 三、模块详解

### 3.1 IAP 引导程序

#### 文件结构

```
Code/Source/iap/
├── main.c     - 主入口
├── Sci.c      - 串口通信
└── I2C.c      - I2C 通信
```

#### 工作流程

```c
// iap/main.c:44-67
int main(void)
{
    SystemInit();
    if (FlashReadOneHalfWord(FLASH_ADDR_UPDATE_FLAG) == FLASH_TO_APP_VALUE)
    {
        IAP_To_APP_Jump();  // 跳转到 APP
    }
    else
    {
        // 进入 IAP 循环
        while (1)
        {
            App_SysTime();
            App_UpgrateFaultMonitor();
            App_FlashUpgrate();     // 接收固件
            App_UpdateFinishChk(); // 升级完成检查
        }
    }
}
```

#### 关键功能

| 功能 | 说明 |
|------|------|
| 升级标志检测 | `FLASH_ADDR_UPDATE_FLAG` (0x08001C00) |
| 固件接收 | 通过串口接收 bin 文件 |
| 超时监控 | 10ms 无数据则超时复位 |
| APP 跳转 | 设置 MSP 并跳转 |

#### 问题分析

1. **升级标志未自动清除** - 升级完成后需 APP 手动清除
2. **与 APP 代码耦合** - 共用 `InitIO()` 等函数但未分离编译
3. **缺少 Flash 错误处理** - 写入失败无重试机制

---

### 3.2 APP 应用层

#### 任务调度

```c
// main.c:47-54
static const AppTaskConfig g_app_core_tasks[] = {
    {App_AFEGet,             0,   200},  // 200ms - AFE 数据采集
    {App_WarnCtrl,           8,    10},  // 10ms  - 保护检测
    {App_AnlogCal,           2,    10},  // 10ms  - 模拟校准
    {App_SOC,                5,   100},  // 100ms - SOC 计算
    {App_LogRecord,          6,  1000},  // 1s    - 日志记录
    {AppRuntimeMonitor_RunTask, 11, 1000}, // 1s - 运行时监控
};
```

#### 模块清单

| 模块 | 文件 | 职责 |
|------|------|------|
| 任务调度 | `main.c` | 周期任务分发 |
| 通信协议 | `Comm.c`, `Sci_Upper.c` | ASCII/Modbus 帧处理 |
| 保护策略 | `Fault.c` | 三级故障检测 (Second/Third) |
| SOC 计算 | `SocEnhance.c` | 安时积分 + OCV 校正 |
| AFE 驱动 | `SH367309_Func.c`, `SH367309_DataDeal.c` | 芯片通信 |
| 数据处理 | `DataDeal.c` | 电流/电压滤波 |
| 存储管理 | `EEPROM.c` | 参数持久化 |
| 均衡控制 | `Cell_balance.c` | 被动均衡 |
| 热管理 | `Heat_Cool.c` | 加热/制冷 |
| 充电检测 | `ChargerLoadFunc.c` | 充电器/负载检测 |
| 休眠管理 | `SleepDeal.c` | 低功耗控制 |

#### 核心流程

```
上电初始化
  ├── InitDevice()         → 硬件初始化
  │   ├── App_InitPlatform()
  │   │   ├── SystemInit()
  │   │   ├── Init_IAPAPP()
  │   │   ├── bsp_Init()
  │   │   ├── InitE2PROM()
  │   │   ├── InitAFE1()
  │   │   └── ...
  │   └── App_InitRuntimeState()
  │       └── Comm_InitAll()
  │
主循环 (while 1)
  ├── SCH_Dispatch_Tasks()     → 后台任务
  │   └── 按周期调用各个任务
  │
  └── App_RunForegroundServices()  → 前台服务
      ├── Comm_PollAll()         → 通信轮询
      ├── App_E2promDeal()       → EEPROM 写入
      ├── App_FlashUpdateDet()   → 升级检测
      └── Feed_IWatchDog()      → 喂狗
```

---

### 3.3 通信协议

#### 协议对比

| 特性 | ASCII | Modbus RTU |
|------|-------|------------|
| 格式 | 文本 `:` 开头 | 二进制 |
| 校验 | LRC | CRC-16 |
| 分隔符 | `\r\n` | 无 |
| 效率 | 低 | 高 |

#### 寄存器映射

| 地址范围 | 功能 |
|----------|------|
| 0x00-0xFF | 保留 |
| 0x100-0x1FF | 系统参数 (电压/电流/SOC) |
| 0x200-0x2FF | 保护参数 (OVP/UVP/OCP/OTP) |
| 0x300-0x3FF | 校准参数 (K/B 系数) |

---

### 3.4 保护策略

#### 三级保护机制

```
┌────────────┬────────────┬────────────┐
│   级别     │   动作     │   恢复     │
├────────────┼────────────┼────────────┤
│  First     │   告警     │   自动     │
│  Second    │   降功率   │   自动     │
│  Third     │   关 FET   │   手动     │
└────────────┴────────────┴────────────┘
```

#### 保护项

| 类别 | 保护项 | 检测阈值 |
|------|--------|----------|
| 电压 | Cell OVP/UVP | EEPROM 配置 |
| 电压 | Bat OVP/UVP | EEPROM 配置 |
| 电流 | Ichg OCP / Idsg OCP | EEPROM 配置 |
| 温度 | Chg/Dsg OTP/UTP | EEPROM 配置 |
| 温度 | MOS OTP | EEPROM 配置 |
| SOC | 低电量告警 | EEPROM 配置 |
| 压差 | Vdelta 过大 | EEPROM 配置 |

---

### 3.5 SOC 算法

#### 实现方案

- **安时积分法**: `Q = ∫I dt`
- **OCV 校正**: 开路电压查表
- **端点校正**: 充满/放空时修正

#### 电池类型支持

```c
// SocEnhance.c:78-213
const UINT16 SOC_Table_LiFePO[]      // 磷酸铁锂
const UINT16 SocTable_TernaryLi[]    // 三元锂
const UINT16 SocTable_LiFePO2[]     // 磷酸铁锂 (类型2)
```

---

### 3.6 上位机

#### 技术栈

- .NET Framework (WinForms)
- 串口通信 (SerialPort)
- CSV 导出

#### 核心文件

| 文件 | 功能 |
|------|------|
| `Form1.cs` | 主界面 |
| `SerialPort.cs` | 串口封装 |
| `AFE_Parametes.cs` | 参数读写 |
| `BatchProcess.cs` | 批量处理 |

#### 问题分析

1. UI 与业务逻辑耦合
2. 无 MVVM 模式
3. 缺少数据模型层

---

## 四、代码问题汇总

### 4.1 高优先级

| 位置 | 问题 | 影响 |
|------|------|------|
| IAP | 与 APP 代码未分离编译 | 升级风险 |
| IAP | 升级标志未自动清除 | 反复升级 |
| Flash | 无双备份机制 | 升级失败变砖 |
| 整体 | 无统一错误处理框架 | 异常难定位 |

### 4.2 中优先级

| 位置 | 问题 | 影响 |
|------|------|------|
| `Fault.c` | 硬编码时间参数 | 需改代码调参 |
| `SocEnhance.c` | 单位注释混乱 | 维护困难 |
| `main.c` | 任务静态注册 | 扩展性差 |
| 上位机 | UI/业务未分离 | 维护困难 |

### 4.3 低优先级

| 位置 | 问题 | 影响 |
|------|------|------|
| `Fault.c` | 重复代码 | 可维护性 |
| `CorrectionTerminal_CC` | 空实现 | 代码冗余 |
| `EEPROM.c` | 写入频繁 | EEPROM 寿命 |

---

## 五、改进建议

### 5.1 IAP 独立编译

```
Code/
├── App/         # APP 代码
│   ├── main.c
│   └── ...
├── IAP/         # IAP 代码 (独立工程)
│   ├── main.c
│   └── ...
└── Common/      # 公共代码
    ├── comm.c
    └── ...
```

### 5.2 Flash 双分区

```
┌────────────────┬────────────────┬────────────────┐
│ IAP (16KB)     │ APP A (24KB)   │ APP B (24KB)  │
│ 0x08000000    │ 0x08004000    │ 0x0800A000    │
└────────────────┴────────────────┴────────────────┘
```

### 5.3 通信协议抽象

```c
typedef struct {
    uint8_t (*parse)(uint8_t *in, uint16_t len, uint8_t *out);
    uint8_t (*build)(uint8_t cmd, void *data, uint8_t *out);
} ProtocolOps;
```

### 5.4 保护策略抽象

```c
typedef struct {
    const char *name;
    uint16_t threshold;
    uint16_t filter_ms;
    uint8_t level;
    void (*action)(uint8_t level);
} ProtectionRule;
```

---

## 六、文件索引

### 6.1 固件文件

```
Code/Source/
├── main.c                    # APP 入口
├── iap/main.c               # IAP 入口
├── Comm.c                   # 通信框架
├── Sci_Upper.c              # 协议处理
├── modbus_*.c               # Modbus 实现
├── ascii_slave.c            # ASCII 从机
├── Fault.c                  # 保护策略
├── SocEnhance.c             # SOC 算法
├── SH367309_*.c             # AFE 驱动
├── DataDeal.c               # 数据处理
├── EEPROM.c                 # 存储
├── Cell_balance.c           # 均衡
├── Heat_Cool.c              # 热管理
├── SleepDeal.c              # 休眠
└── LogRecord.c              # 日志
```

### 6.2 仿真文件

```
host_sim/
├── tools/
│   ├── modbus_parser_host_test.c
│   ├── modbus_parser_host_replay.c
│   ├── scheduler_host_replay.c
│   └── protection_host_replay.c
├── support/
│   ├── protection_sim.c
│   └── app_sim_snapshot.c
└── scenarios/
    ├── modbus_frames.txt
    └── protection/
        ├── protection_basic.csv
        └── ...
```

### 6.3 自动化脚本

```
scripts/
├── check_toolchain.py       # 工具链检查
├── new_project.py           # 项目生成
├── patch_project.py         # 占位符替换
├── analyze_map.py           # Map 分析
├── debug_capture.py        # 调试/烧录命令
└── flash_stlink.py         # ST-Link 烧录
```

---

## 七、总结

### 7.1 系统完整性

| 模块 | 状态 | 备注 |
|------|------|------|
| IAP | ⚠️ 需改进 | 建议独立编译 |
| APP | ✅ 基本完整 | 需重构提升可维护性 |
| 通信协议 | ✅ 功能完整 | 框架可抽象 |
| 保护策略 | ⚠️ 需配置化 | 硬编码过多 |
| SOC | ⚠️ 需整理 | 单位注释混乱 |
| 上位机 | ⚠️ 需重构 | 无 MVVM |

### 7.2 建议优先级

| 优先级 | 任务 | 收益 |
|--------|------|------|
| P0 | IAP 独立编译 | 升级安全 |
| P0 | Flash 双分区 | 防变砖 |
| P1 | 保护参数配置化 | 调参便捷 |
| P1 | 统一协议框架 | 代码复用 |
| P2 | SOC 单位整理 | 可维护性 |
| P2 | 任务调度增强 | 实时性 |
| P3 | 上位机重构 | 长期维护 |
