#ifndef _PERIP_POWER_H_
#define _PERIP_POWER_H_

#include "low_power_config.h"

#ifdef __cplusplus
extern "C" {
#endif

/* ==================================================================== */
/*                          外设枚举                                    */
/* ==================================================================== */

typedef enum {
    PERIP_USART1 = 0,
    PERIP_USART2,
    PERIP_I2C1,
    PERIP_ADC1,
    PERIP_TIM17,
    PERIP_GPIO,
    PERIP_COUNT
} PeripId;

/* ==================================================================== */
/*                          外设功耗数据                                */
/* ==================================================================== */

typedef struct {
    void (*save_context)(void);     /* 保存上下文 */
    void (*suspend)(void);          /* 挂起外设 */
    void (*resume)(void);           /* 恢复外设 */
    void (*restore_context)(void);  /* 恢复上下文 */
    uint16_t current_uA;            /* 功耗 (uA) */
    const char *name;               /* 外设名称 */
} PeripPowerData;

/* ==================================================================== */
/*                          接口函数                                    */
/* ==================================================================== */

/* 初始化 */
void PeripPower_Init(void);

/* 挂起所有外设 */
void PeripPower_SuspendAll(void);

/* 恢复所有外设 */
void PeripPower_ResumeAll(void);

/* 挂起指定外设 */
void PeripPower_Suspend(PeripId id);

/* 恢复指定外设 */
void PeripPower_Resume(PeripId id);

/* 保存指定外设上下文 */
void PeripPower_SaveContext(PeripId id);

/* 恢复指定外设上下文 */
void PeripPower_RestoreContext(PeripId id);

/* 保存所有外设上下文 */
void PeripPower_SaveAllContext(void);

/* 恢复所有外设上下文 */
void PeripPower_RestoreAllContext(void);

/* 获取总功耗 */
uint32_t PeripPower_GetTotalCurrent(void);

#ifdef __cplusplus
}
#endif

#endif /* _PERIP_POWER_H_ */
