// #include "LogRecord.h"
#include "stm32f0xx_flash.h"
#include <string.h>
#include "main.h"

#define E2P_ADDR_START_EVENT_RECORD 	0x0000	//��1198		//100����
#define E2P_ADDR_E2POS_EVENT_POINT		0x0200	//��һ��1202


/* ------------------- 全局变量 ------------------- */
#define EVENT_RECORD_LENGTH 100
UINT8 BMS_LOG_POINT = 0;
UINT8 BMS_LOG_RECORD[EVENT_RECORD_LENGTH][2];
LOG_RECORD_FLAG LogRecord_Flag;
UINT8 gu8_Reset_EventRecord = 0;

/* ------------------- Flash 仿 EEPROM 配置 ------------------- */
#define EEPROM_FLASH_BASE ((uint32_t)FLASH_ADDR_LOG_FLASH_START) /* 修改为你芯片 flash 尾部空白页 */
#define EEPROM_FLASH_PAGE_SIZE ((uint32_t)1024)  /* STM32F0 = 1K page */

#define PAGE0_BASE (EEPROM_FLASH_BASE)
#define PAGE1_BASE (EEPROM_FLASH_BASE + EEPROM_FLASH_PAGE_SIZE)

#define PAGE_STATUS_ERASED ((uint16_t)0xFFFF)
#define PAGE_STATUS_RECEIVE ((uint16_t)0xEEEE)
#define PAGE_STATUS_VALID ((uint16_t)0x0000)

#define RECORD_HALFWORDS 3U /* addr | data | crc */
#define PAGE_HALFWORDS_TOTAL (EEPROM_FLASH_PAGE_SIZE / 2U)
#define PAGE_DATA_HALFWORDS (PAGE_HALFWORDS_TOTAL - 1U)
#define PAGE_RECORD_CAPACITY (PAGE_DATA_HALFWORDS / RECORD_HALFWORDS)
#define EEPROM_WORD_COUNT (EEPROM_FLASH_PAGE_SIZE / 2U)

static inline uint16_t rec_crc(uint16_t addr, uint16_t data) { return addr ^ data; }

/* ------------------- EEPROM 仿真底层 ------------------- */
static uint16_t read_page_status(uint8_t page)
{
    return *(volatile uint16_t *)(page == 0 ? PAGE0_BASE : PAGE1_BASE);
}

static void erase_page(uint8_t page)
{
    FLASH_Unlock();
    FLASH_ErasePage(page == 0 ? PAGE0_BASE : PAGE1_BASE);
    FLASH_Lock();
}

static void write_page_status(uint8_t page, uint16_t status)
{
    FLASH_Unlock();
    FLASH_ProgramHalfWord(page == 0 ? PAGE0_BASE : PAGE1_BASE, status);
    FLASH_Lock();
}

static int find_active_page(void)
{
    uint16_t s0 = read_page_status(0);
    uint16_t s1 = read_page_status(1);
    if (s0 == PAGE_STATUS_VALID)
        return 0;
    if (s1 == PAGE_STATUS_VALID)
        return 1;
    erase_page(0);
    write_page_status(0, PAGE_STATUS_VALID);
    return 0;
}

static int find_free_index(uint8_t page)
{
    uint16_t *p = (uint16_t *)(page == 0 ? PAGE0_BASE : PAGE1_BASE);
    for (int i = 1; i + 2 < PAGE_HALFWORDS_TOTAL; i += RECORD_HALFWORDS)
    {
        if (p[i] == 0xFFFF && p[i + 1] == 0xFFFF && p[i + 2] == 0xFFFF)
            return (i - 1) / RECORD_HALFWORDS;
    }
    return -1;
}

static uint32_t rec_addr(uint8_t page, uint32_t idx)
{
    return (page == 0 ? PAGE0_BASE : PAGE1_BASE) + (1 + idx * RECORD_HALFWORDS) * 2;
}

static int search_latest(uint8_t page, uint16_t vaddr, uint16_t *out)
{
    uint16_t *p = (uint16_t *)(page == 0 ? PAGE0_BASE : PAGE1_BASE);
    for (int i = PAGE_HALFWORDS_TOTAL - RECORD_HALFWORDS; i >= 1; i -= RECORD_HALFWORDS)
    {
        uint16_t a = p[i], d = p[i + 1], c = p[i + 2];
        if (a == 0xFFFF && d == 0xFFFF)
            continue;
        if (c == rec_crc(a, d) && a == vaddr)
        {
            *out = d;
            return 0;
        }
    }
    return -1;
}

