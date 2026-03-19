这是我目前的串口框架
#include "main.h"

struct RS485MSG g_stCurrentMsgPtr_SCI1;
UINT16 gu16_CommuErrCnt_SCI1 = 0; // SCI通信异常计数
UINT8 gu8_TxEnable_SCI1 = 0;
UINT8 gu8_TxFinishFlag_SCI1 = 0;

struct RS485MSG g_stCurrentMsgPtr_SCI2;
UINT16 gu16_CommuErrCnt_SCI2 = 0; // SCI通信异常计数
UINT8 gu8_TxEnable_SCI2 = 0;
UINT8 gu8_TxFinishFlag_SCI2 = 0;

UINT8 g_u8SCITxBuff[SCI_TX_BUF_LEN];

struct stCell_Info g_stCellInfoReport;
UINT8 u8FlashUpdateFlag = 0;
UINT8 u8FlashUpdateE2PROM = 0;

UINT8 BlueToothFlag = 0; // 用于判断蓝牙是否在显示

UINT8 RTC_ExtComCnt1 = 0;
uint16_t SuspendFlag1 = 0;
uint16_t SuspendFlag2 = 0;

void Sci_WrRegs_0x10_CalibCoef(UINT16 u16Channel, struct RS485MSG *s);
void Sci_WrRegs_0x10_Protect(UINT16 u16Channel, struct RS485MSG *s);
void Sci_WrRegs_0x10_SocTable(struct RS485MSG *s);
void Sci_WrRegs_0x10_CopperLoss(struct RS485MSG *s);
void Sci_WrRegs_0x10_RTC(struct RS485MSG *s);
void Sci_WrRegs_0x10_Balance(struct RS485MSG *s);
void Sci_WrRegs_0x10_SysOther(struct RS485MSG *s);
void Sci_WrRegs_0x10_SleepElement(struct RS485MSG *s);
void Sci_WrRegs_0x10_SocElement(struct RS485MSG *s);
void Sci_WrRegs_0x10_SystemElement(struct RS485MSG *s);
void Sci_WrRegs_0x10_HeatCoolElement(struct RS485MSG *s);
void Sci_WrRegs_0x10_FlashConnect(struct RS485MSG *s);
void Sci_WrRegs_0x10_SN_Version(UINT16 startADDR, struct RS485MSG *s);

void Sci_WrReg_0x06_Reset_CalibCoef(struct RS485MSG *s);
void Sci_WrReg_0x06_Reset_ProtectRecord(struct RS485MSG *s);
void Sci_WrReg_0x06_Reset_ProtectElement(struct RS485MSG *s);
void Sci_WrReg_0x06_Reset_OtherCanAdd(struct RS485MSG *s);
void Sci_WrReg_0x06_Reset_HeatCool(struct RS485MSG *s);
void Sci_WrReg_0x06_SwitchON(struct RS485MSG *s);
void Sci_WrReg_0x06_SwitchOFF(struct RS485MSG *s);
void Sci_WrReg_0x06_BMS_FunctionON(struct RS485MSG *s);
void Sci_WrReg_0x06_BMS_FunctionOFF(struct RS485MSG *s);
void Sci_WrReg_0x06_SetSocOnce(struct RS485MSG *s);

void Sci_DataInit(struct RS485MSG *s)
{
	UINT16 i;

	s->ptr_no = 0;
	s->csr = RS485_STA_IDLE;
	s->enRs485CmdType = RS485_CMD_READ_REGS;
	for (i = 0; i < RS485_MAX_BUFFER_SIZE; i++)
	{
		s->u16Buffer[i] = 0;
	}
	for (i = 0; i < SCI_TX_BUF_LEN; i++)
	{
		g_u8SCITxBuff[i] = 0;
	}
}

void CRC_verify(struct RS485MSG *s)
{
	UINT16 u16SciVerify;
	UINT16 t_u16FrameLenth;

	t_u16FrameLenth = s->ptr_no - 2;
	u16SciVerify = s->u16Buffer[t_u16FrameLenth] + (s->u16Buffer[t_u16FrameLenth + 1] << 8);
	if (u16SciVerify == Sci_CRC16RTU((UINT8 *)s->u16Buffer, t_u16FrameLenth))
	{
		s->AckType = RS485_ACK_POS;
	}
	else
	{
		s->u16RdRegByteNum = 0;
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_CRC_ERROR;
	}
}

void Sci_Deal_ReadRegs_0x03(struct RS485MSG *s)
{
	UINT16 t_u16Temp;

	t_u16Temp = s->u16Buffer[3] + (s->u16Buffer[2] << 8);
	s->u16RdRegStartAddrActure = t_u16Temp;

	if (t_u16Temp >= RS485_ADDR_RO_START2)
	{ // 1个字
		t_u16Temp -= (RS485_ADDR_RO_START2 - 63 - 33);
	}

	else if (t_u16Temp >= RS485_ADDR_RO_START1)
	{ // 33个字
		t_u16Temp -= (RS485_ADDR_RO_START1 - 63);
	}

	else if (t_u16Temp >= RS485_ADDR_RO_START0)
	{ // 63个字
		t_u16Temp -= RS485_ADDR_RO_START0;
	}
	// 新加进来的
	else if (t_u16Temp >= RS485_ADDR_RO_LCD)
	{
		t_u16Temp -= RS485_ADDR_RO_LCD; // LCD，有一次顺序乱了，显示数据不对导致找不到原因
	}
	else if (t_u16Temp >= RS485_ADDR_RW_AFE_PARAMETER)
	{
		t_u16Temp -= RS485_ADDR_RW_AFE_PARAMETER; // AFE，耕耘代码添加，前面出问题是忘了这里要添加
	}
	else if (t_u16Temp >= RS485_ADDR_RW_OTHER_CANADD)
	{
		t_u16Temp -= RS485_ADDR_RW_OTHER_CANADD;
	}
	else if (t_u16Temp >= RS485_ADDR_RW_OTHER)
	{
		t_u16Temp -= RS485_ADDR_RW_OTHER;
	}
	else if (t_u16Temp >= RS485_ADDR_RW_PORTECT)
	{
		t_u16Temp -= RS485_ADDR_RW_PORTECT;
	}
	else if (t_u16Temp >= RS485_ADDR_RW_CALIB)
	{
		t_u16Temp -= RS485_ADDR_RW_CALIB;
	}

	s->u16RdRegStartAddr = t_u16Temp;
	s->u16RdRegByteNum = (s->u16Buffer[5] + (s->u16Buffer[4] << 8)) << 1;
}

void Sci_Deal_WrReg_0x06(struct RS485MSG *s)
{
	UINT16 u16SciRegAddr;
	u16SciRegAddr = s->u16Buffer[3] + (s->u16Buffer[2] << 8);
	switch (u16SciRegAddr)
	{
	case RS485_CMD_ADDR_RESET_CALIB_COEF:
		Sci_WrReg_0x06_Reset_CalibCoef(s);
		break;

	case RS485_CMD_ADDR_RESET_PROTECT_RECORD:
		Sci_WrReg_0x06_Reset_ProtectRecord(s);
		break;

	case RS485_CMD_ADDR_RESET_PROTECT_ELEMENT:
		Sci_WrReg_0x06_Reset_ProtectElement(s);
		break;

	case RS485_CMD_ADDR_RESET_OTHER_CANADD:
		Sci_WrReg_0x06_Reset_OtherCanAdd(s);
		break;

	case RS485_CMD_ADDR_RESET_HEAT_COOL:
		Sci_WrReg_0x06_Reset_HeatCool(s);
		break;

	case RS485_CMD_ADDR_SWITCH_ON:
		Sci_WrReg_0x06_SwitchON(s);
		break;

	case RS485_CMD_ADDR_SWITCH_OFF:
		Sci_WrReg_0x06_SwitchOFF(s);
		break;

	case RS485_CMD_ADDR_SYSTEM_FUNCTION_ON:
		Sci_WrReg_0x06_BMS_FunctionON(s);
		break;

	case RS485_CMD_ADDR_SYSTEM_FUNCTION_OFF:
		Sci_WrReg_0x06_BMS_FunctionOFF(s);
		break;

	case RS485_CMD_ADDR_SET_ONCE_SOC:
		Sci_WrReg_0x06_SetSocOnce(s);
		break;

	// 中颖AFE参数可读可写新增
	case RS485_CMD_ADDR_RESET_AFE_PARAMETERS:
		Sci_WrReg_0x06_Reset_AFE_Parameters(s);
		break;

	case RS485_CMD_ADDR_RESET_EVENT_RECORD:
		Sci_WrReg_0x06_Reset_EventRecord(s);
		break;

	default:
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_NO_PERMISSION;
		break;
	}
}

// 主体OK
void Sci_Deal_WrRegs_0x10(struct RS485MSG *s)
{
	UINT16 u16SciRegStartAddr;
	u16SciRegStartAddr = s->u16Buffer[3] + (s->u16Buffer[2] << 8);

	// if (Sci_WrRegs_0x10_AFE_Parameters(u16SciRegStartAddr, s))
	// {
	// 	return;
	// }

	switch (u16SciRegStartAddr)
	{
	case RS485_CMD_ADDR_VC1CALIB_K:
	case RS485_CMD_ADDR_VC2CALIB_K:
	case RS485_CMD_ADDR_VC3CALIB_K:
	case RS485_CMD_ADDR_VC4CALIB_K:
	case RS485_CMD_ADDR_VC5CALIB_K:
	case RS485_CMD_ADDR_VC6CALIB_K:
	case RS485_CMD_ADDR_VC7CALIB_K:
	case RS485_CMD_ADDR_VC8CALIB_K:
	case RS485_CMD_ADDR_VC9CALIB_K:
	case RS485_CMD_ADDR_VC10CALIB_K:
	case RS485_CMD_ADDR_VC11CALIB_K:
	case RS485_CMD_ADDR_VC12CALIB_K:
	case RS485_CMD_ADDR_VC13CALIB_K:
	case RS485_CMD_ADDR_VC14CALIB_K:
	case RS485_CMD_ADDR_VC15CALIB_K:
	case RS485_CMD_ADDR_VC16CALIB_K:
	case RS485_CMD_ADDR_VC17CALIB_K:
	case RS485_CMD_ADDR_VC18CALIB_K:
	case RS485_CMD_ADDR_VC19CALIB_K:
	case RS485_CMD_ADDR_VC20CALIB_K:
	case RS485_CMD_ADDR_VC21CALIB_K:
	case RS485_CMD_ADDR_VC22CALIB_K:
	case RS485_CMD_ADDR_VC23CALIB_K:
	case RS485_CMD_ADDR_VC24CALIB_K:
	case RS485_CMD_ADDR_VC25CALIB_K:
	case RS485_CMD_ADDR_VC26CALIB_K:
	case RS485_CMD_ADDR_VC27CALIB_K:
	case RS485_CMD_ADDR_VC28CALIB_K:
	case RS485_CMD_ADDR_VC29CALIB_K:
	case RS485_CMD_ADDR_VC30CALIB_K:
	case RS485_CMD_ADDR_VC31CALIB_K:
	case RS485_CMD_ADDR_VC32CALIB_K:
	case RS485_CMD_ADDR_AFE1CALIB_K:
	case RS485_CMD_ADDR_AFE2CALIB_K:
	case RS485_CMD_ADDR_VBUSCALIB_K:
	case RS485_CMD_ADDR_ICHGCALIB_K:
	case RS485_CMD_ADDR_IDISCHGCALIB_K:
	case RS485_CMD_ADDR_TEMP1_CALIB_K:
	case RS485_CMD_ADDR_TEMP2_CALIB_K:
	case RS485_CMD_ADDR_TEMP3_CALIB_K:
	case RS485_CMD_ADDR_TEMP4_CALIB_K:
	case RS485_CMD_ADDR_TEMP5_CALIB_K:
	case RS485_CMD_ADDR_TEMP6_CALIB_K:
	case RS485_CMD_ADDR_TEMP_ENV1_CALIB_K:
	case RS485_CMD_ADDR_TEMP_ENV2_CALIB_K:
	case RS485_CMD_ADDR_TEMP_ENV3_CALIB_K:
	case RS485_CMD_ADDR_TEMP_MOS_CALIB_K:
		Sci_WrRegs_0x10_CalibCoef(u16SciRegStartAddr, s);
		break;

	case RS485_CMD_ADDR_VCELL_OVP_FIRST:
	case RS485_CMD_ADDR_VCELL_UVP_FIRST:
	case RS485_CMD_ADDR_VBUS_OVP_FIRST:
	case RS485_CMD_ADDR_VBUS_UVP_FIRST:
	case RS485_CMD_ADDR_ICHG_OCP_FIRST:
	case RS485_CMD_ADDR_IDSG_OCP_FIRST:
	case RS485_CMD_ADDR_TCHG_OTP_FIRST:
	case RS485_CMD_ADDR_TCHG_UTP_FIRST:
	case RS485_CMD_ADDR_TDSG_OTP_FIRST:
	case RS485_CMD_ADDR_TDSG_UTP_FIRST:
	case RS485_CMD_ADDR_TMOS_OTP_FIRST:
	case RS485_CMD_ADDR_VDELTA_OP_FIRST:
	case RS485_CMD_ADDR_SOC_UP_FIRST:
		Sci_WrRegs_0x10_Protect(u16SciRegStartAddr, s);
		break;

	case RS485_CMD_ADDR_SOC_VOLTAGE1:
		Sci_WrRegs_0x10_SocTable(s);
		break;

	case RS485_CMD_ADDR_COPPERLOSS1:
		Sci_WrRegs_0x10_CopperLoss(s);
		break;

	case RS485_CMD_ADDR_RTC_TIME_YEAR:
		Sci_WrRegs_0x10_RTC(s);
		break;

	case RS485_CMD_ADDR_BALANCE_OV:
		Sci_WrRegs_0x10_Balance(s);
		break;

	case RS485_CMD_ADDR_CS_CUR_CHGMAX:
		Sci_WrRegs_0x10_SysOther(s);
		break;

	case RS485_CMD_ADDR_SLEEP_V_NORMAL:
		Sci_WrRegs_0x10_SleepElement(s);
		break;

	case RS485_CMD_ADDR_SOC_AH:
		Sci_WrRegs_0x10_SocElement(s);
		break;

	case RS485_CMD_ADDR_SYS_SERIES_NUM:
		Sci_WrRegs_0x10_SystemElement(s);
		break;

	case RS485_CMD_ADDR_HEAT_DSG_HIGH:
		Sci_WrRegs_0x10_HeatCoolElement(s);
		break;

	case RS485_ADDR_SN_SERIAL_NUM:
	case RS485_ADDR_SN_HAEDWARE_VER:
	case RS485_ADDR_SN_SOFTWARE_VER:
		Sci_WrRegs_0x10_SN_Version(u16SciRegStartAddr, s);
		break;

	case RS485_CMD_ADDR_FLASH_CONNECT:
		Sci_WrRegs_0x10_FlashConnect(s);
		break; // 少了个BREAK导致OVER。
	default:
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_CMD_INVALID;
		break;
	}
}

void Sci_ACK_0x03_ReadRegs_LCD(struct RS485MSG *s, UINT8 t_u8BuffTemp[])
{
	UINT16 u16SciTemp;
	UINT16 i, j;
	INT8 k, x;

	i = 0;
	switch (s->u16RdRegStartAddr)
	{
	case 0: // LCD
		u16SciTemp = 1;
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

		u16SciTemp = (g_stCellInfoReport.u16VCellTotle + 50) / 100;
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

		if (g_stCellInfoReport.u16Ichg > 0)
		{
			u16SciTemp = (g_stCellInfoReport.u16Ichg + 5005) / 10;
		}
		else
		{
			u16SciTemp = (5000 - g_stCellInfoReport.u16IDischg) / 10;
		}
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

		u16SciTemp = (g_stCellInfoReport.u16TempMax + 5) / 10;
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

		u16SciTemp = g_stCellInfoReport.SocElement.u16Soc;
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
		break;

	case 1: // 上位机第三级保护，60+10=70个
		for (j = 0; j < Record_len; j++)
		{
			k = FaultPoint_Third - 1 - j;
			if (k < 0)
			{
				k = Record_len + k;
			}
			u16SciTemp = Fault_record_Third[k];
			t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
			t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

			for (x = 0; x < 6; ++x)
			{
				u16SciTemp = RTC_Fault_record_Third[k][x];
				t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
				t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
			}
		}
		break;

	case 2: // 序列号，硬件版本号，软件版本号
		for (j = 0; j < PRODUCT_ID_LENGTH_MAX; j++)
		{
			t_u8BuffTemp[i++] = ProductionInfor.BMS_SerialNumber[j];
		}
		for (j = 0; j < PRODUCT_ID_LENGTH_MAX; j++)
		{
			t_u8BuffTemp[i++] = ProductionInfor.BMS_HardWareVersion[j];
		}
		for (j = 0; j < PRODUCT_ID_LENGTH_MAX; j++)
		{
			t_u8BuffTemp[i++] = ProductionInfor.BMS_SoftWareVersion[j];
		}
		break;

	case 3: // 三级安全状态
		u16SciTemp = 1;
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

		u16SciTemp = (g_stCellInfoReport.u16VCellTotle + 50) / 100; // // v *100
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

		if (g_stCellInfoReport.u16Ichg > 0)
		{
			u16SciTemp = (g_stCellInfoReport.u16Ichg + 5005) / 10; // 总电流？
		}
		else
		{
			u16SciTemp = (5000 - g_stCellInfoReport.u16IDischg) / 10; // A *10
		}
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

		u16SciTemp = (g_stCellInfoReport.u16TempMax + 5) / 10; // 最大温度
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

		u16SciTemp = g_stCellInfoReport.SocElement.u16Soc; // 当前电池SOC     0—100 为相对容量百分比
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

		// SuspendFlag1 = SuspendFlag2;
		// SuspendFlag2 = RTC_ExtComCnt1;
		// // 蓝牙
		// if (SuspendFlag1 != SuspendFlag2)
		// {
		// 	BlueToothFlag = 1;
		// }
		// else
		// {
		// 	BlueToothFlag = 0;
		// }
		u16SciTemp = BlueToothFlag; // 蓝牙
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

		// u16SciTemp = System_OnOFF_Func.bits.b1OnOFF_Heat; // 加热
		u16SciTemp = SystemStatus.bits.b1Status_Heat; // 加热
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

		for (j = 0; j < 12; j++)
		{																															   // 实时信息		两个拼在一起
			u16SciTemp = ((*(&System_ErrFlag.u8ErrFlag_Com_AFE1 + 2 * j)) << 8) | (*(&System_ErrFlag.u8ErrFlag_Com_AFE1 + 2 * j + 1)); // 结构体
			t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
			t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
		}

		u16SciTemp = (g_stCellInfoReport.unMdlFault_Third.all); // 三级状态
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

		u16SciTemp = (g_stCellInfoReport.u16VCellTotle + 50) / 10;
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

		break;

	case 8:
		Sci_ACK_0x03_ReadRegs_EventRecord(t_u8BuffTemp);
		break;

	default:
		s->u16RdRegStartAddr = 0;
		break;
	}
	s->u16RdRegStartAddr = 0;
}

