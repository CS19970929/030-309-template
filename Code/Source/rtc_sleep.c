#include "main.h"

static enum _SLEEP_MODE g_sleepModeSelect = NO_SLEEP;

void BQ769x0_SleepMode_Ctrl(void)
{
    static UINT8 su8_StartUp_Flag = 0;
    static UINT8 su8_SleepExtComCnt = 0;
    static UINT16 su16_RTC2_100msTCnt = 0;
    static uint32_t deepsleep_cnt = 0;

    UINT8 u8_CurComDelay_Flag = 0;

    // todo çťä¸rtc_sleep()ĺApp_SleepDeal()čżćžäźç 
    if (AFE_SleepMode_Judge() == 1)
    {
        su16_RTC2_100msTCnt = 0;
        // print_vcell();
        if (++deepsleep_cnt >= (uint32_t)g_tParam.other.u16Sleep_TimeVlow * 60)
        {
            entersleep(DEEP_MODE);
        }
        log_w("%d s enter deep sleep", (60 * g_tParam.other.u16Sleep_TimeVlow - deepsleep_cnt));
        return;
    }
    else
    {
        deepsleep_cnt = 0;
    }

    switch (su8_StartUp_Flag)
    {
    case 0:
        su8_StartUp_Flag = 1;
        break;
    case 1:
        if (isErr_enterRTC())
        {
            u8_CurComDelay_Flag = 1;
        }
        else if (su8_SleepExtComCnt != RTC_ExtComCnt)
        {
            su8_SleepExtComCnt = RTC_ExtComCnt;
            u8_CurComDelay_Flag = 1;
        }

        if (u8_CurComDelay_Flag)
        {
            su16_RTC2_100msTCnt = 0;
        }
        else
        {
            if (AFE_SleepMode_Judge() == 0)
            {
                if (++su16_RTC2_100msTCnt >= g_tParam.other.time_enter_rtc)
                // if (++su16_RTC2_100msTCnt >= ENTER_RTC_TIME)
                {
                    su16_RTC2_100msTCnt = 0;

                    entersleep(HICCUP_MODE);
                }
                // log_w("%d s enter rtc mode1", (ENTER_RTC_TIME - su16_RTC2_100msTCnt));
                log_w("%d s enter rtc mode1", (g_tParam.other.time_enter_rtc - su16_RTC2_100msTCnt));
            }
            else
            {
                log_a("err");
            }
        }
        break;
    default:
        break;
    }
}