static int append_record(uint16_t vaddr, uint16_t data)
{
    int page = find_active_page();
    int idx = find_free_index(page);
    if (idx < 0)
        return -1;
    uint32_t addr = rec_addr(page, idx);
    FLASH_Unlock();
    FLASH_ProgramHalfWord(addr, vaddr);
    FLASH_ProgramHalfWord(addr + 2, data);
    FLASH_ProgramHalfWord(addr + 4, rec_crc(vaddr, data));
    FLASH_Lock();
    return 0;
}

uint16_t ReadEEPROM_Word_NoZone_flash(uint32_t u32ByteAddr)
{
    uint16_t vaddr = (uint16_t)(u32ByteAddr >> 1);
    int page = find_active_page();
    uint16_t val;
    if (search_latest(page, vaddr, &val) == 0)
        return val;
    if (search_latest(page ^ 1, vaddr, &val) == 0)
        return val;
    return 0xFFFF;
}

void WriteEEPROM_Word_NoZone_flash(uint32_t u32ByteAddr, uint16_t u16Data)
{
    uint16_t vaddr = (uint16_t)(u32ByteAddr >> 1);
    uint16_t cur = ReadEEPROM_Word_NoZone_flash(u32ByteAddr);
    if (cur == u16Data)
        return;
    if (append_record(vaddr, u16Data) == 0)
        return;

    int active = find_active_page();
    int other = active ^ 1;
    erase_page(other);
    write_page_status(other, PAGE_STATUS_RECEIVE);

    /* 简单搬运最新值 */
    for (uint16_t a = 0; a < EEPROM_WORD_COUNT; a++)
    {
        uint16_t val;
        if (search_latest(active, a, &val) == 0)
            append_record(a, val);
    }
    write_page_status(other, PAGE_STATUS_VALID);
    erase_page(active);

    append_record(vaddr, u16Data);
}

/* ------------------- 日志功能 ------------------- */
UINT8 LogTime_Map(UINT32 *Time_S_Cnt)
{
    UINT8 result = 0;
    if (*Time_S_Cnt <= 60)
        result = 171;
    else if (*Time_S_Cnt <= 3600 * 168)
        result = (UINT8)((*Time_S_Cnt + 3599) / 3600);
    else
        result = 170;
    *Time_S_Cnt = 0;
    return result;
}

void LogEvent_EEPROM(LogEventArray event, UINT32 *Time_S_Cnt)
{
    UINT16 temp;
    if (BMS_LOG_POINT >= EVENT_RECORD_LENGTH)
        BMS_LOG_POINT = 0;
    BMS_LOG_RECORD[BMS_LOG_POINT][0] = (UINT8)event;
    BMS_LOG_RECORD[BMS_LOG_POINT][1] = LogTime_Map(Time_S_Cnt);
    if (event == BMS_START_UP)
        BMS_LOG_RECORD[BMS_LOG_POINT][1] = 0;
    ++BMS_LOG_POINT;
    temp = (UINT16)(BMS_LOG_RECORD[BMS_LOG_POINT - 1][0] + (BMS_LOG_RECORD[BMS_LOG_POINT - 1][1] << 8));
    WriteEEPROM_Word_NoZone_flash(E2P_ADDR_START_EVENT_RECORD + ((BMS_LOG_POINT - 1) << 1), temp);
    WriteEEPROM_Word_NoZone_flash(E2P_ADDR_E2POS_EVENT_POINT, BMS_LOG_POINT);
}

void LogEvent_Record(UINT8 temp, LogEventArray event, UINT32 *Time_S_Cnt)
{
    static UINT8 su8_Event[EVENT_NUM] = {0};
    static UINT8 su8_CBC_Temp = 0;
    if (BMS_START_UP == event)
    {
        if (LogRecord_Flag.bits.Log_StartUp)
        {
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
            Sleep_Mode.bits.b1_ToSleepFlag = 0;
        }
    }
    else if (CBC_ERR == event)
    {
        if (su8_CBC_Temp != temp)
        {
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
            }
            break;
        case 1:
            if (!temp)
                su8_Event[event] = 0;
            break;
        default:
            break;
        }
    }
}