void Sci_ACK_0x03_ReadRegs_Data(struct RS485MSG *s, UINT8 t_u8BuffTemp[])
{
	UINT16 u16SciTemp;
	UINT16 i = 0, j;
	INT8 k;
	UINT8 a[4];

	for (j = 0; j < 63; j++)
	{ // 0xD000_63
		u16SciTemp = *(&g_stCellInfoReport.u16VCell[0] + j);
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
	}

	// 0xD100_33
	// u16SciTemp = (UINT16)(RTC_time.RTC_Time_Month) | (RTC_time.RTC_Time_Year<<8);
	u16SciTemp = 0;
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	// u16SciTemp = (UINT16)(RTC_time.RTC_Time_Hour) | (RTC_time.RTC_Time_Day<<8);
	u16SciTemp = 0;
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	// u16SciTemp = (UINT16)(RTC_time.RTC_Time_Second) | (RTC_time.RTC_Time_Minute<<8);
	u16SciTemp = 0;
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	for (j = 0; j < 4; j++)
	{
		k = FaultPoint_First2 - 1 - j;
		if (k < 0)
		{
			k = Record_len + k;
		}
		a[j] = k;
	}
	u16SciTemp = (Fault_record_First2[a[0]] << 8) | Fault_record_First2[a[1]];
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
	u16SciTemp = (Fault_record_First2[a[2]] << 8) | Fault_record_First2[a[3]];
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	for (j = 0; j < 4; j++)
	{
		k = FaultPoint_Second2 - 1 - j;
		if (k < 0)
		{
			k = Record_len + k;
		}
		a[j] = k;
	}
	u16SciTemp = (Fault_record_Second2[a[0]] << 8) | Fault_record_Second2[a[1]];
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
	u16SciTemp = (Fault_record_Second2[a[2]] << 8) | Fault_record_Second2[a[3]];
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	for (j = 0; j < 4; j++)
	{
		k = FaultPoint_Third2 - 1 - j;
		if (k < 0)
		{
			k = Record_len + k;
		}
		a[j] = k;
	}
	u16SciTemp = (Fault_record_Third2[a[0]] << 8) | Fault_record_Third2[a[1]];
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
	u16SciTemp = (Fault_record_Third2[a[2]] << 8) | Fault_record_Third2[a[3]];
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	for (j = 0; j < 12; j++)
	{ // 0xD002到这里。
		u16SciTemp = ((*(&System_ErrFlag.u8ErrFlag_Com_AFE1 + 2 * j)) << 8) | (*(&System_ErrFlag.u8ErrFlag_Com_AFE1 + 2 * j + 1));
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
	}

	switch (OPEN)
	{
	case 0:
		u16SciTemp = ((~((UINT16)(SystemStatus.all & 0x0000FFFF))) & 0x00FE) | (((UINT16)(SystemStatus.all & 0x0000FFFF)) & 0xFF01);
		break;
	case 1:
		u16SciTemp = (UINT16)(SystemStatus.all & 0x0000FFFF);
		break;
	default:
		u16SciTemp = (UINT16)(SystemStatus.all & 0x0000FFFF);
		break;
	}
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	u16SciTemp = (UINT16)(SystemStatus.all >> 16);
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	u16SciTemp = (UINT16)(System_OnOFF_Func.all & 0x0000FFFF);
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	u16SciTemp = (UINT16)(System_OnOFF_Func.all >> 16);
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	u16SciTemp = 0;
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	u16SciTemp = 0;
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	u16SciTemp = 0;
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	u16SciTemp = 0; // 可以加多一个
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	u16SciTemp = 0; // 可以加多一个
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	u16SciTemp = 0; // 可以加多一个
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	u16SciTemp = 0; // 可以加多一个
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	u16SciTemp = 0; // 可以加多一个
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;

	// 0xD200_1
	u16SciTemp = 0; // 可以加多一个
	t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
	t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
}

/*=================================================================
 * FUNCTION: Sci_Tx_RW_Fun
 * PURPOSE : 将需要发送的数据进行更新
 * INPUT:    void
 *
 * RETURN:   void
 *
 * CALLS:    void
 *
 * CALLED BY:Sci2_Updata()
 *
 *=================================================================*/
void Sci_ACK_0x03_RW_Data_Pro(struct RS485MSG *s, UINT8 t_u8BuffTemp[])
{ // 65个
	UINT16 u16SciTemp;
	UINT16 i, j;
	i = 0;
	for (j = 0; j < E2P_PARA_NUM_PROTECT; j++)
	{
		u16SciTemp = *(&PRT_E2ROMParas.u16VcellOvp_First + j);
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
	}
}

void Sci_ACK_0x03_RW_Data_Cali(struct RS485MSG *s, UINT8 t_u8BuffTemp[])
{ // 94个
	UINT16 u16SciTemp;
	UINT16 i, j;
	i = 0;
	for (j = 0; j < KB_NUM; j++)
	{
		u16SciTemp = g_u16CalibCoefK[j];
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
		u16SciTemp = g_i16CalibCoefB[j];
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
	}
}

void Sci_ACK_0x03_RW_Data_Other(struct RS485MSG *s, UINT8 t_u8BuffTemp[])
{ // 86
	UINT16 u16SciTemp;
	UINT16 i, j;
	i = 0;
	for (j = 0; j < SOC_TABLE_SIZE; j++)
	{ // 由于GetEndValue()函数的问题，只能混在一起
		switch (OtherElement.u16Soc_TableSelect)
		{
		case SOC_TABLE_TEST:
			u16SciTemp = SOC_Table_Set[j];
			break;
		case SOC_TABLE_LIFEPO:
			u16SciTemp = SOC_Table_LiFePO[j];
			break;
		case SOC_TABLE_TERNARYLI:
			u16SciTemp = SocTable_TernaryLi[j];
			break;
		case SOC_TABLE_LIFEPO2:
			// u16SciTemp = SocTable_LiFePO2[j];
			break;
		default:
			u16SciTemp = SOC_Table_Set[j];
			break;
		}
		// u16SciTemp = SOC_Table_Set[j];
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
	}

	for (j = 0; j < CompensateNUM; j++)
	{
		u16SciTemp = CopperLoss[j];
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
	}

	for (j = 0; j < CompensateNUM; j++)
	{
		u16SciTemp = CopperLoss_Num[j];
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
	}

	for (j = 0; j < E2P_PARA_NUM_RTC; j++)
	{
		// u16SciTemp = *(&RTC_time.RTC_Time_Year+j);
		u16SciTemp = 0;
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
	}
}

void Sci_ACK_0x03_RW_Data_OtherCanAdd(struct RS485MSG *s, UINT8 t_u8BuffTemp[])
{ // 32+24=56个
	UINT16 u16SciTemp;
	UINT16 i = 0, j;

	for (j = 0; j < E2P_PARA_NUM_OTHER_ELEMENT1; j++)
	{
		u16SciTemp = *(&OtherElement.u16Balance_OpenVoltage + j);
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
	}

	for (j = 0; j < E2P_PARA_NUM_HEAT_COOL; j++)
	{
		u16SciTemp = *(&Heat_Cool_Element.u16Heat_OpenTemp + j);
		// u16SciTemp = 0;
		t_u8BuffTemp[i++] = (u16SciTemp >> 8) & 0x00FF;
		t_u8BuffTemp[i++] = u16SciTemp & 0x00FF;
	}
}

void Sci_ACK_0x03(struct RS485MSG *s)
{
	UINT8 i;
	UINT16 u16SciTemp;
	if (s->AckType == RS485_ACK_POS)
	{
		if (s->u16RdRegStartAddrActure >= RS485_ADDR_RW_CALIB)
		{
			if (s->u16RdRegStartAddrActure >= RS485_ADDR_RO_START0)
			{
				Sci_ACK_0x03_ReadRegs_Data(s, g_u8SCITxBuff);
			}
			else if (s->u16RdRegStartAddrActure >= RS485_ADDR_RO_LCD)
			{
				Sci_ACK_0x03_ReadRegs_LCD(s, g_u8SCITxBuff);
			}
			else if (s->u16RdRegStartAddrActure >= RS485_ADDR_RW_AFE_PARAMETER)
			{
				Sci_ACK_0x03_RW_AFE_Parameters(s, g_u8SCITxBuff);
			}
			else if (s->u16RdRegStartAddrActure >= RS485_ADDR_RW_OTHER_CANADD)
			{
				Sci_ACK_0x03_RW_Data_OtherCanAdd(s, g_u8SCITxBuff);
			}
			else if (s->u16RdRegStartAddrActure >= RS485_ADDR_RW_OTHER)
			{
				Sci_ACK_0x03_RW_Data_Other(s, g_u8SCITxBuff);
			}
			else if (s->u16RdRegStartAddrActure >= RS485_ADDR_RW_PORTECT)
			{
				Sci_ACK_0x03_RW_Data_Pro(s, g_u8SCITxBuff);
			}
			else
			{
				Sci_ACK_0x03_RW_Data_Cali(s, g_u8SCITxBuff);
			}
			// 头码，前三个字节保持不变
			s->u16Buffer[0] = (s->u16Buffer[0] != 0) ? RS485_SLAVE_ADDR : s->u16Buffer[0];
			s->u16Buffer[1] = s->enRs485CmdType;
			s->u16Buffer[2] = s->u16RdRegByteNum;
			// 数据
			for (i = 0; i < (s->u16RdRegByteNum); i++)
			{
				s->u16Buffer[i + 3] = g_u8SCITxBuff[i + ((s->u16RdRegStartAddr) << 1)];
			}
			i = s->u16RdRegByteNum + 3;
		}
	}
	else
	{
		i = 1;
		s->u16Buffer[i++] = s->enRs485CmdType | 0x80;
		s->u16Buffer[i++] = s->ErrorType;
	}
	u16SciTemp = Sci_CRC16RTU((UINT8 *)s->u16Buffer, i);
	s->u16Buffer[i++] = u16SciTemp & 0x00FF;
	s->u16Buffer[i++] = u16SciTemp >> 8;
	s->AckLenth = i;

	s->ptr_no = 0;
	s->csr = RS485_STA_TX_COMPLETE;
}

void Sci_ACK_0x06_0x10(struct RS485MSG *s)
{
	UINT8 i;
	UINT16 u16SciTemp;

	if (s->AckType == RS485_ACK_POS)
	{
		i = 6;
	}
	else
	{
		i = 1;
		s->u16Buffer[i++] = s->enRs485CmdType | 0x80;
		s->u16Buffer[i++] = s->ErrorType;
	}

	u16SciTemp = Sci_CRC16RTU((UINT8 *)s->u16Buffer, i);
	s->u16Buffer[i++] = u16SciTemp & 0x00FF;
	s->u16Buffer[i++] = u16SciTemp >> 8;
	s->AckLenth = i;

	s->ptr_no = 0;
	s->csr = RS485_STA_TX_COMPLETE;
}

#if (defined _COMMOM_UPPER_SCI1)
void Sci1_CommonUpper_FaultChk(void)
{
	UINT8 FaultCnt = 0;

	if (USART1->ISR & 0x08)
	{						   // 接收溢出错误，RXNEIE或EIE使能产生中断，开
		USART1->ICR |= 1 << 3; // 清除
		FaultCnt++;
	}

	if (USART1->ISR & 0x04)
	{						   // 检测到噪声，默认开，不开的话CR3的ONEBIT置1，不开
							   // USART_CR3的EIE使能中断
		USART1->ICR |= 1 << 2; // 清除
		FaultCnt++;
	}

	if (USART1->ISR & 0x02)
	{						   // 帧错误，USART_CR3的EIE使能中断，开
		USART1->ICR |= 1 << 1; // 清除
		FaultCnt++;
	}

	if (USART1->ISR & 0x01)
	{						   // 校验错误标志 USART_CR1的PEIE使能该中断，不开
		USART1->ICR |= 1 << 0; // 清除
		FaultCnt++;
	}

	if (FaultCnt)
	{
		gu16_CommuErrCnt_SCI1++;
	}
}

// 将接收数据解码，接收中断中调用
/*=================================================================
 * FUNCTION: Sci2_Rx_Deal
 * PURPOSE : 串口数据接收解码
 * INPUT:    void
 *
 * RETURN:   void
 *
 * CALLS:    void
 *
 * CALLED BY:ISR()
 *
 *=================================================================*/
void Sci1_CommonUpper_Rx_Deal(struct RS485MSG *s)
{
	// RC1IE = 0;// 禁止EUSART2 接收中断
	// s->u16Buffer[s->ptr_no] = RCREG1;                 //读RCREG寄存器来读取接收到的8位数据
	// NVIC_DisableIRQ(USART1_IRQn);
	USART1->CR1 &= ~(1 << 5);			   // 和上面那句话二选一
	s->u16Buffer[s->ptr_no] = USART1->RDR; // 从RXFIFO 中读取接收到的数据
	if ((s->ptr_no == 0) && (s->u16Buffer[0] != RS485_SLAVE_ADDR) && (s->u16Buffer[0] != RS485_BROADCAST_ADDR))
	{
		s->ptr_no = 0;
		s->u16Buffer[0] = 0;
	}
	else
	{
		if (s->ptr_no == 1)
		{
			switch (s->u16Buffer[s->ptr_no])
			{
			case RS485_CMD_READ_REGS:
				s->enRs485CmdType = RS485_CMD_READ_REGS;
				break;
			case RS485_CMD_WRITE_REG:
				s->enRs485CmdType = RS485_CMD_WRITE_REG;
				break;
			case RS485_CMD_WRITE_REGS:
				s->enRs485CmdType = RS485_CMD_WRITE_REGS;
				break;
			default:
				s->ptr_no = RS485_MAX_BUFFER_SIZE;
				s->u16Buffer[0] = 0;
				s->u16Buffer[1] = 0;
				break;
			}
		}
		else if (s->ptr_no >= 2)
		{
			switch (s->enRs485CmdType)
			{
			case RS485_CMD_READ_REGS:
			case RS485_CMD_WRITE_REG:
				if (s->ptr_no == 7)
				{ //	receive complete
					s->csr = RS485_STA_RX_COMPLETE;
					// RCSTA1bits.CREN = 0;  //禁止接收
					// RC1IE = 0;			// 禁止EUSART2 接收中断
					USART1->CR1 &= ~(1 << 2);
					USART1->CR1 &= ~(1 << 5);
				}
				break;
			case RS485_CMD_WRITE_REGS:
				if ((s->ptr_no >= 7) && (s->ptr_no == (s->u16Buffer[6] + 8)))
				{
					s->csr = RS485_STA_RX_COMPLETE;
					// disable rx TODO
					// disable rx/tx interrupt TODO
					// RCSTA1bits.CREN = 0;    //禁止接收
					// RC1IE = 0;				// 禁止EUSART2 接收中断
					USART1->CR1 &= ~(1 << 2);
					USART1->CR1 &= ~(1 << 5);
				}
				break;
			default:
				s->ptr_no = RS485_MAX_BUFFER_SIZE;
				s->u16Buffer[0] = 0;
				break;
			}
		}
		s->ptr_no++;
		if (s->ptr_no >= RS485_MAX_BUFFER_SIZE)
		{
			s->ptr_no = 0;
			s->u16Buffer[0] = 0;
		}
	}
	USART1->CR1 |= (1 << 5);
}

void Sci1_CommonUpper_Tx_Deal(struct RS485MSG *s)
{
	if (0 == gu8_TxEnable_SCI1)
	{
		return;
	}

	if (gu16_CommuErrCnt_SCI1)
	{ // 出现错误也得把数据全部接收完，然后不回复
		s->ptr_no = 0;
		s->csr = RS485_STA_TX_COMPLETE;
		gu8_TxFinishFlag_SCI1 = 1;
		gu8_TxEnable_SCI1 = 0;
		gu16_CommuErrCnt_SCI1 = 0;
		return;
	}

	TRANS_EN_485();
	while (gu8_TxEnable_SCI1)
	{
		if (s->ptr_no < s->AckLenth)
		{
			TRANS_485_WAIT_COMPLETE();
			// while (!((USART1->ISR) & (1 << 7)))
			// 	;
			USART1->TDR = s->u16Buffer[s->ptr_no]; // load data
			// USART_Tx(USART1, g_tModS.TxBuf[j]);
			s->ptr_no++;
		}
		else
		{
			TRANS_485_WAIT_COMPLETE();
			__delay_ms(1);
			RECV_EN_485();
			s->ptr_no = 0;
			s->csr = RS485_STA_TX_COMPLETE;
			gu8_TxFinishFlag_SCI1 = 1;
			gu8_TxEnable_SCI1 = 0;
			if (u8FlashUpdateE2PROM)
			{
				u8FlashUpdateE2PROM = 0;
				u8FlashUpdateFlag = 1;
			}
		}
	}
}

// 串口初始化函数
void InitSCI1_CommonUpper(void)
{
	GPIO_InitTypeDef GPIO_InitStructure;
	USART_InitTypeDef USART_InitStructure;
	NVIC_InitTypeDef NVIC_InitStructure;

	RCC_APB2PeriphClockCmd(RCC_APB2Periph_USART1, ENABLE); // 开启USART1外设时钟
	// RCC->AHBENR |= 1<<17;										//开启GPIOA的外设时钟

	// Enable the USART1 Interrupt(使能USART1中断)
	NVIC_InitStructure.NVIC_IRQChannel = USART1_IRQn;
	NVIC_InitStructure.NVIC_IRQChannelPriority = 0;
	NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE;
	NVIC_Init(&NVIC_InitStructure);

	// USART1_TX -> PA9 , USART1_RX -> PA10
	GPIO_PinAFConfig(GPIOA, GPIO_PinSource9, GPIO_AF_1); // 030的AF表格在非reg的datasheet里
	GPIO_PinAFConfig(GPIOA, GPIO_PinSource10, GPIO_AF_1);
	GPIO_InitStructure.GPIO_Pin = GPIO_Pin_9 | GPIO_Pin_10;
	GPIO_InitStructure.GPIO_Mode = GPIO_Mode_AF;
	GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
	GPIO_InitStructure.GPIO_PuPd = GPIO_PuPd_UP;
	GPIO_InitStructure.GPIO_Speed = GPIO_Speed_2MHz;
	GPIO_Init(GPIOA, &GPIO_InitStructure);

	// 串口初始化
	USART_InitStructure.USART_BaudRate = 19200;										// 设置串口波特率
	USART_InitStructure.USART_WordLength = USART_WordLength_8b;						// 设置数据位
	USART_InitStructure.USART_StopBits = USART_StopBits_1;							// 设置停止位
	USART_InitStructure.USART_Parity = USART_Parity_No;								// 设置效验位
	USART_InitStructure.USART_HardwareFlowControl = USART_HardwareFlowControl_None; // 设置流控制
	USART_InitStructure.USART_Mode = USART_Mode_Rx | USART_Mode_Tx;					// 设置工作模式
	USART_Init(USART1, &USART_InitStructure);										// 配置入结构体

	USART1->CR3 |= 1 << 0;	// EIE，开帧错误中断，同时开启噪声中断
	USART1->CR3 |= 1 << 11; // 未被使能前改写，禁止噪声中断

	USART_Cmd(USART1, ENABLE);					   // 使能串口1
	USART_ITConfig(USART1, USART_IT_RXNE, ENABLE); // 使能接收中断

	Sci_DataInit(&g_stCurrentMsgPtr_SCI1);
}

