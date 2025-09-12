#include "main.h"

struct CBC_ELEMENT CBC_Element;

static INT8 fac_us = 0;	 // us
static INT16 fac_ms = 0; // ms

UINT8 g_u81msCnt = 0;
UINT8 g_u810msClockCnt = 0;
UINT8 g_u81msClockCnt = 0;

UINT8 gu8_200msCnt = 0;
UINT8 gu8_10msCnt = 0;
UINT8 gu8_200msAccClock_Flag = 0;

void IWDG_HaltConfig(void);


void __delay_us(UINT32 nus)
{
	bsp_DelayUS(nus);
}

// 这个是非中断方式的延时，倘若使用中断式延时，在中断中使用延时会出现中断嵌套问题，很容易出错
void __delay_ms(UINT16 ms)
{
	bsp_DelayMS(ms);
}

void InitIO(void)
{
	GPIO_InitTypeDef GPIO_InitStructure;

	//
	RCC_AHBPeriphClockCmd(RCC_AHBPeriph_GPIOA, ENABLE); // 开启GPIOA的外设时钟
	RCC_AHBPeriphClockCmd(RCC_AHBPeriph_GPIOB, ENABLE); // 开启GPIOB的外设时钟
	RCC_AHBPeriphClockCmd(RCC_AHBPeriph_GPIOC, ENABLE); // 开启GPIOC的外设时钟
	// RCC_AHBPeriphClockCmd(RCC_AHBPeriph_GPIOD, ENABLE); // 开启GPIOB的外设时钟
	// RCC_AHBPeriphClockCmd(RCC_AHBPeriph_GPIOE, ENABLE); // 开启GPIOB的外设时钟
	RCC_AHBPeriphClockCmd(RCC_AHBPeriph_GPIOF, ENABLE); // 开启GPIOF的外设时钟

	// PA1_MCUO_AFE_VPRO
	GPIO_InitStructure.GPIO_Pin = GPIO_Pin_1 | GPIO_Pin_8;
	// GPIO_InitStructure.GPIO_Pin = GPIO_Pin_1;
	GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
	GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
	GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
	GPIO_Init(GPIOA, &GPIO_InitStructure);
	// ctr 休眠置底   mos温度
	GPIO_SetBits(GPIOA, GPIO_Pin_8);

	// PB1_PWSV_STB，PB2_LED1，PB5_PWSV_LDO，PB15_PWSV_CTR，PB6_MCUO_DRV_WLM_PW
	// GPIO_InitStructure.GPIO_Pin = GPIO_Pin_1 | GPIO_Pin_5 | GPIO_Pin_6 | GPIO_Pin_2 | GPIO_Pin_15;
	// GPIO_InitStructure.GPIO_Pin = GPIO_Pin_1 | GPIO_Pin_5 | GPIO_Pin_6 | GPIO_Pin_2 | GPIO_Pin_15;
	// GPIO_InitStructure.GPIO_Pin |= GPIO_Pin_12 | GPIO_Pin_13;
	// GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
	// GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
	// GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
	// GPIO_Init(GPIOB, &GPIO_InitStructure);
	{
		// PB1_PWSV_STB，PB2_LED1，PB15_PWSV_CTR，PB6_MCUO_DRV_WLM_PW
		// TODO PB5替换为SOC-DI		PB6替换为LOAD-OL	PB15替换为BLE-EN
		GPIO_InitStructure.GPIO_Pin = GPIO_Pin_1 | GPIO_Pin_2 | GPIO_Pin_15;
		// GPIO_InitStructure.GPIO_Pin |= GPIO_Pin_12 | GPIO_Pin_13;
		GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
		GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
		GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
		GPIO_Init(GPIOB, &GPIO_InitStructure);
	}
	// AFE
	GPIO_InitStructure.GPIO_Pin = GPIO_Pin_12 | GPIO_Pin_13 | GPIO_Pin_14;
	GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
	GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
	GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
	GPIO_Init(GPIOB, &GPIO_InitStructure);

	// TODO M-DI
	GPIO_InitStructure.GPIO_Pin = GPIO_Pin_13; // 选择要用的GPIO引脚
	GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IN;
	GPIO_InitStructure.GPIO_PuPd = GPIO_PuPd_NOPULL; // 设置引脚模式为上拉输入模式
	GPIO_Init(GPIOC, &GPIO_InitStructure);

	// DO唤醒
	{
		GPIO_InitStructure.GPIO_Pin = PIN_DO1_EN;
		GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
		GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
		GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
		GPIO_Init(PORT_DO1_EN, &GPIO_InitStructure);

		MCUO_DO1_EN = 0;
	}

	// soc提前初始化	解决20%问题，暂时没时间找问题出在哪儿
	GPIO_InitStructure.GPIO_Pin = PIN_SOC_20 | PIN_SOC_40 | PIN_SOC_60 | PIN_SOC_80 | PIN_SOC_100;
	GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;	 // 推挽输出
	GPIO_InitStructure.GPIO_Speed = GPIO_Speed_2MHz; // IO口速度为2MHz
	GPIO_Init(PORT_SOC_20, &GPIO_InitStructure);
	GPIO_Init(PORT_SOC_100, &GPIO_InitStructure);

	GPIO_InitStructure.GPIO_Pin = PIN_SOC_RUN | PIN_SOC_ALM;
	GPIO_Init(PORT_SOC_RUN, &GPIO_InitStructure);

	GPIO_InitStructure.GPIO_Pin = PIN_SOC_BLE;
	GPIO_Init(PORT_SOC_BLE, &GPIO_InitStructure);

	GPIO_InitStructure.GPIO_Pin = PIN_SOC_KEY; // 选择要用的GPIO引脚,PA0也可以唤醒
	GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IN;
	// GPIO_InitStructure.GPIO_Mode = GPIO_PuPd_NOPULL;
	GPIO_Init(PORT_SOC_KEY, &GPIO_InitStructure);

	// LedBar_Command = LED_BAR_NORMAL;

	// MCUO_SOC_BLE = 0;

	// MCUO_DO1_EN = 0;
}

// 使用LSI，38KHz
void Init_IWDG(void)
{
	RCC_APB1PeriphClockCmd(RCC_APB1Periph_PWR, ENABLE); // 使能PWR外设时钟，待机模式，RTC，看门狗
	IWDG_WriteAccessCmd(IWDG_WriteAccess_Enable);		// 打开独立看门狗寄存器操作权限
	IWDG_SetPrescaler(IWDG_Prescaler_64);				// 预分频系数
	IWDG_SetReload(160);								// 设置重载计数值，k = Xms / (1 / (40KHz/64)) = X/64*40; 4096最高
														// 800——1.28s，80——128ms
	IWDG_ReloadCounter();								// 喂狗
	IWDG_Enable();										// 使能IWDG
}