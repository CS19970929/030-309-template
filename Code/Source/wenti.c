
void InitSCI2_CommonUpper(void)
{
    GPIO_InitTypeDef GPIO_InitStructure;
    USART_InitTypeDef USART_InitStructure;
    NVIC_InitTypeDef NVIC_InitStructure;

    RCC_APB1PeriphClockCmd(RCC_APB1Periph_USART2, ENABLE);
    // RCC->AHBENR |= 1<<17;										//开启GPIOA的外设时钟

    // Enable the USART2 Interrupt(使能USART2中断)
    NVIC_InitStructure.NVIC_IRQChannel = USART2_IRQn;
    NVIC_InitStructure.NVIC_IRQChannelPriority = 0;
    NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE;
    NVIC_Init(&NVIC_InitStructure);

    // USART2_TX -> PA9 , USART2_RX -> PA3
    GPIO_PinAFConfig(GPIOA, GPIO_PinSource2, GPIO_AF_1); // 030的AF表格在非reg的datasheet里
    GPIO_PinAFConfig(GPIOA, GPIO_PinSource3, GPIO_AF_1);
    GPIO_InitStructure.GPIO_Pin = GPIO_Pin_2 | GPIO_Pin_3;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_AF;
    GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
    GPIO_InitStructure.GPIO_PuPd = GPIO_PuPd_UP;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_2MHz;
    GPIO_Init(GPIOA, &GPIO_InitStructure);

    // 串口初始化
    USART_InitStructure.USART_BaudRate = 115200;                                    // 设置串口波特率
    USART_InitStructure.USART_WordLength = USART_WordLength_8b;                     // 设置数据位
    USART_InitStructure.USART_StopBits = USART_StopBits_1;                          // 设置停止位
    USART_InitStructure.USART_Parity = USART_Parity_No;                             // 设置效验位
    USART_InitStructure.USART_HardwareFlowControl = USART_HardwareFlowControl_None; // 设置流控制
    USART_InitStructure.USART_Mode = USART_Mode_Rx | USART_Mode_Tx;                 // 设置工作模式
    USART_Init(USART2, &USART_InitStructure);                                       // 配置入结构体

    USART2->CR3 |= 1 << 0;  // EIE，开帧错误中断，同时开启噪声中断
    USART2->CR3 |= 1 << 11; // 未被使能前改写，禁止噪声中断

    USART_Cmd(USART2, ENABLE);                     // 使能串口1
    USART_ITConfig(USART2, USART_IT_RXNE, ENABLE); // 使能接收中断

    g_stCurrentMsgPtr_SCI2.uart = USART2;
    Sci_DataInit(&g_stCurrentMsgPtr_SCI2);
}

void InitDevice(void)
{
	SystemInit();
	//SystemCoreClockUpdate();
	Init_IAPAPP();

#if (defined _DEBUG_CODE)
	InitDelay();
#else
	InitDelay();
	IsSleepStartUp();
	
	InitIO();
	InitTimer();
	InitSystemWakeUp();
	InitE2PROM(); // 内部EEPROM，不需要初始化
	InitAFE1();
	InitADC();
	InitData_SOC();
	Init_ChargerLoad_Det();
#ifdef __FUNC__HEAT__
	InitHeat_Cool();
#endif
	InitMosRelay_DOx();
	InitUSART_CommonUpper();
	cellular_protocol_init();
	USART_ITConfig(USART2, USART_IT_RXNE, ENABLE); // 使能接收中断
	// bsp_Init();

#ifdef wdog_enable
	Init_IWDG();
#endif // !1

#endif
}