void App_LogRecord(void)
{
    static UINT32 su32_Interval_S_Tcnt = 0;
    if (!g_st_SysTimeFlag.bits.b1Sys1000msFlag3)
        return;
    if (gu8_Reset_EventRecord)
        return;
    ++su32_Interval_S_Tcnt;

    LogEvent_Record(LogRecord_Flag.bits.Log_StartUp, BMS_START_UP, &su32_Interval_S_Tcnt);
    LogEvent_Record(LogRecord_Flag.bits.Log_Sleep, BMS_SLEEP, &su32_Interval_S_Tcnt);
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

    LogEvent_Record(System_ERROR_UserCallback(ERROR_STATUS_AFE1), AFE1_ERR, &su32_Interval_S_Tcnt);
    LogEvent_Record(System_ERROR_UserCallback(ERROR_STATUS_AFE2), AFE2_ERR, &su32_Interval_S_Tcnt);
    UINT8 temp = (UINT8)(System_ERROR_UserCallback(ERROR_STATUS_EEPROM_STORE) +
                         System_ERROR_UserCallback(ERROR_STATUS_EEPROM_COM));
    LogEvent_Record(temp, EEPROM_ERR, &su32_Interval_S_Tcnt);
    LogEvent_Record(System_ERROR_UserCallback(ERROR_STATUS_CBC_DSG), CBC_ERR, &su32_Interval_S_Tcnt);
}

void Sci_ACK_0x03_ReadRegs_EventRecord(UINT8 t_u8BuffTemp[])
{
    UINT16 i = 0, j;
    INT8 k;
    for (j = 0; j < EVENT_RECORD_LENGTH; j++)
    {
        k = (INT8)BMS_LOG_POINT - 1 - (INT8)j;
        if (k < 0)
            k = EVENT_RECORD_LENGTH + k;
        t_u8BuffTemp[i++] = BMS_LOG_RECORD[k][0];
        t_u8BuffTemp[i++] = BMS_LOG_RECORD[k][1];
    }
}

void Sci_WrReg_0x06_Reset_EventRecord(struct RS485MSG *s)
{
    UINT16 u16SciRegData = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
    if (0x0001 == u16SciRegData)
    {
        for (UINT8 i = 0; i < EVENT_RECORD_LENGTH; i++)
        {
            BMS_LOG_RECORD[i][0] = 0;
            BMS_LOG_RECORD[i][1] = 0;
        }
        BMS_LOG_POINT = 0;
        EEPROM_ResetData_EventRecord_ToDefault();
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
    for (UINT8 i = 0; i < EVENT_RECORD_LENGTH; i++)
    {
        BMS_LOG_RECORD[i][0] = 0;
        BMS_LOG_RECORD[i][1] = 0;
        WriteEEPROM_Word_NoZone_flash(E2P_ADDR_START_EVENT_RECORD + (i << 1), 0);
    }
    BMS_LOG_POINT = 0;
    WriteEEPROM_Word_NoZone_flash(E2P_ADDR_E2POS_EVENT_POINT, BMS_LOG_POINT);
}

void ReadEEPROM_EventRecord_Parameters(void)
{
    BMS_LOG_POINT = (UINT8)ReadEEPROM_Word_NoZone_flash(E2P_ADDR_E2POS_EVENT_POINT);
    if (BMS_LOG_POINT >= 101)
    {
        System_ERROR_UserCallback(ERROR_EEPROM_STORE);
        EEPROM_ResetData_EventRecord_ToDefault();
    }
    for (UINT8 i = 0; i < EVENT_RECORD_LENGTH; i++)
    {
        UINT16 t = ReadEEPROM_Word_NoZone_flash(E2P_ADDR_START_EVENT_RECORD + (i << 1));
        if (((t & 0x00FF) <= EVENT_NUM) && ((t >> 8) <= 171))
        {
            BMS_LOG_RECORD[i][0] = (UINT8)(t & 0xFF);
            BMS_LOG_RECORD[i][1] = (UINT8)(t >> 8);
        }
        else
        {
            if (!System_ErrFlag.u8ErrFlag_Com_EEPROM)
                System_ERROR_UserCallback(ERROR_EEPROM_STORE);
            BMS_LOG_RECORD[i][0] = 0;
            BMS_LOG_RECORD[i][1] = 0;
            WriteEEPROM_Word_NoZone_flash(E2P_ADDR_START_EVENT_RECORD + (i << 1), 0);
        }
    }
}
