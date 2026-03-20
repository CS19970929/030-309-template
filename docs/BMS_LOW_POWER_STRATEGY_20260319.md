# BMS 低功耗策略设计文档

> 日期: 2026-03-19
> 目标: 调研主流低功耗策略，梳理当前项目，给出改进建议

---

## 一、BMS 主流低功耗策略调研

### 1.1 低功耗分层架构

```
┌─────────────────────────────────────────────────────────────────────┐
│                      BMS 低功耗分层架构                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   应用层策略                                                       │
│   ┌─────────────────────────────────────────────────────────────┐   │
│   │  ┌───────────┐  ┌───────────┐  ┌───────────┐  ┌───────────┐│   │
│   │  │ 主动监控  │  │ 被动保护  │  │ 通信就绪  │  │ 均衡等待  ││   │
│   │  │ 工作模式  │  │ 深度休眠  │  │ 浅层休眠  │  │ 定时唤醒  ││   │
│   │  └───────────┘  └───────────┘  └───────────┘  └───────────┘│   │
│   └─────────────────────────────────────────────────────────────┘   │
│                            ▼                                         │
│   MCU 硬件模式                                                     │
│   ┌─────────────────────────────────────────────────────────────┐   │
│   │  ┌───────────┐  ┌───────────┐  ┌───────────┐               │   │
│   │  │ Sleep     │  │ Stop      │  │ Standby   │               │   │
│   │  │ 2-10mA    │  │ 10-50μA   │  │ 1-5μA     │               │   │
│   │  └───────────┘  └───────────┘  └───────────┘               │   │
│   └─────────────────────────────────────────────────────────────┘   │
│                            ▲                                         │
│   AFE 硬件模式                                                     │
│   ┌─────────────────────────────────────────────────────────────┐   │
│   │  ┌───────────┐  ┌───────────┐  ┌───────────┐  ┌───────────┐│   │
│   │  │ Normal    │  │ IDLE      │  │ Sleep     │  │ SHIP      ││   │
│   │  │ 1-2mA     │  │ 200-500μA │  │ 50-100μA  │  │ 2-5μA     ││   │
│   │  └───────────┘  └───────────┘  └───────────┘  └───────────┘│   │
│   └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.2 MCU 低功耗模式对比

| 模式 | 功耗 (μA) | 保留状态 | 唤醒源 | 唤醒时间 |
|------|-----------|----------|--------|----------|
| **Sleep** | 1000-5000 | 全部 | 任意中断 | <1μs |
| **Stop** | 10-100 | SRAM + 寄存器 | EXTI | 5-10μs |
| **Standby** | 1-5 | 备份域 | WKUP/RTC | 2-5ms |
| **Shutdown** | <1 | 无 | WKUP/RTC | >10ms |

### 1.3 AFE 低功耗模式对比 (以 SH367309 为例)

| 模式 | 功能 | 功耗 (μA) | 通信 | 典型场景 |
|------|------|-----------|------|----------|
| **Normal** | 全功能 | 1000-2000 | I2C | 主动监控 |
| **IDLE** | 数据保持 | 200-500 | I2C | 短暂休息 |
| **Sleep** | 深度休眠 | 50-100 | 唤醒触发 | 长期待机 |
| **SHIP** | 运输模式 | 2-5 | 需重上电 | 出厂运输 |

### 1.4 主流 BMS 低功耗策略

| 策略 | 触发条件 | MCU模式 | AFE模式 | 功耗目标 |
|------|----------|---------|---------|----------|
| **主动监控** | 正常运行 | Run | Normal | 5-15mA |
| **浅层休眠** | 30分钟无通信 | Sleep | IDLE | 0.5-2mA |
| **标准休眠** | 2小时无通信 | Stop | Sleep | 50-200μA |
| **深度休眠** | 24小时无通信 | Standby | Sleep | 5-50μA |
| **运输模式** | 出厂设置 | Standby | SHIP | <5μA |
| **异常休眠** | 过流/过压保护 | Standby | SHIP | <5μA |

---

## 二、当前项目低功耗实现梳理

### 2.1 现有休眠模式

| 模式 | 代码定义 | MCU方式 | AFE方式 |
|------|----------|---------|---------|
| HICCUP_MODE | 打嗝模式 | Stop | Sleep |
| NORMAL_MODE | 普通模式 | Stop | Sleep |
| DEEP_MODE | 深度模式 | Standby? | Sleep |

### 2.2 现有触发条件

| 触发来源 | 代码位置 | 级别 |
|----------|----------|------|
| 测试休眠 | `Sleep_Mode.bits.b1TestSleep` | HICCUP |
| 过流休眠 | `Sleep_Mode.bits.b1OverCurSleep` | DEEP |
| 压差过大 | `Sleep_Mode.bits.b1OverVdeltaSleep` | DEEP |
| CBC 休眠 | `Sleep_Mode.bits.b1CBCSleep` | DEEP |
| 强制休眠 L1 | `Sleep_Mode.bits.b1ForceToSleep_L1` | HICCUP |
| 强制休眠 L2 | `Sleep_Mode.bits.b1ForceToSleep_L2` | NORMAL |
| 强制休眠 L3 | `Sleep_Mode.bits.b1ForceToSleep_L3` | DEEP |
| 电池过压 | `Sleep_Mode.bits.b1VcellOVP` | DEEP |
| 电池欠压 | `Sleep_Mode.bits.b1VcellUVP` | DEEP |
| 正常休眠 L2 | `Sleep_Mode.bits.b1NormalSleep_L2` | NORMAL |
| 正常休眠 L3 | `Sleep_Mode.bits.b1NormalSleep_L3` | DEEP |

### 2.3 现有唤醒源

| 唤醒源 | 引脚 | 触发方式 |
|--------|------|----------|
| 485 通信 | PB8 | EXTI Rising |
| 按键 | PB9 | EXTI Falling |
| RTC 闹钟 | 内部 | RTC Alarm |
| AFE 中断 | PC13 | EXTI Falling |

### 2.4 现有问题分析

| 问题 | 描述 | 影响 |
|------|------|------|
| **分层不清** | 休眠模式命名混乱 (HICCUP/NORMAL/DEEP) | 难以维护 |
| **阈值硬编码** | 唤醒延迟、休眠等待时间硬编码 | 灵活性差 |
| **缺少渐进策略** | 无状态机渐进休眠 | 功耗不优 |
| **外设管理缺失** | 进入休眠前未关闭非必要外设 | 功耗偏高 |
| **GPIO 状态管理** | 休眠时 GPIO 全部配置为模拟输入 | 可能导致问题 |
| **缺少通信超时** | 无通信超时自动休眠 | 功耗偏高 |
| **无动态唤醒间隔** | RTC 唤醒间隔固定 | 功耗/响应矛盾 |

---

## 三、低功耗改进方案

### 3.1 状态机架构

```
                    ┌─────────────┐
           ┌───────│  主动监控    │◄────── 充放电/通信
           │       │  (Run)      │
           │       └──────┬──────┘
           │              │ 30分钟无通信
           │              ▼
           │       ┌─────────────┐
           │       │  浅层休眠    │◄────── 485/按键
           │       │  (Sleep)    │
           │       └──────┬──────┘
           │              │ 2小时无通信
           │              ▼
           │       ┌─────────────┐
           │       │  标准休眠    │◄────── 按键/RTC
           │       │  (Stop)     │
           │       └──────┬──────┘
           │              │ 24小时无通信
           │              ▼
           │       ┌─────────────┐
           │       │  深度休眠    │◄────── 按键
           │       │  (Standby)  │
           │       └─────────────┘
           │
           │  异常触发
           └──────────────────────────►  运输模式 (SHIP)
