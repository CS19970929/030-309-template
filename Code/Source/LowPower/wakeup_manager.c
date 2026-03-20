#include "wakeup_manager.h"
#include "main.h"

/* ==================================================================== */
/*                          全局状态                                    */
/* ==================================================================== */

static uint32_t g_last_wakeup_source = WAKEUP_SOURCE_NONE;
static WakeupReason g_last_wakeup_reason = WAKEUP_REASON_NONE;
static uint8_t g_is_sleep_wakeup = 0;

/* ==================================================================== */
/*                          初始化                                      */
/* ==================================================================== */

void WakeupManager_Init(void)
{
    g_last_wakeup_source = WAKEUP_SOURCE_NONE;
    g_last_wakeup_reason = WAKEUP_REASON_NONE;
    g_is_sleep_wakeup = 0;
}

/* ==================================================================== */
/*                          唤醒原因分析                                */
/* ==================================================================== */

WakeupReason WakeupManager_GetReason(void)
{
    WakeupReason reason = WAKEUP_REASON_UNKNOWN;

    /* 检查各个唤醒源 */
    if (EXTI_GetITStatus(EXTI_Line8) != RESET) {
        reason = WAKEUP_REASON_485_COMM;
        g_last_wakeup_source = WAKEUP_SOURCE_485;
        EXTI_ClearITPendingBit(EXTI_Line8);
    }
    else if (EXTI_GetITStatus(EXTI_Line9) != RESET) {
        reason = WAKEUP_REASON_KEY_PRESS;
        g_last_wakeup_source = WAKEUP_SOURCE_KEY;
        EXTI_ClearITPendingBit(EXTI_Line9);
    }
    else if (EXTI_GetITStatus(EXTI_Line13) != RESET) {
        reason = WAKEUP_REASON_AFE_IRQ;
        g_last_wakeup_source = WAKEUP_SOURCE_AFE;
        EXTI_ClearITPendingBit(EXTI_Line13);
    }
    else if (RTC_GetITStatus(RTC_IT_ALRA) != RESET) {
        reason = WAKEUP_REASON_RTC_ALARM;
        g_last_wakeup_source = WAKEUP_SOURCE_RTC;
        RTC_ClearITPendingBit(RTC_IT_ALRA);
    }
    else {
        reason = WAKEUP_REASON_UNKNOWN;
    }

    g_last_wakeup_reason = reason;
    g_is_sleep_wakeup = 1;

    return reason;
}

/* ==================================================================== */
/*                          唤醒标志                                    */
/* ==================================================================== */

void WakeupManager_ClearFlags(void)
{
    g_last_wakeup_source = WAKEUP_SOURCE_NONE;
    g_last_wakeup_reason = WAKEUP_REASON_NONE;
    g_is_sleep_wakeup = 0;
}

uint8_t WakeupManager_IsSleepWakeup(void)
{
    return g_is_sleep_wakeup;
}

uint32_t WakeupManager_GetLastWakeupSource(void)
{
    return g_last_wakeup_source;
}

/* ==================================================================== */
/*                          外部中断处理                                */
/* ==================================================================== */

void EXTI4_15_IRQHandler(void)
{
    /* 485 唤醒 */
    if (EXTI_GetITStatus(EXTI_Line8) != RESET) {
        g_last_wakeup_source = WAKEUP_SOURCE_485;
        g_last_wakeup_reason = WAKEUP_REASON_485_COMM;
        EXTI_ClearITPendingBit(EXTI_Line8);
    }

    /* 按键唤醒 */
    if (EXTI_GetITStatus(EXTI_Line9) != RESET) {
        g_last_wakeup_source = WAKEUP_SOURCE_KEY;
        g_last_wakeup_reason = WAKEUP_REASON_KEY_PRESS;
        EXTI_ClearITPendingBit(EXTI_Line9);
    }

    /* AFE 中断 */
    if (EXTI_GetITStatus(EXTI_Line13) != RESET) {
        g_last_wakeup_source = WAKEUP_SOURCE_AFE;
        g_last_wakeup_reason = WAKEUP_REASON_AFE_IRQ;
        EXTI_ClearITPendingBit(EXTI_Line13);
    }
}
