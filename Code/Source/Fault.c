#include "main.h"

struct PRT_E2ROM_PARAS PRT_E2ROMParas;

union FAULT_FLAG_FIRST Fault_Flag_Fisrt;
union FAULT_FLAG_SECOND Fault_Flag_Second;
union FAULT_FLAG_THIRD Fault_Flag_Third;

UINT16 Fault_record_First[Record_len];
UINT16 Fault_record_Second[Record_len];
UINT16 Fault_record_Third[Record_len];
UINT16 RTC_Fault_record_Third[Record_len][6];

UINT16 Fault_record_First2[Record_len];
UINT16 Fault_record_Second2[Record_len];
UINT16 Fault_record_Third2[Record_len];

UINT8 FaultPoint_First;
UINT8 FaultPoint_Second;
UINT8 FaultPoint_Third;

UINT8 FaultPoint_First2;
UINT8 FaultPoint_Second2;
UINT8 FaultPoint_Third2;

// 原来是和休眠虚电路挂钩的，现在分开，OtherElement.u16Sleep_VirCur_Chg
// 如果原来设置3A的话，就会出现，低温小电流，问题很大。
// 现在默认虚电流大于0.1A，也即0.2A才生效
#define OTP_UTP_VirCur_Chg 1
#define OTP_UTP_VirCur_Dsg 1

void FaultWarnRecord(enum FaultFlag num);
void FaultWarnRecord2(enum FaultFlag num);
void PwrMag_Protect_Record(enum FaultFlag num);
void PwrMag_Protect_Record_StartUp(void);
typedef void (*ProtectCheckFunc)(void);

#define FAULT_CHECK_BODY(MDL_FAULT, WARN_FAULT, RECORD_ID, CHK_VAL, OP_VAL_B, OP_VAL_S, CHK_CNT_PTR, TIME_CNT_B, TIME_CNT_S, FLAG_LOGIC, ON_SET, ON_CLEAR) \
	do                                                                                                                                                    \
	{                                                                                                                                                     \
		SPUBOPUPCHK t_sPubOPUPChk;                                                                                                                        \
		t_sPubOPUPChk.u16ChkVal = (CHK_VAL);                                                                                                              \
		t_sPubOPUPChk.u16OPValB = (OP_VAL_B);                                                                                                             \
		t_sPubOPUPChk.u16OPValS = (OP_VAL_S);                                                                                                             \
		t_sPubOPUPChk.i16ChkCnt = (CHK_CNT_PTR);                                                                                                          \
		t_sPubOPUPChk.u16TimeCntB = (TIME_CNT_B);                                                                                                         \
		t_sPubOPUPChk.u16TimeCntS = (TIME_CNT_S);                                                                                                         \
		t_sPubOPUPChk.u8FlagLogic = (FLAG_LOGIC);                                                                                                         \
		t_sPubOPUPChk.u8FlagBit = (MDL_FAULT);                                                                                                            \
		if (App_PubOPUPChk(&t_sPubOPUPChk))                                                                                                               \
		{                                                                                                                                                 \
			(MDL_FAULT) = t_sPubOPUPChk.u8FlagBit;                                                                                                        \
			if (t_sPubOPUPChk.u8FlagBit == 1)                                                                                                             \
			{                                                                                                                                             \
				if (0 == (WARN_FAULT))                                                                                                                    \
				{                                                                                                                                         \
					FaultWarnRecord(RECORD_ID);                                                                                                           \
					FaultWarnRecord2(RECORD_ID);                                                                                                          \
					ON_SET;                                                                                                                               \
					(WARN_FAULT) = 1;                                                                                                                     \
				}                                                                                                                                         \
			}                                                                                                                                             \
			if ((t_sPubOPUPChk.u8FlagBit == 0) && ((WARN_FAULT) == 1))                                                                                   \
			{                                                                                                                                             \
				(WARN_FAULT) = 0;                                                                                                                         \
				ON_CLEAR;                                                                                                                                 \
			}                                                                                                                                             \
		}                                                                                                                                                 \
	} while (0)

