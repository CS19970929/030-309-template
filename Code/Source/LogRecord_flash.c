#include "main.h"

#define EVENT_RECORD_LENGTH 100

UINT8 BMS_LOG_POINT = 0;
UINT8 BMS_LOG_RECORD[EVENT_RECORD_LENGTH][2]; // 0是事件编号，1是与上一个事件的时间间隔
LOG_RECORD_FLAG LogRecord_Flag;
UINT8 gu8_Reset_EventRecord = 0;

UINT8 LogTime_Map(UINT32 *Time_S_Cnt)
{
	UINT8 result = 0;

	if ((*Time_S_Cnt) <= 60)
	{ // 1min以内
		result = 171;
	}
	else if ((*Time_S_Cnt) <= 3600 * 168)
	{ // 7d以内，按小时算
		result = (*Time_S_Cnt) / 3600 + ((*Time_S_Cnt) % 3600) > 0 ? 1 : 0;
	}
	else
	{
		result = 170; // 大于7d，统一为大于7d
	}

	(*Time_S_Cnt) = 0; // 清零，统计下一个间隔

	return result;
}

void LogEvent_EEPROM(LogEventArray event, UINT32 *Time_S_Cnt)
{
	UINT16 temp = 0;
	union LOG_ITEM_T log;

	if (BMS_LOG_POINT >= EVENT_RECORD_LENGTH)
		BMS_LOG_POINT = 0;
	BMS_LOG_RECORD[BMS_LOG_POINT][0] = event;
	BMS_LOG_RECORD[BMS_LOG_POINT][1] = LogTime_Map(Time_S_Cnt);
	if (event == BMS_START_UP)
		BMS_LOG_RECORD[BMS_LOG_POINT][1] = 0;
	++BMS_LOG_POINT;
	// if(++BMS_LOG_POINT >= 100)BMS_LOG_POINT = 0;		//这样写完蛋了，下面需要减1

	temp = BMS_LOG_RECORD[BMS_LOG_POINT - 1][0] + (BMS_LOG_RECORD[BMS_LOG_POINT - 1][1] << 8);

	log.byte.event = BMS_LOG_RECORD[BMS_LOG_POINT - 1][0];
	log.byte.time  = BMS_LOG_RECORD[BMS_LOG_POINT - 1][1];
	log.byte.index = BMS_LOG_POINT;

	FLASH_Unlock();
	FLASH_ProgramWord(FLASH_ADDR_LOG_FLASH_START + 4 * BMS_LOG_POINT, log.data);
	FLASH_Lock();

	// WriteEEPROM_Word_WithZone(E2P_ADDR_START_EVENT_RECORD + ((BMS_LOG_POINT - 1) << 1), temp);
	// WriteEEPROM_Word_WithZone(E2P_ADDR_E2POS_EVENT_POINT, BMS_LOG_POINT);
}

void LogEvent_Record(UINT8 temp, LogEventArray event, UINT32 *Time_S_Cnt)
{
	static UINT8 su8_Event[EVENT_NUM] = {0};
	static UINT8 su8_CBC_Temp = 0;

	if (BMS_START_UP == event)
	{
		if (LogRecord_Flag.bits.Log_StartUp)
		{ // 直接使用，直接修改，别的不能自己修改
			LogEvent_EEPROM(event, Time_S_Cnt);
			LogRecord_Flag.bits.Log_StartUp = 0;
		}
	}
	else if (BMS_SLEEP == event)
	{
		if (LogRecord_Flag.bits.Log_Sleep)
		{
			LogEvent_EEPROM(event, Time_S_Cnt);
			LogRecord_Flag.bits.Log_Sleep = 0;
			Sleep_Mode.bits.b1_ToSleepFlag = 0; // 释放，进入休眠
		}
	}
	else if (CBC_ERR == event)
	{ // 因为CBC不会被清除，只会一直叠加上去，所以用这个记录
		if (su8_CBC_Temp != temp)
		{ // 有一些会瞬间触发好几次中断，导致CBC加几次，问题不大，因为这个是S级别响应。
			su8_CBC_Temp = temp;
			LogEvent_EEPROM(event, Time_S_Cnt);
		}
	}
	else
	{
		switch (su8_Event[event])
		{
		case 0:
			if (temp)
			{
				LogEvent_EEPROM(event, Time_S_Cnt);
				su8_Event[event] = 1;
				// 这里后续会有EEPROM操作
			}
			break;

		case 1:
			if (!temp)
			{
				su8_Event[event] = 0;
			}
			break;

		default:
			break;
		}
	}
}

