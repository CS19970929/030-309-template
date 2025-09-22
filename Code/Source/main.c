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
		App_SH367309();
		App_AnlogCal();

		App_E2promDeal();
		App_SleepDeal(); // 放在App_MOS_Relay_Control()后面
		App_SOC();
		App_CellBalance();
		App_WarnCtrl();
		App_MOS_Relay_Ctrl();

		// APP_LedBar();

		// App_ChargerLoad_Det();
		App_Heat_Cool_Ctrl();

		App_FlashUpdateDet();
		App_LogRecord();
		App_ProID_Deal();

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
	InitSystemWakeUp();
	InitE2PROM(); // 内部EEPROM，不需要初始化
	InitUSART_CommonUpper();
	InitADC();
	InitData_SOC();
	Init_ChargerLoad_Det();
	// InitHeat_Cool();
	InitAFE1();
	InitMosRelay_DOx();

#ifdef wdog_enable
	Init_IWDG();
#endif // !1

#endif
}

void InitVar(void)
{
	InitSystemMonitorData_EEPROM();
	SeriesNum = OtherElement.u16Sys_SeriesNum;
	g_u32CS_Res_AFE = ((UINT32)OtherElement.u16Sys_CS_Res_Num * 1000) / OtherElement.u16Sys_CS_Res;

	SystemStatus.bits.b4Status_ProjectVer = 1;
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
	MCUO_PWSV_STB = 1;
	MCUO_PWSV_CTR = 1;
	MCUO_AFE_CTLC = 1; // 刚上电，默认高阻态，所以不慌AFE刚开机瞬间打开MOS
	MCUO_AFE_SHIP = 0;
	MCUO_AFE_MODE = 0;
	__delay_ms(10);
}