#define DEFINE_FAULT_CHECK(FUNC_NAME, MDL_FAULT, WARN_FAULT, RECORD_ID, CHK_VAL, OP_VAL_B, OP_VAL_S, TIME_CNT_B, TIME_CNT_S, FLAG_LOGIC) \
	void FUNC_NAME(void)                                                                                                                                \
	{                                                                                                                                                   \
		static UINT16 s_i16TimeCnt = 0;                                                                                                                 \
		FAULT_CHECK_BODY(MDL_FAULT, WARN_FAULT, RECORD_ID, CHK_VAL, OP_VAL_B, OP_VAL_S, &s_i16TimeCnt, TIME_CNT_B, TIME_CNT_S, FLAG_LOGIC, ((void)0), ((void)0)); \
	}

#define DEFINE_FAULT_CHECK_EX(FUNC_NAME, MDL_FAULT, WARN_FAULT, RECORD_ID, CHK_VAL, OP_VAL_B, OP_VAL_S, TIME_CNT_B, TIME_CNT_S, FLAG_LOGIC, ON_SET, ON_CLEAR) \
	void FUNC_NAME(void)                                                                                                                                     \
	{                                                                                                                                                        \
		static UINT16 s_i16TimeCnt = 0;                                                                                                                      \
		FAULT_CHECK_BODY(MDL_FAULT, WARN_FAULT, RECORD_ID, CHK_VAL, OP_VAL_B, OP_VAL_S, &s_i16TimeCnt, TIME_CNT_B, TIME_CNT_S, FLAG_LOGIC, ON_SET, ON_CLEAR); \
	}

#define DEFINE_FAULT_CHECK_GATED(FUNC_NAME, MDL_FAULT, WARN_FAULT, RECORD_ID, CHK_VAL, OP_VAL_B, OP_VAL_S, TIME_CNT_B, TIME_CNT_S, FLAG_LOGIC, ENABLE_EXPR) \
	void FUNC_NAME(void)                                                                                                                                    \
	{                                                                                                                                                       \
		static UINT16 s_i16TimeCnt = 0;                                                                                                                     \
		if (((MDL_FAULT) == 0) && !(ENABLE_EXPR))                                                                                                           \
		{                                                                                                                                                   \
			return;                                                                                                                                         \
		}                                                                                                                                                   \
		FAULT_CHECK_BODY(MDL_FAULT, WARN_FAULT, RECORD_ID, CHK_VAL, OP_VAL_B, OP_VAL_S, &s_i16TimeCnt, TIME_CNT_B, TIME_CNT_S, FLAG_LOGIC, ((void)0), ((void)0)); \
	}

#define DEFINE_OVERCURRENT_CHECK(FUNC_NAME, MDL_FAULT, WARN_FAULT, RECORD_ID, CHK_VAL, OP_VAL_B, OP_VAL_S, CHK_CNT_PTR, TIME_CNT_B, TIME_CNT_S) \
	void FUNC_NAME(void)                                                                                                                                \
	{                                                                                                                                                   \
		FAULT_CHECK_BODY(MDL_FAULT, WARN_FAULT, RECORD_ID, CHK_VAL, OP_VAL_B, OP_VAL_S, CHK_CNT_PTR, TIME_CNT_B, TIME_CNT_S, 1, ((void)0), ((void)0)); \
	}

DEFINE_FAULT_CHECK(App_CellOvp_SecondCheck,
				   g_stCellInfoReport.unMdlFault_Second.bits.b1CellOvp,
				   Fault_Flag_Second.bits.CellOvp_Second,
				   CellOvp_Second,
				   g_stCellInfoReport.u16VCellMax,
				   PRT_E2ROMParas.u16VcellOvp_Second,
				   PRT_E2ROMParas.u16VcellOvp_First,
				   PRT_E2ROMParas.u16VcellOvp_Filter,
				   PRT_E2ROMParas.u16VcellOvp_Filter,
				   1);