```

### 3.2 休眠策略配置表

```c
typedef struct {
    const char *name;
    uint16_t idle_timeout_s;    // 无操作超时 (秒)
    uint8_t mcu_mode;           // MCU 模式: 0=Sleep, 1=Stop, 2=Standby
    uint8_t afe_mode;           // AFE 模式: 0=IDLE, 1=Sleep, 2=SHIP
    uint32_t wakeup_sources;    // 唤醒源位掩码
    uint16_t rtc_period_s;      // RTC 唤醒周期 (秒)
    uint16_t target_current_uA; // 目标电流 (μA)
} LowPowerModeConfig;

static const LowPowerModeConfig g_low_power_modes[] = {
    // 名称         超时   MCU    AFE    唤醒源              RTC     目标电流
    {"Active",     0,     0,     0,     0xFFFFFFFF,         0,      10000},  // 主动模式
    {"Light",      1800,  0,     0,     EXTI|RTC|USART,     60,     1000},   // 浅层休眠
    {"Standard",   7200,  1,     1,     EXTI|RTC,           300,    200},    // 标准休眠
    {"Deep",       86400, 2,     1,     EXTI|RTC,           3600,   50},     // 深度休眠
    {"Ship",       0,     2,     2,     EXTI,               0,      5},      // 运输模式
    {"Protect",    0,     2,     2,     EXTI,               3600,   5},      // 异常休眠
};
```

### 3.3 唤醒源管理

```c
typedef struct {
    uint8_t enabled;
    uint16_t port;
    uint16_t pin;
    uint8_t trigger;   // 0=falling, 1=rising, 2=both
    uint8_t priority;  // 唤醒优先级
} WakeupSourceConfig;

