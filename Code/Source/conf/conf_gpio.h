#ifndef CONF_GPIO_H
#define CONF_GPIO_H

#define __STM32F0__
// #define __STM32F1__

#ifdef __STM32F0__
#include "stm32f0xx.h"
#endif // __STM32F0__
#ifdef __STM32F1__
#include "stm32f10x.h"
#endif // __STM32F1__


#define GPIO_MCU_ADC1              GPIOA
#define PIN_MCU_ADC1               GPIO_Pin_4

#define GPIO_LED_KEY              GPIOA
#define PIN_LED_KEY               GPIO_Pin_15
#define GPIO_LED20              GPIOB
#define PIN_LED20               GPIO_Pin_3
#define GPIO_LED40              GPIOB
#define PIN_LED40               GPIO_Pin_4
#define GPIO_LED60              GPIOB
#define PIN_LED60               GPIO_Pin_5
#define GPIO_LED80              GPIOB
#define PIN_LED80               GPIO_Pin_8
#define GPIO_LED100              GPIOB
#define PIN_LED100               GPIO_Pin_9

#define GPIO_ALERT_MCU              GPIOB
#define PIN_ALERT_MCU               GPIO_Pin_12
#define GPIO_AFE1_CTL              GPIOB
#define PIN_AFE1_CTL               GPIO_Pin_14

#define GPIO_ADC_EN              GPIOB
#define PIN_ADC_EN               GPIO_Pin_6

#define GPIO_CW_EN              GPIOB
#define PIN_CW_EN               GPIO_Pin_7

#define GPIO_MCU_DRV              GPIOF
#define PIN_MCU_DRV               GPIO_Pin_6

#define GPIO_RF_IN              GPIOF
#define PIN_RF_IN               GPIO_Pin_7

#define GPIO_INT_WK_MCU              GPIOA
#define PIN_INT_WK_MCU               GPIO_Pin_0

#define GPIO_AFE1_PRO_EN              GPIOA
#define PIN_AFE1_PRO_EN               GPIO_Pin_1
#define GPIO_AFE1_MODE              GPIOA
#define PIN_AFE1_MODE               GPIO_Pin_8

#define GPIO_AD_TTC_MOS1              GPIOB
#define PIN_AD_TTC_MOS1               GPIO_Pin_0

#define GPIO_DB_LED1              GPIOB
#define PIN_DB_LED1               GPIO_Pin_2

#define GPIO_KEY1              GPIOC
#define PIN_KEY1               GPIO_Pin_13



// #define M_STB_PORT          GPIOB
// #define M_STB_PIN           GPIO_Pin_1

// #define M_CMNT_EN_PORT          GPIOB
// #define M_CMNT_EN_PIN           GPIO_Pin_1

// #define M_CTR_PORT          GPIOA
// #define M_CTR_PIN           GPIO_Pin_8
	
// #define M_BLE_EN_PORT          GPIOB
// #define M_BLE_EN_PIN           GPIO_Pin_15







#endif
