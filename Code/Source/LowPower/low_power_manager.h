#ifndef _LOW_POWER_MANAGER_H_
#define _LOW_POWER_MANAGER_H_

#include "low_power_config.h"

#ifdef __cplusplus
extern "C" {
#endif

/* ==================================================================== */
/*                          数据结构                                    */
/* ==================================================================== */

/* 休眠模式配置 */
typedef struct {
    const char *name;           /* 模式名称 */
    uint16_t idle_timeout_s;    /* 无操作超时 (秒) */
    uint8_t mcu_mode;           /* MCU 模式 */
    uint8_t afe_mode;           /* AFE 模式 */
    uint32_t wakeup_sources;    /* 唤醒源位掩码 */
    uint16_t rtc_period_s;      /* RTC 唤醒周期 (秒) */
    uint16_t target_current_uA; /* 目标电流 (uA) */
} LowPowerModeConfig;

/* 外设功耗操作 */
typedef struct {
    void (*save)(void);     /* 进入休眠前保存 */
    void (*suspend)(void);  /* 挂起 */
    void (*restore)(void);  /* 唤醒后恢复 */
    uint16_t current_uA;    /* 功耗 (uA) */
} PeripPowerOps;

/* 低功耗状态 */
typedef struct {
    uint8_t current_mode;       /* 当前模式 */
    uint8_t target_mode;        /* 目标模式 */
    uint32_t last_activity;     /* 上次活动时间戳 */
    uint32_t sleep_start;       /* 休眠开始时间戳 */
    uint32_t wakeup_source;     /* 唤醒源 */
    uint8_t is_sleeping;        /* 是否处于休眠 */
    uint8_t force_sleep_flag;   /* 强制休眠标志 */
    uint32_t comm_timestamp;    /* 通信时间戳 */
} LowPowerState;

/* 外设功耗管理 */
typedef struct {
    void (*save)(void);         /* 保存上下文 */
    void (*suspend)(void);      /* 挂起外设 */
    void (*restore)(void);      /* 恢复外设 */
    uint16_t current_uA;        /* 功耗 (uA) */
    const char *name;           /* 外设名称 */
} LowPowerPeripheral;

/* 唤醒源配置 */
typedef struct {
    uint8_t enabled;            /* 是否启用 */
    uint16_t port;              /* GPIO 端口 */
    uint16_t pin;               /* GPIO 引脚 */
    uint8_t trigger;            /* 触发方式: 0=falling, 1=rising, 2=both */
    uint8_t priority;           /* 唤醒优先级 */
    const char *name;           /* 唤醒源名称 */
} WakeupSourceConfig;

/* ==================================================================== */
/*                          全局状态                                    */
/* ==================================================================== */

extern LowPowerState g_low_power_state;

/* ==================================================================== */
/*                          接口函数                                    */
/* ==================================================================== */

/* 初始化 */
void LowPower_Init(void);

/* 主循环调用 */
void LowPower_Process(void);

/* 进入指定模式 */
void LowPower_EnterMode(LowPowerMode mode);

/* 唤醒后恢复 */
void LowPower_Restore(void);

/* 获取当前模式 */
LowPowerMode LowPower_GetCurrentMode(void);

/* 设置强制休眠 */
void LowPower_SetForceSleep(uint8_t level);

/* 通信活动通知 */
void LowPower_OnCommActivity(void);

/* 检查是否应进入休眠 */
uint8_t LowPower_ShouldSleep(void);

/* 获取动态 RTC 间隔 */
uint16_t LowPower_GetDynamicRtcInterval(void);

/* 获取 idle 时间 */
uint32_t LowPower_GetIdleTime(void);

/* 外设管理 */
void LowPower_SuspendPeripherals(void);
void LowPower_RestorePeripherals(void);

/* 唤醒源管理 */
void LowPower_ConfigureWakeupSources(uint32_t sources);
void LowPower_EnableWakeupSource(uint8_t source);
void LowPower_DisableWakeupSource(uint8_t source);

/* GPIO 管理 */
void LowPower_ConfigureGpiosForSleep(void);
void LowPower_RestoreGpios(void);

/* AFE 管理 */
void LowPower_AfeEnterIdle(void);
void LowPower_AfeEnterSleep(void);
void LowPower_AfeEnterShip(void);
void LowPower_AfeWakeup(void);

/* MCU 低功耗 */
void LowPower_McuEnterSleep(void);
void LowPower_McuEnterStop(void);
void LowPower_McuEnterStandby(void);

/* RTC 唤醒 */
void LowPower_RtcConfigureWakeup(uint16_t period_s);

/* 上下文保存/恢复 */
void LowPower_SaveContext(void);
void LowPower_RestoreContext(void);

/* 低电压保护 */
uint8_t LowPower_CheckLowVoltage(void);

#ifdef __cplusplus
}
#endif

#endif /* _LOW_POWER_MANAGER_H_ */