DEFINE_FAULT_CHECK(App_CellOvp_ThirdCheck,
				   g_stCellInfoReport.unMdlFault_Third.bits.b1CellOvp,
				   Fault_Flag_Third.bits.CellOvp_Third,
				   CellOvp_Third,
				   g_stCellInfoReport.u16VCellMax,
				   PRT_E2ROMParas.u16VcellOvp_Third,
				   PRT_E2ROMParas.u16VcellOvp_Rcv,
				   PRT_E2ROMParas.u16VcellOvp_Filter,
				   PRT_E2ROMParas.u16VcellOvp_Filter,
				   1);

DEFINE_FAULT_CHECK(App_CellUvp_SecondCheck,
				   g_stCellInfoReport.unMdlFault_Second.bits.b1CellUvp,
				   Fault_Flag_Second.bits.CellUvp_Second,
				   CellUvp_Second,
				   g_stCellInfoReport.u16VCellMin,
				   PRT_E2ROMParas.u16VcellUvp_First,
				   PRT_E2ROMParas.u16VcellUvp_Second,
				   PRT_E2ROMParas.u16VcellUvp_Filter,
				   PRT_E2ROMParas.u16VcellUvp_Filter,
				   0);

DEFINE_FAULT_CHECK(App_CellUvp_ThirdCheck,
				   g_stCellInfoReport.unMdlFault_Third.bits.b1CellUvp,
				   Fault_Flag_Third.bits.CellUvp_Third,
				   CellUvp_Third,
				   g_stCellInfoReport.u16VCellMin,
				   PRT_E2ROMParas.u16VcellUvp_Rcv,
				   PRT_E2ROMParas.u16VcellUvp_Third,
				   PRT_E2ROMParas.u16VcellUvp_Filter,
				   PRT_E2ROMParas.u16VcellUvp_Filter,
				   0);

DEFINE_FAULT_CHECK(App_BatOvp_SecondCheck,
				   g_stCellInfoReport.unMdlFault_Second.bits.b1BatOvp,
				   Fault_Flag_Second.bits.BatOvp_Second,
				   BatOvp_Second,
				   g_stCellInfoReport.u16VCellTotle,
				   PRT_E2ROMParas.u16VbusOvp_Second,
				   PRT_E2ROMParas.u16VbusOvp_First,
				   PRT_E2ROMParas.u16VbusOvp_Filter,
				   PRT_E2ROMParas.u16VbusOvp_Filter,
				   1);

DEFINE_FAULT_CHECK(App_BatOvp_ThirdCheck,
				   g_stCellInfoReport.unMdlFault_Third.bits.b1BatOvp,
				   Fault_Flag_Third.bits.BatOvp_Third,
				   BatOvp_Third,
				   g_stCellInfoReport.u16VCellTotle,
				   PRT_E2ROMParas.u16VbusOvp_Third,
				   PRT_E2ROMParas.u16VbusOvp_Rcv,
				   PRT_E2ROMParas.u16VbusOvp_Filter,
				   PRT_E2ROMParas.u16VbusOvp_Filter,
				   1);

DEFINE_FAULT_CHECK(App_BatUvp_SecondCheck,
				   g_stCellInfoReport.unMdlFault_Second.bits.b1BatUvp,
				   Fault_Flag_Second.bits.BatUvp_Second,
				   BatUvp_Second,
				   g_stCellInfoReport.u16VCellTotle,
				   PRT_E2ROMParas.u16VbusUvp_First,
				   PRT_E2ROMParas.u16VbusUvp_Second,
				   PRT_E2ROMParas.u16VbusUvp_Filter,
				   PRT_E2ROMParas.u16VbusUvp_Filter,
				   0);

