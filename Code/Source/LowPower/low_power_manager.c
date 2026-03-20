#include "low_power_manager.h"
#include "main.h"

/* ==================================================================== */
/*                          外部引用                                    */
/* ==================================================================== */

extern struct OtherElement OtherElement;
extern volatile union SYS_TIME g_st_SysTimeFlag;
extern volatile union SLEEP_MODE Sleep_Mode;
extern UINT16 g_stCellInfoReport_u16VCellMin;
extern UINT16 g_stCellInfoReport_u16Ichg;
extern UINT16 g_stCellInfoReport_u16IDischg;

/* ==================================================================== */
/*                          全局状态                                    */
/* ==================================================================== */

LowPowerState g_low_power_state = {
    .current_mode = LP_MODE_ACTIVE,
    .target_mode = LP_MODE_ACTIVE,
    .last_activity = 0,
    .sleep_start = 0,
    .wakeup_source = WAKEUP_SOURCE_NONE,
    .is_sleeping = 0,
    .force_sleep_flag = 0,
    .comm_timestamp = 0
};

/* ==================================================================== */
/*                          配置表                                      */
/* ==================================================================== */

static const LowPowerModeConfig g_low_power_modes[LP_MODE_COUNT] = {
    /* name       timeout  MCU  AFE  wakeup      RTC   目标电流 */
    {"Active",     0,       0,   0,   0xFFFFFFFF, 0,    10000},  /* 主动模式 */
    {"Light",      1800,    0,   0,   0x0F,       60,   1000},   /* 浅层休眠 */
    {"Standard",   7200,    1,   1,   0x0B,       300,  200},    /* 标准休眠 */
    {"Deep",       86400,   2,   1,   0x0B,       3600, 50},     /* 深度休眠 */
    {"Ship",       0,       2,   2,   0x03,       0,    5},      /* 运输模式 */
    {"Protect",    0,       2,   2,   0x03,       3600, 5},      /* 异常休眠 */
};

/* ==================================================================== */
/*                          初始化                                      */
/* ==================================================================== */

void LowPower_Init(void)
{
    g_low_power_state.current_mode = LP_MODE_ACTIVE;
    g_low_power_state.target_mode = LP_MODE_ACTIVE;
    g_low_power_state.is_sleeping = 0;
    g_low_power_state.force_sleep_flag = 0;
    g_low_power_state.last_activity = 0;
    g_low_power_state.comm_timestamp = 0;
}

/* ==================================================================== */
/*                          主流程                                      */
/* ==================================================================== */

void LowPower_Process(void)
{
    if (!g_st_SysTimeFlag.bits.b1Sys1000msFlag1) {
        return;
    }

    /* 检查低电压保护 */
    if (LowPower_CheckLowVoltage()) {
        return;
    }

    /* 检查通信超时 */
    if (LowPower_ShouldSleep()) {
        if (g_low_power_state.force_sleep_flag) {
            LowPower_EnterMode(LP_MODE_PROTECT);
        } else {
            LowPower_EnterMode(LP_MODE_STANDARD);
        }
    }

    /* 检查 SOC 低电量 */
    if (g_stCellInfoReport.SocElement.u16Soc < 10) {
        LowPower_EnterMode(LP_MODE_DEEP);
    }
}

/* ==================================================================== */
/*                          状态机                                      */
/* ==================================================================== */

void LowPower_EnterMode(LowPowerMode mode)
{
    const LowPowerModeConfig *cfg;

    if (mode >= LP_MODE_COUNT) {
        return;
    }

    cfg = &g_low_power_modes[mode];

    /* 1. 通知应用层保存状态 */
    /* LogRecord_Flag.bits.Log_Sleep = 1; */

    /* 2. 保存上下文 */
    LowPower_SaveContext();

    /* 3. 配置唤醒源 */
    LowPower_ConfigureWakeupSources(cfg->wakeup_sources);

    /* 4. 配置 RTC */
    if (cfg->rtc_period_s > 0) {
        LowPower_RtcConfigureWakeup(cfg->rtc_period_s);
    }

    /* 5. 挂起外设 */
    LowPower_SuspendPeripherals();

    /* 6. 配置 AFE */
    switch (cfg->afe_mode) {
        case AFE_MODE_IDLE:   LowPower_AfeEnterIdle();  break;
        case AFE_MODE_SLEEP:  LowPower_AfeEnterSleep(); break;
        case AFE_MODE_SHIP:   LowPower_AfeEnterShip();  break;
        default: break;
    }

    /* 7. 配置 GPIO */
    LowPower_ConfigureGpiosForSleep();

    /* 8. 更新状态 */
    g_low_power_state.current_mode = mode;
    g_low_power_state.is_sleeping = 1;
    g_low_power_state.sleep_start = 0;

    /* 9. 进入 MCU 低功耗 */
    switch (cfg->mcu_mode) {
        case MCU_MODE_SLEEP:   LowPower_McuEnterSleep();   break;
        case MCU_MODE_STOP:    LowPower_McuEnterStop();    break;
        case MCU_MODE_STANDBY: LowPower_McuEnterStandby(); break;
        default: break;
    }

    /* 10. 唤醒后恢复 */
    LowPower_Restore();
}

