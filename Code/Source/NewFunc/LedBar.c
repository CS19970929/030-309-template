#include "main.h"

LEDBAR_COMMAND LedBar_Command = LED_BAR_STARTUP;

void LedBar_StartUp(void)
{
    static UINT16 su16_ShowDelay_Tcnt = 0;

    if (++su16_ShowDelay_Tcnt <= 10 * 5)
    {
        apply_led(cur_level);
    }
    else
    {
        apply_led(0);
        su16_ShowDelay_Tcnt = 0;

        LedBar_Command = LED_BAR_NORMAL;
    }
}

extern void apply_led(int level);

void LedBar_Show_Normal(void)
{
    static UINT8 su8_ShowStatus = 0; // 开机亮5s
    static UINT16 su16_ShowDelay_Tcnt = 0;

    switch (su8_ShowStatus)
    {
    case 0:

        if (MCUI_SOC_KEY == 0)
        {
            su8_ShowStatus = 1;
            // MCUO_DO1_EN = !MCUO_DO1_EN;
        }

        // if (g_stCellInfoReport.u16Ichg)
        // {
        //     LedBar_Command = LED_BAR_CHG;
        // }

        // if (g_stCellInfoReport.u16IDischg)
        // {
        //     LedBar_Command = LED_BAR_DSG;
        // }
        break;
        // fixme 不起作用

    case 1:
        // 5s
        if (++su16_ShowDelay_Tcnt <= 10 * 5)
        {
            uint8_t level = 0;
            if (g_stCellInfoReport.u16VCellTotle >= 5740)
                level = 5;
            else if (g_stCellInfoReport.u16VCellTotle >= 5460)
            {
                level = 4;
            }
            else if (g_stCellInfoReport.u16VCellTotle >= 5040)
            {
                level = 3;
            }
            else if (g_stCellInfoReport.u16VCellTotle >= 4620)
            {
                level = 2;
            }
            else if (g_stCellInfoReport.u16VCellTotle >= 4340)
            {
                level = 1;
            }
            else
            {
                level = 0;
            }
            apply_led(level);
        }
        else
        {
            apply_led(0);

            su16_ShowDelay_Tcnt = 0;
            su8_ShowStatus = 0;
        }

        // 一直按着
        if (!MCUI_SOC_KEY)
            su16_ShowDelay_Tcnt = 0;
        break;

    default:
        break;
    }
}

void LedBar_Show_CHG(void)
{
    static UINT8 su8_temp = 0;
    static UINT16 su16_ShowDelay = 0;

    if (++su16_ShowDelay <= 5)
    {
        su8_temp = ~(1 << (g_stCellInfoReport.SocElement.u16Soc / 20)); // 充电的灭
    }
    else if (++su16_ShowDelay <= 10)
    {
        su8_temp = 0xFF; // 全亮
    }
    else
    {
        su16_ShowDelay = 0;
    }

    // SOC =100的时候，为0x20，运算结果相当于全亮，不会有闪的
    // SOC =0的时候，也要闪
    MCUO_SOC_RUN = 1;
    MCUO_SOC_20 = (g_stCellInfoReport.SocElement.u16Soc >= 0 ? 1 : 0) && (su8_temp & 0x01); // 这里的三目运算符为>=，而不是>，因为这个灯一定要亮
    MCUO_SOC_40 = (g_stCellInfoReport.SocElement.u16Soc >= 20 ? 1 : 0) && (su8_temp & 0x02);
    MCUO_SOC_60 = (g_stCellInfoReport.SocElement.u16Soc >= 40 ? 1 : 0) && (su8_temp & 0x04);
    MCUO_SOC_80 = (g_stCellInfoReport.SocElement.u16Soc >= 60 ? 1 : 0) && (su8_temp & 0x08);
    MCUO_SOC_100 = (g_stCellInfoReport.SocElement.u16Soc >= 80 ? 1 : 0) && (su8_temp & 0x10);

    if (g_stCellInfoReport.u16Ichg == 0)
    {
        MCUO_SOC_RUN = 0;
        MCUO_SOC_20 = 0;
        MCUO_SOC_40 = 0;
        MCUO_SOC_60 = 0;
        MCUO_SOC_80 = 0;
        MCUO_SOC_100 = 0;

        LedBar_Command = LED_BAR_NORMAL;
    }
}

void LedBar_Show_DSG(void)
{
    MCUO_SOC_RUN = 1;
    MCUO_SOC_20 = g_stCellInfoReport.SocElement.u16Soc > 0 ? 1 : 0;
    MCUO_SOC_40 = g_stCellInfoReport.SocElement.u16Soc >= 20 ? 1 : 0;
    MCUO_SOC_60 = g_stCellInfoReport.SocElement.u16Soc >= 40 ? 1 : 0;
    MCUO_SOC_80 = g_stCellInfoReport.SocElement.u16Soc >= 60 ? 1 : 0;
    MCUO_SOC_100 = g_stCellInfoReport.SocElement.u16Soc >= 80 ? 1 : 0;

    if (g_stCellInfoReport.u16IDischg == 0)
    {
        MCUO_SOC_RUN = 0;
        MCUO_SOC_20 = 0;
        MCUO_SOC_40 = 0;
        MCUO_SOC_60 = 0;
        MCUO_SOC_80 = 0;
        MCUO_SOC_100 = 0;

        LedBar_Command = LED_BAR_NORMAL;
    }
}

void LedBar_Show_Fault(void)
{
    if (g_stCellInfoReport.unMdlFault_Third.all & 0x3FFB ||
        System_ERROR_UserCallback(ERROR_STATUS_TEMP_BREAK) ||
        System_ERROR_UserCallback(ERROR_STATUS_CBC_DSG))
    {
        MCUO_SOC_ALARM = !MCUO_SOC_ALARM;
    }
    else
    {
        MCUO_SOC_ALARM = 0;
    }
}

void LedBar_Show_Sleep(void)
{
    static UINT16 su16_SleepDelay_Tcnt = 0;

    if (!MCUI_SOC_KEY)
    {
        if (++su16_SleepDelay_Tcnt >= 20)
        {
            // Sleep_Mode.bits.b1ForceToSleep_L2 = 1;
            Sleep_Mode.bits.b1ForceToSleep_L3 = 1;
            // SleepElement.Sleep_Mode.bits.b1ForceToSleep_L2 = 1;
        }
    }
    else
    {
        su16_SleepDelay_Tcnt = 0;
    }
}

#if 1
void APP_LedBar(void)
{
    if (0 == g_st_SysTimeFlag.bits.b1Sys100msFlag)
    {
        return;
    }

    if (SystemStatus.bits.b1StartUpBMS)
    {
        return;
    }

    switch (LedBar_Command)
    {
    case LED_BAR_STARTUP:
        LedBar_StartUp();
        break;
    case LED_BAR_NORMAL:
        LedBar_Show_Normal();
        break;
    case LED_BAR_CHG:
        LedBar_Show_CHG();
        break;
    case LED_BAR_DSG:
        LedBar_Show_DSG();
        break;
    case LED_BAR_FAULT:
        // 下面长期监控
        break;

    default:
        break;
    }

    LedBar_Show_Fault();
    // 好像有问题
    LedBar_Show_Sleep();

    if (BlueToothFlag)
    {
        MCUO_SOC_BLE = 1;
    }
    else
        MCUO_SOC_BLE = 0;
}

#endif

void APP_LedBar(void)
{
    if (0 == g_st_SysTimeFlag.bits.b1Sys100msFlag)
    {
        return;
    }

    if (SystemStatus.bits.b1StartUpBMS)
    {
        return;
    }

    LedBar_Show_Normal();
}