static const WakeupSourceConfig g_wakeup_sources[] = {
    // 启用  端口    引脚    触发      优先级
    {1,     PORT_485, PIN_485,  EXTI_RISING,  0},  // 485 通信
    {1,     PORT_KEY, PIN_KEY,  EXTI_FALLING, 1},  // 按键
    {1,     PORT_RTC, PIN_NONE, RTC_ALARM,    2},  // RTC 定时
    {1,     PORT_AFE, PIN_AFE,  EXTI_FALLING, 0},  // AFE 中断
};
```

### 3.4 外设低功耗管理

```c
typedef struct {
    void (*save)(void);     // 进入休眠前保存状态
    void (*suspend)(void);  // 进入休眠时挂起
    void (*restore)(void);  // 唤醒后恢复
    uint8_t power;          // 功耗等级
} PeripPowerOps;

static const PeripPowerOps g_perip_power[] = {
    // save    suspend    restore   功耗
    {USART_Save, USART_Suspend, USART_Restore, 200},  // 串口
    {I2C_Save,   I2C_Suspend,   I2C_Restore,   50},   // I2C
    {ADC_Save,   ADC_Suspend,   ADC_Restore,   100},  // ADC
    {TIMER_Save, TIMER_Suspend, TIMER_Restore, 80},   // 定时器
};
```

### 3.5 休眠流程

```c
void LowPower_EnterSleep(uint8_t mode_index)
{
    const LowPowerModeConfig *mode = &g_low_power_modes[mode_index];
    
    // 1. 通知应用层保存状态
    LowPower_NotifyEnterSleep();
    
    // 2. 保存上下文到 EEPROM/Flash
    LowPower_SaveContext();
    
    // 3. 配置唤醒源
    LowPower_ConfigureWakeup(mode->wakeup_sources);
    
    // 4. 配置 RTC 唤醒周期
    if (mode->rtc_period_s > 0) {
        RTC_ConfigureAlarm(mode->rtc_period_s);
    }
    
    // 5. 挂起外设
    for (int i = 0; i < ARRAY_SIZE(g_perip_power); i++) {
        g_perip_power[i].suspend();
    }
    
    // 6. 配置 AFE
    switch (mode->afe_mode) {
        case 0: AFE_EnterIDLE();   break;
        case 1: AFE_EnterSleep();  break;
        case 2: AFE_EnterSHIP();   break;
    }
    
    // 7. 配置 GPIO
    LowPower_ConfigureGpios(mode_index);
    
    // 8. 进入 MCU 低功耗模式
    switch (mode->mcu_mode) {
        case 0: Sys_SleepMode();    break;
        case 1: Sys_StopMode();     break;
        case 2: Sys_StandbyMode();  break;
    }
    
    // 9. 唤醒后恢复 (以下代码在唤醒后执行)
    LowPower_RestoreAfterWakeup();
}
```

### 3.6 GPIO 休眠配置

```c
void LowPower_ConfigureGpios(uint8_t mode_index)
{
    GPIO_InitTypeDef gpio;
    
    // 1. 将所有非唤醒源 GPIO 配置为模拟输入 (最低功耗)
    // PA: 全部模拟输入
    GPIOA->MODER = 0xFFFFFFFF;  // 模拟模式
    GPIOA->PUPDR = 0x00000000;  // 无上下拉
    
    // PB: 全部模拟输入 (除唤醒源)
    GPIOB->MODER = 0xFFFFFFFF;
    GPIOB->PUPDR = 0x00000000;
    
    // 2. 配置唤醒源引脚为中断模式
    // 485 引脚
    gpio.GPIO_Pin   = PIN_485_WKUP;
    gpio.GPIO_Mode  = GPIO_Mode_IN;
    gpio.GPIO_PuPd  = GPIO_PuPd_UP;
    GPIO_Init(GPIO_485, &gpio);
    
    // 3. 关闭 LED 等输出
    GPIO_ResetBits(PIN_LED);
    
    // 4. 关闭 AFE 电源 (如可控)
    // GPIO_ResetBits(PIN_AFE_PWR);
}
```

### 3.7 通信超时自动休眠

```c
static uint32_t g_last_comm_timestamp = 0;
static uint32_t g_idle_timeout_ms = 30 * 60 * 1000;  // 30 分钟

