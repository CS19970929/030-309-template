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

#if 0
void USART1_IRQHandler(void)
{
  if (USART_GetITStatus(USART1, USART_IT_RXNE) != RESET)
  {
    RTC_ExtComCnt++;
    RTC_ExtComCnt1++;

#if (defined _COMMOM_UPPER_SCI1)
    Sci1_CommonUpper_FaultChk();
    Sci1_CommonUpper_Rx_Deal(&g_stCurrentMsgPtr_SCI1);
#endif
  }
}

void USART2_IRQHandler(void)
{
  if (USART_GetITStatus(USART2, USART_IT_RXNE) != RESET)
  {
    RTC_ExtComCnt++;

#ifdef _COMMOM_UPPER_SCI2
    Sci2_CommonUpper_FaultChk();
    Sci2_CommonUpper_Rx_Deal(&g_stCurrentMsgPtr_SCI2);
#endif
  }
}

#endif

void USART1_IRQHandler(void)
{
	Sci1_CommonUpper_FaultChk();

	if (USART_GetITStatus(USART1, USART_IT_RXNE) != RESET)
	{
		RTC_ExtComCnt++;
		RTC_ExtComCnt1++;

#ifdef _COMMOM_UPPER_SCI1
		CommonUpper_irq(&g_stCurrentMsgPtr_SCI1);
#endif
	}

	/* 处理发送缓冲区空中断 */
	if (USART_GetITStatus(USART1, USART_IT_TXE) != RESET)
	{
		if (g_stCurrentMsgPtr_SCI1.ptr_no < g_stCurrentMsgPtr_SCI1.AckLenth)
		{
			/* 从发送FIFO取1个字节写入串口发送数据寄存器 */
			USART_SendData(USART1, g_stCurrentMsgPtr_SCI1.u16Buffer[g_stCurrentMsgPtr_SCI1.ptr_no]);
			g_stCurrentMsgPtr_SCI1.ptr_no++;
		}
		else
		{
			if (u8FlashUpdateE2PROM)
			{
				u8FlashUpdateE2PROM = 0;
				u8FlashUpdateFlag = 1;
			}
			{
				// s->csr = RS485_STA_IDLE;
				// s->ptr_no = 0;
				// s->uart->CR1 |= (1 << 2); // 使能接收
				// s->uart->CR1 |= (1 << 5); // 使能接收中断

				g_stCurrentMsgPtr_SCI1.csr = RS485_STA_IDLE;
				g_stCurrentMsgPtr_SCI1.ptr_no = 0;
				g_stCurrentMsgPtr_SCI1.uart->CR1 |= (1 << 2); // 使能接收
				g_stCurrentMsgPtr_SCI1.uart->CR1 |= (1 << 5); // 使能接收中断
			}

			/* 发送缓冲区的数据已取完时， 禁止发送缓冲区空中断 （注意：此时最后1个数据还未真正发送完毕）*/
			USART_ITConfig(USART1, USART_IT_TXE, DISABLE);

			/* 使能数据发送完毕中断 */
			USART_ITConfig(USART1, USART_IT_TC, ENABLE);
		}
	}
	/* 数据bit位全部发送完毕的中断 */
	else if (USART_GetITStatus(USART1, USART_IT_TC) != RESET)
	{
		// if (_pUart->usTxRead == _pUart->usTxWrite)
		// if (_pUart->usTxCount == 0)
		{
			/* 如果发送FIFO的数据全部发送完毕，禁止数据发送完毕中断 */
			USART_ITConfig(USART1, USART_IT_TC, DISABLE);

			// /* 回调函数, 一般用来处理RS485通信，将RS485芯片设置为接收模式，避免抢占总线 */
			// if (_pUart->SendOver)
			// {
			// 	_pUart->SendOver();
			// }
		}
		// else
		{
			// sys_time.error++;
#if 0
			/* 正常情况下，不会进入此分支 */

			/* 如果发送FIFO的数据还未完毕，则从发送FIFO取1个数据写入发送数据寄存器 */
			USART_SendData(_pUart->uart, _pUart->pTxBuf[_pUart->usTxRead]);
			if (++_pUart->usTxRead >= _pUart->usTxBufSize)
			{
				_pUart->usTxRead = 0;
			}
			_pUart->usTxCount--;
#endif
		}
	}
}