DEFINE_FAULT_CHECK(App_BatUvp_ThirdCheck,
				   g_stCellInfoReport.unMdlFault_Third.bits.b1BatUvp,
				   Fault_Flag_Third.bits.BatUvp_Third,
				   BatUvp_Third,
				   g_stCellInfoReport.u16VCellTotle,
				   PRT_E2ROMParas.u16VbusUvp_Rcv,
				   PRT_E2ROMParas.u16VbusUvp_Third,
				   PRT_E2ROMParas.u16VbusUvp_Filter,
				   PRT_E2ROMParas.u16VbusUvp_Filter,
				   0);

DEFINE_OVERCURRENT_CHECK(App_IchgOcp_SecondCheck,
						 g_stCellInfoReport.unMdlFault_Second.bits.b1IchgOcp,
						 Fault_Flag_Second.bits.IchgOcp_Second,
						 IchgOcp_Second,
						 g_stCellInfoReport.u16Ichg,
						 PRT_E2ROMParas.u16IchgOcp_Second,
						 PRT_E2ROMParas.u16IchgOcp_First,
						 &sys_time.occ2_cnt,
						 (100 * 5),
						 (PRT_E2ROMParas.u16IchgOcp_Filter + CurOverFaultDelay));

DEFINE_FAULT_CHECK(App_IchgOcp_ThirdCheck,
				   g_stCellInfoReport.unMdlFault_Third.bits.b1IchgOcp,
				   Fault_Flag_Third.bits.IchgOcp_Third,
				   IchgOcp_Third,
				   g_stCellInfoReport.u16Ichg,
				   PRT_E2ROMParas.u16IchgOcp_Third,
				   PRT_E2ROMParas.u16IchgOcp_Rcv,
				   PRT_E2ROMParas.u16IchgOcp_Filter,
				   (PRT_E2ROMParas.u16IchgOcp_Filter + CurOverFaultDelay),
				   1);

DEFINE_OVERCURRENT_CHECK(App_IdischgOcp_SecondCheck,
						 g_stCellInfoReport.unMdlFault_Second.bits.b1IdischgOcp,
						 Fault_Flag_Second.bits.IdischgOcp_Second,
						 IdischgOcp_Second,
						 g_stCellInfoReport.u16IDischg,
						 PRT_E2ROMParas.u16IdsgOcp_Second,
						 PRT_E2ROMParas.u16IdsgOcp_First,
						 &sys_time.odc2_cnt,
						 (100 * 5),
						 (PRT_E2ROMParas.u16IdsgOcp_Filter + CurOverFaultDelay));

DEFINE_FAULT_CHECK(App_IdischgOcp_ThirdCheck,
				   g_stCellInfoReport.unMdlFault_Third.bits.b1IdischgOcp,
				   Fault_Flag_Third.bits.IdischgOcp_Third,
				   IdischgOcp_Third,
				   g_stCellInfoReport.u16IDischg,
				   PRT_E2ROMParas.u16IdsgOcp_Third,
				   PRT_E2ROMParas.u16IdsgOcp_Rcv,
				   PRT_E2ROMParas.u16IdsgOcp_Filter,
				   (PRT_E2ROMParas.u16IdsgOcp_Filter + CurOverFaultDelay),
				   1);

DEFINE_FAULT_CHECK_GATED(App_CellChgOtp_SecondCheck,
						 g_stCellInfoReport.unMdlFault_Second.bits.b1CellChgOtp,
						 Fault_Flag_Second.bits.CellChgOTp_Second,
						 CellChgOTp_Second,
						 g_stCellInfoReport.u16TempMax,
						 PRT_E2ROMParas.u16TChgOTp_Second,
						 PRT_E2ROMParas.u16TChgOTp_First,
						 PRT_E2ROMParas.u16TChgOTp_Filter,
						 PRT_E2ROMParas.u16TChgOTp_Filter,
						 1,
						 (g_stCellInfoReport.u16Ichg > OTP_UTP_VirCur_Chg));

