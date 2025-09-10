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
void App_MOS_Relay_Control(void);
void MCU_ClockTest(void);

void TwiStart22(void)
{
	TWI_DAT_HIGH;
	TWI_CLK_HIGH;
	TWI_DAT_OUT;
	TWI_CLK_OUT;
	TWI_DAT_LOW;
	__delay_us(4);
	TWI_CLK_LOW;
}

int main(void)
{
	// UINT8 i;
	InitDevice(); // 初始化外设，这两个函数的位置需要斟酌一下，现在换回去先
	InitVar();	  // 初始化变量

	while (1)
	{
#if (defined _DEBUG_CODE)
		App_SysTime();
		// App_RTCSleepTest();
		// App_SleepDeal();

		App_AFEGet();
		App_CommonUpper();

		App_AnlogCal();

		App_SOC();

		App_WarnCtrl();

		// APP_WAKEUP_LCD();

		App_SleepDeal(); // 放在App_MOS_Relay_Control()后面

		APP_LedBar();

		Feed_IWatchDog;

#else
		App_SysTime();
		App_CommonUpper();
		App_AFEGet();
		App_SH367309();
		App_AnlogCal();

		App_E2promDeal();
		App_SleepDeal(); // 放在App_MOS_Relay_Control()后面
		App_SOC();
		App_CellBalance();
		App_WarnCtrl();

		APP_LedBar();

		App_ChargerLoad_Det();
		App_Heat_Cool_Ctrl();

		APP_WAKEUP_LCD();

		App_FlashUpdateDet();
		App_LogRecord();
		App_ProID_Deal();

		// MCUO_DO1_EN = 0;   //低  lcd上拉，检测到高		高 pmos低

		// fixme LCD闪烁
		if (g_st_SysTimeFlag.bits.b1Sys1000msFlag1)
		{
			{
				SuspendFlag1 = SuspendFlag2;
				SuspendFlag2 = RTC_ExtComCnt1;

				if (SuspendFlag1 != SuspendFlag2)
				{
					BlueToothFlag = 1;
				}
				else
				{
					BlueToothFlag = 0;
				}
			}
		}

		Feed_IWatchDog;
#endif
	}
}

void InitDevice(void)
{
	//?????????????
	SystemInit();

	Init_IAPAPP();

#if (defined _DEBUG_CODE)
	IsSleepStartUp();
	InitIO();
	InitDelay();
	InitTimer();
	// InitSystemWakeUp();
	// InitE2PROM(); // 内部EEPROM，不需要初始化
	// Init_RTC();
	InitUSART_CommonUpper();

	// InitData_SOC();
	// InitADC();

	// InitAFE1();

#else
	IsSleepStartUp();
	InitIO();
	InitDelay();
	InitTimer();
	InitSystemWakeUp();
	InitE2PROM(); // 内部EEPROM，不需要初始化
	InitUSART_CommonUpper();
	InitADC();
	InitData_SOC();
	Init_ChargerLoad_Det();
	InitHeat_Cool();
	InitAFE1();

#ifndef _DEBUG_
	Init_IWDG();
#endif // !1

#endif
}

void InitVar(void)
{
	UINT16 i;

	InitSystemMonitorData_EEPROM();
	SystemStatus.bits.b1Status_MOS_DSG = CLOSE;
	SystemStatus.bits.b1StartUpBMS = 0;

	// Switch功能
	// Switch_OnOFF_Func.all = 0;
	// InitSwitchData_EEPROM();

	// 总系统错误监控系统初始化
	for (i = 0; i < ERROR_NUM; ++i)
	{
		//*(&System_ErrFlag.u8ErrFlag_Com_AFE1+i)  =  0;		//有病，这样写就把错误清除了
	}

	// 保护标志位初始化
	g_stCellInfoReport.unMdlFault_First.all = 0;
	g_stCellInfoReport.unMdlFault_Second.all = 0;
	g_stCellInfoReport.unMdlFault_Third.all = 0;
	// 当次保护记录初始化
	FaultPoint_First = 0;
	FaultPoint_Second = 0;
	FaultPoint_Third = 0;

	FaultPoint_First2 = 0;
	FaultPoint_Second2 = 0;
	FaultPoint_Third2 = 0;
	for (i = 0; i < Record_len; ++i)
	{
		Fault_record_First[i] = 0;
		Fault_record_Second[i] = 0;
		Fault_record_Third[i] = 0;

		Fault_record_First2[i] = 0;
		Fault_record_Second2[i] = 0;
		Fault_record_Third2[i] = 0;
	}
	Fault_Flag_Fisrt.all = 0;
	Fault_Flag_Second.all = 0;
	Fault_Flag_Third.all = 0;

	// 继电器驱动开启初始化							//不打算放在这里
	// RelayCtrl_Command = RELAY_PRE_DET;	//不打算放在这里
	// HeatCtrl_Command = ST_HEAT_DET_SELF;
	// CoolCtrl_Command = ST_COOL_DET_SELF;

	// 休眠相关

	// 这样写就不用管前面到底读出来还是复位了(在EEPROM很多个地方算)
	SeriesNum = OtherElement.u16Sys_SeriesNum;
	g_u32CS_Res_AFE = ((UINT32)OtherElement.u16Sys_CS_Res_Num * 1000) / OtherElement.u16Sys_CS_Res;

	SystemStatus.bits.b4Status_ProjectVer = 1;
	LogRecord_Flag.bits.Log_StartUp = 1;
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
	// MCUO_SD_DRV_CHG = 0;

	MCUO_PWSV_STB = 1;

	// MCUO_PWSV_LDO = 1;
	MCUO_PWSV_CTR = 1;

	// MCUO_DRV_WLM_PW = 1;

	MCUO_AFE_CTLC = 0; // 刚上电，默认高阻态，所以不慌AFE刚开机瞬间打开MOS
	// bug fixme 注意
	MCUO_AFE_SHIP = 0;
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