void LowPower_Restore(void)
{
    /* 1. 恢复 GPIO */
    LowPower_RestoreGpios();

    /* 2. 恢复 AFE */
    LowPower_AfeWakeup();

    /* 3. 恢复外设 */
    LowPower_RestorePeripherals();

    /* 4. 恢复上下文 */
    LowPower_RestoreContext();

    /* 5. 更新状态 */
    g_low_power_state.is_sleeping = 0;
    g_low_power_state.current_mode = LP_MODE_ACTIVE;
    g_low_power_state.wakeup_source = WAKEUP_SOURCE_NONE;
}

/* ==================================================================== */
/*                          获取/设置                                   */
/* ==================================================================== */

LowPowerMode LowPower_GetCurrentMode(void)
{
    return (LowPowerMode)g_low_power_state.current_mode;
}

void LowPower_SetForceSleep(uint8_t level)
{
    g_low_power_state.force_sleep_flag = level;
}

void LowPower_OnCommActivity(void)
{
    g_low_power_state.comm_timestamp = 0;  /* 使用系统 tick */
}

uint8_t LowPower_ShouldSleep(void)
{
    if (g_low_power_state.is_sleeping) {
        return 0;
    }

    if (g_low_power_state.force_sleep_flag) {
        return 1;
    }

    /* 检查通信超时 */
    if (g_low_power_state.comm_timestamp > LP_COMM_TIMEOUT_MS) {
        return 1;
    }

    return 0;
}

uint16_t LowPower_GetDynamicRtcInterval(void)
{
    UINT8 soc = g_stCellInfoReport.SocElement.u16Soc;

    if (Fault_Flag_Third.all != 0) {
        return LP_RTC_FAULT_MONITOR;
    }

    if (soc < 10) {
        return LP_RTC_DYNAMIC_LOW_SOC;
    }

    if (soc < 30) {
        return LP_RTC_DYNAMIC_MID_SOC;
    }

    return LP_RTC_DYNAMIC_NORMAL;
}

uint32_t LowPower_GetIdleTime(void)
{
    return g_low_power_state.comm_timestamp;
}

/* ==================================================================== */
/*                          低电压保护                                  */
/* ==================================================================== */

uint8_t LowPower_CheckLowVoltage(void)
{
    static uint8_t force_sleep_delay = 0;

    if (g_stCellInfoReport_u16VCellMin < LP_UVP_FORCE_SLEEP_VOLTAGE &&
        g_stCellInfoReport_u16Ichg == 0) {
        
        force_sleep_delay++;
        if (force_sleep_delay >= LP_UVP_FORCE_SLEEP_DELAY) {
            force_sleep_delay = 0;
            LowPower_EnterMode(LP_MODE_DEEP);
            return 1;
        }
    } else {
        force_sleep_delay = 0;
    }

    return 0;
}

/* ==================================================================== */
/*                          外设管理 (弱定义)                           */
/* ==================================================================== */

__attribute__((weak)) void LowPower_SuspendPeripherals(void)
{
    /* 默认实现: 关闭所有非必要外设时钟 */
    RCC_AHBPeriphClockCmd(RCC_AHBPeriph_GPIOA, ENABLE);
    RCC_AHBPeriphClockCmd(RCC_AHBPeriph_GPIOB, ENABLE);
    RCC_AHBPeriphClockCmd(RCC_AHBPeriph_GPIOC, ENABLE);

    /* 关闭 ADC */
    ADC_DeInit(ADC1);

    /* 关闭定时器 */
    /* TIM_Cmd(TIM17, DISABLE); */
}

__attribute__((weak)) void LowPower_RestorePeripherals(void)
{
    /* 唤醒后由 main 初始化流程恢复 */
}

/* ==================================================================== */
/*                          唤醒源管理 (弱定义)                         */
/* ==================================================================== */