void App_CommonUpperSCI1(struct RS485MSG *s)
{
	switch (s->csr)
	{
	// IDLE-空闲态，保持50ms后使能接收（物理层）receive set
	case RS485_STA_IDLE:
	{
		break;
	}
	// receive complete, to deal the receive data
	case RS485_STA_RX_COMPLETE:
	{
		USART1->CR1 &= ~(1 << 5); // 禁止产生中断
		CRC_verify(s);
		if (s->AckType == RS485_ACK_POS)
		{
			switch (s->enRs485CmdType)
			{
			case RS485_CMD_READ_REGS:
				Sci_Deal_ReadRegs_0x03(s);
				break;
			case RS485_CMD_WRITE_REG:
				Sci_Deal_WrReg_0x06(s);
				break;
			case RS485_CMD_WRITE_REGS:
				Sci_Deal_WrRegs_0x10(s);
				break;
			default:
				s->u16RdRegByteNum = 0;
				s->AckType = RS485_ACK_NEG;
				s->ErrorType = RS485_ERROR_NULL;
				break;
			}
		}
		s->csr = RS485_STA_RX_OK; // receive the correct data, switch to transmit wait 50ms
		break;					  // 下一轮再来
	}
	// receive ok, to transmit wait 50ms
	case RS485_STA_RX_OK:
	{
		switch (s->enRs485CmdType)
		{
		case RS485_CMD_READ_REGS:
			Sci_ACK_0x03(s);
			break;
		case RS485_CMD_WRITE_REG:
		case RS485_CMD_WRITE_REGS:
			Sci_ACK_0x06_0x10(s);
			break;
		default: // 这个defualt不用加错误操作
			break;
		}
		USART1->CR1 |= (1 << 3); // 使能发送
		gu8_TxEnable_SCI1 = 1;
	}
	// transmit complete, to switch receive wait 20ms
	case RS485_STA_TX_COMPLETE:
	{
		if (gu8_TxFinishFlag_SCI1)
		{
			s->csr = RS485_STA_IDLE;
			s->u16Buffer[0] = 0;
			s->u16Buffer[1] = 0;
			s->u16Buffer[2] = 0;
			s->u16Buffer[3] = 0;
			gu8_TxFinishFlag_SCI1 = 0;
			s->ptr_no = 0;
			USART1->CR1 |= (1 << 2); // 使能接收
			USART1->CR1 |= (1 << 5); // 使能接收中断
			gu8_TxEnable_SCI1 = 0;
		}
		break;
	}

	default:
	{
		s->csr = RS485_STA_IDLE;
		break;
	}
	}
	Sci1_CommonUpper_Tx_Deal(s);
	// Sci1_FaultChk();	//没必要在这加
}

#endif

#if (defined _COMMOM_UPPER_SCI2)

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

// 将接收数据解码，接收中断中调用
/*=================================================================
 * FUNCTION: Sci2_Rx_Deal
 * PURPOSE : 串口数据接收解码
 * INPUT:    void
 *
 * RETURN:   void
 *
 * CALLS:    void
 *
 * CALLED BY:ISR()
 *
 *=================================================================*/
void Sci2_CommonUpper_Rx_Deal(struct RS485MSG *s)
{
	// RC1IE = 0;// 禁止EUSART2 接收中断
	// s->u16Buffer[s->ptr_no] = RCREG1;                 //读RCREG寄存器来读取接收到的8位数据
	// NVIC_DisableIRQ(USART2_IRQn);
	USART2->CR1 &= ~(1 << 5);			   // 和上面那句话二选一
	s->u16Buffer[s->ptr_no] = USART2->RDR; // 从RXFIFO 中读取接收到的数据
	if ((s->ptr_no == 0) && (s->u16Buffer[0] != RS485_SLAVE_ADDR) && (s->u16Buffer[0] != RS485_BROADCAST_ADDR))
	{
		s->ptr_no = 0;
		s->u16Buffer[0] = 0;
	}
	else
	{
		if (s->ptr_no == 1)
		{
			switch (s->u16Buffer[s->ptr_no])
			{
			case RS485_CMD_READ_REGS:
				s->enRs485CmdType = RS485_CMD_READ_REGS;
				break;
			case RS485_CMD_WRITE_REG:
				s->enRs485CmdType = RS485_CMD_WRITE_REG;
				break;
			case RS485_CMD_WRITE_REGS:
				s->enRs485CmdType = RS485_CMD_WRITE_REGS;
				break;
			default:
				s->ptr_no = RS485_MAX_BUFFER_SIZE;
				s->u16Buffer[0] = 0;
				s->u16Buffer[1] = 0;
				break;
			}
		}
		else if (s->ptr_no >= 2)
		{
			switch (s->enRs485CmdType)
			{
			case RS485_CMD_READ_REGS:
			case RS485_CMD_WRITE_REG:
				if (s->ptr_no == 7)
				{ //	receive complete
					s->csr = RS485_STA_RX_COMPLETE;
					// RCSTA1bits.CREN = 0;  //禁止接收
					// RC1IE = 0;			// 禁止EUSART2 接收中断
					USART2->CR1 &= ~(1 << 2);
					USART2->CR1 &= ~(1 << 5);
				}
				break;
			case RS485_CMD_WRITE_REGS:
				if ((s->ptr_no >= 7) && (s->ptr_no == (s->u16Buffer[6] + 8)))
				{
					s->csr = RS485_STA_RX_COMPLETE;
					// disable rx TODO
					// disable rx/tx interrupt TODO
					// RCSTA1bits.CREN = 0;    //禁止接收
					// RC1IE = 0;				// 禁止EUSART2 接收中断
					USART2->CR1 &= ~(1 << 2);
					USART2->CR1 &= ~(1 << 5);
				}
				break;
			default:
				s->ptr_no = RS485_MAX_BUFFER_SIZE;
				s->u16Buffer[0] = 0;
				break;
			}
		}
		s->ptr_no++;
		if (s->ptr_no >= RS485_MAX_BUFFER_SIZE)
		{
			s->ptr_no = 0;
			s->u16Buffer[0] = 0;
		}
	}
	USART2->CR1 |= (1 << 5);
}

void Sci2_CommonUpper_Tx_Deal(struct RS485MSG *s)
{
	static int delayFlag = 0;

	if (0 == gu8_TxEnable_SCI2)
	{
		return;
	}

	if (gu16_CommuErrCnt_SCI2)
	{ // 出现错误也得把数据全部接收完，然后不回复
		s->ptr_no = 0;
		s->csr = RS485_STA_TX_COMPLETE;
		gu8_TxFinishFlag_SCI2 = 1;
		gu8_TxEnable_SCI2 = 0;
		gu16_CommuErrCnt_SCI2 = 0;
		return;
	}

	while (!((USART2->ISR) & (1 << 7)))
		; // 1<<6 也可以
	if (s->ptr_no < s->AckLenth)
	{
		USART2->TDR = s->u16Buffer[s->ptr_no]; // load data
		s->ptr_no++;
		if ((s->ptr_no == 19) || (s->ptr_no == 39) || (s->ptr_no == 59))
		{
			delayFlag = 1;
		}
	}
	else
	{
		s->ptr_no = 0;
		s->csr = RS485_STA_TX_COMPLETE;
		gu8_TxFinishFlag_SCI2 = 1;
		gu8_TxEnable_SCI2 = 0;
		if (u8FlashUpdateE2PROM)
		{
			u8FlashUpdateE2PROM = 0;
			u8FlashUpdateFlag = 1;
		}
	}
}

// 串口初始化函数
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
	USART_InitStructure.USART_BaudRate = 19200;										// 设置串口波特率
	USART_InitStructure.USART_WordLength = USART_WordLength_8b;						// 设置数据位
	USART_InitStructure.USART_StopBits = USART_StopBits_1;							// 设置停止位
	USART_InitStructure.USART_Parity = USART_Parity_No;								// 设置效验位
	USART_InitStructure.USART_HardwareFlowControl = USART_HardwareFlowControl_None; // 设置流控制
	USART_InitStructure.USART_Mode = USART_Mode_Rx | USART_Mode_Tx;					// 设置工作模式
	USART_Init(USART2, &USART_InitStructure);										// 配置入结构体

	USART2->CR3 |= 1 << 0;	// EIE，开帧错误中断，同时开启噪声中断
	USART2->CR3 |= 1 << 11; // 未被使能前改写，禁止噪声中断

	USART_Cmd(USART2, ENABLE);					   // 使能串口1
	USART_ITConfig(USART2, USART_IT_RXNE, ENABLE); // 使能接收中断

	Sci_DataInit(&g_stCurrentMsgPtr_SCI2);
}

void App_CommonUpperSCI2(struct RS485MSG *s)
{
	switch (s->csr)
	{
	// IDLE-空闲态，保持50ms后使能接收（物理层）receive set
	case RS485_STA_IDLE:
	{
		break;
	}
	// receive complete, to deal the receive data
	case RS485_STA_RX_COMPLETE:
	{
		USART2->CR1 &= ~(1 << 5); // 禁止产生中断
		CRC_verify(s);
		if (s->AckType == RS485_ACK_POS)
		{
			switch (s->enRs485CmdType)
			{
			case RS485_CMD_READ_REGS:
				Sci_Deal_ReadRegs_0x03(s);
				break;
			case RS485_CMD_WRITE_REG:
				Sci_Deal_WrReg_0x06(s);
				break;
			case RS485_CMD_WRITE_REGS:
				Sci_Deal_WrRegs_0x10(s);
				break;
			default:
				s->u16RdRegByteNum = 0;
				s->AckType = RS485_ACK_NEG;
				s->ErrorType = RS485_ERROR_NULL;
				break;
			}
		}
		s->csr = RS485_STA_RX_OK; // receive the correct data, switch to transmit wait 50ms
		break;					  // 下一轮再来
	}
	// receive ok, to transmit wait 50ms
	case RS485_STA_RX_OK:
	{
		switch (s->enRs485CmdType)
		{
		case RS485_CMD_READ_REGS:
			Sci_ACK_0x03(s);
			break;
		case RS485_CMD_WRITE_REG:
		case RS485_CMD_WRITE_REGS:
			Sci_ACK_0x06_0x10(s);
			break;
		default: // 这个defualt不用加错误操作
			break;
		}
		USART2->CR1 |= (1 << 3); // 使能发送
		gu8_TxEnable_SCI2 = 1;
	}
	// transmit complete, to switch receive wait 20ms
	case RS485_STA_TX_COMPLETE:
	{
		if (gu8_TxFinishFlag_SCI2)
		{
			s->csr = RS485_STA_IDLE;
			s->u16Buffer[0] = 0;
			s->u16Buffer[1] = 0;
			s->u16Buffer[2] = 0;
			s->u16Buffer[3] = 0;
			gu8_TxFinishFlag_SCI2 = 0;
			s->ptr_no = 0;
			USART2->CR1 |= (1 << 2); // 使能接收
			USART2->CR1 |= (1 << 5); // 使能接收中断
			gu8_TxEnable_SCI2 = 0;
		}
		break;
	}

	default:
	{
		s->csr = RS485_STA_IDLE;
		break;
	}
	}
	Sci2_CommonUpper_Tx_Deal(s);
	// Sci1_FaultChk();	//没必要在这加
}

#endif

void Sci_WrRegs_0x10_CalibCoef(UINT16 u16Channel, struct RS485MSG *s)
{
	UINT16 t_u16K, t_u16B, t_u16Temp;
	INT16 t_i16B;
	UINT16 u16WrRegNum;
	u16WrRegNum = s->u16Buffer[5] + (s->u16Buffer[4] << 8);

	if (u16WrRegNum == 2)
	{
		t_u16K = s->u16Buffer[8] + (s->u16Buffer[7] << 8);
		t_u16B = s->u16Buffer[10] + (s->u16Buffer[9] << 8);

		t_u16Temp = t_u16B & 0x8000;
		if (t_u16Temp == 0)
		{
			t_i16B = t_u16B & 0x7FFF;
		}
		else
		{
			t_i16B = -(t_u16B & 0x7FFF);
		}

		if ((t_u16K < SYSKMIN) || (t_u16K > SYSKMAX))
		{
			s->AckType = RS485_ACK_NEG;
			s->ErrorType = RS485_ERROR_DATA_INVALID;
			return;
		}

		if ((t_i16B < SYSBMIN) || (t_i16B > SYSBMAX))
		{
			s->AckType = RS485_ACK_NEG;
			s->ErrorType = RS485_ERROR_DATA_INVALID;
			return;
		}

		t_u16Temp = (u16Channel - RS485_CMD_ADDR_VC1CALIB_K) >> 1;
		g_u16CalibCoefK[t_u16Temp] = t_u16K;
		g_i16CalibCoefB[t_u16Temp] = t_i16B;
		u8E2P_KB_WriteFlag = 1;
		u8E2P_KB_WritePos = t_u16Temp;
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_CMD_INVALID;
	}
}

// 节省了很多代码量吧？
void Sci_WrRegs_0x10_Protect(UINT16 u16Channel, struct RS485MSG *s)
{
	UINT16 t_u16Temp, i;
	UINT16 u16WrRegNum;
	u16WrRegNum = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (u16WrRegNum == 5)
	{
		t_u16Temp = u16Channel - RS485_CMD_ADDR_VCELL_OVP_FIRST;
		if (t_u16Temp == 20 || t_u16Temp == 25)
		{
			AFE_PARAM_WRITE_Flag = 1;
		}
		for (i = 0; i < 5; ++i)
		{
			*(&PRT_E2ROMParas.u16VcellOvp_First + i + t_u16Temp) = (UINT16)(s->u16Buffer[2 * i + 8] + (s->u16Buffer[2 * i + 7] << 8));
		}

		if (u16Channel >= RS485_CMD_ADDR_VDELTA_OP_FIRST)
		{
			u32E2P_Pro_Other_WriteFlag = (EE_FLAG_VCELL_OVP_FIRST | EE_FLAG_VCELL_OVP_SECOND | EE_FLAG_VCELL_OVP_THIRD | EE_FLAG_VCELL_OVP_RCV | EE_FLAG_VCELL_OVP_FILTER)
										 << (t_u16Temp - E2P_PARA_NUM_VOLCUR_PROTECT - E2P_PARA_NUM_TEM_PROTECT);
		}
		else if (u16Channel >= RS485_CMD_ADDR_TCHG_OTP_FIRST)
		{
			u32E2P_Pro_Temp_WriteFlag = (EE_FLAG_VCELL_OVP_FIRST | EE_FLAG_VCELL_OVP_SECOND | EE_FLAG_VCELL_OVP_THIRD | EE_FLAG_VCELL_OVP_RCV | EE_FLAG_VCELL_OVP_FILTER)
										<< (t_u16Temp - E2P_PARA_NUM_VOLCUR_PROTECT);
		}
		else
		{
			u32E2P_Pro_VolCur_WriteFlag = (EE_FLAG_VCELL_OVP_FIRST | EE_FLAG_VCELL_OVP_SECOND | EE_FLAG_VCELL_OVP_THIRD | EE_FLAG_VCELL_OVP_RCV | EE_FLAG_VCELL_OVP_FILTER) << (t_u16Temp);
		}
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_CMD_INVALID;
	}
}

// 这种写法其实也有问题，主要是，倘若写失败，但是上传上位机是修改成功，就尴尬
// 但是上位机会有EEPROM写失败标志位弥补
void Sci_WrRegs_0x10_SocTable(struct RS485MSG *s)
{
	/*
	UINT8 i;
	UINT16  u16WrRegNum;
	u16WrRegNum = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if(u16WrRegNum == E2P_PARA_NUM_SOC_TABLE) {
		for(i = 0; i < E2P_PARA_NUM_SOC_TABLE; ++i) {
			SOC_Table_Set[i] = (UINT16)(s->u16Buffer[2*i+8] + (s->u16Buffer[2*i+7] << 8));
		}
		u8E2P_SocTable_WriteFlag = E2P_PARA_NUM_SOC_TABLE;
	}
	else {
		s ->AckType = RS485_ACK_NEG;
		s ->ErrorType = RS485_ERROR_CMD_INVALID;
	}
	*/
}

void Sci_WrRegs_0x10_CopperLoss(struct RS485MSG *s)
{
	/*
	UINT8 i;
	UINT16  u16WrRegNum;
	u16WrRegNum = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if(u16WrRegNum == E2P_PARA_NUM_COPPERLOSS*2) {
		for(i = 0; i < E2P_PARA_NUM_COPPERLOSS; ++i) {
			CopperLoss[i] = (UINT16)(s->u16Buffer[2*i+8] + (s->u16Buffer[2*i+7] << 8));
			CopperLoss_Num[i] = (UINT16)(s->u16Buffer[2*(i+16)+8] + (s->u16Buffer[2*(i+16)+7] << 8));
		}
		u8E2P_CopperLoss_WriteFlag = E2P_PARA_NUM_COPPERLOSS;
	}
	else {
		s ->AckType = RS485_ACK_NEG;
		s ->ErrorType = RS485_ERROR_CMD_INVALID;
	}
	*/
}

void Sci_WrRegs_0x10_RTC(struct RS485MSG *s)
{
	/*
	UINT8 i;
	UINT16  u16WrRegNum;
	u16WrRegNum = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if(u16WrRegNum == E2P_PARA_NUM_RTC) {
		for(i = 0; i < E2P_PARA_NUM_RTC; ++i) {
			*(&RTC_time.RTC_Time_Year+i) = (UINT16)(s->u16Buffer[2*i+8] + (s->u16Buffer[2*i+7] << 8));
		}
		u32E2P_RTC_Element_WriteFlag = E2P_PARA_ALL_RTC_ELEMENT;
	}
	else {
		s ->AckType = RS485_ACK_NEG;
		s ->ErrorType = RS485_ERROR_CMD_INVALID;
	}
	*/
}

void Sci_WrRegs_0x10_Balance(struct RS485MSG *s)
{
	UINT8 i;
	UINT16 u16WrRegNum;
	u16WrRegNum = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (u16WrRegNum == 8)
	{
		for (i = 0; i < 8; ++i)
		{
			*(&OtherElement.u16Balance_OpenVoltage + i) = (UINT16)(s->u16Buffer[2 * i + 8] + (s->u16Buffer[2 * i + 7] << 8));
		}
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_BALANCE_OV;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_BALANCE_OW;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_BALANCE_CW1;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_BALANCE_CW2;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_OPENTIME_ODD;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_OPENTIME_EVEN;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_OPENTIME_MOS;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_RES;
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_CMD_INVALID;
	}
}

void Sci_WrRegs_0x10_SysOther(struct RS485MSG *s)
{
	UINT8 i;
	UINT16 u16WrRegNum;
	u16WrRegNum = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (u16WrRegNum == 8)
	{
		for (i = 0; i < 8; ++i)
		{
			*(&OtherElement.u16CS_Cur_CHGmax + i) = (UINT16)(s->u16Buffer[2 * i + 8] + (s->u16Buffer[2 * i + 7] << 8));
		}
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_CS_CUR_CHGMAX;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_CS_CUR_DSGMAX;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_CBC_CUR_CHG;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_CBC_CUR_DSG;
		// u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_COOL_DSG_H;		//不保存
		// u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_COOL_DSG_L;
		// u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_COOL_CHG_H;
		// u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_COOL_CHG_L;
		AFE_PARAM_WRITE_Flag = 1;

		// todo
		// if (SH367309_SC_DelayT_Set())
		// {
		// 	s->AckType = RS485_ACK_NEG;
		// 	s->ErrorType = RS485_ERROR_CMD_INVALID;
		// }
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_CMD_INVALID;
	}
}

void Sci_WrRegs_0x10_SleepElement(struct RS485MSG *s)
{
	UINT8 i;
	UINT16 u16WrRegNum;
	u16WrRegNum = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (u16WrRegNum == 8)
	{
		for (i = 0; i < 8; ++i)
		{
			*(&OtherElement.u16Sleep_VNormal + i) = (UINT16)(s->u16Buffer[2 * i + 8] + (s->u16Buffer[2 * i + 7] << 8));
		}
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SLEEP_V_NORMAL;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SLEEP_TIME_NORMAL;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SLEEP_V_LOW;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SLEEP_TIME_LOW;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SLEEP_I_CHG;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SLEEP_I_DSG;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SLEEP_RES1;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SLEEP_RES2;
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_CMD_INVALID;
	}
}

