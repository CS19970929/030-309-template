#include "main.h"
#include "Comm.h"
#include "bsp.h"
#include "Time_Triggered.h"
#include "app_runtime_monitor.h"

UINT8 SeriesNum = 16;

const unsigned char SeriesSelect_AFE1[16][16] = {
	{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
	{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
	{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
	{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
	{0, 1, 2, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
	{0, 1, 2, 3, 4, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
	{0, 1, 2, 3, 4, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0},
	{0, 1, 2, 3, 4, 5, 6, 7, 0, 0, 0, 0, 0, 0, 0, 0},
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 0, 0, 0, 0, 0, 0},
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0},
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0, 0, 0, 0, 0},
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 0, 0, 0},
	{0, 1, 2, 3, 4, 5, 6, 7, 9, 9, 10, 11, 12, 0, 0, 0},
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 0, 0},
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 0},
	{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15}
};

typedef void (*AppTaskHandler)(void);

typedef struct
{
	AppTaskHandler handler;
	UINT16 start_delay;
	UINT16 period;
} AppTaskConfig;

static void InitVar(void);
static void InitDevice(void);
static void App_RunMainLoop(void);
static void App_RunForegroundServices(void);
static void App_InitPlatform(void);
static void App_InitRuntimeState(void);
static void App_ApplyPrechargeStartup(void);
static void App_RegisterTaskTable(const AppTaskConfig *tasks, UINT8 count);
static void App_RegisterScheduledTasks(void);

static const AppTaskConfig g_app_core_tasks[] = {
	{App_AFEGet, 0, 200},
	{App_WarnCtrl, 8, 10},
	{App_AnlogCal, 2, 10},
	{App_SOC, 5, 100},
	{App_LogRecord, 6, 1000},
	{AppRuntimeMonitor_RunTask, 11, 1000},
};

static const AppTaskConfig g_app_release_tasks[] = {
	{App_SleepDeal, 7, 1000},
#ifdef __FUNC__HEAT__
	{App_Heat_Cool_Ctrl, 9, 1000},
#endif
	{App_ChargerLoad_Det, 9, 1000},
};

int main(void)
{
	InitDevice();
	InitVar();
	App_RunMainLoop();
}

static void App_RunMainLoop(void)
{
	while (1)
	{
		SCH_Dispatch_Tasks();
		App_RunForegroundServices();
	}
}

static void App_RunForegroundServices(void)
{
	Comm_PollAll();

#ifndef _DEBUG_CODE
	App_E2promDeal();
	App_FlashUpdateDet();
	App_ProID_Deal();

#ifdef wdog_enable
	Feed_IWatchDog;
#endif
#endif

}

static void InitDevice(void)
{
	App_InitPlatform();
	App_InitRuntimeState();
	App_RegisterScheduledTasks();
}

static void App_InitPlatform(void)
{
	SystemInit();
	Init_IAPAPP();
	InitDelay();
	bsp_Init();
	IsSleepStartUp();
	InitIO();
	InitSystemWakeUp();
	InitE2PROM();
	InitAFE1();

#ifndef _DEBUG_CODE
	App_ApplyPrechargeStartup();
	InitADC();
	InitData_SOC();
	Init_ChargerLoad_Det();
#ifdef __FUNC__HEAT__
	InitHeat_Cool();
#endif
	InitMosRelay_DOx();
#ifdef wdog_enable
	Init_IWDG();
#endif
#endif
}

static void App_InitRuntimeState(void)
{
	Comm_InitAll();
	AppRuntimeMonitor_Init();
}

static void App_ApplyPrechargeStartup(void)
{
	if (OtherElement.u16Sys_PreChg_Time >= 1)
	{
		GPIO_WriteBit(GPIO_RES_EN, PIN_RES_EN, 1);
		if (OtherElement.u16Sys_PreChg_Time > 1000)
		{
			OtherElement.u16Sys_PreChg_Time = 100;
		}
		__delay_ms(OtherElement.u16Sys_PreChg_Time - 1);
		MCUO_AFE_CTLC = 1;
		__delay_ms(1);
		GPIO_WriteBit(GPIO_RES_EN, PIN_RES_EN, 0);
	}
	else
	{
		MCUO_AFE_CTLC = 1;
	}
}

static void App_RegisterTaskTable(const AppTaskConfig *tasks, UINT8 count)
{
	UINT8 i;

	for (i = 0; i < count; ++i)
	{
		SCH_Add_Task(tasks[i].handler, tasks[i].start_delay, tasks[i].period);
	}
}

static void App_RegisterScheduledTasks(void)
{
	App_RegisterTaskTable(g_app_core_tasks, (UINT8)(sizeof(g_app_core_tasks) / sizeof(g_app_core_tasks[0])));

#ifndef _DEBUG_CODE
	App_RegisterTaskTable(g_app_release_tasks, (UINT8)(sizeof(g_app_release_tasks) / sizeof(g_app_release_tasks[0])));
#endif
}

static void InitVar(void)
{
	InitSystemMonitorData_EEPROM();
	SeriesNum = OtherElement.u16Sys_SeriesNum;
	g_u32CS_Res_AFE = ((UINT32)OtherElement.u16Sys_CS_Res_Num * 1000) / OtherElement.u16Sys_CS_Res;

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
	/* 保留当剝唤醒时庝接坣，便于坎�?按硬件版�?扩展�? */
}