__attribute__((weak)) void LowPower_ConfigureWakeupSources(uint32_t sources)
{
    EXTI_InitTypeDef EXTI_InitStruct;
    NVIC_InitTypeDef NVIC_InitStructure;
    GPIO_InitTypeDef GPIO_InitStructure;

    RCC_APB1PeriphClockCmd(RCC_APB1Periph_PWR, ENABLE);

    /* 485 唤醒源 */
    if (sources & WAKEUP_SOURCE_485) {
        GPIO_InitStructure.GPIO_Pin = GPIO_Pin_8;
        GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IN;
        GPIO_InitStructure.GPIO_PuPd = GPIO_PuPd_NOPULL;
        GPIO_Init(GPIOB, &GPIO_InitStructure);

        SYSCFG_EXTILineConfig(EXTI_PortSourceGPIOB, EXTI_PinSource8);
        EXTI_InitStruct.EXTI_Line = EXTI_Line8;
        EXTI_InitStruct.EXTI_Mode = EXTI_Mode_Interrupt;
        EXTI_InitStruct.EXTI_Trigger = EXTI_Trigger_Rising;
        EXTI_InitStruct.EXTI_LineCmd = ENABLE;
        EXTI_Init(&EXTI_InitStruct);

        NVIC_InitStructure.NVIC_IRQChannel = EXTI4_15_IRQn;
        NVIC_InitStructure.NVIC_IRQChannelPriority = 0;
        NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE;
        NVIC_Init(&NVIC_InitStructure);
    }

    /* 按键唤醒源 */
    if (sources & WAKEUP_SOURCE_KEY) {
        GPIO_InitStructure.GPIO_Pin = GPIO_Pin_9;
        GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IN;
        GPIO_InitStructure.GPIO_PuPd = GPIO_PuPd_NOPULL;
        GPIO_Init(GPIOB, &GPIO_InitStructure);

        SYSCFG_EXTILineConfig(EXTI_PortSourceGPIOB, EXTI_PinSource9);
        EXTI_InitStruct.EXTI_Line = EXTI_Line9;
        EXTI_InitStruct.EXTI_Mode = EXTI_Mode_Interrupt;
        EXTI_InitStruct.EXTI_Trigger = EXTI_Trigger_Falling;
        EXTI_InitStruct.EXTI_LineCmd = ENABLE;
        EXTI_Init(&EXTI_InitStruct);

        NVIC_InitStructure.NVIC_IRQChannel = EXTI4_15_IRQn;
        NVIC_InitStructure.NVIC_IRQChannelPriority = 0;
        NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE;
        NVIC_Init(&NVIC_InitStructure);
    }

    /* AFE 中断唤醒源 */
    if (sources & WAKEUP_SOURCE_AFE) {
        GPIO_InitStructure.GPIO_Pin = GPIO_Pin_13;
        GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IN;
        GPIO_InitStructure.GPIO_PuPd = GPIO_PuPd_NOPULL;
        GPIO_Init(GPIOC, &GPIO_InitStructure);

        SYSCFG_EXTILineConfig(EXTI_PortSourceGPIOC, EXTI_PinSource13);
        EXTI_InitStruct.EXTI_Line = EXTI_Line13;
        EXTI_InitStruct.EXTI_Mode = EXTI_Mode_Interrupt;
        EXTI_InitStruct.EXTI_Trigger = EXTI_Trigger_Falling;
        EXTI_InitStruct.EXTI_LineCmd = ENABLE;
        EXTI_Init(&EXTI_InitStruct);

        NVIC_InitStructure.NVIC_IRQChannel = EXTI4_15_IRQn;
        NVIC_InitStructure.NVIC_IRQChannelPriority = 0;
        NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE;
        NVIC_Init(&NVIC_InitStructure);
    }

    /* RTC 唤醒源 */
    if (sources & WAKEUP_SOURCE_RTC) {
        /* RTC 唤醒在 LowPower_RtcConfigureWakeup 中配置 */
    }
}

void LowPower_EnableWakeupSource(uint8_t source)
{
    (void)source;
}

void LowPower_DisableWakeupSource(uint8_t source)
{
    (void)source;
}

/* ==================================================================== */
/*                          GPIO 管理 (弱定义)                          */
/* ==================================================================== */