void Sci_WrRegs_0x10_SocElement(struct RS485MSG *s)
{
	UINT8 i;
	UINT16 u16WrRegNum;
	u16WrRegNum = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (u16WrRegNum == 4)
	{
		for (i = 0; i < 4; ++i)
		{
			*(&OtherElement.u16Soc_Ah + i) = (UINT16)(s->u16Buffer[2 * i + 8] + (s->u16Buffer[2 * i + 7] << 8));
		}
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SOC_AH;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SOC_CYCLE_TIME;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SOC_RES1;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SOC_RES2;

		InitData_SOC();
		SOC_Enhance_Element.u16_RefreshData_Flag = 2;
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_CMD_INVALID;
	}
}

void Sci_WrRegs_0x10_SystemElement(struct RS485MSG *s)
{
	UINT8 i;
	UINT16 u16WrRegNum;
	u16WrRegNum = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (u16WrRegNum == 4)
	{
		for (i = 0; i < 4; ++i)
		{
			*(&OtherElement.u16Sys_SeriesNum + i) = (UINT16)(s->u16Buffer[2 * i + 8] + (s->u16Buffer[2 * i + 7] << 8));
		}
		if (OtherElement.u16Sys_PreChg_Time > 1000)
			OtherElement.u16Sys_PreChg_Time = 100;
			
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SYS_SERIES_NUM;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SYS_CS_RESIS;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SYS_CS_NUM;
		u32E2P_OtherElement1_WriteFlag |= EE_FLAG_OTHER1_SYS_PRECHG_TIME;
		SeriesNum = OtherElement.u16Sys_SeriesNum;
		// CS，直接使用不需要再赋值，TODO
		// 还是赋值吧，提高效率
		g_u32CS_Res_AFE = ((UINT32)OtherElement.u16Sys_CS_Res_Num * 1000) / OtherElement.u16Sys_CS_Res;
		AFE_PARAM_WRITE_Flag = 1;
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_CMD_INVALID;
	}
}

void Sci_WrRegs_0x10_HeatCoolElement(struct RS485MSG *s)
{
	UINT8 i;
	UINT16 u16WrRegNum;
	u16WrRegNum = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (u16WrRegNum == E2P_PARA_NUM_HEAT_COOL)
	{
		for (i = 0; i < E2P_PARA_NUM_HEAT_COOL; ++i)
		{
			*(&Heat_Cool_Element.u16Heat_OpenTemp + i) = (UINT16)(s->u16Buffer[2 * i + 8] + (s->u16Buffer[2 * i + 7] << 8));
		}
		u32E2P_HeatCool_WriteFlag |= E2P_PARA_ALL_HEAT_COOL_ELE;
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_CMD_INVALID;
	}
}

void Sci_WrRegs_0x10_FlashConnect(struct RS485MSG *s)
{
	UINT16 u16WrRegNum;
	u16WrRegNum = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (u16WrRegNum == 1)
	{
		if (FLASH_COMPLETE != FlashWriteOneHalfWord(FLASH_ADDR_UPDATE_FLAG, FLASH_TO_IAP_VALUE))
		{
			// System_ERROR_UserCallback(ERROR_FLASH);
			s->AckType = RS485_ACK_NEG;
			s->ErrorType = RS485_ERROR_CMD_INVALID;
		}
		else
		{
			u8FlashUpdateE2PROM = 1;
		}
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_CMD_INVALID;
	}
}

/* 把BMS序列号，硬件版本号， 软件版本号写入 ohterInfor结构体
 * 并把写入到EEPROM标志置位
 * startADDR  如起始地址
 */
void Sci_WrRegs_0x10_SN_Version(UINT16 startADDR, struct RS485MSG *s)
{
	UINT8 i;
	UINT16 u16WrSNlength;

	u16WrSNlength = (UINT16)((UINT16)s->u16Buffer[5] + ((UINT16)s->u16Buffer[4] << 8)) << 1;

	switch (startADDR - RS485_ADDR_SN_SERIAL_NUM)
	{
	case 0:
		for (i = 0; i < PRODUCT_ID_LENGTH_MAX; ++i)
		{
			if (i < u16WrSNlength)
			{
				ProductionInfor.BMS_SerialNumber[i] = s->u16Buffer[7 + i];
			}
			else
			{
				ProductionInfor.BMS_SerialNumber[i] = '\0';
			}
		}
		ProductionInfor.BMS_SerialNumberLength = u16WrSNlength;
		ProductionInfor.BMS_SerialNumber_WriteFlag = 1;
		break;

	case 1:
		for (i = 0; i < PRODUCT_ID_LENGTH_MAX; ++i)
		{
			if (i < u16WrSNlength)
			{
				ProductionInfor.BMS_HardWareVersion[i] = s->u16Buffer[7 + i];
			}
			else
			{
				ProductionInfor.BMS_HardWareVersion[i] = '\0';
			}
		}
		ProductionInfor.BMS_HardWareVersionLength = u16WrSNlength;
		ProductionInfor.BMS_HardWareVersion_WriteFlag = 1;
		break;

	case 2:
		for (i = 0; i < PRODUCT_ID_LENGTH_MAX; ++i)
		{
			if (i < u16WrSNlength)
			{
				ProductionInfor.BMS_SoftWareVersion[i] = s->u16Buffer[7 + i];
			}
			else
			{
				ProductionInfor.BMS_SoftWareVersion[i] = '\0';
			}
		}
		ProductionInfor.BMS_SoftWareVersionLength = u16WrSNlength;
		ProductionInfor.BMS_SoftWareVersion_WriteFlag = 1;
		break;

	default:
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_CMD_INVALID;
		break;
	}
}

void Sci_WrReg_0x06_Reset_CalibCoef(struct RS485MSG *s)
{
	UINT8 i;
	switch (s->u16Buffer[5] + (s->u16Buffer[4] << 8))
	{
	case 0x55AA:
		for (i = 0; i < 32; i++)
		{
			g_u16CalibCoefK[i] = SYSKDEFAULT;
			g_i16CalibCoefB[i] = SYSBDEFAULT;
			WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_K + (i << 1)), g_u16CalibCoefK[i]);
			WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_B + (i << 1)), g_i16CalibCoefB[i]);
		}
		break;
	case 0x55AB:

		g_u16CalibCoefK[VOLT_AFE1] = SYSKDEFAULT;
		g_i16CalibCoefB[VOLT_AFE1] = SYSBDEFAULT;
		WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_K + (VOLT_AFE1 << 1)), g_u16CalibCoefK[VOLT_AFE1]);
		WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_B + (VOLT_AFE1 << 1)), g_i16CalibCoefB[VOLT_AFE1]);
		break;
	case 0x55AC:
		g_u16CalibCoefK[VOLT_AFE2] = SYSKDEFAULT;
		g_i16CalibCoefB[VOLT_AFE2] = SYSBDEFAULT;
		WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_K + (VOLT_AFE2 << 1)), g_u16CalibCoefK[VOLT_AFE2]);
		WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_B + (VOLT_AFE2 << 1)), g_i16CalibCoefB[VOLT_AFE2]);
		break;
	case 0x55AD:
		g_u16CalibCoefK[VOLT_VBUS] = SYSKDEFAULT;
		g_i16CalibCoefB[VOLT_VBUS] = SYSBDEFAULT;
		WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_K + (VOLT_VBUS << 1)), g_u16CalibCoefK[VOLT_VBUS]);
		WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_B + (VOLT_VBUS << 1)), g_i16CalibCoefB[VOLT_VBUS]);
		break;
	case 0x55AE:
		for (i = 0; i < 10; i++)
		{
			g_u16CalibCoefK[MDL_TEMP1 + i] = SYSKDEFAULT;
			g_i16CalibCoefB[MDL_TEMP1 + i] = SYSBDEFAULT;
			WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_K + ((MDL_TEMP1 + i) << 1)), g_u16CalibCoefK[i]);
			WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_B + ((MDL_TEMP1 + i) << 1)), g_i16CalibCoefB[i]);
		}
		break;
	case 0x55AF:
		g_u16CalibCoefK[MDL_IDSG] = SYSKDEFAULT;
		g_i16CalibCoefB[MDL_IDSG] = SYSBDEFAULT;
		WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_K + (MDL_IDSG << 1)), g_u16CalibCoefK[MDL_IDSG]);
		WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_B + (MDL_IDSG << 1)), g_i16CalibCoefB[MDL_IDSG]);
		break;
	case 0x55B0:
		g_u16CalibCoefK[MDL_ICHG] = SYSKDEFAULT;
		g_i16CalibCoefB[MDL_ICHG] = SYSBDEFAULT;
		WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_K + (MDL_ICHG << 1)), g_u16CalibCoefK[MDL_ICHG]);
		WriteEEPROM_Word_NoZone((E2P_ADDR_START_CALIB_B + (MDL_ICHG << 1)), g_i16CalibCoefB[MDL_ICHG]);
		break;
	default:
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_DATA_INVALID;
		break;
	}
}

void Sci_WrReg_0x06_Reset_ProtectRecord(struct RS485MSG *s)
{
	UINT16 u16SciRegData;
	UINT8 i;
	u16SciRegData = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (0x0001 == u16SciRegData)
	{
		for (i = 0; i < Record_len; ++i)
		{
			Fault_record_First2[i] = 0;
			Fault_record_Second2[i] = 0;
			Fault_record_Third2[i] = 0;
		}
		FaultPoint_First2 = 0;
		FaultPoint_Second2 = 0;
		FaultPoint_Third2 = 0;
		Fault_Flag_Fisrt.all = 0;
		Fault_Flag_Second.all = 0;
		Fault_Flag_Third.all = 0;
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_DATA_INVALID;
	}
}

void Sci_WrReg_0x06_Reset_ProtectElement(struct RS485MSG *s)
{
	UINT16 u16SciRegData;
	UINT8 i;
	const struct PRT_E2ROM_PARAS PrtE2PARAS_Default = E2P_PROTECT_DEFAULT_PRT;
	u16SciRegData = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (0x0001 == u16SciRegData)
	{
		for (i = 0; i < E2P_PARA_NUM_PROTECT; ++i)
		{
			*(&PRT_E2ROMParas.u16VcellOvp_First + i) = *(&PrtE2PARAS_Default.u16VcellOvp_First + i);
		}
		u32E2P_Pro_VolCur_WriteFlag = E2P_PARA_ALL_VOLCUR_PROTECT;
		u32E2P_Pro_Temp_WriteFlag = E2P_PARA_ALL_TEM_PROTECT;
		u32E2P_Pro_Other_WriteFlag = E2P_PARA_ALL_OTHER_PROTECT;
		AFE_PARAM_WRITE_Flag = 1;
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_DATA_INVALID;
	}
}

void Sci_WrReg_0x06_Reset_OtherCanAdd(struct RS485MSG *s)
{
	UINT16 u16SciRegData;
	UINT8 i;
	const struct OTHER_ELEMENT OtherElement_Default = OtherElement_default;
	u16SciRegData = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (0x0001 == u16SciRegData)
	{
		for (i = 0; i < E2P_PARA_NUM_OTHER_ELEMENT1; ++i)
		{
			*(&OtherElement.u16Balance_OpenVoltage + i) = *(&OtherElement_Default.u16Balance_OpenVoltage + i);
		}
		u32E2P_OtherElement1_WriteFlag = E2P_PARA_ALL_OTHER_ELEMENT1;
		SeriesNum = OtherElement.u16Sys_SeriesNum;
		g_u32CS_Res_AFE = ((UINT32)OtherElement.u16Sys_CS_Res_Num * 1000) / OtherElement.u16Sys_CS_Res;
		AFE_PARAM_WRITE_Flag = 1; // CS检流电阻修改，则过流保护等要跟着修改。

		InitData_SOC();
		// 同步更新安时数，循环次数等
		SOC_Enhance_Element.u16_RefreshData_Flag = 2;
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_DATA_INVALID;
	}
}

void Sci_WrReg_0x06_Reset_HeatCool(struct RS485MSG *s)
{
	UINT16 u16SciRegData;
	UINT8 i;
	const struct HEAT_COOL_ELEMENT HeatCoolEle_Default = HeatCoolElement_Default;

	u16SciRegData = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (0x0001 == u16SciRegData)
	{
		for (i = 0; i < E2P_PARA_NUM_HEAT_COOL; ++i)
		{
			*(&Heat_Cool_Element.u16Heat_OpenTemp + i) = *(&HeatCoolEle_Default.u16Heat_OpenTemp + i);
		}
		u32E2P_HeatCool_WriteFlag = E2P_PARA_ALL_HEAT_COOL_ELE;
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_DATA_INVALID;
	}
}

void Sci_WrReg_0x06_SwitchON(struct RS485MSG *s)
{
}

void Sci_WrReg_0x06_SwitchOFF(struct RS485MSG *s)
{
}

// 关于这个函数
// A:第一次打开这个功能，以前从来没打开过，则因为各种标志位变量都没变过(switch结构里面的)，所以会进行初始化验证
// B:其中关闭了，又打开，则已经初始化过一次，这次打开就继续按照上一次的进度继续下去
void Sci_WrReg_0x06_BMS_FunctionON(struct RS485MSG *s)
{
	UINT16 u16SciRegData;
	u16SciRegData = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (u16SciRegData >= 1 && u16SciRegData <= 32)
	{
		switch (u16SciRegData)
		{		// 如果是以下功能被打开，则需要初始化验证，别的功能直接关就好
		case 1: // 均衡
			if (!System_OnOFF_Func_StartUpRec.bits.b1OnOFF_Balance)
			{
				System_OnOFF_Func_StartUpRec.bits.b1OnOFF_Balance = 1;
				System_Func_StartUp.bits.b1StartUpFlag_Balance = 1;
			}
			break;

		case 3: // MOS或者接触器功能
			if (!System_OnOFF_Func_StartUpRec.bits.b1OnOFF_MOS_Relay)
			{
				System_OnOFF_Func_StartUpRec.bits.b1OnOFF_MOS_Relay = 1;
				System_Func_StartUp.bits.b1StartUpFlag_MOS = 1;
				System_Func_StartUp.bits.b1StartUpFlag_Relay = 1;
			}
			break;

		case 6: // 加热功能
			if (!System_OnOFF_Func_StartUpRec.bits.b1OnOFF_Heat)
			{
				System_OnOFF_Func_StartUpRec.bits.b1OnOFF_Heat = 1;
				System_Func_StartUp.bits.b1StartUpFlag_Heat = 1;
			}
			break;

		case 7: // 冷凝功能
			if (!System_OnOFF_Func_StartUpRec.bits.b1OnOFF_Cool)
			{
				System_OnOFF_Func_StartUpRec.bits.b1OnOFF_Cool = 1;
				System_Func_StartUp.bits.b1StartUpFlag_Cool = 1;
			}
			break;

		case 8: // 激活模拟前端AFE1
			App_WakeUpAFE();
			// InitialisebqMaximo(DEVICE_ADDR_AFE1);
			break;

		case 0x0A: // 立刻进入休眠
			Sleep_Mode.bits.b1ForceToSleep_L3 = 1;
			break;
		default:
			break;
		}

		System_OnOFF_Func.all |= ((UINT32)1 << (u16SciRegData - 1));
		if (u16SciRegData == 0x0B)
		{
			// System_OnOFF_Func.bits.b1OnOFF_SOC_Zero
			// 默认为0，不需要保存
		}
		else
		{
			WriteEEPROM_Word_NoZone(EEPROM_ADDR_SYS_FUNC_SELECT, (UINT16)(System_OnOFF_Func.all & 0x0000FFFF));
			WriteEEPROM_Word_NoZone(EEPROM_ADDR_SYS_FUNC_SELECT + 2, (UINT16)(System_OnOFF_Func.all >> 16));
		}

		if (System_OnOFF_Func.bits.b1OnOFF_SOC_Fixed)
		{
			SOC_Enhance_Element.u16_RefreshData_Flag = 1;
		}
		if (System_OnOFF_Func.bits.b1OnOFF_SOC_Zero)
		{
			SOC_Enhance_Element.u16_RefreshData_Flag = 2;
		}
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_DATA_INVALID;
	}
}

void Sci_WrReg_0x06_BMS_FunctionOFF(struct RS485MSG *s)
{
	UINT16 u16SciRegData;
	u16SciRegData = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (u16SciRegData >= 1 && u16SciRegData <= 32)
	{
		//*(&System_OnOFF_Func.bits.b1OnOFF_Balance+(u16SciRegData-1)) = 0;
		System_OnOFF_Func.all &= ~((UINT32)1 << (u16SciRegData - 1)); // 功能途中关闭不需要初始化验证

		if (u16SciRegData == 0x0B)
		{
			// System_OnOFF_Func.bits.b1OnOFF_SOC_Zero
			// 默认为0，不需要保存
		}
		else
		{
			WriteEEPROM_Word_NoZone(EEPROM_ADDR_SYS_FUNC_SELECT, (UINT16)(System_OnOFF_Func.all & 0x0000FFFF));
			WriteEEPROM_Word_NoZone(EEPROM_ADDR_SYS_FUNC_SELECT + 2, (UINT16)(System_OnOFF_Func.all >> 16));
		}
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_DATA_INVALID;
	}
}

void Sci_WrReg_0x06_SetSocOnce(struct RS485MSG *s)
{
	UINT16 u16SciRegData;
	u16SciRegData = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
	if (u16SciRegData <= 100)
	{
		SOC_Enhance_Element.u16_RefreshData_Flag = 3;
		SOC_Enhance_Element.u8_SetSocOnce = u16SciRegData;
	}
	else
	{
		s->AckType = RS485_ACK_NEG;
		s->ErrorType = RS485_ERROR_DATA_INVALID;
	}
}

void InitUSART_CommonUpper(void)
{
#ifdef _COMMOM_UPPER_SCI1
	InitSCI1_CommonUpper();
#endif

#ifdef _COMMOM_UPPER_SCI2
	InitSCI2_CommonUpper();
#endif
}

void App_CommonUpper(void)
{
#ifdef _COMMOM_UPPER_SCI1
	App_CommonUpperSCI1(&g_stCurrentMsgPtr_SCI1);
#endif

#ifdef _COMMOM_UPPER_SCI2
	App_CommonUpperSCI2(&g_stCurrentMsgPtr_SCI2);
#endif
}

#ifndef SCI_H
#define SCI_H

#define	RS485_BROADCAST_ADDR		(( UINT8 ) 0x00 )
#define	RS485_SLAVE_ADDR			(( UINT8 ) 0x01 )

#define	SCI_TX_BUF_LEN			251   	//日志记录导致提升为250
#define RS485_MAX_BUFFER_SIZE 	251		//

//485 cmd type
enum RS485_CMD_E {
	RS485_CMD_READ_REGS = 3,
	RS485_CMD_WRITE_REG = 6,
	RS485_CMD_WRITE_REGS = 16,
	
	//UART_CLIENT_CMD_0x01 = 0xA1,	//客户的
	//UART_CLIENT_CMD_0x02 = 0xA2,	
};


struct SOC_CAL_ELEMENT_UPPER {
	UINT16 u16Soc;                 	//当前电池SOC     0—100 为相对容量百分比
	UINT16 u16Soh;                 	//为绝对容量百分比0——100
	UINT16 u16CapacityNow;        	//当前容量	Ah*100
	UINT16 u16CapacityFull;        	//当前满电容量	Ah*100		//为什么*100为单位呢，因为上位机是mAh，所以能提高显示精度
	UINT16 u16CapacityFactory;     	//出厂满电容量	Ah*100		//带来的结果是650Ah最大
	UINT16 u16Cycle_times;     		//循环次数
};


