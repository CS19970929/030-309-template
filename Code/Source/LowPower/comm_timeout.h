#ifndef _COMM_TIMEOUT_H_
#define _COMM_TIMEOUT_H_

#include "low_power_config.h"

#ifdef __cplusplus
extern "C" {
#endif

/* ==================================================================== */
/*                          通信超时配置                                */
/* ==================================================================== */

/* 超时时间 (ms) */
#define COMM_TIMEOUT_DEFAULT_MS     (30 * 60 * 1000)  /* 30 分钟 */
#define COMM_TIMEOUT_SHORT_MS       (5 * 60 * 1000)   /* 5 分钟 */
#define COMM_TIMEOUT_LONG_MS        (60 * 60 * 1000)  /* 1 小时 */

/* 活动检测 */
#define COMM_ACTIVITY_NONE          0x00
#define COMM_ACTIVITY_485_RX        0x01
#define COMM_ACTIVITY_485_TX        0x02
#define COMM_ACTIVITY_KEY           0x04
#define COMM_ACTIVITY_AFE           0x08

/* ==================================================================== */
/*                          数据结构                                    */
/* ==================================================================== */

typedef struct {
    uint32_t last_activity_tick;    /* 上次活动 tick */
    uint32_t timeout_ms;            /* 超时时间 */
    uint8_t activity_type;          /* 活动类型 */
    uint8_t timeout_enabled;        /* 超时检测使能 */
    uint32_t total_idle_ms;         /* 总空闲时间 */
} CommTimeoutState;

/* ==================================================================== */
/*                          接口函数                                    */
/* ==================================================================== */

/* 初始化 */
void CommTimeout_Init(void);

/* 主循环调用 */
void CommTimeout_Process(void);

/* 通知活动 */
void CommTimeout_NotifyActivity(uint8_t type);

/* 重置超时 */
void CommTimeout_Reset(void);

/* 设置超时时间 */
void CommTimeout_SetTimeout(uint32_t timeout_ms);

/* 启用/禁用超时 */
void CommTimeout_Enable(uint8_t enable);

/* 检查是否超时 */
uint8_t CommTimeout_IsExpired(void);

/* 获取空闲时间 */
uint32_t CommTimeout_GetIdleTime(void);

/* 获取剩余时间 */
uint32_t CommTimeout_GetRemaining(void);

#ifdef __cplusplus
}
#endif

#endif /* _COMM_TIMEOUT_H_ */
