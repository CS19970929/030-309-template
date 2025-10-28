/**
 ******************************************************************************
 * @file    IO_Toggle/stm32f0xx_it.c
 * @author  MCD Application Team
 * @version V1.0.0
 * @date    23-March-2012
 * @brief   Main Interrupt Service Routines.
 *          This file provides template for all exceptions handler and
 *          peripherals interrupt service routine.
 ******************************************************************************
 * @attention
 *
 * <h2><center>&copy; COPYRIGHT 2012 STMicroelectronics</center></h2>
 *
 * Licensed under MCD-ST Liberty SW License Agreement V2, (the "License");
 * You may not use this file except in compliance with the License.
 * You may obtain a copy of the License at:
 *
 *        http://www.st.com/software_license_agreement_liberty_v2
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 ******************************************************************************
 */

/* Includes ------------------------------------------------------------------*/
#include "stm32f0xx_it.h"
#include "main.h"
/** @addtogroup STM32F0_Discovery_Peripheral_Examples
 * @{
 */

/** @addtogroup IO_Toggle
 * @{
 */

/* Private typedef -----------------------------------------------------------*/
/* Private define ------------------------------------------------------------*/
/* Private macro -------------------------------------------------------------*/
/* Private variables ---------------------------------------------------------*/
/* Private function prototypes -----------------------------------------------*/
/* Private functions ---------------------------------------------------------*/

/******************************************************************************/
/*            Cortex-M0 Processor Exceptions Handlers                         */
/******************************************************************************/

/**
 * @brief  This function handles NMI exception.
 * @param  None
 * @retval None
 */
void NMI_Handler(void)
{
}

/**
 * @brief  This function handles Hard Fault exception.
 * @param  None
 * @retval None
 */
void HardFault_Handler(void)
{
	/* Go to infinite loop when Hard Fault exception occurs */
	while (1)
	{
	}
}

/**
 * @brief  This function handles SVCall exception.
 * @param  None
 * @retval None
 */
void SVC_Handler(void)
{
}

/**
 * @brief  This function handles PendSVC exception.
 * @param  None
 * @retval None
 */
void PendSV_Handler(void)
{
}

/**
 * @brief  This function handles SysTick Handler.
 * @param  None
 * @retval None
 */
void SysTick_Handler(void)
{
}

/******************************************************************************/
/*                 STM32F0xx Peripherals Interrupt Handlers                   */
/*  Add here the Interrupt Handler for the used peripheral(s) (PPP), for the  */
/*  available peripheral interrupt handler's name please refer to the startup */
/*  file (startup_stm32f0xx.s).                                               */
/******************************************************************************/

/**
 * @brief  This function handles PPP interrupt request.
 * @param  None
 * @retval None
 */
/*void PPP_IRQHandler(void)
{
}*/

/**
 * @}
 */

/**
 * @}
 */

/************************ (C) COPYRIGHT STMicroelectronics *****END OF FILE****/

// 外部中断0服务程序，没用
void EXTI0_1_IRQHandler(void)
{
	// delay_ms(10);//消抖
	if (EXTI_GetITStatus(EXTI_Line0) != RESET)
	{
		sys_time.isCHG_wake = true;
		// WKUP
		EXTI_ClearITPendingBit(EXTI_Line0);
		ChargerLoad_Func.bits.b1ON_Charger_AllSeries = 1;
	}
	if (EXTI_GetITStatus(EXTI_Line1) != RESET)
	{
		EXTI_ClearITPendingBit(EXTI_Line1);
	}
}

void EXTI2_3_IRQHandler(void)
{
	if (EXTI_GetITStatus(EXTI_Line3) != RESET)
	{
		EXTI_ClearITPendingBit(EXTI_Line3);
	}
}