void InitTimer(void)
{
	TIM_TimeBaseInitTypeDef TIM_TimeBaseStructure;
	NVIC_InitTypeDef NVIC_InitStructure;

	// RCC_APB1PeriphClockCmd(RCC_APB1Periph_TIM6, ENABLE);		//时钟3使能
	RCC_APB2PeriphClockCmd(RCC_APB2Periph_TIM17, ENABLE);

	// 定时器初始化
	TIM_TimeBaseStructure.TIM_Period = 500 - 1;							 // 设置在下一个更新事件装入活动的自动重装载寄存器周期的值
	TIM_TimeBaseStructure.TIM_Prescaler = SystemCoreClock / 1000000 - 1; // 设置用来作为TIMx时钟频率除数的预分频值——计数分频
	// TIM_TimeBaseStructure.TIM_Prescaler = 1; 					//设置用来作为TIMx时钟频率除数的预分频值——计数分频
	// TIM_TimeBaseStructure.TIM_Prescaler = 16;
	TIM_TimeBaseStructure.TIM_ClockDivision = TIM_CKD_DIV1;		// 设置时钟分割:TDTS = Tck_tim——时钟分频
	TIM_TimeBaseStructure.TIM_CounterMode = TIM_CounterMode_Up; // TIM向上计数模式
	TIM_TimeBaseInit(TIM17, &TIM_TimeBaseStructure);			// 根据指定的参数初始化TIMx的时间基数单位
	TIM_ITConfig(TIM17, TIM_IT_Update, ENABLE);					// 使能指定中断,允许更新中断

	/*中断嵌套设计*/
	NVIC_InitStructure.NVIC_IRQChannel = TIM17_IRQn;
	NVIC_InitStructure.NVIC_IRQChannelPriority = 0; // 抢占优先级0级，没响应优先级
	NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE; // IRQ通道被使能
	NVIC_Init(&NVIC_InitStructure);

	TIM_Cmd(TIM17, ENABLE); // 使能TIMx
}

void TIM17_IRQHandler(void)
{
	if (TIM_GetITStatus(TIM17, TIM_IT_Update) != RESET)
	{												 // 检查TIM3更新中断发生与否
		TIM_ClearITPendingBit(TIM17, TIM_IT_Update); // 清除TIMx更新中断标志
		if ((++g_u81msCnt) >= 2)
		{ // 1ms
			// Display_ScanTask();

			g_u81msCnt = 0;
			g_u81msClockCnt++;
			gu8_200msCnt++;

			if (g_u81msClockCnt >= 2)
			{ // 2ms
				g_u81msClockCnt = 0;
				g_u810msClockCnt++;
				if (g_u810msClockCnt >= 5)
				{ // 10ms
					g_u810msClockCnt = 0;
				}
			}

			if (gu8_200msCnt >= 200)
			{
				gu8_200msCnt = 0;
				gu8_200msAccClock_Flag = 1;
			}
		}
	}
}

void SendByte_4G(unsigned char data)
{
    // while ((USART3->SR & USART_FLAG_TXE) != USART_FLAG_TXE)
    //     ;
    // USART3->DR = data;

    while (!((USART2->ISR) & (1 << 7)))
        ;               // 1<<6 也可以
    USART2->TDR = data; // load data
}

void RecvByte_4G(void)
{
    unsigned char Res = 0;

    if ((USART2->ISR & USART_IT_RXNE) != 0)
    {
        Res = USART2->RDR;
        uart_receive_input(Res);
    }
}


void USART2_IRQHandler(void)
{
#if (defined _COMMOM_UPPER_SCI2)
	Sci2_CommonUpper_FaultChk();
#endif
	// 	if (USART_GetITStatus(USART2, USART_IT_RXNE) != RESET)
	// 	{
	// 		RTC_ExtComCnt++;

	// #ifdef _COMMOM_UPPER_SCI2
	// 		Sci2_CommonUpper_Rx_Deal(&g_stCurrentMsgPtr_SCI2);
	// #endif
	// 	}
	RecvByte_4G();
}

void Sci2_CommonUpper_FaultChk(void)
{
	UINT8 FaultCnt = 0;

	if (USART2->ISR & 0x08)
	{						   // 接收溢出错误，RXNEIE或EIE使能产生中断，开
		USART2->ICR |= 1 << 3; // 清除
		FaultCnt++;
	}

	if (USART2->ISR & 0x04)
	{						   // 检测到噪声，默认开，不开的话CR3的ONEBIT置1，不开
							   // USART_CR3的EIE使能中断
		USART2->ICR |= 1 << 2; // 清除
		FaultCnt++;
	}

	if (USART2->ISR & 0x02)
	{						   // 帧错误，USART_CR3的EIE使能中断，开
		USART2->ICR |= 1 << 1; // 清除
		FaultCnt++;
	}

	if (USART2->ISR & 0x01)
	{						   // 校验错误标志 USART_CR1的PEIE使能该中断，不开
		USART2->ICR |= 1 << 0; // 清除
		FaultCnt++;
	}

	if (FaultCnt)
	{
		gu16_CommuErrCnt_SCI2++;
	}
}


int main(void)
{
    InitDevice(); // 初始化外设，这两个函数的位置需要斟酌一下，现在换回去先
    InitVar();    // 初始化变量

    while (1)
    {
#if (defined _DEBUG_CODE)
        App_SysTime();
#else
		App_SysTime();
		// test_main();
		// App_SOC();
		cellular_uart_service();

    }
}
