/*
*********************************************************************************************************
*
*	模块名称 : 应用程序参数模块
*	文件名称 : param.c
*	版    本 : V1.0
*	说    明 : 读取和保存应用程序的参数
*	修改记录 :
*		版本号  日期        作者     说明
*		V1.0    2013-01-01 armfly  正式发布
*
*	Copyright (C), 2012-2013, 安富莱电子 www.armfly.com
*
*********************************************************************************************************
*/

#include "bsp.h"
#include "param.h"
#include "conf.h"
#include "main.h"

PARAM_T g_tParam;

/* 将16KB 一个扇区的空间预留出来做为参数区 For MDK */
// const uint8_t para_flash_area[16*1024] __attribute__((at(ADDR_FLASH_SECTOR_3)));

/*
*********************************************************************************************************
*	函 数 名: LoadParam
*	功能说明: 从Flash读参数到g_tParam
*	形    参：无
*	返 回 值: 无
*********************************************************************************************************
*/
void LoadParam(void)
{
	// sys_time.test_sizeof_g_tParam = sizeof(g_tParam);
#ifdef PARAM_SAVE_TO_FLASH
	/* 读取CPU Flash中的参数 */
	bsp_ReadCpuFlash(PARAM_ADDR, (uint8_t *)&g_tParam, sizeof(PARAM_T));
#endif
	{
		g_u32CS_Res_AFE = ((UINT32)g_tParam.other.u16Sys_CS_Res_Num * 1000) / g_tParam.other.u16Sys_CS_Res;
		curr_offset = g_tParam.current_offset_309;
		if ((curr_offset & 0x8000) == 0)
		{
			OffsetValue_CHG = (UINT32)curr_offset * 200 * g_u32CS_Res_AFE / (21470);
		}
		else
		{
			OffsetValue_DSG = (UINT32)((UINT16)(0xFFFF - curr_offset + 1)) * 200 * g_u32CS_Res_AFE / (21470); // mA
		}
	}

#ifdef PARAM_SAVE_TO_EEPROM
	/* 读取EEPROM中的参数 */
	ee_ReadBytes((uint8_t *)&g_tParam, PARAM_ADDR, sizeof(PARAM_T));
#endif

	/* 填充缺省参数 */
	if (g_tParam.ParamVer != PARAM_VER)
	{
		PARAM_T Param_default = {
			.ParamVer = PARAM_VER,
			.protect = E2P_PROTECT_DEFAULT_PRT,
			.other = OtherElement_default,
			.heat = HeatCoolElement_Default,
		};
		for (uint16_t i = 0; i < E2P_PARA_NUM_CALIB_K; ++i)
		{
			Param_default.CalibCoefK[i] = SYSKDEFAULT;
			Param_default.CalibCoefB[i] = SYSBDEFAULT;
		}

		g_tParam.ParamVer = PARAM_VER;

		g_tParam = Param_default;
		{
			bool ret = false;
			do
			{
				initAFE1_IIC();
				AFE_IsReady();
				AFE_PARAM_WRITE_Flag = 1;
				ret = SH367309_UpdataAfeConfig();
			} while (ret == false);
			DataLoad_CurrentCali_startup();
		}

		SaveParam(); /* 将新参数写入Flash */
		MCU_RESET();
	}

	// if(g_tParam.protect != PRT_E2ROMParas)
	// {

	// }
	// if (memcmp(&g_tParam.protect, &PRT_E2ROMParas, sizeof(PRT_E2ROMParas)) != 0)
	// {
	// 	// System_ERROR_UserCallback(ERROR_CBC_CHG);
	// }
	// if (memcmp(&g_tParam.other, &g_tParam.other, sizeof(g_tParam.other)) != 0)
	// {
	// 	// System_ERROR_UserCallback(ERROR_CBC_CHG);
	// }
	// if (memcmp(&g_tParam.heat, &g_tParam.heat, sizeof(g_tParam.heat)) != 0)
	// {
	// 	// System_ERROR_UserCallback(ERROR_CBC_CHG);
	// }
}

/*
*********************************************************************************************************
*	函 数 名: SaveParam
*	功能说明: 将全局变量g_tParam 写入到CPU内部Flash
*	形    参: 无
*	返 回 值: 无
*********************************************************************************************************
*/
void SaveParam(void)
{
#ifdef PARAM_SAVE_TO_FLASH
	/* 将全局的参数变量保存到 CPU Flash */
	bsp_WriteCpuFlash(PARAM_ADDR, (unsigned char *)&g_tParam, sizeof(PARAM_T));
#endif

#ifdef PARAM_SAVE_TO_EEPROM
	/* 将全局的参数变量保存到EEPROM */
	ee_WriteBytes((uint8_t *)&g_tParam, PARAM_ADDR, sizeof(PARAM_T));
#endif

	LoadParam();
}

/***************************** 安富莱电子 www.armfly.com (END OF FILE) *********************************/