void App_LogRecord(void)
{
	UINT8 temp;
	static UINT32 su32_Interval_S_Tcnt = 0;

	if (0 == g_st_SysTimeFlag.bits.b1Sys1000msFlag3)
	{
		return;
	}

	if (gu8_Reset_EventRecord)
	{
		return;
	}

	++su32_Interval_S_Tcnt;

	LogEvent_Record(LogRecord_Flag.bits.Log_StartUp, BMS_START_UP, &su32_Interval_S_Tcnt);
	LogEvent_Record(LogRecord_Flag.bits.Log_Sleep, BMS_SLEEP, &su32_Interval_S_Tcnt);
	// LogEvent_Record(g_stCellInfoReport.u16BalanceFlag1, BALANCE_OPEN, &su32_Interval_S_Tcnt);

	LogEvent_Record(SystemStatus.bits.b1Status_Heat, HEAT_OPEN, &su32_Interval_S_Tcnt);
	LogEvent_Record(SystemStatus.bits.b1Status_Cool, COOL_OPEN, &su32_Interval_S_Tcnt);

	LogEvent_Record(g_stCellInfoReport.unMdlFault_Third.bits.b1CellOvp, VCELL_OVP, &su32_Interval_S_Tcnt);
	LogEvent_Record(g_stCellInfoReport.unMdlFault_Third.bits.b1BatOvp, VBUS_OVP, &su32_Interval_S_Tcnt);
	LogEvent_Record(g_stCellInfoReport.unMdlFault_Third.bits.b1IchgOcp, CHG_OCP, &su32_Interval_S_Tcnt);

	LogEvent_Record(g_stCellInfoReport.unMdlFault_Third.bits.b1CellUvp, VCELL_UVP, &su32_Interval_S_Tcnt);
	LogEvent_Record(g_stCellInfoReport.unMdlFault_Third.bits.b1BatUvp, VBUS_UVP, &su32_Interval_S_Tcnt);
	LogEvent_Record(g_stCellInfoReport.unMdlFault_Third.bits.b1IdischgOcp, DSG_OCP, &su32_Interval_S_Tcnt);

	LogEvent_Record(g_stCellInfoReport.unMdlFault_Third.bits.b1CellChgUtp, CHG_UTP, &su32_Interval_S_Tcnt);
	LogEvent_Record(g_stCellInfoReport.unMdlFault_Third.bits.b1CellDischgUtp, DSG_UTP, &su32_Interval_S_Tcnt);
	LogEvent_Record(g_stCellInfoReport.unMdlFault_Third.bits.b1CellChgOtp, CHG_OTP, &su32_Interval_S_Tcnt);
	LogEvent_Record(g_stCellInfoReport.unMdlFault_Third.bits.b1CellDischgOtp, DSG_OTP, &su32_Interval_S_Tcnt);
	LogEvent_Record(g_stCellInfoReport.unMdlFault_Third.bits.b1VcellDeltaBig, VDELTA_OP, &su32_Interval_S_Tcnt);

	// 以下这3个选项都不会清零，所以只会运行一次。
	// AFE和EEPROM(无复原机制)，出现过一次之后，5min会进入休眠，AFE1因为是++所以可能会有小BUG，但是影响不大。
	LogEvent_Record(System_ERROR_UserCallback(ERROR_STATUS_AFE1), AFE1_ERR, &su32_Interval_S_Tcnt);
	LogEvent_Record(System_ERROR_UserCallback(ERROR_STATUS_AFE2), AFE2_ERR, &su32_Interval_S_Tcnt);
	temp = System_ERROR_UserCallback(ERROR_STATUS_EEPROM_STORE) + System_ERROR_UserCallback(ERROR_STATUS_EEPROM_COM);
	LogEvent_Record(temp, EEPROM_ERR, &su32_Interval_S_Tcnt);

	// 其中如果CBC连续触发三次，关掉驱动功能，但是不会进入休眠，如果没人管，会直到深度休眠
	LogEvent_Record(System_ERROR_UserCallback(ERROR_STATUS_CBC_DSG), CBC_ERR, &su32_Interval_S_Tcnt);

	/*
	switch(su8_VcellOVP) {
		case 0:
			if(g_stCellInfoReport.unMdlFault_Third.bits.b1CellOvp) {
				BMS_LOG_RECORD[BMS_LOG_POINT][0] = VCELL_OVP;
				BMS_LOG_RECORD[BMS_LOG_POINT][1] = LogTime_Map(&su32_Interval_S_Tcnt);
				BMS_LOG_POINT++;
				su8_VcellOVP = 1;
			}
			break;

		case 1:
			if(!g_stCellInfoReport.unMdlFault_Third.bits.b1CellOvp) {
				su8_VcellOVP = 0;
			}
			break;

		default:
			break;
	}
	*/
}

