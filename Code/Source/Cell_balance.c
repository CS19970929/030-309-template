#include "main.h"

UINT16 g_u16CBnFLAG_ToUpper = 0;
UINT8 g_u8CBn_StatusFlag = 0;
UINT8 g_u8CBn_AFECloseFlag = 0;

void CellBalance_DataInit(void)
{
	g_u16CBnFLAG_ToUpper = 0;
	g_u8CBn_StatusFlag = 0;
	g_u8CBn_AFECloseFlag = 0;
	g_stCellInfoReport.u16BalanceFlag1 = 0;
	g_stCellInfoReport.u16BalanceFlag2 = 0;
}

void CellBalanceTest(void)
{
}

void App_CellBalance(void)
{
	if (!System_OnOFF_Func.bits.b1OnOFF_Balance)
	{
		CellBalance_DataInit();
	}
}