DEFINE_FAULT_CHECK_GATED(App_CellChgOtp_ThirdCheck,
						 g_stCellInfoReport.unMdlFault_Third.bits.b1CellChgOtp,
						 Fault_Flag_Third.bits.CellChgOTp_Third,
						 CellChgOTp_Third,
						 g_stCellInfoReport.u16TempMax,
						 PRT_E2ROMParas.u16TChgOTp_Third,
						 PRT_E2ROMParas.u16TChgOTp_Rcv,
						 PRT_E2ROMParas.u16TChgOTp_Filter,
						 PRT_E2ROMParas.u16TChgOTp_Filter,
						 1,
						 (g_stCellInfoReport.u16Ichg > OTP_UTP_VirCur_Chg));

DEFINE_FAULT_CHECK_GATED(App_CellDisChgOtp_SecondCheck,
						 g_stCellInfoReport.unMdlFault_Second.bits.b1CellDischgOtp,
						 Fault_Flag_Second.bits.CellDsgOTp_Second,
						 CellDsgOTp_Second,
						 g_stCellInfoReport.u16TempMax,
						 PRT_E2ROMParas.u16TdischgOTp_Second,
						 PRT_E2ROMParas.u16TdischgOTp_First,
						 PRT_E2ROMParas.u16TdischgOTp_Filter,
						 PRT_E2ROMParas.u16TdischgOTp_Filter,
						 1,
						 (g_stCellInfoReport.u16IDischg > OTP_UTP_VirCur_Dsg));

DEFINE_FAULT_CHECK_GATED(App_CellDisChgOtp_ThirdCheck,
						 g_stCellInfoReport.unMdlFault_Third.bits.b1CellDischgOtp,
						 Fault_Flag_Third.bits.CellDsgOTp_Third,
						 CellDsgOTp_Third,
						 g_stCellInfoReport.u16TempMax,
						 PRT_E2ROMParas.u16TdischgOTp_Third,
						 PRT_E2ROMParas.u16TdischgOTp_Rcv,
						 PRT_E2ROMParas.u16TdischgOTp_Filter,
						 PRT_E2ROMParas.u16TdischgOTp_Filter,
						 1,
						 (g_stCellInfoReport.u16IDischg > OTP_UTP_VirCur_Dsg));

DEFINE_FAULT_CHECK(App_MosOtp_SecondCheck,
				   g_stCellInfoReport.unMdlFault_Second.bits.b1TmosOtp,
				   Fault_Flag_Second.bits.MosOTp_Second,
				   MosOTp_Second,
				   g_stCellInfoReport.u16Temperature[MOS_TEMP1],
				   PRT_E2ROMParas.u16TmosOTp_Second,
				   PRT_E2ROMParas.u16TmosOTp_First,
				   PRT_E2ROMParas.u16TmosOTp_Filter,
				   PRT_E2ROMParas.u16TmosOTp_Filter,
				   1);

DEFINE_FAULT_CHECK(App_MosOtp_ThirdCheck,
				   g_stCellInfoReport.unMdlFault_Third.bits.b1TmosOtp,
				   Fault_Flag_Third.bits.MosOTp_Third,
				   MosOTp_Third,
				   g_stCellInfoReport.u16Temperature[MOS_TEMP1],
				   PRT_E2ROMParas.u16TmosOTp_Third,
				   PRT_E2ROMParas.u16TmosOTp_Rcv,
				   PRT_E2ROMParas.u16TmosOTp_Filter,
				   PRT_E2ROMParas.u16TmosOTp_Filter,
				   1);

