#ifndef _WAKEUP_MANAGER_H_
#define _WAKEUP_MANAGER_H_

#include "low_power_config.h"

#ifdef __cplusplus
extern "C" {
#endif

/* ==================================================================== */
/*                          唤醒源枚举                                  */
/* ==================================================================== */

typedef enum {
    WAKEUP_REASON_NONE = 0,
    WAKEUP_REASON_485_COMM,     /* 485 通信唤醒 */
    WAKEUP_REASON_KEY_PRESS,    /* 按键唤醒 */
    WAKEUP_REASON_RTC_ALARM,    /* RTC 定时唤醒 */
    WAKEUP_REASON_AFE_IRQ,      /* AFE 中断唤醒 */
    WAKEUP_REASON_LOAD_DETECT,  /* 负载检测唤醒 */
    WAKEUP_REASON_OVER_CURRENT, /* 过流保护唤醒 */
    WAKEUP_REASON_EXTERNAL,     /* 外部中断唤醒 */
    WAKEUP_REASON_UNKNOWN       /* 未知唤醒 */
} WakeupReason;

/* ==================================================================== */
/*                          接口函数                                    */
/* ==================================================================== */

/* 初始化 */
void WakeupManager_Init(void);

/* 获取唤醒原因 */
WakeupReason WakeupManager_GetReason(void);

/* 清除唤醒标志 */
void WakeupManager_ClearFlags(void);

/* 是否是休眠唤醒 */
uint8_t WakeupManager_IsSleepWakeup(void);

/* 获取上次唤醒源 */
uint32_t WakeupManager_GetLastWakeupSource(void);

#ifdef __cplusplus
}
#endif

#endif /* _WAKEUP_MANAGER_H_ */