__attribute__((weak)) void LowPower_ConfigureGpiosForSleep(void)
{
    GPIO_InitTypeDef GPIO_InitStructure;

    /* 1. 关闭 LED */
    GPIO_ResetBits(GPIOB, GPIO_Pin_13);  /* 假设 LED 在 PB13 */

    /* 2. 配置所有 GPIO 为模拟输入 (最低功耗) */
    /* 注意: 需要保留唤醒源引脚为中断模式 */

    /* PA 全部模拟输入 */
    GPIO_InitStructure.GPIO_Pin = GPIO_Pin_All;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_AN;
    GPIO_InitStructure.GPIO_PuPd = GPIO_PuPd_NOPULL;
    GPIO_Init(GPIOA, &GPIO_InitStructure);

    /* PB 保留 PB8, PB9 为中断模式 (485, Key) */
    GPIO_InitStructure.GPIO_Pin = ~(GPIO_Pin_8 | GPIO_Pin_9);
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_AN;
    GPIO_InitStructure.GPIO_PuPd = GPIO_PuPd_NOPULL;
    GPIO_Init(GPIOB, &GPIO_InitStructure);

    /* PC 保留 PC13 为中断模式 (AFE) */
    GPIO_InitStructure.GPIO_Pin = ~(GPIO_Pin_13);
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_AN;
    GPIO_InitStructure.GPIO_PuPd = GPIO_PuPd_NOPULL;
    GPIO_Init(GPIOC, &GPIO_InitStructure);
}

__attribute__((weak)) void LowPower_RestoreGpios(void)
{
    /* 由 main 初始化流程恢复 */
}

/* ==================================================================== */
/*                          AFE 管理 (弱定义)                           */
/* ==================================================================== */

__attribute__((weak)) void LowPower_AfeEnterIdle(void)
{
    /* AFE IDLE 模式 */
    /* AFE_IDLE(); */
}

__attribute__((weak)) void LowPower_AfeEnterSleep(void)
{
    /* AFE Sleep 模式 */
    /* InitAFE1_Sleep(0); */
    /* AFE_Sleep(); */
}

__attribute__((weak)) void LowPower_AfeEnterShip(void)
{
    /* AFE SHIP 模式 */
    /* AFE_SHIP(); */
}

__attribute__((weak)) void LowPower_AfeWakeup(void)
{
    /* AFE 唤醒 */
    /* AFE_WAKEUP(); */
}

/* ==================================================================== */
/*                          MCU 低功耗 (弱定义)                         */
/* ==================================================================== */

__attribute__((weak)) void LowPower_McuEnterSleep(void)
{
    NVIC_SystemLPConfig(NVIC_LP_SLEEPONEXIT, ENABLE);
    __ASM volatile("wfi");
}

__attribute__((weak)) void LowPower_McuEnterStop(void)
{
    PWR_EnterSTOPMode(PWR_Regulator_LowPower, PWR_STOPEntry_WFI);

    /* 唤醒后恢复时钟 */
#if (defined _HSE_8M_PLL_48M) || (defined _HSE_12M_PLL_48M)
    RCC_HSEConfig(RCC_HSE_ON);
    while (RCC_GetFlagStatus(RCC_FLAG_HSERDY) == RESET);
    RCC_PLLCmd(ENABLE);
    while (RCC_GetFlagStatus(RCC_FLAG_PLLRDY) == RESET);
    RCC_SYSCLKConfig(RCC_SYSCLKSource_PLLCLK);
    while (RCC_GetSYSCLKSource() != 0x08);
#endif
}

__attribute__((weak)) void LowPower_McuEnterStandby(void)
{
    RCC_APB1PeriphClockCmd(RCC_APB1Periph_PWR, ENABLE);
    PWR_WakeUpPinCmd(PWR_WakeUpPin_1, ENABLE);
    PWR_ClearFlag(PWR_FLAG_WU);
    PWR_EnterSTANDBYMode();
}

/* ==================================================================== */
/*                          RTC 管理 (弱定义)                           */
/* ==================================================================== */

__attribute__((weak)) void LowPower_RtcConfigureWakeup(uint16_t period_s)
{
    /* RTC 定时唤醒配置 */
    (void)period_s;
    /* RTC_TimeConfig(); */
    /* RTC_AlarmConfig(); */
}

/* ==================================================================== */
/*                          上下文管理 (弱定义)                         */
/* ==================================================================== */

__attribute__((weak)) void LowPower_SaveContext(void)
{
    /* 保存到 EEPROM */
    /* SOC_DealEEPROM_Data(EEPROM_DATA_REFRESH); */
}

__attribute__((weak)) void LowPower_RestoreContext(void)
{
    /* 从 EEPROM 恢复 */
    /* SOC_DealEEPROM_Data(EEPROM_DATA_READ); */
}