void LowPower_CommTimeoutCheck(void)
{
    if (g_last_comm_timestamp == 0) {
        return;
    }
    
    uint32_t idle_ms = HAL_GetTick() - g_last_comm_timestamp;
    
    if (idle_ms >= g_idle_timeout_ms) {
        LowPower_EnterSleep(LOW_POWER_MODE_LIGHT);
    }
}

// 通信回调中重置计时器
void LowPower_OnCommActivity(void)
{
    g_last_comm_timestamp = HAL_GetTick();
}
```

### 3.8 动态 RTC 唤醒间隔

```c
// 根据电池状态动态调整唤醒间隔
uint16_t LowPower_GetDynamicRtcInterval(void)
{
    uint8_t soc = g_stCellInfoReport.SocElement.u16Soc;
    uint8_t has_fault = (Fault_Flag_Third.all != 0);
    
    if (has_fault) {
        return 60;    // 有故障: 1分钟唤醒一次
    }
    if (soc < 10) {
        return 300;   // 低电量: 5分钟唤醒一次
    }
    if (soc < 30) {
        return 1800;  // 较低电量: 30分钟唤醒一次
    }
    return 3600;      // 正常: 1小时唤醒一次
}
```

---

## 四、实现计划

### 4.1 文件结构

```
Code/Source/
├── LowPower/
│   ├── low_power_manager.c    # 低功耗管理器
│   ├── low_power_config.h     # 配置
│   ├── wakeup_manager.c       # 唤醒源管理
│   └── perip_power.c          # 外设功耗管理
├── SleepDeal.c                # 现有休眠代码 (重构)
```

### 4.2 代码清单

| 文件 | 职责 |
|------|------|
| `low_power_manager.c` | 状态机、进入/退出休眠 |
| `low_power_config.h` | 模式配置表、阈值配置 |
| `wakeup_manager.c` | 唤醒源配置、中断处理 |
| `perip_power.c` | 外设挂起/恢复 |
| `SleepDeal.c` | 触发条件判断 (重构) |

### 4.3 优先级

| 优先级 | 任务 | 预期功耗 |
|--------|------|----------|
| P0 | 状态机架构 | - |
| P1 | GPIO 休眠管理 | -20% |
| P1 | 外设时钟关闭 | -10% |
| P2 | 通信超时休眠 | -50% |
| P2 | 动态 RTC 间隔 | -30% |
| P3 | 运输模式 | 新增功能 |

---

## 五、功耗估算

| 场景 | 当前 | 改进后 | 节省 |
|------|------|--------|------|
| 正常监控 | 15mA | 10mA | 33% |
| 浅层休眠 | 2mA | 0.5mA | 75% |
| 标准休眠 | 500μA | 200μA | 60% |
| 深度休眠 | 100μA | 50μA | 50% |
| 运输模式 | N/A | 5μA | 新增 |

---

## 六、测试计划

### 6.1 功耗测试

| 模式 | 测试方法 | 目标 |
|------|----------|------|
| Run | 万用表 | <15mA |
| Sleep | 高精度表 | <1mA |
| Stop | μA 表 | <200μA |
| Standby | μA 表 | <50μA |

### 6.2 唤醒测试

| 唤醒源 | 测试方法 | 目标 |
|--------|----------|------|
| 485 通信 | 发送帧 | <100ms 响应 |
| 按键 | 按下按键 | <50ms 响应 |
| RTC | 等待定时 | 误差 <1s |
| AFE 中断 | 故障注入 | <100ms |

---

## 七、总结

本项目当前的低功耗实现存在以下问题:
1. 休眠模式命名混乱
2. 阈值硬编码
3. 缺少渐进休眠策略
4. 外设功耗管理缺失
5. 无通信超时自动休眠

改进后预期:
- 正常运行功耗降低 33%
- 待机功耗降低 60-75%
- 新增运输模式支持