void Sci_ACK_0x03_ReadRegs_EventRecord(UINT8 t_u8BuffTemp[])
{
	UINT16 i, j;
	INT8 k;

	i = 0; // 少了这句话导致上传数值错误，意味着函数内的局部参数初始化不为0

	for (j = 0; j < EVENT_RECORD_LENGTH; j++)
	{
		k = BMS_LOG_POINT - 1 - j;
		if (k < 0)
		{
			k = EVENT_RECORD_LENGTH + k;
		}
		t_u8BuffTemp[i++] = BMS_LOG_RECORD[k][0];
		t_u8BuffTemp[i++] = BMS_LOG_RECORD[k][1];
	}
}

// 感觉在通讯之间reset会很慢呀，先看看这样有没有问题，没问题就算了
// 这样写最方便，不需要搞这么多WriteEEPROM_ByteData_Circle()
void Sci_WrReg_0x06_Reset_EventRecord(struct RS485MSG *s)
{
	UINT8 i;

	UINT16 u16SciRegData = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (0x0001 == u16SciRegData)
	{
		// EEPROM_ResetData_EventRecord_ToDefault();

		for (i = 0; i < EVENT_RECORD_LENGTH; ++i)
		{
			BMS_LOG_RECORD[i][0] = 0;
			BMS_LOG_RECORD[i][1] = 0;
		}
		BMS_LOG_POINT = 0;

		gu8_Reset_EventRecord = EVENT_RECORD_LENGTH;
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_DATA_INVALID;
	}
}

void EEPROM_ResetData_EventRecord_ToDefault(void)
{
	UINT8 i;
	union LOG_ITEM_T log;

	for (i = 0; i < EVENT_RECORD_LENGTH; ++i)
	{
		BMS_LOG_RECORD[i][0] = 0;
		BMS_LOG_RECORD[i][1] = 0;
	}
	BMS_LOG_POINT = 0;

	// for (i = 0; i < EVENT_RECORD_LENGTH; ++i)
	// {
	// 	WriteEEPROM_Word_WithZone(E2P_ADDR_START_EVENT_RECORD + (i << 1), 0);
	// }
	// WriteEEPROM_Word_WithZone(E2P_ADDR_E2POS_EVENT_POINT, BMS_LOG_POINT);
	FLASH_Status result;
	FLASH_Unlock();
	FLASH_ClearFlag(FLASH_FLAG_EOP | FLASH_FLAG_PGERR | FLASH_FLAG_WRPERR);
	while (FLASH_ErasePage(FLASH_ADDR_LOG_FLASH_START) != FLASH_COMPLETE)
		;

	// log.event = HEAT_OPEN;
	// // log.time  = 3600 * 5;
	// log.time = 171;
	// log.index = 0;
	// log.res = 0;
	// FLASH_ProgramWord(FLASH_ADDR_LOG_FLASH_START, (uint32_t)log);
	log.byte.event = HEAT_OPEN;
	log.byte.time = 3;
	log.byte.index = BMS_LOG_POINT;
	FLASH_ProgramWord(FLASH_ADDR_LOG_FLASH_START, log.data);

	log.byte.event = BMS_SLEEP;
	log.byte.time = 7;
	BMS_LOG_POINT++;
	log.byte.index = BMS_LOG_POINT;
	FLASH_ProgramWord(FLASH_ADDR_LOG_FLASH_START + 4 * BMS_LOG_POINT, log.data);

	FLASH_Lock();
}

void ReadEEPROM_EventRecord_Parameters(void)
{
	UINT8 i;
	UINT16 t_u16RdTemp;
	union LOG_ITEM_T log;

	for (i = 0; i < EVENT_RECORD_LENGTH; ++i)
	{
		log.data = FlashReadOneWord(FLASH_ADDR_LOG_FLASH_START + 4 * i);
		if(log.byte.index >= EVENT_RECORD_LENGTH)
		{
			break;
		}
		else
		{
			BMS_LOG_POINT = log.byte.index;
			BMS_LOG_RECORD[i][0] = log.byte.event;
			BMS_LOG_RECORD[i][1] = log.byte.time;
		}
	}
} 