void EXTI4_15_IRQHandler(void)
{

	if (EXTI_GetITStatus(EXTI_Line7) != RESET)
	{
		EXTI_ClearITPendingBit(EXTI_Line7);
	}
	if (EXTI_GetITStatus(EXTI_Line8) != RESET)
	{
		EXTI_ClearITPendingBit(EXTI_Line8);
	}
	if (EXTI_GetITStatus(EXTI_Line9) != RESET)
	{
		EXTI_ClearITPendingBit(EXTI_Line9);
	}

	if (EXTI_GetITStatus(EXTI_Line10) != RESET)
	{
		EXTI_ClearITPendingBit(EXTI_Line10);
	}

	if (EXTI_GetITStatus(EXTI_Line12) != RESET)
	{
		EXTI_ClearITPendingBit(EXTI_Line12);
	}

	if (EXTI_GetITStatus(EXTI_Line13) != RESET)
	{
		EXTI_ClearITPendingBit(EXTI_Line13);
	}

	if (EXTI_GetITStatus(EXTI_Line5) != RESET)
	{
		EXTI_ClearITPendingBit(EXTI_Line5);
	}
}

#if 1
void USART1_IRQHandler(void)
{
	// Sci1_CommonUpper_FaultChk();
	uint32_t isr = USART1->ISR;

	// ---- 1. 错误检测与清除 ----
	if (isr & (USART_ISR_ORE | USART_ISR_FE | USART_ISR_NE | USART_ISR_PE))
	{
		volatile uint32_t dump = USART1->RDR;
		(void)dump;

		// 手动清除错误标志（ICR是写1清零）
		USART1->ICR = (1 << 3) | (1 << 2) | (1 << 1) | (1 << 0);

		// gu16_CommuErrCnt_SCI2++;
		return;
	}

	if (USART_GetITStatus(USART1, USART_IT_RXNE) != RESET)
	{
		RTC_ExtComCnt++;
		RTC_ExtComCnt1++;

#if (defined _COMMOM_UPPER_SCI1)
		Sci1_CommonUpper_Rx_Deal(&g_stCurrentMsgPtr_SCI1);
#endif
	}
	// ---- 2. 循环读取所有接收到的数据 ----
	// while (USART2->ISR & USART_ISR_RXNE)
	// {
	// 	uint8_t data = (uint8_t)USART2->RDR;
	// 	uart_receive_input(data);
	// }

	// ---- 3. 可选IDLE清除 ----
	if (isr & USART_ISR_IDLE)
	{
		volatile uint32_t dump = USART1->RDR;
		(void)dump;
		USART1->ICR = (1 << 4); // IDLECF
	}
}

// void USART2_IRQHandler(void)
// {
// #if (defined _COMMOM_UPPER_SCI2)
// 	Sci2_CommonUpper_FaultChk();
// #endif
// 	// 	if (USART_GetITStatus(USART2, USART_IT_RXNE) != RESET)
// 	// 	{
// 	// 		RTC_ExtComCnt++;

// 	// #ifdef _COMMOM_UPPER_SCI2
// 	// 		Sci2_CommonUpper_Rx_Deal(&g_stCurrentMsgPtr_SCI2);
// 	// #endif
// 	// 	}
// 	RecvByte_4G();
// }

void USART2_IRQHandler(void)
{
	uint32_t isr = USART2->ISR;

	// ---- 1. 错误检测与清除 ----
	if (isr & (USART_ISR_ORE | USART_ISR_FE | USART_ISR_NE | USART_ISR_PE))
	{
		volatile uint32_t dump = USART2->RDR;
		(void)dump;

		// 手动清除错误标志（ICR是写1清零）
		USART2->ICR = (1 << 3) | (1 << 2) | (1 << 1) | (1 << 0);

		// gu16_CommuErrCnt_SCI2++;
		return;
	}

	// ---- 2. 循环读取所有接收到的数据 ----
	while (USART2->ISR & USART_ISR_RXNE)
	{
		uint8_t data = (uint8_t)USART2->RDR;
		uart_receive_input(data);
	}

	// ---- 3. 可选IDLE清除 ----
	if (isr & USART_ISR_IDLE)
	{
		volatile uint32_t dump = USART2->RDR;
		(void)dump;
		USART2->ICR = (1 << 4); // IDLECF
	}
}

#endif