void USART2_IRQHandler(void)
{
	Sci2_CommonUpper_FaultChk();

	if (USART_GetITStatus(USART2, USART_IT_RXNE) != RESET)
	{
		RTC_ExtComCnt++;
		// lcd_com_cnt++;
#ifdef _COMMOM_UPPER_SCI2
		CommonUpper_irq(&g_stCurrentMsgPtr_SCI2);
#endif
	}
	/* 处理发送缓冲区空中断 */
	if (USART_GetITStatus(USART2, USART_IT_TXE) != RESET)
	{
		if (g_stCurrentMsgPtr_SCI2.ptr_no < g_stCurrentMsgPtr_SCI2.AckLenth)
		{
			/* 从发送FIFO取1个字节写入串口发送数据寄存器 */
			USART_SendData(USART2, g_stCurrentMsgPtr_SCI2.u16Buffer[g_stCurrentMsgPtr_SCI2.ptr_no]);
			g_stCurrentMsgPtr_SCI2.ptr_no++;
		}
		else
		{
			if (u8FlashUpdateE2PROM)
			{
				u8FlashUpdateE2PROM = 0;
				u8FlashUpdateFlag = 1;
			}
			{
				g_stCurrentMsgPtr_SCI2.csr = RS485_STA_IDLE;
				g_stCurrentMsgPtr_SCI2.ptr_no = 0;
				g_stCurrentMsgPtr_SCI2.uart->CR1 |= (1 << 2); // 使能接收
				g_stCurrentMsgPtr_SCI2.uart->CR1 |= (1 << 5); // 使能接收中断
			}

			/* 发送缓冲区的数据已取完时， 禁止发送缓冲区空中断 （注意：此时最后1个数据还未真正发送完毕）*/
			USART_ITConfig(USART2, USART_IT_TXE, DISABLE);

			/* 使能数据发送完毕中断 */
			USART_ITConfig(USART2, USART_IT_TC, ENABLE);
		}
	}
	/* 数据bit位全部发送完毕的中断 */
	else if (USART_GetITStatus(USART2, USART_IT_TC) != RESET)
	{
		// if (_pUart->usTxRead == _pUart->usTxWrite)
		// if (_pUart->usTxCount == 0)
		{
			/* 如果发送FIFO的数据全部发送完毕，禁止数据发送完毕中断 */
			USART_ITConfig(USART2, USART_IT_TC, DISABLE);

			// /* 回调函数, 一般用来处理RS485通信，将RS485芯片设置为接收模式，避免抢占总线 */
			// if (_pUart->SendOver)
			// {
			// 	_pUart->SendOver();
			// }
		}
		// else
		{
			// sys_time.error++;
#if 0
			/* 正常情况下，不会进入此分支 */

			/* 如果发送FIFO的数据还未完毕，则从发送FIFO取1个数据写入发送数据寄存器 */
			USART_SendData(_pUart->uart, _pUart->pTxBuf[_pUart->usTxRead]);
			if (++_pUart->usTxRead >= _pUart->usTxBufSize)
			{
				_pUart->usTxRead = 0;
			}
			_pUart->usTxCount--;
#endif
		}
	}
}

#if 0
void USART3_IRQHandler(void)
{
	// Sci3_CommonUpper_FaultChk();

	if (USART_GetITStatus(USART3, USART_IT_RXNE) != RESET)
	{
		RTC_ExtComCnt++;

#ifdef _COMMOM_UPPER_SCI3
		CommonUpper_irq(&g_stCurrentMsgPtr_SCI3);
#endif
	}

	/* 处理发送缓冲区空中断 */
	if (USART_GetITStatus(USART3, USART_IT_TXE) != RESET)
	{
		if (g_stCurrentMsgPtr_SCI3.ptr_no < g_stCurrentMsgPtr_SCI3.AckLenth)
		{
			/* 从发送FIFO取1个字节写入串口发送数据寄存器 */
			USART_SendData(USART3, g_stCurrentMsgPtr_SCI3.u16Buffer[g_stCurrentMsgPtr_SCI3.ptr_no]);
			g_stCurrentMsgPtr_SCI3.ptr_no++;
		}
		else
		{
			if (u8FlashUpdateE2PROM)
			{
				u8FlashUpdateE2PROM = 0;
				u8FlashUpdateFlag = 1;
			}
			{
				// s->csr = RS485_STA_IDLE;
				// s->ptr_no = 0;
				// s->uart->CR1 |= (1 << 2); // 使能接收
				// s->uart->CR1 |= (1 << 5); // 使能接收中断

				g_stCurrentMsgPtr_SCI3.csr = RS485_STA_IDLE;
				g_stCurrentMsgPtr_SCI3.ptr_no = 0;
				g_stCurrentMsgPtr_SCI3.uart->CR1 |= (1 << 2); // 使能接收
				g_stCurrentMsgPtr_SCI3.uart->CR1 |= (1 << 5); // 使能接收中断
			}

			/* 发送缓冲区的数据已取完时， 禁止发送缓冲区空中断 （注意：此时最后1个数据还未真正发送完毕）*/
			USART_ITConfig(USART3, USART_IT_TXE, DISABLE);

			/* 使能数据发送完毕中断 */
			USART_ITConfig(USART3, USART_IT_TC, ENABLE);
		}
	}
	/* 数据bit位全部发送完毕的中断 */
	else if (USART_GetITStatus(USART3, USART_IT_TC) != RESET)
	{
		// if (_pUart->usTxRead == _pUart->usTxWrite)
		// if (_pUart->usTxCount == 0)
		{
			/* 如果发送FIFO的数据全部发送完毕，禁止数据发送完毕中断 */
			USART_ITConfig(USART3, USART_IT_TC, DISABLE);

			// /* 回调函数, 一般用来处理RS485通信，将RS485芯片设置为接收模式，避免抢占总线 */
			// if (_pUart->SendOver)
			// {
			// 	_pUart->SendOver();
			// }
		}
		// else
		{
			// sys_time.error++;
#if 0
			/* 正常情况下，不会进入此分支 */

			/* 如果发送FIFO的数据还未完毕，则从发送FIFO取1个数据写入发送数据寄存器 */
			USART_SendData(_pUart->uart, _pUart->pTxBuf[_pUart->usTxRead]);
			if (++_pUart->usTxRead >= _pUart->usTxBufSize)
			{
				_pUart->usTxRead = 0;
			}
			_pUart->usTxCount--;
#endif
		}
	}
}

/******************* (C) COPYRIGHT 2011 STMicroelectronics *****END OF FILE****/

#endif
