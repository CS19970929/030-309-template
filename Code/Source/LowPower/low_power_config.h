#ifndef _LOW_POWER_CONFIG_H_
#define _LOW_POWER_CONFIG_H_

#include "main.h"

#ifdef __cplusplus
extern "C" {
#endif

/* ==================================================================== */
/*                          低功耗模式定义                              */
/* ==================================================================== */

typedef enum {
    LP_MODE_ACTIVE = 0,     /* 主动模式 - 正常运行 */
    LP_MODE_LIGHT,          /* 浅层休眠 - Sleep + AFE IDLE */
    LP_MODE_STANDARD,       /* 标准休眠 - Stop + AFE Sleep */
    LP_MODE_DEEP,           /* 深度休眠 - Standby + AFE Sleep */
    LP_MODE_SHIP,           /* 运输模式 - Standby + AFE SHIP */
    LP_MODE_PROTECT,        /* 异常休眠 - Standby + AFE SHIP */
    LP_MODE_COUNT
} LowPowerMode;

/* ==================================================================== */
/*                          MCU 功耗模式定义                            */
/* ==================================================================== */

typedef enum {
    MCU_MODE_RUN = 0,       /* 全速运行 */
    MCU_MODE_SLEEP,         /* WFI 休眠 */
    MCU_MODE_STOP,          /* 停止模式 */
    MCU_MODE_STANDBY,       /* 待机模式 */
    MCU_MODE_SHUTDOWN       /* 关机模式 */
} McuPowerMode;

/* ==================================================================== */
/*                          AFE 功耗模式定义                            */
/* ==================================================================== */

typedef enum {
    AFE_MODE_NORMAL = 0,    /* 正常模式 */
    AFE_MODE_IDLE,          /* IDLE 模式 */
    AFE_MODE_SLEEP,         /* Sleep 模式 */
    AFE_MODE_SHIP           /* SHIP 模式 */
} AfePowerMode;

/* ==================================================================== */
/*                          唤醒源定义                                  */
/* ==================================================================== */

#define WAKEUP_SOURCE_NONE      0x00000000
#define WAKEUP_SOURCE_485       0x00000001
#define WAKEUP_SOURCE_KEY       0x00000002
#define WAKEUP_SOURCE_RTC       0x00000004
#define WAKEUP_SOURCE_AFE       0x00000008
#define WAKEUP_SOURCE_LOAD      0x00000010
#define WAKEUP_SOURCE_ALL       0xFFFFFFFF

/* ==================================================================== */
/*                          配置参数 (可 EEPROM)                       */
/* ==================================================================== */

/* 休眠超时时间 (秒) */
#define LP_IDLE_TIMEOUT_LIGHT       1800    /* 浅层休眠: 30分钟 */
#define LP_IDLE_TIMEOUT_STANDARD    7200    /* 标准休眠: 2小时 */
#define LP_IDLE_TIMEOUT_DEEP        86400   /* 深度休眠: 24小时 */
#define LP_IDLE_TIMEOUT_SHIP        0       /* 运输模式: 立即 */

/* RTC 唤醒周期 (秒) */
#define LP_RTC_PERIOD_LIGHT         60      /* 浅层休眠: 1分钟 */
#define LP_RTC_PERIOD_STANDARD      300     /* 标准休眠: 5分钟 */
#define LP_RTC_PERIOD_DEEP          3600    /* 深度休眠: 1小时 */
#define LP_RTC_PERIOD_PROTECT       3600    /* 异常休眠: 1小时 */

/* 目标电流 (uA) */
#define LP_TARGET_CURRENT_ACTIVE    10000   /* 主动模式: 10mA */
#define LP_TARGET_CURRENT_LIGHT     1000    /* 浅层休眠: 1mA */
#define LP_TARGET_CURRENT_STANDARD  200     /* 标准休眠: 200uA */
#define LP_TARGET_CURRENT_DEEP      50      /* 深度休眠: 50uA */
#define LP_TARGET_CURRENT_SHIP      5       /* 运输模式: 5uA */

/* 动态 RTC 间隔 */
#define LP_RTC_DYNAMIC_LOW_SOC      60      /* 低SOC: 1分钟 */
#define LP_RTC_DYNAMIC_MID_SOC      1800    /* 中SOC: 30分钟 */
#define LP_RTC_DYNAMIC_NORMAL       3600    /* 正常: 1小时 */

/* 故障注入唤醒间隔 */
#define LP_RTC_FAULT_MONITOR        60      /* 故障监控: 1分钟 */

/* 通信超时 */
#define LP_COMM_TIMEOUT_MS          (30 * 60 * 1000)  /* 30分钟 */

/* 低电压保护 */
#define LP_UVP_FORCE_SLEEP_VOLTAGE  2600    /* mV */
#define LP_UVP_FORCE_SLEEP_DELAY    60      /* 秒 */

/* 压差保护 */
#define LP_VDELTA_THRESHOLD         600     /* mV */

#ifdef __cplusplus
}
#endif

#endif /* _LOW_POWER_CONFIG_H_ */
