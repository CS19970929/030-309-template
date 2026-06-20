#include "main.h"

struct PRT_E2ROM_PARAS PRT_E2ROMParas;

union FAULT_FLAG_SECOND Fault_Flag_Second;
union FAULT_FLAG_THIRD Fault_Flag_Third;

UINT16 Fault_record_First2[Record_len];
UINT16 Fault_record_Second2[Record_len];
UINT16 Fault_record_Third2[Record_len];

UINT8 FaultPoint_First2;
UINT8 FaultPoint_Second2;
UINT8 FaultPoint_Third2;

#define OTP_UTP_VirCur_Chg 1
#define OTP_UTP_VirCur_Dsg 1

void FaultWarnRecord2(enum FaultFlag num);
void PwrMag_Protect_Record(enum FaultFlag num);
void PwrMag_Protect_Record_StartUp(void);

/* ---- Data-driven fault check engine ---- */

/* Static counters: 24 for internal use; indices 12,14 (OCP Second) use extern sys_time counters */
static UINT16 s_counters[24];

/* Hardcoded OCP second-level time constant (100ms * 5 = 500ms) */
static const UINT16 s_u16OcpSecondTimeB = (100 * 5);

static const FaultCheckDesc s_faultDesc[26] = {
	/*  0 - App_CellOvp_SecondCheck */
	{ &g_stCellInfoReport.u16VCellMax,
	  &PRT_E2ROMParas.u16VcellOvp_Second, &PRT_E2ROMParas.u16VcellOvp_First,
	  &s_counters[0],
	  &PRT_E2ROMParas.u16VcellOvp_Filter, &PRT_E2ROMParas.u16VcellOvp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(0) | FAULT_CTRL_FLAGREG_BIT_POS(0) | FAULT_CTRL_FLAG_LOGIC(1),
	  CellOvp_Second },
	/*  1 - App_CellOvp_ThirdCheck */
	{ &g_stCellInfoReport.u16VCellMax,
	  &PRT_E2ROMParas.u16VcellOvp_Third, &PRT_E2ROMParas.u16VcellOvp_Rcv,
	  &s_counters[1],
	  &PRT_E2ROMParas.u16VcellOvp_Filter, &PRT_E2ROMParas.u16VcellOvp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(0) | FAULT_CTRL_FLAGREG_BIT_POS(0) | FAULT_CTRL_FLAG_LOGIC(1),
	  CellOvp_Third },
	/*  2 - App_CellUvp_SecondCheck */
	{ &g_stCellInfoReport.u16VCellMin,
	  &PRT_E2ROMParas.u16VcellUvp_First, &PRT_E2ROMParas.u16VcellUvp_Second,
	  &s_counters[2],
	  &PRT_E2ROMParas.u16VcellUvp_Filter, &PRT_E2ROMParas.u16VcellUvp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(1) | FAULT_CTRL_FLAGREG_BIT_POS(1) | FAULT_CTRL_FLAG_LOGIC(0),
	  CellUvp_Second },
	/*  3 - App_CellUvp_ThirdCheck */
	{ &g_stCellInfoReport.u16VCellMin,
	  &PRT_E2ROMParas.u16VcellUvp_Rcv, &PRT_E2ROMParas.u16VcellUvp_Third,
	  &s_counters[3],
	  &PRT_E2ROMParas.u16VcellUvp_Filter, &PRT_E2ROMParas.u16VcellUvp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(1) | FAULT_CTRL_FLAGREG_BIT_POS(1) | FAULT_CTRL_FLAG_LOGIC(0),
	  CellUvp_Third },
	/*  4 - App_BatOvp_SecondCheck */
	{ &g_stCellInfoReport.u16VCellTotle,
	  &PRT_E2ROMParas.u16VbusOvp_Second, &PRT_E2ROMParas.u16VbusOvp_First,
	  &s_counters[4],
	  &PRT_E2ROMParas.u16VbusOvp_Filter, &PRT_E2ROMParas.u16VbusOvp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(2) | FAULT_CTRL_FLAGREG_BIT_POS(2) | FAULT_CTRL_FLAG_LOGIC(1),
	  BatOvp_Second },
	/*  5 - App_BatOvp_ThirdCheck */
	{ &g_stCellInfoReport.u16VCellTotle,
	  &PRT_E2ROMParas.u16VbusOvp_Third, &PRT_E2ROMParas.u16VbusOvp_Rcv,
	  &s_counters[5],
	  &PRT_E2ROMParas.u16VbusOvp_Filter, &PRT_E2ROMParas.u16VbusOvp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(2) | FAULT_CTRL_FLAGREG_BIT_POS(2) | FAULT_CTRL_FLAG_LOGIC(1),
	  BatOvp_Third },
	/*  6 - App_BatUvp_SecondCheck */
	{ &g_stCellInfoReport.u16VCellTotle,
	  &PRT_E2ROMParas.u16VbusUvp_First, &PRT_E2ROMParas.u16VbusUvp_Second,
	  &s_counters[6],
	  &PRT_E2ROMParas.u16VbusUvp_Filter, &PRT_E2ROMParas.u16VbusUvp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(3) | FAULT_CTRL_FLAGREG_BIT_POS(3) | FAULT_CTRL_FLAG_LOGIC(0),
	  BatUvp_Second },
	/*  7 - App_BatUvp_ThirdCheck */
	{ &g_stCellInfoReport.u16VCellTotle,
	  &PRT_E2ROMParas.u16VbusUvp_Rcv, &PRT_E2ROMParas.u16VbusUvp_Third,
	  &s_counters[7],
	  &PRT_E2ROMParas.u16VbusUvp_Filter, &PRT_E2ROMParas.u16VbusUvp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(3) | FAULT_CTRL_FLAGREG_BIT_POS(3) | FAULT_CTRL_FLAG_LOGIC(0),
	  BatUvp_Third },
	/*  8 - App_MosOtp_SecondCheck */
	{ &g_stCellInfoReport.u16Temperature[MOS_TEMP1],
	  &PRT_E2ROMParas.u16TmosOTp_Second, &PRT_E2ROMParas.u16TmosOTp_First,
	  &s_counters[8],
	  &PRT_E2ROMParas.u16TmosOTp_Filter, &PRT_E2ROMParas.u16TmosOTp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(13) | FAULT_CTRL_FLAGREG_BIT_POS(10) | FAULT_CTRL_FLAG_LOGIC(1),
	  MosOTp_Second },
	/*  9 - App_MosOtp_ThirdCheck */
	{ &g_stCellInfoReport.u16Temperature[MOS_TEMP1],
	  &PRT_E2ROMParas.u16TmosOTp_Third, &PRT_E2ROMParas.u16TmosOTp_Rcv,
	  &s_counters[9],
	  &PRT_E2ROMParas.u16TmosOTp_Filter, &PRT_E2ROMParas.u16TmosOTp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(13) | FAULT_CTRL_FLAGREG_BIT_POS(10) | FAULT_CTRL_FLAG_LOGIC(1),
	  MosOTp_Third },
	/* 10 - App_VdeltaOp_SecondCheck */
	{ &g_stCellInfoReport.u16VCellDelta,
	  &PRT_E2ROMParas.u16VdeltaOvp_Second, &PRT_E2ROMParas.u16VdeltaOvp_First,
	  &s_counters[10],
	  &PRT_E2ROMParas.u16VdeltaOvp_Filter, &PRT_E2ROMParas.u16VdeltaOvp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(10) | FAULT_CTRL_FLAGREG_BIT_POS(11) | FAULT_CTRL_FLAG_LOGIC(1),
	  VdeltaOvp_Second },
	/* 11 - App_VdeltaOp_ThirdCheck (special: +200 TimeS, System_ERROR_UserCallback) */
	{ &g_stCellInfoReport.u16VCellDelta,
	  &PRT_E2ROMParas.u16VdeltaOvp_Third, &PRT_E2ROMParas.u16VdeltaOvp_Rcv,
	  &s_counters[11],
	  &PRT_E2ROMParas.u16VdeltaOvp_Filter, &PRT_E2ROMParas.u16VdeltaOvp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(10) | FAULT_CTRL_FLAGREG_BIT_POS(11) | FAULT_CTRL_FLAG_LOGIC(1) | FAULT_CTRL_TIMES_OFFSET(1),
	  VdeltaOvp_Third },
	/* 12 - App_IdischgOcp_SecondCheck (external counter, hardcoded timeB, +CurOverFaultDelay) */
	{ &g_stCellInfoReport.u16IDischg,
	  &PRT_E2ROMParas.u16IdsgOcp_Second, &PRT_E2ROMParas.u16IdsgOcp_First,
	  (UINT16 *)&sys_time.odc2_cnt, &s_u16OcpSecondTimeB, &PRT_E2ROMParas.u16IdsgOcp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(5) | FAULT_CTRL_FLAGREG_BIT_POS(5) | FAULT_CTRL_FLAG_LOGIC(1) | FAULT_CTRL_TIMES_OFFSET(2),
	  IdischgOcp_Second },
	/* 13 - App_IdischgOcp_ThirdCheck (+CurOverFaultDelay) */
	{ &g_stCellInfoReport.u16IDischg,
	  &PRT_E2ROMParas.u16IdsgOcp_Third, &PRT_E2ROMParas.u16IdsgOcp_Rcv,
	  &s_counters[12],
	  &PRT_E2ROMParas.u16IdsgOcp_Filter, &PRT_E2ROMParas.u16IdsgOcp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(5) | FAULT_CTRL_FLAGREG_BIT_POS(5) | FAULT_CTRL_FLAG_LOGIC(1) | FAULT_CTRL_TIMES_OFFSET(2),
	  IdischgOcp_Third },
	/* 14 - App_IchgOcp_SecondCheck (external counter, hardcoded timeB, +CurOverFaultDelay) */
	{ &g_stCellInfoReport.u16Ichg,
	  &PRT_E2ROMParas.u16IchgOcp_Second, &PRT_E2ROMParas.u16IchgOcp_First,
	  (UINT16 *)&sys_time.occ2_cnt, &s_u16OcpSecondTimeB, &PRT_E2ROMParas.u16IchgOcp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(4) | FAULT_CTRL_FLAGREG_BIT_POS(4) | FAULT_CTRL_FLAG_LOGIC(1) | FAULT_CTRL_TIMES_OFFSET(2),
	  IchgOcp_Second },
	/* 15 - App_IchgOcp_ThirdCheck (+CurOverFaultDelay) */
	{ &g_stCellInfoReport.u16Ichg,
	  &PRT_E2ROMParas.u16IchgOcp_Third, &PRT_E2ROMParas.u16IchgOcp_Rcv,
	  &s_counters[13],
	  &PRT_E2ROMParas.u16IchgOcp_Filter, &PRT_E2ROMParas.u16IchgOcp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(4) | FAULT_CTRL_FLAGREG_BIT_POS(4) | FAULT_CTRL_FLAG_LOGIC(1) | FAULT_CTRL_TIMES_OFFSET(2),
	  IchgOcp_Third },
	/* 16 - App_CellSocUp_SecondCheck */
	{ &g_stCellInfoReport.SocElement.u16Soc,
	  &PRT_E2ROMParas.u16SocUp_First, &PRT_E2ROMParas.u16SocUp_Second,
	  &s_counters[14],
	  &PRT_E2ROMParas.u16SocUp_Filter, &PRT_E2ROMParas.u16SocUp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(12) | FAULT_CTRL_FLAGREG_BIT_POS(12) | FAULT_CTRL_FLAG_LOGIC(0),
	  CellSocUp_Second },
	/* 17 - App_CellSocUp_ThirdCheck */
	{ &g_stCellInfoReport.SocElement.u16Soc,
	  &PRT_E2ROMParas.u16SocUp_Rcv, &PRT_E2ROMParas.u16SocUp_Third,
	  &s_counters[15],
	  &PRT_E2ROMParas.u16SocUp_Filter, &PRT_E2ROMParas.u16SocUp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(12) | FAULT_CTRL_FLAGREG_BIT_POS(12) | FAULT_CTRL_FLAG_LOGIC(0),
	  CellSocUp_Third },
	/* 18 - App_CellDisChgOtp_SecondCheck (OTP/UTP, virCur=Idischg) */
	{ &g_stCellInfoReport.u16TempMax,
	  &PRT_E2ROMParas.u16TdischgOTp_Second, &PRT_E2ROMParas.u16TdischgOTp_First,
	  &s_counters[16],
	  &PRT_E2ROMParas.u16TdischgOTp_Filter, &PRT_E2ROMParas.u16TdischgOTp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(7) | FAULT_CTRL_FLAGREG_BIT_POS(7) | FAULT_CTRL_FLAG_LOGIC(1) | FAULT_CTRL_VIRCUR_TYPE(2),
	  CellDsgOTp_Second },
	/* 19 - App_CellDisChgOtp_ThirdCheck (OTP/UTP, virCur=Idischg) */
	{ &g_stCellInfoReport.u16TempMax,
	  &PRT_E2ROMParas.u16TdischgOTp_Third, &PRT_E2ROMParas.u16TdischgOTp_Rcv,
	  &s_counters[17],
	  &PRT_E2ROMParas.u16TdischgOTp_Filter, &PRT_E2ROMParas.u16TdischgOTp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(7) | FAULT_CTRL_FLAGREG_BIT_POS(8) | FAULT_CTRL_FLAG_LOGIC(1) | FAULT_CTRL_VIRCUR_TYPE(2),
	  CellDsgOTp_Third },
	/* 20 - App_CellDischgUtp_SecondCheck (OTP/UTP, virCur=Idischg) */
	{ &g_stCellInfoReport.u16TempMin,
	  &PRT_E2ROMParas.u16TdischgUTp_First, &PRT_E2ROMParas.u16TdischgUTp_Second,
	  &s_counters[18],
	  &PRT_E2ROMParas.u16TdischgUTp_Filter, &PRT_E2ROMParas.u16TdischgUTp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(9) | FAULT_CTRL_FLAGREG_BIT_POS(9) | FAULT_CTRL_FLAG_LOGIC(0) | FAULT_CTRL_VIRCUR_TYPE(2),
	  CellDsgUTp_Second },
	/* 21 - App_CellDischgUtp_ThirdCheck (OTP/UTP, virCur=Idischg) */
	{ &g_stCellInfoReport.u16TempMin,
	  &PRT_E2ROMParas.u16TdischgUTp_Rcv, &PRT_E2ROMParas.u16TdischgUTp_Third,
	  &s_counters[19],
	  &PRT_E2ROMParas.u16TdischgUTp_Filter, &PRT_E2ROMParas.u16TdischgUTp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(9) | FAULT_CTRL_FLAGREG_BIT_POS(9) | FAULT_CTRL_FLAG_LOGIC(0) | FAULT_CTRL_VIRCUR_TYPE(2),
	  CellDsgUTp_Third },
	/* 22 - App_CellChgOtp_SecondCheck (OTP/UTP, virCur=Ichg) */
	{ &g_stCellInfoReport.u16TempMax,
	  &PRT_E2ROMParas.u16TChgOTp_Second, &PRT_E2ROMParas.u16TChgOTp_First,
	  &s_counters[20],
	  &PRT_E2ROMParas.u16TChgOTp_Filter, &PRT_E2ROMParas.u16TChgOTp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(6) | FAULT_CTRL_FLAGREG_BIT_POS(6) | FAULT_CTRL_FLAG_LOGIC(1) | FAULT_CTRL_VIRCUR_TYPE(1),
	  CellChgOTp_Second },
	/* 23 - App_CellChgOtp_ThirdCheck (OTP/UTP, virCur=Ichg) */
	{ &g_stCellInfoReport.u16TempMax,
	  &PRT_E2ROMParas.u16TChgOTp_Third, &PRT_E2ROMParas.u16TChgOTp_Rcv,
	  &s_counters[21],
	  &PRT_E2ROMParas.u16TChgOTp_Filter, &PRT_E2ROMParas.u16TChgOTp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(6) | FAULT_CTRL_FLAGREG_BIT_POS(6) | FAULT_CTRL_FLAG_LOGIC(1) | FAULT_CTRL_VIRCUR_TYPE(1),
	  CellChgOTp_Third },
	/* 24 - App_CellChgUtp_SecondCheck (OTP/UTP, virCur=Ichg) */
	{ &g_stCellInfoReport.u16TempMin,
	  &PRT_E2ROMParas.u16TchgUTp_First, &PRT_E2ROMParas.u16TchgUTp_Second,
	  &s_counters[22],
	  &PRT_E2ROMParas.u16TchgUTp_Filter, &PRT_E2ROMParas.u16TchgUTp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(8) | FAULT_CTRL_FLAGREG_BIT_POS(8) | FAULT_CTRL_FLAG_LOGIC(0) | FAULT_CTRL_VIRCUR_TYPE(1),
	  CellChgUTp_Second },
	/* 25 - App_CellChgUtp_ThirdCheck (OTP/UTP, virCur=Ichg) */
	{ &g_stCellInfoReport.u16TempMin,
	  &PRT_E2ROMParas.u16TchgUTp_Rcv, &PRT_E2ROMParas.u16TchgUTp_Third,
	  &s_counters[23],
	  &PRT_E2ROMParas.u16TchgUTp_Filter, &PRT_E2ROMParas.u16TchgUTp_Filter,
	  FAULT_CTRL_FAULTREG_BIT_POS(8) | FAULT_CTRL_FLAGREG_BIT_POS(7) | FAULT_CTRL_FLAG_LOGIC(0) | FAULT_CTRL_VIRCUR_TYPE(1),
	  CellChgUTp_Third }
};

