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


#define GPIO_NSS           GPIOA
#define PIN_NSS            GPIO_Pin_4

#define GPIO_SCK           GPIOA
#define PIN_SCK            GPIO_Pin_5

#define GPIO_DR_CHG           GPIOA
#define PIN_DR_CHG            GPIO_Pin_6

#define GPIO_MOSI           GPIOA
#define PIN_MOSI            GPIO_Pin_7

#define GPIO_PWR_4G           GPIOB
#define PIN_PWR_4G            GPIO_Pin_5

#define GPIO_RST_4G           GPIOB
#define PIN_RST_4G            GPIO_Pin_12

#define GPIO_485_EN           GPIOB
#define PIN_485_EN            GPIO_Pin_13

#define GPIO_MCU_DTR           GPIOB
#define PIN_MCU_DTR            GPIO_Pin_14

#define GPIO_CMNT_EN           GPIOB
#define PIN_CMNT_EN            GPIO_Pin_15

#define GPIO_AD_SPS_EN           GPIOB
#define PIN_AD_SPS_EN            GPIO_Pin_6

#define GPIO_MCU_RI           GPIOB
#define PIN_MCU_RI            GPIO_Pin_7

#define GPIO_INT_WK_CMNT           GPIOB
#define PIN_INT_WK_CMNT            GPIO_Pin_8

#define GPIO_MCU_DIG1           GPIOB
#define PIN_MCU_DIG1            GPIO_Pin_9

#define GPIO_MCU_DIG2           GPIOF
#define PIN_MCU_DIG2            GPIO_Pin_6

#define GPIO_MCU_DIG3           GPIOF
#define PIN_MCU_DIG3            GPIO_Pin_7

#define GPIO_INT_WK_MCU           GPIOA
#define PIN_INT_WK_MCU            GPIO_Pin_0

#define GPIO_AFE1_PRO_EN           GPIOA
#define PIN_AFE1_PRO_EN            GPIO_Pin_1
#define GPIO_AFE1_SHIP           GPIOA
#define PIN_AFE1_SHIP            GPIO_Pin_8

#define GPIO_AD_TTC_MOS1           GPIOB
#define PIN_AD_TTC_MOS1            GPIO_Pin_0

#define GPIO_M_STB           GPIOB
#define PIN_M_STB            GPIO_Pin_1

#define GPIO_DBG_LED1           GPIOB
#define PIN_DBG_LED1            GPIO_Pin_2

#define GPIO_KEY1           GPIOC
#define PIN_KEY1            GPIO_Pin_13

#define GPIO_HT_CHG           GPIOA
#define PIN_HT_CHG            GPIO_Pin_11

#define GPIO_SEG_SPS           GPIOA
#define PIN_SEG_SPS            GPIO_Pin_12





// #define TRANS_EN_485()    GPIO_WriteBit(GPIO_485_EN,PIN_485_EN,1);
// #define RECV_EN_485()     GPIO_WriteBit(GPIO_485_EN,PIN_485_EN,0);
// #define TRANS_485_WAIT_COMPLETE()   while (!((USART1->ISR) & (1 << 7))) {}

#endif