struct MDLCHGFAULT_BITS     {    	// bits  description
	UINT8 b1CellOvp			:1;   	//
	UINT8 b1CellUvp			:1;   	//
	UINT8 b1BatOvp			:1;   	//
	UINT8 b1BatUvp			:1;   	//
	
	UINT8 b1IchgOcp			:1;   	//
	UINT8 b1IdischgOcp		:1;   	//
	UINT8 b1CellChgOtp		:1;   	//
	UINT8 b1CellDischgOtp 	:1;   	//

	UINT8 b1CellChgUtp		:1;   	//
	UINT8 b1CellDischgUtp 	:1;   	//
	UINT8 b1VcellDeltaBig	:1;   	//
	UINT8 b1TempDeltaBig 	:1;   	//这个没有，Res可用

	UINT8 b1SocLow			:1;   	//
	UINT8 b1TmosOtp			:1;   	//
	UINT8 b1Rcved1	 		:1;   	//
	UINT8 b1Rcved2  		:1;   	//
};


union  MDLCHGFAULT_REG
{
	UINT16   all;
	struct MDLCHGFAULT_BITS	bits;
};


struct stCell_Info {
	UINT16	u16VCell[32];
	UINT16	u16VCellMax;                    // mv
	UINT16	u16VCellMin;                    // mv
    UINT16	u16VCellMaxPosition;
    UINT16	u16VCellMinPosition;
	UINT16	u16VCellDelta;                  // mv
	UINT16	u16VCellTotle;                  // v *100
    UINT16	u16Temperature[TEMP_NUM];       // +40°C *10
    UINT16	u16TempMax;                     // +40°C *10
	UINT16	u16TempMin;                     // +40°C *10
	UINT16	u16Ichg;                        // A *10
    UINT16	u16IDischg;                     // A *10
    //UINT16	u16Soc;							// %
    struct SOC_CAL_ELEMENT_UPPER SocElement;
    union MDLCHGFAULT_REG unMdlFault_First;
    union MDLCHGFAULT_REG unMdlFault_Second;
	union MDLCHGFAULT_REG unMdlFault_Third;
	UINT16	u16BalanceFlag1;                 //电池均衡标志位1
	UINT16	u16BalanceFlag2;                 //电池均衡标志位2
};


//RS485状态机状态
#define	RS485_STA_IDLE				0
#define	RS485_STA_RX_COMPLETE		1
#define	RS485_STA_RX_OK				2
#define	RS485_STA_TX_COMPLETE		3


#define	RS485_ACK_POS			        0x00	// 正响应
#define	RS485_ACK_NEG			        0x01	// 负响应
//Error type
#define	RS485_ERROR_ADDR_INVALID	    0x01	// 地址不合法
#define	RS485_ERROR_CRC_ERROR			0x02	// CRC校验错误
#define	RS485_ERROR_DATA_INVALID	    0x03	// 参数不合法
#define	RS485_ERROR_CMD_INVALID			0x04	// 当前状态下命令无效
#define	RS485_ERROR_RONLY_NO_W			0x05	// 只读参数拒绝写入
#define	RS485_ERROR_WONLY_NO_R			0x06	// 只写参数拒绝读取
#define	RS485_ERROR_NO_PERMISSION		0x07	// 无权限
#define	RS485_ERROR_NULL			    0x08	// 未知错误


// SCI_485 Message Structure
struct RS485MSG {
	UINT8	ptr_no;          	// Word stating what state msg is in
	UINT8	csr;          		// I2C address of slave msg is intended for
	UINT16	u16RdRegStartAddr;	// read reg start addr
	UINT16	u16RdRegStartAddrActure;	//自定义地址保存
	UINT8	u16RdRegByteNum;    // read byte lenth
	UINT8	AckLenth;			// ack byte lenth
	UINT8	AckType;			// ack type
	UINT8	ErrorType;			// error type
	UINT8 	u16Buffer[RS485_MAX_BUFFER_SIZE];    // Array holding msg data - max that
	enum RS485_CMD_E enRs485CmdType;
};


//可读可写，进enum大单
//#define RS485_ADDR_RW_ORDER		0x1000
#define RS485_ADDR_RW_CALIB				0x2000
#define RS485_ADDR_RW_PORTECT			0x2100
#define RS485_ADDR_RW_OTHER				0x2200
#define RS485_ADDR_RW_OTHER_CANADD		0x2300


//自己给自己埋的坑，变成了不是标准MODBUS协议了
#if 0
//循环只读，要求1s内上传完毕
#define RS485_ADDR_RO_START0			0xD000
#define RS485_ADDR_RO_START1			0xD001
#define RS485_ADDR_RO_START2			0xD002
#define RS485_ADDR_RO_START3			0xD003
#define RS485_ADDR_RO_START4			0xD004
#endif

//0xD000主要是g_stCellInfoReport的东西，目前共63个字
#define RS485_ADDR_RO_START0			(UINT16)0xD000

//0xD100是从RTC开始到结尾几个保留位，共21+12=33个字
#define RS485_ADDR_RO_START1			(UINT16)0xD100

//0xD100是一个保留位，目前只有一个
#define RS485_ADDR_RO_START2			(UINT16)0xD200



//以下是只读一次，无
#define RS485_ADDR_RO_LCD       		0xC000
#define RS485_ADDR_RO_FA_RTC    		0xC001
#define RS485_ADDR_SN_READ				0xC002
//#define RS485_ADDR_SeriesMode			0xC003_0xC007
#define RS485_ADDR_EVENT_RECORD			0xC008


#define RS485_ADDR_SN_SERIAL_NUM		0xFFF0
#define RS485_ADDR_SN_HAEDWARE_VER		0xFFF1
#define RS485_ADDR_SN_SOFTWARE_VER		0xFFF2


#define RS485_CMD_ADDR_FLASH_CONNECT	0xFFFD		//MCU在线升级连接命令


enum RS485_CMD_RW_E {
	RS485_CMD_ADDR_RESET_CALIB_COEF = 0x1000,
	RS485_CMD_ADDR_RESET_PROTECT_RECORD,
	RS485_CMD_ADDR_RESET_PROTECT_ELEMENT,
	RS485_CMD_ADDR_RESET_OTHER_CANADD,
	RS485_CMD_ADDR_RESET_HEAT_COOL,
	RS485_CMD_ADDR_SET_ONCE_SOC,
	RS485_CMD_ADDR_RESET_AFE_PARAMETERS,
	RS485_CMD_ADDR_RESET_EVENT_RECORD,

	#if 0	//妈的，不是连续研发，中断再开始很容易做无用功，而且系统和实现没那么巧妙完整。
	RS485_CMD_ADDR_SYSFUNC_ONOFF_BALANCE = 0x1100,
	RS485_CMD_ADDR_SYSFUNC_ONOFF_BMS_SOURCE,
	RS485_CMD_ADDR_SYSFUNC_ONOFF_MOS,
	RS485_CMD_ADDR_SYSFUNC_ONOFF_RELAY,
	RS485_CMD_ADDR_SYSFUNC_ONOFF_SOC_FIXED,
	RS485_CMD_ADDR_SYSFUNC_ONOFF_HEAT,
	RS485_CMD_ADDR_SYSFUNC_ONOFF_COOL,
	RS485_CMD_ADDR_SYSFUNC_ONOFF_AFE1,
	RS485_CMD_ADDR_SYSFUNC_ONOFF_AFE2,
	RS485_CMD_ADDR_SYSFUNC_ONOFF_SLEEP,
	#endif
	
	RS485_CMD_ADDR_SWITCH_ON = 0x1100,		//巧妙！
	RS485_CMD_ADDR_SWITCH_OFF,
	RS485_CMD_ADDR_SYSTEM_FUNCTION_ON,
	RS485_CMD_ADDR_SYSTEM_FUNCTION_OFF,


	RS485_CMD_ADDR_VC1CALIB_K = 0x2000,		//读取
	RS485_CMD_ADDR_VC1CALIB_B,
	RS485_CMD_ADDR_VC2CALIB_K,
	RS485_CMD_ADDR_VC2CALIB_B,
	RS485_CMD_ADDR_VC3CALIB_K,
	RS485_CMD_ADDR_VC3CALIB_B,
	RS485_CMD_ADDR_VC4CALIB_K,
	RS485_CMD_ADDR_VC4CALIB_B,
	RS485_CMD_ADDR_VC5CALIB_K,
	RS485_CMD_ADDR_VC5CALIB_B,
	RS485_CMD_ADDR_VC6CALIB_K,
	RS485_CMD_ADDR_VC6CALIB_B,
	RS485_CMD_ADDR_VC7CALIB_K,
	RS485_CMD_ADDR_VC7CALIB_B,
	RS485_CMD_ADDR_VC8CALIB_K,
	RS485_CMD_ADDR_VC8CALIB_B,
	RS485_CMD_ADDR_VC9CALIB_K,
	RS485_CMD_ADDR_VC9CALIB_B,
	RS485_CMD_ADDR_VC10CALIB_K,
	RS485_CMD_ADDR_VC10CALIB_B,
	RS485_CMD_ADDR_VC11CALIB_K,
	RS485_CMD_ADDR_VC11CALIB_B,
	RS485_CMD_ADDR_VC12CALIB_K,
	RS485_CMD_ADDR_VC12CALIB_B,
	RS485_CMD_ADDR_VC13CALIB_K,
	RS485_CMD_ADDR_VC13CALIB_B,
	RS485_CMD_ADDR_VC14CALIB_K,
	RS485_CMD_ADDR_VC14CALIB_B,
	RS485_CMD_ADDR_VC15CALIB_K,
	RS485_CMD_ADDR_VC15CALIB_B,
	RS485_CMD_ADDR_VC16CALIB_K,
	RS485_CMD_ADDR_VC16CALIB_B,
	RS485_CMD_ADDR_VC17CALIB_K,
	RS485_CMD_ADDR_VC17CALIB_B,
	RS485_CMD_ADDR_VC18CALIB_K,
	RS485_CMD_ADDR_VC18CALIB_B,
	RS485_CMD_ADDR_VC19CALIB_K,
	RS485_CMD_ADDR_VC19CALIB_B,
	RS485_CMD_ADDR_VC20CALIB_K,
	RS485_CMD_ADDR_VC20CALIB_B,
	RS485_CMD_ADDR_VC21CALIB_K,
	RS485_CMD_ADDR_VC21CALIB_B,
	RS485_CMD_ADDR_VC22CALIB_K,
	RS485_CMD_ADDR_VC22CALIB_B,
	RS485_CMD_ADDR_VC23CALIB_K,
	RS485_CMD_ADDR_VC23CALIB_B,
	RS485_CMD_ADDR_VC24CALIB_K,
	RS485_CMD_ADDR_VC24CALIB_B,
	RS485_CMD_ADDR_VC25CALIB_K,
	RS485_CMD_ADDR_VC25CALIB_B,
	RS485_CMD_ADDR_VC26CALIB_K,
	RS485_CMD_ADDR_VC26CALIB_B,
	RS485_CMD_ADDR_VC27CALIB_K,
	RS485_CMD_ADDR_VC27CALIB_B,
	RS485_CMD_ADDR_VC28CALIB_K,
	RS485_CMD_ADDR_VC28CALIB_B,
	RS485_CMD_ADDR_VC29CALIB_K,
	RS485_CMD_ADDR_VC29CALIB_B,
	RS485_CMD_ADDR_VC30CALIB_K,
	RS485_CMD_ADDR_VC30CALIB_B,
	RS485_CMD_ADDR_VC31CALIB_K,
	RS485_CMD_ADDR_VC31CALIB_B,
	RS485_CMD_ADDR_VC32CALIB_K,
	RS485_CMD_ADDR_VC32CALIB_B,
	RS485_CMD_ADDR_AFE1CALIB_K,			//读取
	RS485_CMD_ADDR_AFE1CALIB_B,
	RS485_CMD_ADDR_AFE2CALIB_K,
	RS485_CMD_ADDR_AFE2CALIB_B,
	RS485_CMD_ADDR_VBUSCALIB_K,
	RS485_CMD_ADDR_VBUSCALIB_B,

	//RS485_CMD_ADDR_ICHGCALIB_K = 0x2100,
	RS485_CMD_ADDR_ICHGCALIB_K,		//第二页		//读取
	RS485_CMD_ADDR_ICHGCALIB_B,
	RS485_CMD_ADDR_IDISCHGCALIB_K,
	RS485_CMD_ADDR_IDISCHGCALIB_B,
	RS485_CMD_ADDR_TEMP1_CALIB_K,		//读取
	RS485_CMD_ADDR_TEMP1_CALIB_B,
	RS485_CMD_ADDR_TEMP2_CALIB_K,
	RS485_CMD_ADDR_TEMP2_CALIB_B,
	RS485_CMD_ADDR_TEMP3_CALIB_K,
	RS485_CMD_ADDR_TEMP3_CALIB_B,
	RS485_CMD_ADDR_TEMP4_CALIB_K,
	RS485_CMD_ADDR_TEMP4_CALIB_B,
	RS485_CMD_ADDR_TEMP5_CALIB_K,
	RS485_CMD_ADDR_TEMP5_CALIB_B,
	RS485_CMD_ADDR_TEMP6_CALIB_K,
	RS485_CMD_ADDR_TEMP6_CALIB_B,
	RS485_CMD_ADDR_TEMP_ENV1_CALIB_K,
	RS485_CMD_ADDR_TEMP_ENV1_CALIB_B,
	RS485_CMD_ADDR_TEMP_ENV2_CALIB_K,
	RS485_CMD_ADDR_TEMP_ENV2_CALIB_B,
	RS485_CMD_ADDR_TEMP_ENV3_CALIB_K,
	RS485_CMD_ADDR_TEMP_ENV3_CALIB_B,
	RS485_CMD_ADDR_TEMP_MOS_CALIB_K,
	RS485_CMD_ADDR_TEMP_MOS_CALIB_B,

	RS485_CMD_ADDR_VCELL_OVP_FIRST = 0x2100,
	RS485_CMD_ADDR_VCELL_OVP_SECOND,
	RS485_CMD_ADDR_VCELL_OVP_THIRD,
	RS485_CMD_ADDR_VCELL_OVP_RCV,
	RS485_CMD_ADDR_VCELL_OVP_FILTER,

	RS485_CMD_ADDR_VCELL_UVP_FIRST,
	RS485_CMD_ADDR_VCELL_UVP_SECOND,
	RS485_CMD_ADDR_VCELL_UVP_THIRD,
	RS485_CMD_ADDR_VCELL_UVP_RCV,
	RS485_CMD_ADDR_VCELL_UVP_FILTER,

	RS485_CMD_ADDR_VBUS_OVP_FIRST,
	RS485_CMD_ADDR_VBUS_OVP_SECOND,
	RS485_CMD_ADDR_VBUS_OVP_THIRD,
	RS485_CMD_ADDR_VBUS_OVP_RCV,
	RS485_CMD_ADDR_VBUS_OVP_FILTER,	
	
	RS485_CMD_ADDR_VBUS_UVP_FIRST,
	RS485_CMD_ADDR_VBUS_UVP_SECOND,
	RS485_CMD_ADDR_VBUS_UVP_THIRD,
	RS485_CMD_ADDR_VBUS_UVP_RCV,
	RS485_CMD_ADDR_VBUS_UVP_FILTER,	

	RS485_CMD_ADDR_ICHG_OCP_FIRST,
	RS485_CMD_ADDR_ICHG_OCP_SECOND,
	RS485_CMD_ADDR_ICHG_OCP_THIRD,
	RS485_CMD_ADDR_ICHG_OCP_RCV,
	RS485_CMD_ADDR_ICHG_OCP_FILTER,	

	RS485_CMD_ADDR_IDSG_OCP_FIRST,
	RS485_CMD_ADDR_IDSG_OCP_SECOND,
	RS485_CMD_ADDR_IDSG_OCP_THIRD,
	RS485_CMD_ADDR_IDSG_OCP_RCV,
	RS485_CMD_ADDR_IDSG_OCP_FILTER,	

	RS485_CMD_ADDR_TCHG_OTP_FIRST,
	RS485_CMD_ADDR_TCHG_OTP_SECOND,
	RS485_CMD_ADDR_TCHG_OTP_THIRD,
	RS485_CMD_ADDR_TCHG_OTP_RCV,
	RS485_CMD_ADDR_TCHG_OTP_FILTER,	

	RS485_CMD_ADDR_TCHG_UTP_FIRST,
	RS485_CMD_ADDR_TCHG_UTP_SECOND,
	RS485_CMD_ADDR_TCHG_UTP_THIRD,
	RS485_CMD_ADDR_TCHG_UTP_RCV,
	RS485_CMD_ADDR_TCHG_UTP_FILTER,
	
	RS485_CMD_ADDR_TDSG_OTP_FIRST,
	RS485_CMD_ADDR_TDSG_OTP_SECOND,
	RS485_CMD_ADDR_TDSG_OTP_THIRD,
	RS485_CMD_ADDR_TDSG_OTP_RCV,
	RS485_CMD_ADDR_TDSG_OTP_FILTER, 
	
	RS485_CMD_ADDR_TDSG_UTP_FIRST,
	RS485_CMD_ADDR_TDSG_UTP_SECOND,
	RS485_CMD_ADDR_TDSG_UTP_THIRD,
	RS485_CMD_ADDR_TDSG_UTP_RCV,
	RS485_CMD_ADDR_TDSG_UTP_FILTER, 

	RS485_CMD_ADDR_TMOS_OTP_FIRST,
	RS485_CMD_ADDR_TMOS_OTP_SECOND,
	RS485_CMD_ADDR_TMOS_OTP_THIRD,
	RS485_CMD_ADDR_TMOS_OTP_RCV,
	RS485_CMD_ADDR_TMOS_OTP_FILTER, 

	RS485_CMD_ADDR_VDELTA_OP_FIRST,
	RS485_CMD_ADDR_VDELTA_OP_SECOND,
	RS485_CMD_ADDR_VDELTA_OP_THIRD,
	RS485_CMD_ADDR_VDELTA_OP_RCV,
	RS485_CMD_ADDR_VDELTA_OP_FILTER, 

	RS485_CMD_ADDR_SOC_UP_FIRST,
	RS485_CMD_ADDR_SOC_UP_SECOND,
	RS485_CMD_ADDR_SOC_UP_THIRD,
	RS485_CMD_ADDR_SOC_UP_RCV,
	RS485_CMD_ADDR_SOC_UP_FILTER, 