void App_FaultCheck_Run(UINT8 idx)
{
	SPUBOPUPCHK t;
	const FaultCheckDesc *pD = &s_faultDesc[idx];
	UINT8 level = idx & 1;   /* even = Second, odd = Third */
	UINT8 u8FaultRegBit = FAULT_CTRL_GET_FAULTREG_BIT(pD->u16Control);
	UINT8 u8FlagRegBit  = FAULT_CTRL_GET_FLAGREG_BIT(pD->u16Control);
	UINT8 u8FlagLogic   = FAULT_CTRL_GET_LOGIC(pD->u16Control);
	UINT8 u8TimeSOfs    = FAULT_CTRL_GET_TIMESOFS(pD->u16Control);
	UINT8 u8VirCurType  = FAULT_CTRL_GET_VIRCUR(pD->u16Control);

	union MDLCHGFAULT_REG *pFaultReg;
	UINT16 *pFlagAll;
	if (level == 0) {
		pFaultReg = &g_stCellInfoReport.unMdlFault_Second;
		pFlagAll  = &Fault_Flag_Second.all;
	} else {
		pFaultReg = &g_stCellInfoReport.unMdlFault_Third;
		pFlagAll  = &Fault_Flag_Third.all;
	}

	/* ---- OTP/UTP virtual current gating ---- */
	if (u8VirCurType != 0) {
		UINT8 curFlagBit = (pFaultReg->all >> u8FaultRegBit) & 1;
		if (curFlagBit == 0) {
			UINT16 curVal;
			if (u8VirCurType == 1)
				curVal = g_stCellInfoReport.u16Ichg;
			else
				curVal = g_stCellInfoReport.u16IDischg;

			if (curVal <= 1)
				return;
		}
	}

	/* ---- SPUBOPUPCHK setup ---- */
	t.u16ChkVal    = *(pD->pSrcVal);
	t.u16OPValB    = *(pD->pThreshB);
	t.u16OPValS    = *(pD->pThreshS);
	t.i16ChkCnt    = pD->pCounter;
	t.u16TimeCntB  = *(pD->pTimeB);
	t.u16TimeCntS  = *(pD->pTimeS);
	if (u8TimeSOfs == 1)
		t.u16TimeCntS += 200u;
	else if (u8TimeSOfs == 2)
		t.u16TimeCntS += CurOverFaultDelay;

	t.u8FlagLogic  = u8FlagLogic;
	t.u8FlagBit    = (pFaultReg->all >> u8FaultRegBit) & 1;

	/* ---- Core check ---- */
	if (App_PubOPUPChk(&t)) {
		UINT16 faultMask = (UINT16)(1u << u8FaultRegBit);
		UINT16 flagMask = (UINT16)(1u << u8FlagRegBit);

		if (t.u8FlagBit == 1) {
			pFaultReg->all |= faultMask;
		} else {
			pFaultReg->all = (UINT16)(pFaultReg->all & (UINT16)(~faultMask));
		}

		if (t.u8FlagBit == 1) {
			if ((*pFlagAll & flagMask) == 0) {
				FaultWarnRecord2((enum FaultFlag)pD->u16FaultEnum);
				*pFlagAll |= flagMask;
				if (u8TimeSOfs == 1)
					System_ERROR_UserCallback(ERROR_VDEATLE_OVER);
			}
		}

		if (t.u8FlagBit == 0 && (*pFlagAll & flagMask) != 0) {
			*pFlagAll &= ~flagMask;
			if (u8TimeSOfs == 1)
				System_ERROR_UserCallback(ERROR_REMOVE_VDEATLE_OVER);
		}
	}
}

void App_WarnCtrl(void)
{
	UINT8 i;
	for (i = 0; i < 26; i++) {
		App_FaultCheck_Run(i);
	}
}

void FaultWarnRecord2(enum FaultFlag num)
{
	if (num >= 1 && num <= 13)
	{
		if (FaultPoint_First2 >= Record_len)
		{
			FaultPoint_First2 = 0;
		}
		Fault_record_First2[FaultPoint_First2++] = num;
	}
	else if (num >= 14 && num <= 26)
	{
		if (FaultPoint_Second2 >= Record_len)
		{
			FaultPoint_Second2 = 0;
		}
		Fault_record_Second2[FaultPoint_Second2++] = num;
	}
	else
	{
		if (FaultPoint_Third2 >= Record_len)
		{
			FaultPoint_Third2 = 0;
		}
		Fault_record_Third2[FaultPoint_Third2++] = num;
	}
}
