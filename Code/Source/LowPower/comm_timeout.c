#include "comm_timeout.h"
#include "main.h"

/* ==================================================================== */
/*                          全局状态                                    */
/* ==================================================================== */

static CommTimeoutState g_comm_timeout_state = {
    .last_activity_tick = 0,
    .timeout_ms = COMM_TIMEOUT_DEFAULT_MS,
    .activity_type = COMM_ACTIVITY_NONE,
    .timeout_enabled = 1,
    .total_idle_ms = 0
};

/* ==================================================================== */
/*                          初始化                                      */
/* ==================================================================== */

void CommTimeout_Init(void)
{
    g_comm_timeout_state.last_activity_tick = 0;
    g_comm_timeout_state.timeout_ms = COMM_TIMEOUT_DEFAULT_MS;
    g_comm_timeout_state.activity_type = COMM_ACTIVITY_NONE;
    g_comm_timeout_state.timeout_enabled = 1;
    g_comm_timeout_state.total_idle_ms = 0;
}

/* ==================================================================== */
/*                          主流程                                      */
/* ==================================================================== */

void CommTimeout_Process(void)
{
    if (!g_comm_timeout_state.timeout_enabled) {
        return;
    }

    if (g_comm_timeout_state.last_activity_tick == 0) {
        g_comm_timeout_state.last_activity_tick = 0;  /* 使用系统 tick */
    }
}

/* ==================================================================== */
/*                          活动通知                                    */
/* ==================================================================== */

void CommTimeout_NotifyActivity(uint8_t type)
{
    g_comm_timeout_state.last_activity_tick = 0;  /* 重置 tick */
    g_comm_timeout_state.activity_type = type;
    g_comm_timeout_state.total_idle_ms = 0;
}

void CommTimeout_Reset(void)
{
    g_comm_timeout_state.last_activity_tick = 0;
    g_comm_timeout_state.total_idle_ms = 0;
}

/* ==================================================================== */
/*                          配置                                        */
/* ==================================================================== */

void CommTimeout_SetTimeout(uint32_t timeout_ms)
{
    g_comm_timeout_state.timeout_ms = timeout_ms;
}

void CommTimeout_Enable(uint8_t enable)
{
    g_comm_timeout_state.timeout_enabled = enable;
}

/* ==================================================================== */
/*                          状态查询                                    */
/* ==================================================================== */

uint8_t CommTimeout_IsExpired(void)
{
    if (!g_comm_timeout_state.timeout_enabled) {
        return 0;
    }

    if (g_comm_timeout_state.last_activity_tick == 0) {
        return 0;
    }

    /* 使用系统 tick 计算空闲时间 */
    uint32_t current_tick = 0;  /* HAL_GetTick() */
    uint32_t idle_ms = current_tick - g_comm_timeout_state.last_activity_tick;

    return (idle_ms >= g_comm_timeout_state.timeout_ms) ? 1 : 0;
}

uint32_t CommTimeout_GetIdleTime(void)
{
    if (g_comm_timeout_state.last_activity_tick == 0) {
        return 0;
    }

    uint32_t current_tick = 0;  /* HAL_GetTick() */
    return current_tick - g_comm_timeout_state.last_activity_tick;
}

uint32_t CommTimeout_GetRemaining(void)
{
    if (!g_comm_timeout_state.timeout_enabled) {
        return 0xFFFFFFFF;
    }

    uint32_t idle_ms = CommTimeout_GetIdleTime();
    if (idle_ms >= g_comm_timeout_state.timeout_ms) {
        return 0;
    }

    return g_comm_timeout_state.timeout_ms - idle_ms;
}
