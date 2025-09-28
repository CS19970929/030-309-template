#include "main.h"

UINT8 SeriesNum = 16;

// 不同串数维护的表格
// 中颖
const unsigned char SeriesSelect_AFE1[16][16] = {
	{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},	   // 1串
	{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},	   // 2串
	{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},	   // 3
	{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},	   // 4
	{0, 1, 2, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},	   // 5
	{0, 1, 2, 3, 4, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},	   // 6
	{0, 1, 2, 3, 4, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0},	   // 7
	{0, 1, 2, 3, 4, 5, 6, 7, 0, 0, 0, 0, 0, 0, 0, 0},	   // 8
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 0, 0, 0, 0, 0, 0},	   // 9
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0},	   // 10
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0, 0, 0, 0, 0},	   // 11
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 0, 0, 0},	   // 12
	{0, 1, 2, 3, 4, 5, 6, 7, 9, 9, 10, 11, 12, 0, 0, 0},   // 13
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 0, 0},  // 14
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 0}, // 15
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15} // 16
};

void InitVar(void);
void InitDevice(void);
void InitSystemWakeUp(void);

int main(void)
{
	InitDevice(); // 初始化外设，这两个函数的位置需要斟酌一下，现在换回去先
	InitVar();	  // 初始化变量

	while (1)
	{
#if (defined _DEBUG_CODE)
		App_SysTime();
		App_AFEGet();
		App_CommonUpper();
		App_AnlogCal();
		App_SOC();
		App_WarnCtrl();
		App_SleepDeal(); // 放在App_MOS_Relay_Control()后面
		APP_LedBar();
		Feed_IWatchDog;
#else
		App_SysTime();
		App_CommonUpper();
		App_AFEGet();
		// SOC_LED_Update();
		App_SH367309();
		App_AnlogCal();

		// App_E2promDeal();
		App_SleepDeal(); // 放在App_MOS_Relay_Control()后面
		App_SOC();
		App_CellBalance();
		App_WarnCtrl();
		App_MOS_Relay_Ctrl();

		APP_LedBar();

		// App_ChargerLoad_Det();
		// App_Heat_Cool_Ctrl();

		App_FlashUpdateDet();
		App_LogRecord();
		// App_ProID_Deal();

#ifdef wdog_enable
		Feed_IWatchDog;
#endif

#endif
	}
}

void InitDevice(void)
{
	SystemInit();

	Init_IAPAPP();

#if (defined _DEBUG_CODE)
	IsSleepStartUp();
	InitIO();
	InitDelay();
	InitTimer();
	// InitSystemWakeUp();
	InitUSART_CommonUpper();
#else
	InitDelay();
	IsSleepStartUp();
	InitIO();

	InitTimer();
	// InitE2PROM(); // 内部EEPROM，不需要初始化
	LoadParam();
	InitSystemWakeUp();
	InitUSART_CommonUpper();
	InitADC();
	InitData_SOC();
	// Init_ChargerLoad_Det();
	// InitHeat_Cool();
	InitAFE1();
	InitMosRelay_DOx();

	MCU_GetResetType();

	UpdateVoltageFromBqMaximo();
	DataLoad_CellVolt();
	DataLoad_CellVoltMaxMinFind();
	DataLoad_Temperature();
	DataLoad_TemperatureMaxMinFind();
	DataLoad_Current();

	sys_time.sample_voltage = (float)g_stCellInfoReport.u16VCellTotle / 100;
	SOC_LED_Init(sys_time.sample_voltage);
	Board_PowerOn(); // 上电动画
	// FlashEEPROM_Init();

#ifdef wdog_enable
	Init_IWDG();
#endif // !1

#endif
}

void InitVar(void)
{
	InitSystemMonitorData_EEPROM();
	SeriesNum = g_tParam.other.u16Sys_SeriesNum;
	g_u32CS_Res_AFE = ((UINT32)g_tParam.other.u16Sys_CS_Res_Num * 1000) / g_tParam.other.u16Sys_CS_Res;

	// SystemStatus.bits.b4Status_ProjectVer = 1;
	LogRecord_Flag.bits.Log_StartUp = 1;
	SystemStatus.bits.b1StartUpBMS = 0;
}

void App_WakeUpAFE(void)
{
}

UINT8 App_AFEshutdown(void)
{
	return 0;
}

void InitSystemWakeUp(void)
{
	// MCUO_AFE_SHIP = 0;
	MCUO_AFE_MODE = 0;

	__delay_ms(10);
}

void MCU_ClockTest(void)
{
	GPIO_InitTypeDef GPIO_InitStructure;

	RCC_APB2PeriphClockCmd(RCC_APB2Periph_DBGMCU, ENABLE);

	/*!< Configure sEE_I2C pins: SDA */
	GPIO_InitStructure.GPIO_Pin = GPIO_Pin_8;
	GPIO_InitStructure.GPIO_Mode = GPIO_Mode_AF;
	GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
	GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
	GPIO_Init(GPIOA, &GPIO_InitStructure);

	GPIO_PinAFConfig(GPIOA, GPIO_PinSource8, GPIO_AF_0); // 这个AF选项找芯片手册非reg版

	RCC->CFGR |= RCC_CFGR_MCO_SYSCLK;
	// RCC->CFGR |= RCC_CFGR_MCO_HSE;
}
