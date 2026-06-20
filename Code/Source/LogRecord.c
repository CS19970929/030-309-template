#include "main.h"

#define EVENT_RECORD_LENGTH 30

UINT8 BMS_LOG_POINT = 0;
UINT8 BMS_LOG_RECORD[EVENT_RECORD_LENGTH][2]; // 0���¼���ţ�1������һ���¼���ʱ����
LOG_RECORD_FLAG LogRecord_Flag;
UINT8 gu8_Reset_EventRecord = 0;

UINT8 LogTime_Map(UINT32 *Time_S_Cnt)
{
	UINT8 result = 0;

	if ((*Time_S_Cnt) <= 60)
	{ // 1min����
		result = 171;
	}
	else if ((*Time_S_Cnt) <= 3600 * 168)
	{ // 7d���ڣ���Сʱ��
		result = (*Time_S_Cnt) / 3600 + ((*Time_S_Cnt) % 3600) > 0 ? 1 : 0;
	}
	else
	{
		result = 170; // ����7d��ͳһΪ����7d
	}

	(*Time_S_Cnt) = 0; // ���㣬ͳ����һ�����

	return result;
}

void LogEvent_EEPROM(LogEventArray event, UINT32 *Time_S_Cnt)
{
	UINT16 temp = 0;

	if (BMS_LOG_POINT >= EVENT_RECORD_LENGTH)
		BMS_LOG_POINT = 0;
	BMS_LOG_RECORD[BMS_LOG_POINT][0] = event;
	BMS_LOG_RECORD[BMS_LOG_POINT][1] = LogTime_Map(Time_S_Cnt);
	if (event == BMS_START_UP)
		BMS_LOG_RECORD[BMS_LOG_POINT][1] = 0;
	++BMS_LOG_POINT;
	// if(++BMS_LOG_POINT >= 100)BMS_LOG_POINT = 0;		//����д�군�ˣ�������Ҫ��1

	temp = BMS_LOG_RECORD[BMS_LOG_POINT - 1][0] + (BMS_LOG_RECORD[BMS_LOG_POINT - 1][1] << 8);

	WriteEEPROM_Word_NoZone(E2P_ADDR_START_EVENT_RECORD + ((BMS_LOG_POINT - 1) << 1), temp);
	WriteEEPROM_Word_NoZone(E2P_ADDR_E2POS_EVENT_POINT, BMS_LOG_POINT);
}