	RS485_CMD_ADDR_SOC_VOLTAGE1 = 0x2200,
	RS485_CMD_ADDR_SOC_VALUE1,
	RS485_CMD_ADDR_SOC_VOLTAGE2,
	RS485_CMD_ADDR_SOC_VALUE2,
	RS485_CMD_ADDR_SOC_VOLTAGE3,
	RS485_CMD_ADDR_SOC_VALUE3,
	RS485_CMD_ADDR_SOC_VOLTAGE4,
	RS485_CMD_ADDR_SOC_VALUE4,
	RS485_CMD_ADDR_SOC_VOLTAGE5,
	RS485_CMD_ADDR_SOC_VALUE5,
	RS485_CMD_ADDR_SOC_VOLTAGE6,
	RS485_CMD_ADDR_SOC_VALUE6,
	RS485_CMD_ADDR_SOC_VOLTAGE7,
	RS485_CMD_ADDR_SOC_VALUE7,
	RS485_CMD_ADDR_SOC_VOLTAGE8,
	RS485_CMD_ADDR_SOC_VALUE8,
	RS485_CMD_ADDR_SOC_VOLTAGE9,
	RS485_CMD_ADDR_SOC_VALUE9,
	RS485_CMD_ADDR_SOC_VOLTAGE10,
	RS485_CMD_ADDR_SOC_VALUE10,
	RS485_CMD_ADDR_SOC_VOLTAGE11,
	RS485_CMD_ADDR_SOC_VALUE11,
	RS485_CMD_ADDR_SOC_VOLTAGE12,
	RS485_CMD_ADDR_SOC_VALUE12,
	RS485_CMD_ADDR_SOC_VOLTAGE13,
	RS485_CMD_ADDR_SOC_VALUE13,
	RS485_CMD_ADDR_SOC_VOLTAGE14,
	RS485_CMD_ADDR_SOC_VALUE14,
	RS485_CMD_ADDR_SOC_VOLTAGE15,
	RS485_CMD_ADDR_SOC_VALUE15,
	RS485_CMD_ADDR_SOC_VOLTAGE16,
	RS485_CMD_ADDR_SOC_VALUE16,
	RS485_CMD_ADDR_SOC_VOLTAGE17,
	RS485_CMD_ADDR_SOC_VALUE17,
	RS485_CMD_ADDR_SOC_VOLTAGE18,
	RS485_CMD_ADDR_SOC_VALUE18,
	RS485_CMD_ADDR_SOC_VOLTAGE19,
	RS485_CMD_ADDR_SOC_VALUE19,
	RS485_CMD_ADDR_SOC_VOLTAGE20,
	RS485_CMD_ADDR_SOC_VALUE20,
	RS485_CMD_ADDR_SOC_VOLTAGE21,
	RS485_CMD_ADDR_SOC_VALUE21,


	RS485_CMD_ADDR_COPPERLOSS1,		//读取
	RS485_CMD_ADDR_COPPERLOSS2,
	RS485_CMD_ADDR_COPPERLOSS3,
	RS485_CMD_ADDR_COPPERLOSS4,
	RS485_CMD_ADDR_COPPERLOSS5,
	RS485_CMD_ADDR_COPPERLOSS6,
	RS485_CMD_ADDR_COPPERLOSS7,
	RS485_CMD_ADDR_COPPERLOSS8,
	RS485_CMD_ADDR_COPPERLOSS9,
	RS485_CMD_ADDR_COPPERLOSS10,
	RS485_CMD_ADDR_COPPERLOSS11,
	RS485_CMD_ADDR_COPPERLOSS12,
	RS485_CMD_ADDR_COPPERLOSS13,
	RS485_CMD_ADDR_COPPERLOSS14,
	RS485_CMD_ADDR_COPPERLOSS15,
	RS485_CMD_ADDR_COPPERLOSS16,
	RS485_CMD_ADDR_CELLNUM1,
	RS485_CMD_ADDR_CELLNUM2,
	RS485_CMD_ADDR_CELLNUM3,
	RS485_CMD_ADDR_CELLNUM4,
	RS485_CMD_ADDR_CELLNUM5,
	RS485_CMD_ADDR_CELLNUM6,
	RS485_CMD_ADDR_CELLNUM7,
	RS485_CMD_ADDR_CELLNUM8,
	RS485_CMD_ADDR_CELLNUM9,
	RS485_CMD_ADDR_CELLNUM10,
	RS485_CMD_ADDR_CELLNUM11,
	RS485_CMD_ADDR_CELLNUM12,
	RS485_CMD_ADDR_CELLNUM13,
	RS485_CMD_ADDR_CELLNUM14,
	RS485_CMD_ADDR_CELLNUM15,
	RS485_CMD_ADDR_CELLNUM16,

	RS485_CMD_ADDR_RTC_TIME_YEAR,		//读取
	RS485_CMD_ADDR_RTC_TIME_MONTH,
	RS485_CMD_ADDR_RTC_TIME_DAY,
	RS485_CMD_ADDR_RTC_TIME_HOUR,
	RS485_CMD_ADDR_RTC_TIME_MINUTE,
	RS485_CMD_ADDR_RTC_TIME_SECOND,
	RS485_CMD_ADDR_RTC_ALARM_YEAR,
	RS485_CMD_ADDR_RTC_ALARM_MONTH,
	RS485_CMD_ADDR_RTC_ALARM_DAY,	
	RS485_CMD_ADDR_RTC_ALARM_HOUR,
	RS485_CMD_ADDR_RTC_ALARM_MINUTE,
	RS485_CMD_ADDR_RTC_ALARM_SECOND,

	RS485_CMD_ADDR_BALANCE_OV = 0x2300,		//读取
	RS485_CMD_ADDR_BALANCE_OW,
	RS485_CMD_ADDR_BALANCE_CW1,	
	RS485_CMD_ADDR_BALANCE_CW2,
	RS485_CMD_ADDR_OPENTIME_ODD,
	RS485_CMD_ADDR_OPENTIME_EVEN,
	RS485_CMD_ADDR_OPENTIME_MOS, 
	RS485_CMD_ADDR_OPENTIME_RES,
	
	RS485_CMD_ADDR_CS_CUR_CHGMAX,
	RS485_CMD_ADDR_CS_CUR_DSGMAX,
	RS485_CMD_ADDR_CBC_CUR_CHG,
	RS485_CMD_ADDR_CBC_CUR_DSG,
	RS485_CMD_ADDR_COOL_DSG_H,
	RS485_CMD_ADDR_COOL_DSG_L,
	RS485_CMD_ADDR_COOL_CHG_H,
	RS485_CMD_ADDR_COOL_CHG_L,

	RS485_CMD_ADDR_SLEEP_V_NORMAL,
	RS485_CMD_ADDR_SLEEP_TIME_NORMAL,	   
	RS485_CMD_ADDR_SLEEP_V_LOW,
	RS485_CMD_ADDR_SLEEP_TIME_LOW,
	RS485_CMD_ADDR_SLEEP_I_CHG,
	RS485_CMD_ADDR_SLEEP_I_DSG,	   
	RS485_CMD_ADDR_SLEEP_RES1,
	RS485_CMD_ADDR_SLEEP_RES2,

	RS485_CMD_ADDR_SOC_AH,
	RS485_CMD_ADDR_SOC_CYCLE_TIME,
	RS485_CMD_ADDR_SOC_RES1,
	RS485_CMD_ADDR_SOC_RES2,

	RS485_CMD_ADDR_SYS_SERIES_NUM,
	RS485_CMD_ADDR_SYS_CS_RESIS,
	RS485_CMD_ADDR_SYS_CS_NUM,
	RS485_CMD_ADDR_SYS_RES1,

	RS485_CMD_ADDR_HEAT_DSG_HIGH,
	RS485_CMD_ADDR_HEAT_DSG_LOW,
	RS485_CMD_ADDR_HEAT_CHG_HIGH,
	RS485_CMD_ADDR_HEAT_CHG_LOW,
	RS485_CMD_ADDR_HEAT_CUR_MAX,
	RS485_CMD_ADDR_HEAT_CUR_MIN,
	RS485_CMD_ADDR_HEAT_TIME_MAX,
	RS485_CMD_ADDR_HEAT_RES1,
	RS485_CMD_ADDR_HEAT_RES2,
	RS485_CMD_ADDR_HEAT_RES3,
	RS485_CMD_ADDR_HEAT_RES4,
	RS485_CMD_ADDR_HEAT_RES5,
	RS485_CMD_ADDR_HEAT_RES6,
	
	RS485_CMD_ADDR_COOL_DSG_HIGH,
	RS485_CMD_ADDR_COOL_DSG_LOW,
	RS485_CMD_ADDR_COOL_CHG_HIGH,
	RS485_CMD_ADDR_COOL_CHG_LOW,
	RS485_CMD_ADDR_COOL_CUR_MAX,	   
	RS485_CMD_ADDR_COOL_CUR_MIN,
	RS485_CMD_ADDR_COOL_TIME_MAX,
	RS485_CMD_ADDR_COOL_RES1,
	RS485_CMD_ADDR_COOL_RES2,	   
	RS485_CMD_ADDR_COOL_RES3,
	RS485_CMD_ADDR_COOL_RES4,
};


extern UINT8 u8FlashUpdateFlag;
extern UINT8 u8FlashUpdateE2PROM;
extern UINT8 gu8_TxEnable_SCI1;
extern UINT8 gu8_TxEnable_SCI2;

extern struct RS485MSG g_stCurrentMsgPtr_SCI1;
extern struct RS485MSG g_stCurrentMsgPtr_SCI2;

extern struct stCell_Info g_stCellInfoReport;

extern UINT8 RTC_ExtComCnt1;

extern UINT8  BlueToothFlag ;//用于判断蓝牙是否在显示

//UINT8 RTC_ExtComCnt1 = 0;
extern uint16_t SuspendFlag1;
extern uint16_t SuspendFlag2;



void Sci1_CommonUpper_FaultChk(void);
void Sci1_CommonUpper_Rx_Deal(struct RS485MSG *s);
void Sci2_CommonUpper_FaultChk(void);
void Sci2_CommonUpper_Rx_Deal(struct RS485MSG *s);


void InitUSART_CommonUpper(void);
void App_CommonUpper(void);

#endif	/* SCI_H */

void USART1_IRQHandler(void)
{
  Sci1_CommonUpper_FaultChk();
  if (USART_GetITStatus(USART1, USART_IT_RXNE) != RESET)
  {
    RTC_ExtComCnt++;
    RTC_ExtComCnt1++;

#if (defined _COMMOM_UPPER_SCI1)
    Sci1_CommonUpper_Rx_Deal(&g_stCurrentMsgPtr_SCI1);
#endif
  }
}

void USART2_IRQHandler(void)
{
  Sci2_CommonUpper_FaultChk();
  if (USART_GetITStatus(USART2, USART_IT_RXNE) != RESET)
  {
    RTC_ExtComCnt++;

#ifdef _COMMOM_UPPER_SCI2
    Sci2_CommonUpper_Rx_Deal(&g_stCurrentMsgPtr_SCI2);
#endif
  }
}

以下是我需要新加入的、需要兼容的协议，同时满足我之前的modbus和新加入的协议
#ifndef _ascii_slave_h_
#define _ascii_slave_h_
#include "main.h"

/************************* 协议固定宏定义 *************************/
// 帧首尾固定值
#define SOI                     0x7E       // 帧起始标志
#define EOI                     0x0D       // 帧结束标志
// 固定CID1  
#define CID1_BAT_DATA           0x46       // 电池数据类固定CID1
// 4种核心命令CID2定义
#define CMD_GET_BATTERY_INFO    0x60       // 获取电池组系统基本信息
#define CMD_GET_ANALOG_DATA     0x61       // 获取电池系统运行模拟量信息
#define CMD_GET_ALARM_INFO      0x62       // 获取电池组系统状态告警量信息
#define CMD_GET_CHARGE_DIS_INFO 0x63       // 获取电池组系统充放电管理交互信息
   
// 响应返回码定义 
#define RTN_OK                  0x00       // 正常响应
#define RTN_VER_ERROR           0x01       // 版本错误
#define RTN_CHKSUM_ERROR        0x02       // 整帧校验错误
#define RTN_LCHKSUM_ERROR       0x03       // 长度校验错误
#define RTN_CID2_INVALID        0x04       // CID2命令码无效
#define RTN_FORMAT_ERROR        0x05       // 命令格式错误
#define RTN_DATA_INVALID        0x06       // 数据无效
#define RTN_ADR_ERROR           0x90       // 地址错误
#define RTN_COMM_ERROR          0x91       // 内部通信错误
// 从机配置
#define PROTOCOL_VERSION        0x20       //协议版本号
#define SLAVE_ADDRESS           0x12       // 本机从机地址（协议要求从2开始）
#define MAX_FRAME_LEN           600        // 最大帧长度
#define CELL_MAX_NUM            16         // 最大电芯数量（48V电池16串）
// RS485控制引脚定义
#define RS485_CTRL_PORT         GPIOA
#define RS485_CTRL_PIN          LL_GPIO_PIN_8
#define RS485_TX_ENABLE()       LL_GPIO_SetOutputPin(RS485_CTRL_PORT, RS485_CTRL_PIN)
#define RS485_RX_ENABLE()       LL_GPIO_ResetOutputPin(RS485_CTRL_PORT, RS485_CTRL_PIN)

/************************* 数据结构体定义 *************************/

// 设备基础信息
typedef struct {
    uint8_t  device_name[10];              // 主机设备名称，10字节ASCII
    uint8_t  manufactory_name[20];         // 主机厂商名称，20字节ASCII
    uint8_t  software_ver[2];              // 主机软件版本，2字节
    uint8_t  battery_num;                  // 电池数量
    uint8_t  battery_barcode[CELL_MAX_NUM][16];// 电池1~16条形码
} Battery_Base_Info_T;

// 模拟量数据
typedef struct {
    // 电池组系统核心参数
    uint16_t pack_total_avg_voltage;       // 电池组系统总平均电压
    int16_t pack_total_current;            // 电池组系统总电流
    uint8_t pack_soc;                      // 电池组系统SOC (State of Charge)
    uint16_t pack_avg_cycle_count;         // 平均循环次数
    uint16_t pack_max_cycle_count;         // 最大循环次数
    uint8_t pack_avg_soh;                  // 平均 SOH (State of Health)
    uint8_t pack_min_soh;                  // 最小 SOH
                                           
    // 电芯电压监测                         
    uint16_t cell_max_voltage;             // 单芯最高电压
    uint16_t cell_max_voltage_module;      // 单芯最高电压所在模块
    uint16_t cell_min_voltage;             // 单芯最低电压
    uint16_t cell_min_voltage_module;      // 单芯最低电压所在模块
                                           
    // 电芯温度监测                         
    int16_t cell_avg_temp;                 // 单芯平均温度
    int16_t cell_max_temp;                 // 单芯最高温度
    uint16_t cell_max_temp_module;         // 单芯最高温度所在模块
    int16_t cell_min_temp;                 // 单芯最低温度
    uint16_t cell_min_temp_module;         // 单芯最低温度所在模块
                                           
    // MOSFET 温度监测                     
    int16_t mosfet_avg_temp;               // MOSFET 平均温度
    int16_t mosfet_max_temp;               // MOSFET 最高温度
    uint16_t mosfet_max_temp_module;       // MOSFET 最高温度所在模块
    int16_t mosfet_min_temp;               // MOSFET 最低温度
    uint16_t mosfet_min_temp_module;       // MOSFET 最低温度所在模块
                                           
    // BMS 板载温度监测                     
    int16_t bms_avg_temp;                  // BMS 平均温度
    int16_t bms_max_temp;                  // BMS 最高温度
    uint16_t bms_max_temp_module;          // BMS 最高温度所在模块
    int16_t bms_min_temp;                  // BMS 最低温度
    uint16_t bms_min_temp_module;          // BMS 最低温度所在模块
} Battery_Analog_T;   
   
// 告警状态信息  
typedef struct {   
    uint8_t system_alarm1;                 // 系统告警状态 1         
    uint8_t system_alarm2;                 // 系统告警状态 2     
    uint8_t system_protect1;               // 系统保护状态 1    
    uint8_t system_protect2;               // 系统保护状态 2                             
} Battery_Alarm_T; 
   
// 充放电管理信息 
typedef struct {   
    uint16_t charge_volt_limit;            // 充电电压建议上限，单位mV
    uint16_t discharge_volt_limit;         // 放电电压建议下限，单位mV
    int16_t  max_charge_current;           // 最大充电电流，单位0.01A
    int16_t  max_discharge_current;        // 最大放电电流，单位0.01A
    uint8_t  charge_dis_status;            // 充放电状态
} Battery_Charge_Dis_Info_T;

// 电池全局数据结构体
typedef struct {
    Battery_Base_Info_T     base_info;            // 设备基础信息
    Battery_Analog_T        analog_data;          // 电芯与模拟量数据
    Battery_Alarm_T         alarm_info;           // 告警状态信息
    Battery_Charge_Dis_Info_T charge_dis_info;    // 充放电管理信息
} Battery_Data_T;


extern uint8_t uart_rx_buf[MAX_FRAME_LEN];
extern uint16_t uart_rx_len;
extern uint8_t frame_received_flag;
extern Battery_Data_T g_battery_data;

uint8_t Hex_To_Ascii(uint8_t hex);
uint8_t Ascii_To_Hex(uint8_t ascii);
uint8_t VerToHex(uint8_t * ver);
uint8_t Calc_LCHKSUM(uint16_t lenid);
uint16_t Calc_CHKSUM(uint8_t *data, uint16_t len);
uint16_t Build_LENGTH_Field(uint16_t lenid);
uint8_t Parse_LENGTH_Field(uint16_t length_field, uint16_t *lenid);
uint16_t Build_Response_Frame(uint8_t *tx_buf, uint8_t ver, uint8_t adr, uint8_t rtn, uint8_t *info_data, uint16_t info_hex_len);
uint16_t Cmd_Handle_Manufactory_Info(uint8_t *tx_buf);
uint16_t Cmd_Handle_Analog_Value(uint8_t *tx_buf);
uint16_t Cmd_Handle_Alarm_Info(uint8_t *tx_buf);
uint16_t Cmd_Handle_Charge_Dis_Info(uint8_t *tx_buf);
void Frame_Parse_Process(void);

void Ascii_Send_Byte(uint8_t Modbus_byte,UART_TypeDef *UARTx);
void Ascii_Send_NByte(uint8_t *buff,uint16_t len,UART_TypeDef *UARTx);


#endif

#include "main.h"
#include "stdio.h"
#include "ascii_slave.h"
#include "uart.h"
#include "modbus_host.h"
#include "CRC.h"
#include "string.h"
#include "led.h"


/************************* 全局变量定义 *************************/
// 串口接收缓冲区与状态机
uint8_t uart_rx_buf[MAX_FRAME_LEN] = {0};
uint16_t uart_rx_len = 0;
uint8_t frame_received_flag = 0;

void Ascii_slave(void)
{
    
}