DEFINE_FAULT_CHECK_GATED(App_CellChgUtp_SecondCheck,
						 g_stCellInfoReport.unMdlFault_Second.bits.b1CellChgUtp,
						 Fault_Flag_Second.bits.CellChgUTp_Second,
						 CellChgUTp_Second,
						 g_stCellInfoReport.u16TempMin,
						 PRT_E2ROMParas.u16TchgUTp_First,
						 PRT_E2ROMParas.u16TchgUTp_Second,
						 PRT_E2ROMParas.u16TchgUTp_Filter,
						 PRT_E2ROMParas.u16TchgUTp_Filter,
						 0,
						 (g_stCellInfoReport.u16Ichg > OTP_UTP_VirCur_Chg));

DEFINE_FAULT_CHECK_GATED(App_CellChgUtp_ThirdCheck,
						 g_stCellInfoReport.unMdlFault_Third.bits.b1CellChgUtp,
						 Fault_Flag_Third.bits.CellChgUTp_Third,
						 CellChgUTp_Third,
						 g_stCellInfoReport.u16TempMin,
						 PRT_E2ROMParas.u16TchgUTp_Rcv,
						 PRT_E2ROMParas.u16TchgUTp_Third,
						 PRT_E2ROMParas.u16TchgUTp_Filter,
						 PRT_E2ROMParas.u16TchgUTp_Filter,
						 0,
						 (g_stCellInfoReport.u16Ichg > OTP_UTP_VirCur_Chg));

DEFINE_FAULT_CHECK_GATED(App_CellDischgUtp_SecondCheck,
						 g_stCellInfoReport.unMdlFault_Second.bits.b1CellDischgUtp,
						 Fault_Flag_Second.bits.CellDsgUTp_Second,
						 CellDsgUTp_Second,
						 g_stCellInfoReport.u16TempMin,
						 PRT_E2ROMParas.u16TdischgUTp_First,
						 PRT_E2ROMParas.u16TdischgUTp_Second,
						 PRT_E2ROMParas.u16TdischgUTp_Filter,
						 PRT_E2ROMParas.u16TdischgUTp_Filter,
						 0,
						 (g_stCellInfoReport.u16IDischg > OTP_UTP_VirCur_Dsg));

DEFINE_FAULT_CHECK_GATED(App_CellDischgUtp_ThirdCheck,
						 g_stCellInfoReport.unMdlFault_Third.bits.b1CellDischgUtp,
						 Fault_Flag_Third.bits.CellDsgUTp_Third,
						 CellDsgUTp_Third,
						 g_stCellInfoReport.u16TempMin,
						 PRT_E2ROMParas.u16TdischgUTp_Rcv,
						 PRT_E2ROMParas.u16TdischgUTp_Third,
						 PRT_E2ROMParas.u16TdischgUTp_Filter,
						 PRT_E2ROMParas.u16TdischgUTp_Filter,
						 0,
						 (g_stCellInfoReport.u16IDischg > OTP_UTP_VirCur_Dsg));

DEFINE_FAULT_CHECK(App_CellSocUp_SecondCheck,
				   g_stCellInfoReport.unMdlFault_Second.bits.b1SocLow,
				   Fault_Flag_Second.bits.CellSocUp_Second,
				   CellSocUp_Second,
				   g_stCellInfoReport.SocElement.u16Soc,
				   PRT_E2ROMParas.u16SocUp_First,
				   PRT_E2ROMParas.u16SocUp_Second,
				   PRT_E2ROMParas.u16SocUp_Filter,
				   PRT_E2ROMParas.u16SocUp_Filter,
				   0);

DEFINE_FAULT_CHECK(App_CellSocUp_ThirdCheck,
				   g_stCellInfoReport.unMdlFault_Third.bits.b1SocLow,
				   Fault_Flag_Third.bits.CellSocUp_Third,
				   CellSocUp_Third,
				   g_stCellInfoReport.SocElement.u16Soc,
				   PRT_E2ROMParas.u16SocUp_Rcv,
				   PRT_E2ROMParas.u16SocUp_Third,
				   PRT_E2ROMParas.u16SocUp_Filter,
				   PRT_E2ROMParas.u16SocUp_Filter,
				   0);