void rtc_sleep(void)
{
    if (!gu8_1000msAccClock_Flag)
        return;
    gu8_1000msAccClock_Flag = false;

    if (System_ERROR_UserCallback(ERROR_STATUS_EEPROM_COM))
    {
        ReadEEPROM_Byte(0);
        // System_ERROR_UserCallback(ERROR_REMOVE_EEPROM_COM);
    }

    BQ769x0_SleepMode_Ctrl();
    static uint8_t state_sleep = 0;
    static uint32_t sleep_cnt = 0;

    switch (state_sleep)
    {
    case 0:
    {
        if (g_sleepModeSelect == HICCUP_MODE)
        {
            // Sleep_Mode.bits.b1_ToSleepFlag = 1;
            // LogRecord_Flag.bits.Log_Sleep = 1;
            // USART_DeInit(USART1);
            state_sleep = 1;
            break;
        }
        if (g_sleepModeSelect == DEEP_MODE)
        {
            Sleep_Mode.bits.b1_ToSleepFlag = 1;
            LogRecord_Flag.bits.Log_Sleep = 1;
            state_sleep = 1;
            break;
        }
    }
    case 1:
    {
        if (Sleep_Mode.bits.b1_ToSleepFlag)
        {
            return;
        }
        switch (g_sleepModeSelect)
        {
        case NORMAL_MODE:
            log_e("enter rtc mode2\n");
            break;
        case HICCUP_MODE:
        {
            before_rtcsleep();
            // todoˇĹŐâśůšŚşÄťá¸ßŇťľă
            //  IOstatus_RTCMode();
            // todo ˛ťÄÜˇĹŐâśů
            //  InitWakeUp_RTCMode();
        rtcsleep:
            Init_RTC();
            // RTC_WKTimeConfig();
            IOstatus_RTCMode();
            InitWakeUp_RTCMode();

            // USART_DeInit(USART1);
            // USART_DeInit(USART2);
            // USART_DeInit(USART3);
            is_rtc_wakekup = false;
            g_irq_t = NO_IRQ;
            // __delay_ms(100);

            Feed_IWatchDog;
            Sys_StopMode();
            Feed_IWatchDog;
            // __delay_ms(100);
            // DISABLE_INT();
#if 1
#if defined(UART1_WAKEUP_ENABLE)
            exti_conf(EXTI_Line7, EXTI_Trigger_Rising, DISABLE);
#endif
#if defined(UART3_WAKEUP_ENABLE)
            exti_conf(EXTI_Line3, EXTI_Trigger_Rising, DISABLE);
#endif
#if defined(RS485_CAN_WAKEUP_ENABLE)
            exti_conf(EXTI_Line14, EXTI_Trigger_Rising, DISABLE);
#endif

#ifdef __STM32F0__
            RTC_AlarmCmd(RTC_Alarm_A, DISABLE);
#endif // __STM32F0__
#ifdef __STM32F1__
            RTC_ITConfig(RTC_FLAG_ALR, DISABLE);
#endif // __STM32F1__
#endif
            // deal_wakeup();
            InitSci();
            print_irq_cnt();

            if (is_rtc_wakekup)
            {
                Init();

                ++sleep_cnt;
                g_rtcInfo.rtc_sleepTime = sleep_cnt * g_tParam.other.time_sleep_rtcing;
                log_e("sleep time: %d sec\n", g_rtcInfo.rtc_sleepTime);
                // getdata_and_analyse()
                if (isException())
                {
                    goto error;
                }
                else
                {
                    doWork_rtcing(&sleep_cnt);

                    goto rtcsleep;
                }
            }
        error:
            // todo
            //  deal_exception_and_record();
            is_rtc_wakekup = false;
            Init();

            state_sleep = 0;
            entersleep(NO_SLEEP);

            report_wkup_sig();

            before_wakeup(&sleep_cnt);
            sleep_cnt = 0;
        }
        break;
        case DEEP_MODE:
        DEEP_SLEEP:
            if (FLASH_COMPLETE == FlashWriteOneHalfWord(FLASH_ADDR_SLEEP_FLAG, FLASH_DEEP_SLEEP_VALUE))
            {
                // App_LogRecord();
                LogEvent_Record(LogRecord_Flag.bits.Log_Sleep, BMS_SLEEP, &su32_Interval_S_Tcnt);

                log_w("deep sleep\n");
                MCU_RESET();
                break;
            }
        default:
            break;
        }
    }
    default:
        break;
    }
}

void entersleep(enum _SLEEP_MODE mode)
{
    switch (mode)
    {
    case HICCUP_MODE:
        Sleep_Mode.bits.b1ForceToSleep_L1 = 1;
        g_sleepModeSelect = HICCUP_MODE;
        break;
    case NORMAL_MODE:

        break;
    case DEEP_MODE:
        Sleep_Mode.bits.b1ForceToSleep_L3 = 1;
        g_sleepModeSelect = DEEP_MODE;
#ifdef __FUNC__LED__
        // set_LED_state(LED_BAR_NORMAL, 4);
#endif // DEBUG
        break;
    case NO_SLEEP:
        g_sleepModeSelect = NO_SLEEP;
        Sleep_Status = SLEEP_HICCUP_SHIFT;
        Sleep_Mode.all = 0;
        break;
    default:
        break;
    }
}