/************************* 电池数据初始化（固定默认值） *************************/
Battery_Data_T g_battery_data = 
{
    // 基础信息
    .base_info = {
        
        .device_name = {'F', 'o', 'r', 'c', 'e', '_', 'L', 0, 0, 0},//Force_L
        .manufactory_name = {'P','y','l','o','n',0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},   
        .software_ver = {0, 9},//9
        .battery_num = CELL_MAX_NUM,
        .battery_barcode = {
            {0x30,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "0123456789abcdef"
            {0x31,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "1123456789abcdef"
            {0x32,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "2123456789abcdef"
            {0x33,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "3123456789abcdef"
            {0x34,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "4123456789abcdef"
            {0x35,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "5123456789abcdef"
            {0x36,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "6123456789abcdef"
            {0x37,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "7123456789abcdef"
            {0x38,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "8123456789abcdef"
            {0x39,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "9123456789abcdef"
            {0x61,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "a123456789abcdef"
            {0x62,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "b123456789abcdef"
            {0x63,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "c123456789abcdef"
            {0x64,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "d123456789abcdef"
            {0x65,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "e123456789abcdef"
            {0x66,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}  // "f123456789abcdef"
        }   
    },
    // 模拟量数据（匹配协议示例值）
    .analog_data = {
        // 电池组系统核心参数
        .pack_total_avg_voltage = 0x2E53,       // 电池组系统总平均电压
        .pack_total_current = 0x61A8,           // 电池组系统总电流
        .pack_soc = 0x62,                       // 电池组系统SOC (State of Charge)
        .pack_avg_cycle_count = 0x09D4,         // 平均循环次数
        .pack_max_cycle_count = 0x0B74,         // 最大循环次数
        .pack_avg_soh = 0x62,                   // 平均 SOH (State of Health)
        .pack_min_soh = 0x61,                   // 最小 SOH                                   .                        
        
        // 电芯电压监测 
        .cell_max_voltage = 0x0DB8,             // 单芯最高电压
        .cell_max_voltage_module = 0x0304,      // 单芯最高电压所在模块
        .cell_min_voltage = 0x0CBB,             // 单芯最低电压
        .cell_min_voltage_module = 0x0104,      // 单芯最低电压所在模块
                                       
        // 电芯温度监测                         
        .cell_avg_temp = 0x0BAA,                // 单芯平均温度
        .cell_max_temp = 0x0BB7,                // 单芯最高温度
        .cell_max_temp_module = 0x0305,         // 单芯最高温度所在模块
        .cell_min_temp = 0x0B9D,                // 单芯最低温度
        .cell_min_temp_module = 0x0105,         // 单芯最低温度所在模块
                                           
        // MOSFET 温度监测                     
        .mosfet_avg_temp = 0x0BAA,              // MOSFET 平均温度
        .mosfet_max_temp = 0x0BB8,              // MOSFET 最高温度
        .mosfet_max_temp_module = 0x0306,       // MOSFET 最高温度所在模块
        .mosfet_min_temp = 0x0B9C,              // MOSFET 最低温度
        .mosfet_min_temp_module = 0x0106,       // MOSFET 最低温度所在模块
                                            
        // BMS 板载温度监测                     
        .bms_avg_temp = 0x0BAA,                 // BMS 平均温度
        .bms_max_temp = 0x0BB6,                 // BMS 最高温度
        .bms_max_temp_module = 0x0307,          // BMS 最高温度所在模块
        .bms_min_temp = 0x0B9E,                 // BMS 最低温度
        .bms_min_temp_module = 0x0107           // BMS 最低温度所在模块
    },
    // 告警信息（默认全正常）
    .alarm_info = {
        .system_alarm1 = 0x0,  
        .system_alarm2 = 0x0,  
        .system_protect1 = 0x0,
        .system_protect2 = 0x0    
    },
    // 充放电管理信息
    .charge_dis_info = {
        .charge_volt_limit = 0xDCD3, // 56.531V
        .discharge_volt_limit = 0x5DC0, // 24.00V
        .max_charge_current = 0x09C4, // 25.0A
        .max_discharge_current = 0x07E4, // 20.2A
        .charge_dis_status = 0xC0 // 允许充放电
    }
};


/************************* 工具函数实现 *************************/
/**
 * @brief  HEX半字节转ASCII码
 * @param  hex: 0-15的半字节数据
 * @retval 转换后的ASCII码
 */
uint8_t Hex_To_Ascii(uint8_t hex)
{
    hex &= 0x0F;
    if(hex < 10) return hex + '0';
    else return hex - 10 + 'A';
}

/**
 * @brief  ASCII码转HEX半字节
 * @param  ascii: 0-9/A-F的ASCII码
 * @retval 转换后的HEX半字节（0-15），失败返回0xFF
 */
uint8_t Ascii_To_Hex(uint8_t ascii)
{
    if(ascii >= '0' && ascii <= '9') return ascii - '0';
    else if(ascii >= 'A' && ascii <= 'F') return ascii - 'A' + 10;
    else if(ascii >= 'a' && ascii <= 'f') return ascii - 'a' + 10;
    else return 0xFF;
}

/**
 * @brief  将2字节的版本号转成一字节HEX
 * @param  版本号
 * @retval 一字节HEX形式的版本号
 */
uint8_t VerToHex(uint8_t * ver)
{
    if(ver == NULL)
    {
        return 0;
    }
    
    uint8_t hex = 0;
    for(int i = 0; i < 2; i++)
    {
        hex = (16 * hex) + (ver[i] - 0x30); 
    }
    
    return hex;
}

/**
 * @brief  计算LCHKSUM长度校验码
 * @param  lenid: 12位的INFO ASCII字节数
 * @retval 4位LCHKSUM校验值
 */
uint8_t Calc_LCHKSUM(uint16_t lenid)
{
    uint8_t seg1 = (lenid >> 8) & 0x0F; // D11-D8
    uint8_t seg2 = (lenid >> 4) & 0x0F; // D7-D4
    uint8_t seg3 = lenid & 0x0F;         // D3-D0
    uint8_t sum = seg1 + seg2 + seg3;
    sum = sum % 16;
    return (~sum + 1) & 0x0F;
}

/**
 * @brief  计算CHKSUM整帧校验码
 * @param  data: 待校验的数据（除SOI、EOI、CHKSUM外的所有ASCII字符）
 * @param  len: 数据长度
 * @retval 16位CHKSUM校验值
 */
uint16_t Calc_CHKSUM(uint8_t *data, uint16_t len)
{
    uint32_t sum = 0;
    for(uint16_t i = 0; i < len; i++)
    {
        sum += data[i];
    }
    sum = sum % 65536;
    return (uint16_t)(~sum + 1);
}

/**
 * @brief  组装LENGTH字段（2字节HEX）
 * @param  lenid: INFO的ASCII字节数
 * @retval 16位LENGTH字段值
 */
uint16_t Build_LENGTH_Field(uint16_t lenid)
{
    uint8_t lchksum = Calc_LCHKSUM(lenid);
    return (uint16_t)((lchksum << 12) | (lenid & 0x0FFF));
}

/**
 * @brief  解析LENGTH字段，校验LCHKSUM
 * @param  length_field: 16位LENGTH字段
 * @param  lenid: 输出解析后的LENID
 * @retval true=校验通过，false=校验失败
 */
uint8_t Parse_LENGTH_Field(uint16_t length_field, uint16_t *lenid)
{
    uint8_t lchksum_rx = (length_field >> 12) & 0x0F;
    *lenid = length_field & 0x0FFF;
    uint8_t lchksum_calc = Calc_LCHKSUM(*lenid);
    return (lchksum_rx == lchksum_calc);
}

/************************* 命令处理函数实现 *************************/
/**
 * @brief  通用响应帧组装函数
 * @param  tx_buf: 发送缓冲区
 * @param  ver: 协议版本号
 * @param  adr: 从机地址
 * @param  rtn: 响应返回码
 * @param  info_data: INFO域的HEX数据
 * @param  info_hex_len: INFO域HEX数据长度（字节数）
 * @retval 组装完成的帧总长度
 */
uint16_t Build_Response_Frame(uint8_t *tx_buf, uint8_t ver, uint8_t adr, uint8_t rtn, uint8_t *info_data, uint16_t info_hex_len)
{
    uint16_t idx = 0;
    // 1. 帧起始SOI
    tx_buf[idx++] = SOI;
    // 2. VER和ADR（转ASCII）
    tx_buf[idx++] = Hex_To_Ascii(ver >> 4);
    tx_buf[idx++] = Hex_To_Ascii(ver & 0x0F);
    tx_buf[idx++] = Hex_To_Ascii(adr >> 4);
    tx_buf[idx++] = Hex_To_Ascii(adr & 0x0F);
    // 3. CID1固定0x46（转ASCII）
    tx_buf[idx++] = Hex_To_Ascii(CID1_BAT_DATA >> 4);
    tx_buf[idx++] = Hex_To_Ascii(CID1_BAT_DATA & 0x0F);
    // 4. RTN响应码（转ASCII）
    tx_buf[idx++] = Hex_To_Ascii(rtn >> 4);
    tx_buf[idx++] = Hex_To_Ascii(rtn & 0x0F);
    // 5. 计算LENGTH字段
    uint16_t lenid = info_hex_len * 2; // INFO的ASCII字节数=HEX长度*2
    uint16_t length_field = Build_LENGTH_Field(lenid);
    tx_buf[idx++] = Hex_To_Ascii(length_field >> 12);
    tx_buf[idx++] = Hex_To_Ascii((length_field >> 8) & 0x0F);
    tx_buf[idx++] = Hex_To_Ascii((length_field >> 4) & 0x0F);
    tx_buf[idx++] = Hex_To_Ascii(length_field & 0x0F);
    
    // 6. 填充INFO域（HEX转ASCII）
    for(uint16_t i = 0; i < info_hex_len; i++)
    {
        tx_buf[idx++] = Hex_To_Ascii((info_data[i] >> 4) & 0x0F);
        tx_buf[idx++] = Hex_To_Ascii(info_data[i] &0x0F);
    }
    
    // 7. 计算CHKSUM（SOI之后，CHKSUM之前的所有ASCII字符）
    uint16_t chksum = Calc_CHKSUM(&tx_buf[1], idx - 1);
    tx_buf[idx++] = Hex_To_Ascii(chksum >> 12);
    tx_buf[idx++] = Hex_To_Ascii((chksum >> 8) & 0x0F);
    tx_buf[idx++] = Hex_To_Ascii((chksum >> 4) & 0x0F);
    tx_buf[idx++] = Hex_To_Ascii(chksum & 0x0F);
    // 8. 帧结束EOI
    tx_buf[idx++] = EOI;
    return idx;
}

/**
 * @brief  0x60 获取厂商信息处理
 */
uint16_t Cmd_Handle_Manufactory_Info(uint8_t *tx_buf)
{
    uint8_t info_buf[MAX_FRAME_LEN] = {0};
    uint16_t idx = 0; 

    // 设备名称10字节
    memcpy(&info_buf[idx], g_battery_data.base_info.device_name, 10);
    idx += 10;
    // 厂商名称20字节
    memcpy(&info_buf[idx], g_battery_data.base_info.manufactory_name, 20);
    idx += 20;
    // 软件版本2字节
    memcpy(&info_buf[idx], g_battery_data.base_info.software_ver, 2);
    idx += 2;
    // 电池数量 
    info_buf[idx++] = g_battery_data.base_info.battery_num;
    // 条形码
    uint16_t i;
    for(i = 0; i < g_battery_data.base_info.battery_num; i++)
    {
        memcpy(&info_buf[idx], g_battery_data.base_info.battery_barcode[i], 16);
        idx += 16;
    }
    
    return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

/**
 * @brief  0x61 获取模拟量量化数据处理
 */
uint16_t Cmd_Handle_Analog_Value(uint8_t *tx_buf)
{
    uint8_t info_buf[100] = {0};
    uint16_t idx = 0;
    
    // 电池组系统总平均电压
    info_buf[idx++] = (g_battery_data.analog_data.pack_total_avg_voltage >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.pack_total_avg_voltage & 0xFF;
    // 电池组系统总电流
    info_buf[idx++] = (g_battery_data.analog_data.pack_total_current >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.pack_total_current & 0xFF;
    // 电池组系统 SOC
    info_buf[idx++] = g_battery_data.analog_data.pack_soc;
    // 平均循环次数
    info_buf[idx++] = (g_battery_data.analog_data.pack_avg_cycle_count >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.pack_avg_cycle_count & 0xFF;
    // 最大循环次数
    info_buf[idx++] = (g_battery_data.analog_data.pack_max_cycle_count >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.pack_max_cycle_count & 0xFF;
    // 平均 SOH (State of Health)
    info_buf[idx++] = g_battery_data.analog_data.pack_avg_soh;
    // 最小 SOH
    info_buf[idx++] = g_battery_data.analog_data.pack_min_soh;
    // 单芯最高电压
    info_buf[idx++] = (g_battery_data.analog_data.cell_max_voltage >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_max_voltage & 0xFF;
    // 单芯最高电压所在模块
    info_buf[idx++] = (g_battery_data.analog_data.cell_max_voltage_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_max_voltage_module & 0xFF;
    // 单芯最低电压
    info_buf[idx++] = (g_battery_data.analog_data.cell_min_voltage >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_min_voltage & 0xFF;
    // 单芯最低电压所在模块
    info_buf[idx++] = (g_battery_data.analog_data.cell_min_voltage_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_min_voltage_module & 0xFF;
    // 单芯平均温度
    info_buf[idx++] = (g_battery_data.analog_data.cell_avg_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_avg_temp & 0xFF;
    // 单芯最高温度
    info_buf[idx++] = (g_battery_data.analog_data.cell_max_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_max_temp & 0xFF;
    // 单芯最高温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data.cell_max_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_max_temp_module & 0xFF;
    // 单芯最低温度
    info_buf[idx++] = (g_battery_data.analog_data.cell_min_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_min_temp & 0xFF;
    // 单芯最低温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data.cell_min_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_min_temp_module & 0xFF;
    // MOSFET 平均温度
    info_buf[idx++] = (g_battery_data.analog_data.mosfet_avg_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.mosfet_avg_temp & 0xFF;
    // MOSFET 最高温度
    info_buf[idx++] = (g_battery_data.analog_data.mosfet_max_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.mosfet_max_temp & 0xFF;
    // MOSFET 最高温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data.mosfet_max_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.mosfet_max_temp_module & 0xFF;
    // MOSFET 最低温度
    info_buf[idx++] = (g_battery_data.analog_data.mosfet_min_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.mosfet_min_temp & 0xFF;
    // MOSFET 最低温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data.mosfet_min_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.mosfet_min_temp_module & 0xFF;
    // BMS 平均温度
    info_buf[idx++] = (g_battery_data.analog_data.bms_avg_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.bms_avg_temp & 0xFF;
    // BMS 最高温度
    info_buf[idx++] = (g_battery_data.analog_data.bms_max_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.bms_max_temp & 0xFF;
    // BMS 最高温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data.bms_max_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.bms_max_temp_module & 0xFF;
    // BMS 最低温度
    info_buf[idx++] = (g_battery_data.analog_data.bms_min_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.bms_min_temp & 0xFF;
    // BMS 最低温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data.bms_min_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.bms_min_temp_module & 0xFF;
    
    return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

/**
 * @brief  0x62 获取电池组系统状态告警量信息
 */
uint16_t Cmd_Handle_Alarm_Info(uint8_t *tx_buf)
{
    uint8_t info_buf[16] = {0};
    uint16_t idx = 0;
    Battery_Alarm_T *p = &g_battery_data.alarm_info;
    
    // 系统告警状态 1
    info_buf[idx++] = p->system_alarm1;
    // 系统告警状态 2
    info_buf[idx++] = p->system_alarm2;
    // 系统保护状态 1
    info_buf[idx++] = p->system_protect1;
    // 系统保护状态 2
    info_buf[idx++] = p->system_protect2;
    
    return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

/**
 * @brief  0x63 获取电池组系统充放电管理交互信息
 */
uint16_t Cmd_Handle_Charge_Dis_Info(uint8_t *tx_buf)
{
    uint8_t info_buf[16] = {0};
    uint16_t idx = 0;
    Battery_Charge_Dis_Info_T *p = &g_battery_data.charge_dis_info;
    
    // 充电电压建议上限
    info_buf[idx++] = (p->charge_volt_limit >> 8) & 0xFF;
    info_buf[idx++] = p->charge_volt_limit & 0xFF;
    // 放电电压建议下限
    info_buf[idx++] = (p->discharge_volt_limit >> 8) & 0xFF;
    info_buf[idx++] = p->discharge_volt_limit & 0xFF;
    // 最大充电电流
    info_buf[idx++] = (p->max_charge_current >> 8) & 0xFF;
    info_buf[idx++] = p->max_charge_current & 0xFF;
    // 最大放电电流
    info_buf[idx++] = (p->max_discharge_current >> 8) & 0xFF;
    info_buf[idx++] = p->max_discharge_current & 0xFF;
    // 充放电状态
    info_buf[idx++] = p->charge_dis_status;
    return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

/**
 * @brief  接收帧解析与命令分发
 */
void Frame_Parse_Process(void)
{
    if(!frame_received_flag) return;
    frame_received_flag = 0;
    uint8_t *rx_buf = uart_rx_buf;
    uint16_t rx_len = uart_rx_len;
    uint8_t tx_buf[MAX_FRAME_LEN] = {0};
    uint16_t tx_len = 0;
    // 最小帧长度校验（SOI+VER+ADR+CID1+CID2+LENGTH+CHKSUM+EOI = 1+2+2+2+2+4+4+1=18字节）
    if(rx_len < 18)
    {
        return;
    }
    // 1. 解析基础字段（ASCII转HEX）
    uint8_t ver = (Ascii_To_Hex(rx_buf[1]) << 4) | Ascii_To_Hex(rx_buf[2]);
    uint8_t adr = (Ascii_To_Hex(rx_buf[3]) << 4) | Ascii_To_Hex(rx_buf[4]);
    uint8_t cid1 = (Ascii_To_Hex(rx_buf[5]) << 4) | Ascii_To_Hex(rx_buf[6]);
    uint8_t cid2 = (Ascii_To_Hex(rx_buf[7]) << 4) | Ascii_To_Hex(rx_buf[8]);
    uint16_t length_field = (Ascii_To_Hex(rx_buf[9]) << 12) | (Ascii_To_Hex(rx_buf[10]) << 8) | (Ascii_To_Hex(rx_buf[11]) << 4) | Ascii_To_Hex(rx_buf[12]);
    // 2. 地址校验：只处理本机地址
    if(adr != SLAVE_ADDRESS)
    {
        tx_len = Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_ADR_ERROR, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 3. CID1校验
    if(cid1 != CID1_BAT_DATA)
    {
        tx_len = Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_CID2_INVALID, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 4. LENGTH字段校验
    uint16_t lenid = 0;
    if(!Parse_LENGTH_Field(length_field, &lenid))
    {
        tx_len = Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_LCHKSUM_ERROR, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 5. 帧长度校验
    uint16_t expect_len = 13 + lenid + 4 + 1; // SOI+基础字段+INFO+CHKSUM+EOI
    if(rx_len != expect_len)
    {
        tx_len = Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_FORMAT_ERROR, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 6. CHKSUM整帧校验
    uint16_t chksum_rx = (Ascii_To_Hex(rx_buf[13+lenid]) << 12) | (Ascii_To_Hex(rx_buf[13+lenid+1]) << 8) | (Ascii_To_Hex(rx_buf[13+lenid+2]) << 4) | Ascii_To_Hex(rx_buf[13+lenid+3]);
    uint16_t chksum_calc = Calc_CHKSUM(&rx_buf[1], 12 + lenid);
    if(chksum_rx != chksum_calc)
    {
        tx_len = Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_CHKSUM_ERROR, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 7. 命令分发处理
    switch(cid2)
    {
        case CMD_GET_BATTERY_INFO:
            tx_len = Cmd_Handle_Manufactory_Info(tx_buf);
            break;
        case CMD_GET_ANALOG_DATA:
            tx_len = Cmd_Handle_Analog_Value(tx_buf);
            break;
        case CMD_GET_ALARM_INFO:
            tx_len = Cmd_Handle_Alarm_Info(tx_buf);
            break;
        case CMD_GET_CHARGE_DIS_INFO:
            tx_len = Cmd_Handle_Charge_Dis_Info(tx_buf);
            break;
        default:
            tx_len = Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_CID2_INVALID, NULL, 0);
            break;
    }

    // 8. 发送响应帧
    if(tx_len > 0)
    {
        LED_L4851_ON();
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
    }
    // 清空接收缓冲区
    memset(uart_rx_buf, 0, MAX_FRAME_LEN);
    uart_rx_len = 0;
}

//Ascii串口发送一个字节数据
void Ascii_Send_Byte(uint8_t Modbus_byte,UART_TypeDef *UARTx)
{  
	LL_UART_TransmitData8(UARTx, Modbus_byte);   
	while (!LL_UART_IsActiveFlag_TXCF(UARTx));
	LL_UART_ClearFlag_TXCF(UARTx);                 
}     


//Ascii串口发送N个字节数据
void Ascii_Send_NByte(uint8_t *buff,uint16_t len,UART_TypeDef *UARTx)
{    
	uint16_t t;
    RS485_TX_ENABLE();
	LL_mDelay(1); // 确保DE引脚稳定
	for(t=0;t<len;t++)
	{
		while (!LL_UART_IsActiveFlag_TXEF(UARTx));
		LL_UART_TransmitData8(UARTx, buff[t]);   
		while (!LL_UART_IsActiveFlag_TXCF(UARTx));
		LL_UART_ClearFlag_TXCF(UARTx);
	}		
	LL_mDelay(1); // 确保DE引脚稳定
    RS485_RX_ENABLE();
}
同时我需要优化已有的串口接收、发送框架，目前的架构是否太复杂和效率低





整理一份嵌入式软件头文件包含规范

void USART1_IRQHandler(void)
{
#if (defined _COMMOM_UPPER_SCI1)
  Comm_PortIrqHandler(&g_comm_port1);
#endif
}

#include "Comm.h"

#include "ascii_slave.h"
#include "SleepDeal.h"
#include "System_Init.h"
#include "BSP\\bsp.h"
#include "BSP\\bsp_timer.h"
#include "conf_gpio.h"
#include "modbus_proto.h"
#include "modbus_service.h"
#include "main.h"

CommPortContext g_comm_port1;
CommPortContext g_comm_port2;
static struct RS485MSG g_modbus_service_ctx;
static void Comm_DefaultEnterCritical(void);
static void Comm_DefaultExitCritical(void);
static int32_t Comm_DefaultGetRuntimeMs(void);
static int32_t Comm_DefaultCheckElapsedMs(int32_t last_tick);
static void Comm_DefaultSetRs485TxMode(uint8_t port_id);
static void Comm_DefaultSetRs485RxMode(uint8_t port_id);
static void Comm_DefaultDelayUs(uint32_t us);
static void Comm_DefaultNotifyRxActivity(uint8_t port_id);
static void Comm_DefaultNotifyTxComplete(uint8_t port_id);

static const CommPlatformOps g_comm_platform_ops = {
    Comm_DefaultEnterCritical,
    Comm_DefaultExitCritical,
    Comm_DefaultGetRuntimeMs,
    Comm_DefaultCheckElapsedMs,
    Comm_DefaultSetRs485TxMode,
    Comm_DefaultSetRs485RxMode,
    Comm_DefaultDelayUs,
    Comm_DefaultNotifyRxActivity,
    Comm_DefaultNotifyTxComplete
};

static const CommPortConfig g_comm_port1_config = {
    USART1,
    1,
    USART1_IRQn,
    ENABLE,
    RCC_APB2Periph_USART1,
    GPIOA,
    GPIO_Pin_9 | GPIO_Pin_10,
    GPIO_PinSource9,
    GPIO_PinSource10,
    GPIO_AF_1,
    115200
};

static const CommPortConfig g_comm_port2_config = {
    USART2,
    2,
    USART2_IRQn,
    ENABLE,
    RCC_APB1Periph_USART2,
    GPIOA,
    GPIO_Pin_2 | GPIO_Pin_3,
    GPIO_PinSource2,
    GPIO_PinSource3,
    GPIO_AF_1,
    19200
};

static uint16_t Comm_RingNext(uint16_t index)
{
    return (uint16_t)((index + 1U) % COMM_RX_RING_SIZE);
}

static uint8_t Comm_RingIsEmpty(const CommPortContext *ctx)
{
    return (uint8_t)(ctx->ring_head == ctx->ring_tail);
}

static uint8_t Comm_RingPushByte(CommPortContext *ctx, uint8_t byte)
{
    uint16_t next_head;

    next_head = Comm_RingNext(ctx->ring_head);
    if (next_head == ctx->ring_tail)
    {
        return 0;
    }

    ctx->ring_buf[ctx->ring_head] = byte;
    ctx->ring_head = next_head;
    return 1;
}

static uint8_t Comm_RingPopByte(CommPortContext *ctx, uint8_t *byte)
{
    if (Comm_RingIsEmpty(ctx) != 0)
    {
        return 0;
    }

    *byte = ctx->ring_buf[ctx->ring_tail];
    ctx->ring_tail = Comm_RingNext(ctx->ring_tail);
    return 1;
}

static void Comm_PortDisableRx(CommPortContext *ctx)
{
    USART_ITConfig(ctx->instance, USART_IT_RXNE, DISABLE);
    ctx->instance->CR1 &= ~(1 << 2);
}

static void Comm_PortEnableRx(CommPortContext *ctx)
{
    ctx->instance->CR1 |= (1 << 2);
    USART_ITConfig(ctx->instance, USART_IT_RXNE, ENABLE);
}

static void Comm_PortDisableTxInterrupts(CommPortContext *ctx)
{
    USART_ITConfig(ctx->instance, USART_IT_TXE, DISABLE);
    USART_ITConfig(ctx->instance, USART_IT_TC, DISABLE);
}

static void Comm_PortFinishTx(CommPortContext *ctx)
{
    ctx->ops->delay_us(COMM_RS485_TURNAROUND_US);
    ctx->ops->set_rs485_rx_mode(ctx->port_id);
    ctx->tx_active = 0;
    ctx->tx_len = 0;
    ctx->tx_pos = 0;
    Comm_PortDisableTxInterrupts(ctx);
    Comm_PortEnableRx(ctx);
    ctx->ops->notify_tx_complete(ctx->port_id);
}

static void Comm_PortResetRx(CommPortContext *ctx)
{
    ctx->ops->enter_critical();
    ctx->active_protocol = PROTO_NONE;
    ctx->frame_ready_flag = 0;
    ctx->rx_len = 0;
    ctx->rx_timeout_ms = 0;
    ctx->ring_head = 0;
    ctx->ring_tail = 0;
    ctx->rx_state.protocol = PROTO_NONE;
    ctx->rx_state.length = 0;
    ctx->rx_state.expected_length = 0;
    AsciiParser_Reset(&ctx->rx_state.parser.ascii);
    ModbusRtuParser_Reset(&ctx->rx_state.parser.modbus);
    Comm_PortDisableTxInterrupts(ctx);
    ctx->ops->exit_critical();
}

static void Comm_PortInit(CommPortContext *ctx, const CommPortConfig *config, const CommPlatformOps *ops)
{
    GPIO_InitTypeDef gpio_init;
    USART_InitTypeDef usart_init;
    NVIC_InitTypeDef nvic_init;

    memset(ctx, 0, sizeof(*ctx));
    ctx->config = config;
    ctx->ops = ops;
    ctx->instance = config->instance;
    ctx->port_id = config->port_id;
    Comm_PortResetRx(ctx);

    if (config->instance == USART1)
    {
        RCC_APB2PeriphClockCmd(config->peripheral_clock, config->clock_cmd);
    }
    else
    {
        RCC_APB1PeriphClockCmd(config->peripheral_clock, config->clock_cmd);
    }

    nvic_init.NVIC_IRQChannel = config->irq_channel;
    GPIO_PinAFConfig(config->gpio_port, config->tx_pin_source, config->gpio_af);
    GPIO_PinAFConfig(config->gpio_port, config->rx_pin_source, config->gpio_af);
    gpio_init.GPIO_Pin = config->gpio_pins;

    nvic_init.NVIC_IRQChannelPriority = 0;
    nvic_init.NVIC_IRQChannelCmd = ENABLE;
    NVIC_Init(&nvic_init);

    gpio_init.GPIO_Mode = GPIO_Mode_AF;
    gpio_init.GPIO_OType = GPIO_OType_PP;
    gpio_init.GPIO_PuPd = GPIO_PuPd_UP;
    // gpio_init.GPIO_Speed = GPIO_Speed_2MHz;
    gpio_init.GPIO_Speed = GPIO_Speed_50MHz;
    GPIO_Init(config->gpio_port, &gpio_init);

    usart_init.USART_BaudRate = config->baud_rate;
    usart_init.USART_WordLength = USART_WordLength_8b;
    usart_init.USART_StopBits = USART_StopBits_1;
    usart_init.USART_Parity = USART_Parity_No;
    usart_init.USART_HardwareFlowControl = USART_HardwareFlowControl_None;
    usart_init.USART_Mode = USART_Mode_Rx | USART_Mode_Tx;
    USART_Init(config->instance, &usart_init);

    config->instance->CR3 |= (1 << 0);
    config->instance->CR3 &= ~(1 << 11);
    USART_Cmd(config->instance, ENABLE);
    Comm_PortEnableRx(ctx);
}

static const uint8_t *Comm_PortGetFrameBuffer(const CommPortContext *ctx)
{
    if (ctx->active_protocol == PROTO_ASCII)
    {
        return ctx->rx_state.parser.ascii.buffer;
    }

    return ctx->rx_state.parser.modbus.buffer;
}

static void Comm_PortFeedByte(CommPortContext *ctx, uint8_t byte)
{
    ProtocolParseResult result;

    if (ctx->frame_ready_flag != 0)
    {
        return;
    }

    if (ctx->active_protocol == PROTO_NONE)
    {
        if (byte == SOI)
        {
            ctx->active_protocol = PROTO_ASCII;
            ctx->rx_state.protocol = PROTO_ASCII;
            ctx->rx_timeout_ms = COMM_ASCII_RX_TIMEOUT_MS;
            AsciiParser_Reset(&ctx->rx_state.parser.ascii);
        }
        else if ((byte == MODBUS_SLAVE_ADDR) || (byte == MODBUS_BROADCAST_ADDR))
        {
            ctx->active_protocol = PROTO_MODBUS_RTU;
            ctx->rx_state.protocol = PROTO_MODBUS_RTU;
            ctx->rx_timeout_ms = COMM_RTU_RX_TIMEOUT_MS;
            ModbusRtuParser_Reset(&ctx->rx_state.parser.modbus);
        }
        else
        {
            return;
        }
    }

    if (ctx->active_protocol == PROTO_ASCII)
    {
        result = AsciiParser_ConsumeByte(&ctx->rx_state.parser.ascii, byte);
        if (result == PROTO_PARSE_FRAME_READY)
        {
            ctx->rx_len = ctx->rx_state.parser.ascii.length;
            ctx->frame_ready_flag = 1;
        }
        else if (result == PROTO_PARSE_FRAME_INVALID)
        {
            Comm_PortResetRx(ctx);
        }
    }
    else if (ctx->active_protocol == PROTO_MODBUS_RTU)
    {
        result = ModbusRtuParser_ConsumeByte(&ctx->rx_state.parser.modbus, byte);
        if (result == PROTO_PARSE_FRAME_READY)
        {
            ctx->rx_len = ctx->rx_state.parser.modbus.length;
            ctx->frame_ready_flag = 1;
        }
        else if (result == PROTO_PARSE_FRAME_INVALID)
        {
            Comm_PortResetRx(ctx);
        }
    }
}

static void Comm_PortDrainRxRing(CommPortContext *ctx)
{
    uint8_t rx_byte;

    if (ctx->tx_active != 0)
    {
        return;
    }

    while ((ctx->frame_ready_flag == 0) && (Comm_RingPopByte(ctx, &rx_byte) != 0))
    {
        Comm_PortFeedByte(ctx, rx_byte);
    }
}

static void Comm_PortCheckTimeout(CommPortContext *ctx)
{
    int32_t elapsed_ms;

    if (ctx->active_protocol == PROTO_NONE)
    {
        return;
    }

    if (ctx->frame_ready_flag != 0)
    {
        return;
    }

    elapsed_ms = ctx->ops->check_elapsed_ms(ctx->last_rx_tick);
    if ((elapsed_ms >= 0) && ((uint16_t)elapsed_ms >= ctx->rx_timeout_ms))
    {
        Comm_PortResetRx(ctx);
    }
}

static void Comm_PortHandleErrors(CommPortContext *ctx)
{
    uint8_t fault_count = 0;

    if (ctx->instance->ISR & 0x08)
    {
        ctx->instance->ICR |= 1 << 3;
        fault_count++;
    }
    if (ctx->instance->ISR & 0x04)
    {
        ctx->instance->ICR |= 1 << 2;
        fault_count++;
    }
    if (ctx->instance->ISR & 0x02)
    {
        ctx->instance->ICR |= 1 << 1;
        fault_count++;
    }
    if (ctx->instance->ISR & 0x01)
    {
        ctx->instance->ICR |= 1 << 0;
        fault_count++;
    }

    if (fault_count != 0)
    {
        ctx->error_count++;
        Comm_PortResetRx(ctx);
    }
}

static void Comm_PortDispatch(CommPortContext *ctx)
{
    uint16_t tx_len = 0;

    if (ctx->frame_ready_flag == 0)
    {
        return;
    }

    if (ctx->tx_active != 0)
    {
        return;
    }

    if (ctx->active_protocol == PROTO_ASCII)
    {
        tx_len = Ascii_HandleFrame(Comm_PortGetFrameBuffer(ctx), ctx->rx_len, ctx->tx_buf, MAX_FRAME_LEN);
    }
    else if (ctx->active_protocol == PROTO_MODBUS_RTU)
    {
        tx_len = Modbus_ServiceHandleFrame(&g_modbus_service_ctx, Comm_PortGetFrameBuffer(ctx), ctx->rx_len, ctx->tx_buf, MAX_FRAME_LEN);
    }

    ctx->frame_ready_flag = 0;
    ctx->rx_len = 0;
    ctx->active_protocol = PROTO_NONE;
    ctx->rx_state.protocol = PROTO_NONE;
    AsciiParser_Reset(&ctx->rx_state.parser.ascii);
    ModbusRtuParser_Reset(&ctx->rx_state.parser.modbus);

    if (tx_len > 0)
    {
        Comm_PortStartTx(ctx, ctx->tx_buf, tx_len);
    }
}

void Comm_InitAll(void)
{
    NVIC_SetPriority(SysTick_IRQn, 3);
#ifdef _COMMOM_UPPER_SCI1
    Comm_PortInit(&g_comm_port1, &g_comm_port1_config, &g_comm_platform_ops);
#endif
#ifdef _COMMOM_UPPER_SCI2
    Comm_PortInit(&g_comm_port2, &g_comm_port2_config, &g_comm_platform_ops);
#endif
}

void Comm_PollAll(void)
{
#ifdef _COMMOM_UPPER_SCI1
    Comm_PortDrainRxRing(&g_comm_port1);
    Comm_PortCheckTimeout(&g_comm_port1);
    Comm_PortDispatch(&g_comm_port1);
    Comm_PortTxPump(&g_comm_port1);
#endif
#ifdef _COMMOM_UPPER_SCI2
    Comm_PortDrainRxRing(&g_comm_port2);
    Comm_PortCheckTimeout(&g_comm_port2);
    Comm_PortDispatch(&g_comm_port2);
    Comm_PortTxPump(&g_comm_port2);
#endif
}

void Comm_PortIrqHandler(CommPortContext *ctx)
{
    uint8_t rx_byte;

    Comm_PortHandleErrors(ctx);

    if ((ctx->tx_active != 0) && (USART_GetITStatus(ctx->instance, USART_IT_TXE) != RESET))
    {
        if (ctx->tx_pos < ctx->tx_len)
        {
            ctx->instance->TDR = ctx->tx_buf[ctx->tx_pos++];
        }
        else
        {
            USART_ITConfig(ctx->instance, USART_IT_TXE, DISABLE);
            USART_ITConfig(ctx->instance, USART_IT_TC, ENABLE);
        }
    }

    if ((ctx->tx_active != 0) && (USART_GetITStatus(ctx->instance, USART_IT_TC) != RESET))
    {
        USART_ClearFlag(ctx->instance, USART_FLAG_TC);
        Comm_PortFinishTx(ctx);
        return;
    }

    if (USART_GetITStatus(ctx->instance, USART_IT_RXNE) == RESET)
    {
        return;
    }

    rx_byte = (uint8_t)ctx->instance->RDR;

    ctx->ops->notify_rx_activity(ctx->port_id);
    ctx->last_rx_tick = ctx->ops->get_runtime_ms();

    if (ctx->tx_active != 0)
    {
        return;
    }

    if (Comm_RingPushByte(ctx, rx_byte) == 0)
    {
        ctx->error_count++;
        Comm_PortResetRx(ctx);
        ctx->ring_tail = ctx->ring_head;
    }
}

void Comm_PortStartTx(CommPortContext *ctx, const uint8_t *data, uint16_t len)
{
    if ((len == 0) || (len > MAX_FRAME_LEN))
    {
        return;
    }

    memcpy(ctx->tx_buf, data, len);
    ctx->tx_len = len;
    ctx->tx_pos = 0;
    Comm_PortResetRx(ctx);
    Comm_PortDisableRx(ctx);
    Comm_PortDisableTxInterrupts(ctx);
    USART_ClearFlag(ctx->instance, USART_FLAG_TC);
    ctx->tx_active = 1;
    ctx->ops->set_rs485_tx_mode(ctx->port_id);
    ctx->ops->delay_us(COMM_RS485_TURNAROUND_US);
    USART_ITConfig(ctx->instance, USART_IT_TXE, ENABLE);
}

void Comm_PortTxPump(CommPortContext *ctx)
{
    (void)ctx;
}

static void Comm_DefaultEnterCritical(void)
{
    DISABLE_INT();
}

static void Comm_DefaultExitCritical(void)
{
    ENABLE_INT();
}

static int32_t Comm_DefaultGetRuntimeMs(void)
{
    return bsp_GetRunTime();
}

static int32_t Comm_DefaultCheckElapsedMs(int32_t last_tick)
{
    return bsp_CheckRunTime(last_tick);
}

static void Comm_DefaultSetRs485TxMode(uint8_t port_id)
{
    (void)port_id;
    TRANS_EN_485();
}

static void Comm_DefaultSetRs485RxMode(uint8_t port_id)
{
    (void)port_id;
    RECV_EN_485();
}

static void Comm_DefaultDelayUs(uint32_t us)
{
    __delay_us(us);
}

static void Comm_DefaultNotifyRxActivity(uint8_t port_id)
{
    RTC_ExtComCnt++;
    if (port_id == 1U)
    {
        RTC_ExtComCnt1++;
    }
}

static void Comm_DefaultNotifyTxComplete(uint8_t port_id)
{
    (void)port_id;
    if (u8FlashUpdateE2PROM != 0)
    {
        u8FlashUpdateE2PROM = 0;
        u8FlashUpdateFlag = 1;
    }
}


继续，在旧板子上不能修改iap的板子上解决之前提到的问题

测试备份域可靠性

去掉当前的均衡逻辑，使用afe内部均衡功能，

之前你修改CommomSH367309_16series_030C8T6_C.uvprojx 0xd400后，我怎么没触发之前的异常了

增加上位机部分，整体架构重构，功能不动，iap、app、上位机

单独开一个分支，优化app架构，可以全面重构，保证功能、通信地址、接口不变即可，减小复杂度，保障稳定性、简洁性

只能使用jlink吗，不能使用stlink？

根据当前项目帮我生成一份模板，全部使用utf-8编码，打通各平台编译、仿真，后续我要使用这个新的模板来开发
现在这种开发方式日志还是使用串口吗？是否有更好的方式

对比梳理目前项目和keil开发优缺点

app_runtime_monitor模块是用来干什么的

全面分析梳理目前所做的工作和各种工具链，目前工作链是否主流、无bug，你给的各种python文件，是主流方案吗，保护板bms对安全性要求高，我很担心，需要你全面梳理分析

一个稳定出货版本，一个长期重构、新研发待确认版本、一个用于bug及时更改合并到稳定出货版本，但同时一个代码可能对应很多不同客户，会有一些区别，我的需求该如何管理


codex auto review

commit，只想合并到另一个分支的某个commit，而不是最新的commit


//todo 卡死了？？？，直接调通？功能是怎样的
task build-monitor
task flash-stlink-monitor
task watch-board-stlink

目前工具链可以实现，telink 8251整套工具的替换吗，是否需要我提供给你telink的项目，之前使用本机的telink ide来开发，现在能否都使用当前统一工具链。