DEFINE_FAULT_CHECK(App_VdeltaOp_SecondCheck,
				   g_stCellInfoReport.unMdlFault_Second.bits.b1VcellDeltaBig,
				   Fault_Flag_Second.bits.VdeltaOvp_Second,
				   VdeltaOvp_Second,
				   g_stCellInfoReport.u16VCellDelta,
				   PRT_E2ROMParas.u16VdeltaOvp_Second,
				   PRT_E2ROMParas.u16VdeltaOvp_First,
				   PRT_E2ROMParas.u16VdeltaOvp_Filter,
				   PRT_E2ROMParas.u16VdeltaOvp_Filter,
				   1);

DEFINE_FAULT_CHECK_EX(App_VdeltaOp_ThirdCheck,
					  g_stCellInfoReport.unMdlFault_Third.bits.b1VcellDeltaBig,
					  Fault_Flag_Third.bits.VdeltaOvp_Third,
					  VdeltaOvp_Third,
					  g_stCellInfoReport.u16VCellDelta,
					  PRT_E2ROMParas.u16VdeltaOvp_Third,
					  PRT_E2ROMParas.u16VdeltaOvp_Rcv,
					  PRT_E2ROMParas.u16VdeltaOvp_Filter,
					  (PRT_E2ROMParas.u16VdeltaOvp_Filter + 200),
					  1,
					  System_ERROR_UserCallback(ERROR_VDEATLE_OVER),
					  System_ERROR_UserCallback(ERROR_REMOVE_VDEATLE_OVER));

#undef DEFINE_OVERCURRENT_CHECK
#undef DEFINE_FAULT_CHECK_GATED
#undef DEFINE_FAULT_CHECK_EX
#undef DEFINE_FAULT_CHECK
#undef FAULT_CHECK_BODY

static const ProtectCheckFunc g_protect_check_sequence[] = {
	App_CellOvp_SecondCheck,
	App_CellOvp_ThirdCheck,
	App_CellUvp_SecondCheck,
	App_CellUvp_ThirdCheck,
	App_BatOvp_SecondCheck,
	App_BatOvp_ThirdCheck,
	App_BatUvp_SecondCheck,
	App_BatUvp_ThirdCheck,
	App_MosOtp_SecondCheck,
	App_MosOtp_ThirdCheck,
	App_VdeltaOp_SecondCheck,
	App_VdeltaOp_ThirdCheck,
	App_IdischgOcp_SecondCheck,
	App_IdischgOcp_ThirdCheck,
	App_IchgOcp_SecondCheck,
	App_IchgOcp_ThirdCheck,
	App_CellSocUp_SecondCheck,
	App_CellSocUp_ThirdCheck,
	App_CellDisChgOtp_SecondCheck,
	App_CellDisChgOtp_ThirdCheck,
	App_CellDischgUtp_SecondCheck,
	App_CellDischgUtp_ThirdCheck,
	App_CellChgOtp_SecondCheck,
	App_CellChgOtp_ThirdCheck,
	App_CellChgUtp_SecondCheck,
	App_CellChgUtp_ThirdCheck};

/*******************************************************************************
 *Function name: App_WarnCtrl()
 *Description :  IO port state sample, filter, warning judge and treatment
 *input:         void
 *global vars:   g_u16RunFlag.bit.WARN: run flag, 10ms once
 *output:        void
 *CALLED BY:     main()
 ******************************************************************************/
void App_WarnCtrl(void)
{
	UINT16 i = 0;

	for (i = 0; i < (sizeof(g_protect_check_sequence) / sizeof(g_protect_check_sequence[0])); ++i)
	{
		g_protect_check_sequence[i]();
	}
}

void FaultWarnRecord(enum FaultFlag num)
{

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
