#include "perip_power.h"
#include "main.h"

/* ==================================================================== */
/*                          外部引用                                    */
/* ==================================================================== */

/* 串口上下文 */
extern UART_HandleTypeDef huart1;

/* ==================================================================== */
/*                          外设功耗表                                  */
/* ==================================================================== */

static const PeripPowerData g_perip_power_table[PERIP_COUNT] = {
    /* save     suspend    resume    restore   电流   名称 */
    {NULL,      USART1_Suspend,  USART1_Resume,  NULL,      200,  "USART1"},
    {NULL,      USART2_Suspend,  USART2_Resume,  NULL,      200,  "USART2"},
    {NULL,      I2C1_Suspend,    I2C1_Resume,    NULL,      50,   "I2C1"},
    {NULL,      ADC1_Suspend,    ADC1_Resume,    NULL,      100,  "ADC1"},
    {NULL,      TIM17_Suspend,   TIM17_Resume,   NULL,      80,   "TIM17"},
    {NULL,      GPIO_Suspend,    GPIO_Resume,    NULL,      50,   "GPIO"},
};

/* ==================================================================== */
/*                          弱定义回调                                  */
/* ==================================================================== */

__attribute__((weak)) void USART1_Suspend(void)
{
    USART_Cmd(USART1, DISABLE);
    USART_ITConfig(USART1, USART_IT_RXNE, DISABLE);
    USART_ITConfig(USART1, USART_IT_TXE, DISABLE);
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_USART1, DISABLE);
}

__attribute__((weak)) void USART1_Resume(void)
{
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_USART1, ENABLE);
    USART_Cmd(USART1, ENABLE);
    USART_ITConfig(USART1, USART_IT_RXNE, ENABLE);
}

__attribute__((weak)) void USART2_Suspend(void)
{
    USART_Cmd(USART2, DISABLE);
    USART_ITConfig(USART2, USART_IT_RXNE, DISABLE);
    USART_ITConfig(USART2, USART_IT_TXE, DISABLE);
    RCC_APB1PeriphClockCmd(RCC_APB1Periph_USART2, DISABLE);
}

__attribute__((weak)) void USART2_Resume(void)
{
    RCC_APB1PeriphClockCmd(RCC_APB1Periph_USART2, ENABLE);
    USART_Cmd(USART2, ENABLE);
    USART_ITConfig(USART2, USART_IT_RXNE, ENABLE);
}

__attribute__((weak)) void I2C1_Suspend(void)
{
    I2C_Cmd(I2C1, DISABLE);
    RCC_APB1PeriphClockCmd(RCC_APB1Periph_I2C1, DISABLE);
}

__attribute__((weak)) void I2C1_Resume(void)
{
    RCC_APB1PeriphClockCmd(RCC_APB1Periph_I2C1, ENABLE);
    I2C_Cmd(I2C1, ENABLE);
}

__attribute__((weak)) void ADC1_Suspend(void)
{
    ADC_DeInit(ADC1);
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_ADC1, DISABLE);
}

__attribute__((weak)) void ADC1_Resume(void)
{
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_ADC1, ENABLE);
    /* ADC 需要重新初始化 */
}

__attribute__((weak)) void TIM17_Suspend(void)
{
    TIM_Cmd(TIM17, DISABLE);
    TIM_ITConfig(TIM17, TIM_IT_Update, DISABLE);
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_TIM17, DISABLE);
}

__attribute__((weak)) void TIM17_Resume(void)
{
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_TIM17, ENABLE);
    TIM_ITConfig(TIM17, TIM_IT_Update, ENABLE);
    TIM_Cmd(TIM17, ENABLE);
}

__attribute__((weak)) void GPIO_Suspend(void)
{
    /* 休眠时 GPIO 配置在 LowPower_ConfigureGpiosForSleep 中处理 */
}

__attribute__((weak)) void GPIO_Resume(void)
{
    /* 唤醒后 GPIO 恢复在 LowPower_RestoreGpios 中处理 */
}

/* ==================================================================== */
/*                          初始化                                      */
/* ==================================================================== */

void PeripPower_Init(void)
{
    /* 初始化 */
}

/* ==================================================================== */
/*                          挂起/恢复                                   */
/* ==================================================================== */

void PeripPower_SuspendAll(void)
{
    for (int i = 0; i < PERIP_COUNT; i++) {
        if (g_perip_power_table[i].suspend != NULL) {
            g_perip_power_table[i].suspend();
        }
    }
}

void PeripPower_ResumeAll(void)
{
    for (int i = 0; i < PERIP_COUNT; i++) {
        if (g_perip_power_table[i].resume != NULL) {
            g_perip_power_table[i].resume();
        }
    }
}

void PeripPower_Suspend(PeripId id)
{
    if (id < PERIP_COUNT && g_perip_power_table[id].suspend != NULL) {
        g_perip_power_table[id].suspend();
    }
}

void PeripPower_Resume(PeripId id)
{
    if (id < PERIP_COUNT && g_perip_power_table[id].resume != NULL) {
        g_perip_power_table[id].resume();
    }
}

/* ==================================================================== */
/*                          上下文                                      */
/* ==================================================================== */

void PeripPower_SaveContext(PeripId id)
{
    if (id < PERIP_COUNT && g_perip_power_table[id].save_context != NULL) {
        g_perip_power_table[id].save_context();
    }
}

void PeripPower_RestoreContext(PeripId id)
{
    if (id < PERIP_COUNT && g_perip_power_table[id].restore_context != NULL) {
        g_perip_power_table[id].restore_context();
    }
}

void PeripPower_SaveAllContext(void)
{
    for (int i = 0; i < PERIP_COUNT; i++) {
        if (g_perip_power_table[i].save_context != NULL) {
            g_perip_power_table[i].save_context();
        }
    }
}

void PeripPower_RestoreAllContext(void)
{
    for (int i = 0; i < PERIP_COUNT; i++) {
        if (g_perip_power_table[i].restore_context != NULL) {
            g_perip_power_table[i].restore_context();
        }
    }
}

/* ==================================================================== */
/*                          功耗查询                                    */
/* ==================================================================== */

uint32_t PeripPower_GetTotalCurrent(void)
{
    uint32_t total = 0;
    for (int i = 0; i < PERIP_COUNT; i++) {
        total += g_perip_power_table[i].current_uA;
    }
    return total;
}