void LogEvent_Record(UINT8 temp, LogEventArray event, UINT32 *Time_S_Cnt)
{
	static UINT8 su8_Event[EVENT_NUM] = {0};
	static UINT8 su8_CBC_Temp = 0;

	if (BMS_START_UP == event)
	{
		if (LogRecord_Flag.bits.Log_StartUp)
		{ // ֱ��ʹ�ã�ֱ���޸ģ���Ĳ����Լ��޸�
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
			Sleep_Mode.bits.b1_ToSleepFlag = 0; // �ͷţ���������
		}
	}
	else if (CBC_ERR == event)
	{ // ��ΪCBC���ᱻ�����ֻ��һֱ������ȥ�������������¼
		if (su8_CBC_Temp != temp)
		{ // ��һЩ��˲�䴥���ü����жϣ�����CBC�Ӽ��Σ����ⲻ����Ϊ�����S������Ӧ��
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
				// �����������EEPROM����
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

UINT32 su32_Interval_S_Tcnt = 0;
void App_LogRecord(void)
{
	UINT8 temp;

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

	// ������3��ѡ��������㣬����ֻ������һ�Ρ�
	// AFE��EEPROM(�޸�ԭ����)�����ֹ�һ��֮��5min��������ߣ�AFE1��Ϊ��++���Կ��ܻ���СBUG������Ӱ�첻��
	LogEvent_Record(System_ERROR_UserCallback(ERROR_STATUS_AFE1), AFE1_ERR, &su32_Interval_S_Tcnt);
	LogEvent_Record(System_ERROR_UserCallback(ERROR_STATUS_AFE2), AFE2_ERR, &su32_Interval_S_Tcnt);
	temp = System_ERROR_UserCallback(ERROR_STATUS_EEPROM_STORE) + System_ERROR_UserCallback(ERROR_STATUS_EEPROM_COM);
	LogEvent_Record(temp, EEPROM_ERR, &su32_Interval_S_Tcnt);

	// �������CBC�����������Σ��ص��������ܣ����ǲ���������ߣ����û�˹ܣ���ֱ���������
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

	i = 0; // ������仰�����ϴ���ֵ������ζ�ź����ڵľֲ�������ʼ����Ϊ0

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

// �о���ͨѶ֮��reset�����ѽ���ȿ���������û�����⣬û���������
// ����д��㣬����Ҫ����ô��WriteEEPROM_ByteData_Circle()
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

	for (i = 0; i < EVENT_RECORD_LENGTH; ++i)
	{
		BMS_LOG_RECORD[i][0] = 0;
		BMS_LOG_RECORD[i][1] = 0;
	}
	BMS_LOG_POINT = 0;

	for (i = 0; i < EVENT_RECORD_LENGTH; ++i)
	{
		WriteEEPROM_Word_NoZone(E2P_ADDR_START_EVENT_RECORD + (i << 1), 0);
	}
	WriteEEPROM_Word_NoZone(E2P_ADDR_E2POS_EVENT_POINT, BMS_LOG_POINT);
}

/*
��֤һ�����������EEPROM�洢����
1������Ǹô��ϵ���ָ��ŵ��£�5min������������ʧ�����ⲻ��
2�������������ݴ����޷�����������Ȼ��һ�����õ���������չ�����������ȱ���֪���ĸ����ݳ����⣺
A���������־��¼�����⣬Ȼ������Լ�͵͵�޸ģ���ΪNULL��������������Ϣ��������⣬�м�һ��NULL��
B������Ǳ���������⣬�Լ�͵͵�޸�Ĭ��ֵ�������ִ����⣬������ݲ��Բ���������(������Ԫ﮵�Ĭ�ϱ�����Ū���������)��
C������A��B�����ǳ���EEPROM���󣬲���͵͵�޸ģ�Ҫ��Ϊȥ����������Ǳ���������⣬��ˢ�±����㣬�������־�����⣬��ˢ����־��
D����ô��������������۲챣������޸���һҳ��ɣ�������־��¼�����ҵ�����㡣
E��ʵ����BMS����֮���ȥ�������������״������������ˣ�����Ӧ�û���ʧ����Ȼ����Ӳ�����ˡ�

3�������룬�������־������(�´���д���ɰ�����)��Ϊ�˷��㣬ò��͵͵�޸�ΪNULL����Ҳ����
*/
void ReadEEPROM_EventRecord_Parameters(void)
{
	UINT8 i;
	UINT16 t_u16RdTemp;

	BMS_LOG_POINT = ReadEEPROM_Word_NoZone(E2P_ADDR_E2POS_EVENT_POINT);
	if (BMS_LOG_POINT >= 101)
	{ // ���ָ������⣬ȫ��Reset
		System_ERROR_UserCallback(ERROR_EEPROM_STORE);
		EEPROM_ResetData_EventRecord_ToDefault();
	}

	for (i = 0; i < EVENT_RECORD_LENGTH; ++i)
	{
		t_u16RdTemp = ReadEEPROM_Word_NoZone(E2P_ADDR_START_EVENT_RECORD + (i << 1));
		if (((t_u16RdTemp & 0x00FF) <= EVENT_NUM) && (t_u16RdTemp >> 8) <= 171)
		{														  // NULL��ֵ��������0��255��255��Ϊ�˱�İ��Ӳ���Ҫ��ˢһ��EEPROM
			BMS_LOG_RECORD[i][0] = (UINT8)(t_u16RdTemp & 0x00FF); // ���������Ǹ�ȫ��reset��255λNULLֵȡ��
			BMS_LOG_RECORD[i][1] = (UINT8)(t_u16RdTemp >> 8);	  // ����ɰ���Ҳû���⡣
		}
		else
		{
			if (0 == System_ErrFlag.u8ErrFlag_Com_EEPROM)
			{
				System_ERROR_UserCallback(ERROR_EEPROM_STORE);
			}
			BMS_LOG_RECORD[i][0] = 0;
			BMS_LOG_RECORD[i][1] = 0;
			WriteEEPROM_Word_NoZone(E2P_ADDR_START_EVENT_RECORD + (i << 1), 0); // ���ִ�����־����͵͵�����޸�ΪĬ��ֵ����ľ��Բ����ԡ�
		}
	}
}
