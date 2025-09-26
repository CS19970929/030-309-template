/* LogRecord.c  — 替换版：包含 Flash 仿 EEPROM（双页环形）并保留原有日志接口实现 */
#include "main.h"               /* 你的项目主头（包含 UINT8/UINT16/UINT32 等） */
#include "LogRecord.h"
#include "stm32f0xx_flash.h"
#include <string.h>
#include <stdlib.h>
#include <stdint.h>

/* ---------------- 常量（保持与你原始代码语义一致） ---------------- */
#define EVENT_RECORD_LENGTH 100

UINT8 BMS_LOG_POINT = 0;
UINT8 BMS_LOG_RECORD[EVENT_RECORD_LENGTH][2]; /* 0: event id, 1: time-map */
LOG_RECORD_FLAG LogRecord_Flag;
UINT8 gu8_Reset_EventRecord = 0;

/* ---------------- Log 时间映射（保持原来实现） ---------------- */
UINT8 LogTime_Map(UINT32 *Time_S_Cnt)
{
    UINT8 result = 0;

    if ((*Time_S_Cnt) <= 60)
    { /* 1min 以内 */
        result = 171;
    }
    else if ((*Time_S_Cnt) <= 3600 * 168)
    { /* 7d 以内，按小时算 */
        /* 修正优先级：把小时数 +（如果有余数就加1）*/
        result = (UINT8)(((*Time_S_Cnt) / 3600) + (((*Time_S_Cnt) % 3600) > 0 ? 1 : 0));
    }
    else
    {
        result = 170; /* 大于7d，统一为大于7d */
    }

    (*Time_S_Cnt) = 0; /* 清零，统计下一个间隔 */

    return result;
}

/* ---------------------- Flash 仿 EEPROM 配置 ----------------------
   必须检查并根据你的目标芯片/工程修改下面两个宏：
   - EEPROM_FLASH_BASE: 模拟 EEPROM 区域的 flash 起始地址（必须页对齐）
   - EEPROM_FLASH_PAGE_SIZE: 单页大小（字节），常见 1024 / 2048 等（取决芯片型号）

   我默认把区域放在 flash 的尾部一处：0x0800F800（示例），每页 1024 字节（示例）。
   请根据你的 MCU flash 大小与 linker map 调整。
*/
#ifndef EEPROM_FLASH_BASE
#define EEPROM_FLASH_BASE       ((uint32_t)FLASH_ADDR_LOG_FLASH_START)   /* <<--- 请按你的芯片调整 */
#endif

#ifndef EEPROM_FLASH_PAGE_SIZE
#define EEPROM_FLASH_PAGE_SIZE  ((uint32_t)1024)         /* <<--- 请按你的芯片调整 */
#endif

#define PAGE0_BASE              (EEPROM_FLASH_BASE)
#define PAGE1_BASE              (EEPROM_FLASH_BASE + EEPROM_FLASH_PAGE_SIZE)

#define PAGE_STATUS_ERASED   ((uint16_t)0xFFFF)
#define PAGE_STATUS_RECEIVE  ((uint16_t)0xEEEE)
#define PAGE_STATUS_VALID    ((uint16_t)0x0000)

#define RECORD_HALFWORDS     3U   /* 每条记录占用半字数： addr(16) | data(16) | crc(16) */
#define PAGE_HALFWORDS_TOTAL (EEPROM_FLASH_PAGE_SIZE / 2U)
#define PAGE_DATA_HALFWORDS  (PAGE_HALFWORDS_TOTAL - 1U) /* 第一半字用于 page status */
#define PAGE_RECORD_CAPACITY (PAGE_DATA_HALFWORDS / RECORD_HALFWORDS)

/* 为搬移使用的最大虚地址空间（以半字为单位）——用 page 的半字数作上限 */
#define EEPROM_WORD_COUNT (EEPROM_FLASH_PAGE_SIZE / 2U)

/* 如果项目中有 E2P_ADDR_* 宏，它们可能是“偏移”或“绝对地址”两种风格中的一种。
   为兼容两种风格，下面的 normalize_eeprom_addr 会自动做映射：
   - 如果传入地址已经是 flash 区域内的绝对地址（>= EEPROM_FLASH_BASE），直接用它。
   - 否则把它作为偏移量，加上 EEPROM_FLASH_BASE 后使用。
*/
static uint32_t normalize_eeprom_addr(uint32_t u32ByteAddr)
{
    if ((u32ByteAddr >= EEPROM_FLASH_BASE) && (u32ByteAddr < (EEPROM_FLASH_BASE + 2U * EEPROM_FLASH_PAGE_SIZE)))
    {
        return u32ByteAddr;
    }
    return EEPROM_FLASH_BASE + u32ByteAddr;
}

/* CRC（简单异或） */
static inline uint16_t rec_crc(uint16_t addr, uint16_t data)
{
    return (uint16_t)(addr ^ data);
}

/* page base */
static inline uint32_t page_base_addr(uint8_t page_index)
{
    return (page_index == 0) ? PAGE0_BASE : PAGE1_BASE;
}

/* 读 page 状态 */
static uint16_t read_page_status(uint8_t page_index)
{
    volatile uint16_t *p = (uint16_t *)page_base_addr(page_index);
    return *p;
}

/* 写 page 状态（半字） */
static int write_page_status(uint8_t page_index, uint16_t status)
{
    uint32_t addr = page_base_addr(page_index);
    __disable_irq();
    FLASH_Unlock();
    FLASH_Status st = FLASH_ProgramHalfWord(addr, status);
    FLASH_Lock();
    __enable_irq();
    return (st == FLASH_COMPLETE) ? 0 : -1;
}

/* 擦除 page */
static int erase_page(uint8_t page_index)
{
    uint32_t addr = page_base_addr(page_index);
    __disable_irq();
    FLASH_Unlock();
    FLASH_Status st = FLASH_ErasePage(addr);
    FLASH_Lock();
    __enable_irq();
    return (st == FLASH_COMPLETE) ? 0 : -1;
}

/* 找到活动页（0 或 1）——若两页都 ERASED，则将 page0 置为 VALID 并返回 0 */
static int find_active_page(void)
{
    uint16_t s0 = read_page_status(0);
    uint16_t s1 = read_page_status(1);

    if ((s0 == PAGE_STATUS_VALID) && (s1 != PAGE_STATUS_VALID)) return 0;
    if ((s1 == PAGE_STATUS_VALID) && (s0 != PAGE_STATUS_VALID)) return 1;

    /* 如果一页处于 RECEIVE 且另一页为 VALID，认为接收页应变为 VALID（恢复） */
    if ((s0 == PAGE_STATUS_RECEIVE) && (s1 == PAGE_STATUS_VALID)) {
        write_page_status(0, PAGE_STATUS_VALID);
        return 0;
    }
    if ((s1 == PAGE_STATUS_RECEIVE) && (s0 == PAGE_STATUS_VALID)) {
        write_page_status(1, PAGE_STATUS_VALID);
        return 1;
    }

    /* 如果都 ERASED -> 初始化 page0 为 VALID */
    if ((s0 == PAGE_STATUS_ERASED) && (s1 == PAGE_STATUS_ERASED)) {
        erase_page(0); /* 确保为擦空状态 */
        write_page_status(0, PAGE_STATUS_VALID);
        return 0;
    }

    /* 如果两页都为 VALID（异常） -> 选择记录更多的一页 */
    if ((s0 == PAGE_STATUS_VALID) && (s1 == PAGE_STATUS_VALID)) {
        uint16_t *p0 = (uint16_t *)PAGE0_BASE;
        uint16_t *p1 = (uint16_t *)PAGE1_BASE;
        uint32_t count0 = 0, count1 = 0;
        for (uint32_t hw = 1; hw + 2 < PAGE_HALFWORDS_TOTAL; hw += RECORD_HALFWORDS) {
            if (p0[hw] == 0xFFFF && p0[hw+1] == 0xFFFF && p0[hw+2] == 0xFFFF) break;
            count0++;
        }
        for (uint32_t hw = 1; hw + 2 < PAGE_HALFWORDS_TOTAL; hw += RECORD_HALFWORDS) {
            if (p1[hw] == 0xFFFF && p1[hw+1] == 0xFFFF && p1[hw+2] == 0xFFFF) break;
            count1++;
        }
        return (count0 >= count1) ? 0 : 1;
    }

    /* 兜底：优先返回 VALID 页，否则返回 0 */
    if (s0 == PAGE_STATUS_VALID) return 0;
    if (s1 == PAGE_STATUS_VALID) return 1;
    return 0;
}

/* 查找 page 中第一个空闲记录索引（返回 record index 或 -1 表示满） */
static int find_first_free_record_index(uint8_t page_index)
{
    uint16_t *p = (uint16_t *)page_base_addr(page_index);
    for (uint32_t hw = 1; hw + 2 < PAGE_HALFWORDS_TOTAL; hw += RECORD_HALFWORDS) {
        if ((p[hw] == 0xFFFF) && (p[hw+1] == 0xFFFF) && (p[hw+2] == 0xFFFF)) {
            return (int)((hw - 1U) / RECORD_HALFWORDS);
        }
    }
    return -1;
}

/* 由 record index 计算绝对地址（字节） */
static uint32_t record_addr_by_index(uint8_t page_index, uint32_t rec_index)
{
    uint32_t base_hw = 1U + rec_index * RECORD_HALFWORDS; /* 半字索引 */
    return page_base_addr(page_index) + (base_hw * 2U);
}

/* 从 page 从后往前搜索 vaddr 的最新记录，找到返回 0 并把 data 放到 out_data */
static int search_latest_in_page(uint8_t page_index, uint16_t vaddr, uint16_t *out_data)
{
    uint16_t *p = (uint16_t *)page_base_addr(page_index);
    for (int hw = (int)(PAGE_HALFWORDS_TOTAL - RECORD_HALFWORDS); hw >= 1; hw -= RECORD_HALFWORDS) {
        uint16_t a = p[hw];
        uint16_t d = p[hw+1];
        uint16_t c = p[hw+2];
        if ((a == 0xFFFF) && (d == 0xFFFF) && (c == 0xFFFF)) continue;
        if ((rec_crc(a, d) == c) && (a == vaddr)) {
            *out_data = d;
            return 0;
        }
    }
    return -1;
}

/* 读取虚拟地址 vaddr 的最新值（先在 active page 搜，再去另一页） */
static int read_latest(uint16_t vaddr, uint16_t *out_data)
{
    int active = find_active_page();
    if (search_latest_in_page((uint8_t)active, vaddr, out_data) == 0) return 0;
    if (search_latest_in_page((uint8_t)(active ^ 1), vaddr, out_data) == 0) return 0;
    return -1;
}

/* 将 RAM 的映射（map_valid/map_values）写入 page_to（page_to 已擦空），并标记 VALID */
static int write_map_to_page(uint8_t page_to, uint8_t *map_valid, uint16_t *map_values)
{
    /* 写入前把 page_to 状态置为 RECEIVE */
    if (write_page_status(page_to, PAGE_STATUS_RECEIVE) != 0) return -1;

    int rec_index = 0;
    for (uint32_t a = 0; a < EEPROM_WORD_COUNT; ++a) {
        if (!map_valid[a]) continue;
        if (rec_index >= (int)PAGE_RECORD_CAPACITY) return -2; /* 空间不足 */
        uint32_t addr = record_addr_by_index(page_to, (uint32_t)rec_index);
        __disable_irq();
        FLASH_Unlock();
        FLASH_Status st;
        st = FLASH_ProgramHalfWord(addr, (uint16_t)a);
        if (st != FLASH_COMPLETE) { FLASH_Lock(); __enable_irq(); return -3; }
        st = FLASH_ProgramHalfWord(addr + 2, map_values[a]);
        if (st != FLASH_COMPLETE) { FLASH_Lock(); __enable_irq(); return -4; }
        st = FLASH_ProgramHalfWord(addr + 4, rec_crc((uint16_t)a, map_values[a]));
        FLASH_Lock();
        __enable_irq();
        if (st != FLASH_COMPLETE) return -5;
        rec_index++;
    }

    /* 全写完后把 page_to 标记为 VALID */
    if (write_page_status(page_to, PAGE_STATUS_VALID) != 0) return -6;
    return 0;
}

/* page_transfer：把 page_from 的最新数据搬到 page_to（page_to 必须先擦空） */
static int page_transfer(uint8_t page_from, uint8_t page_to)
{
    /* 使用静态缓冲，避免 malloc（嵌入式更安全） */
    static uint16_t map_values_static[EEPROM_WORD_COUNT];
    static uint8_t map_valid_static[EEPROM_WORD_COUNT];
    uint16_t *map_values = map_values_static;
    uint8_t *map_valid = map_valid_static;

    memset(map_valid, 0, EEPROM_WORD_COUNT);
    /* 扫描 page_from，从头到尾（早写先见，后写覆盖） */
    uint16_t *p = (uint16_t *)page_base_addr(page_from);
    for (uint32_t hw = 1; hw + 2 < PAGE_HALFWORDS_TOTAL; hw += RECORD_HALFWORDS) {
        uint16_t a = p[hw];
        uint16_t d = p[hw+1];
        uint16_t c = p[hw+2];
        if ((a == 0xFFFF) && (d == 0xFFFF) && (c == 0xFFFF)) continue;
        if (rec_crc(a, d) == c) {
            if (a < EEPROM_WORD_COUNT) {
                map_values[a] = d;
                map_valid[a] = 1;
            }
        }
    }

    /* 把 map 写入 to 页 */
    if (write_map_to_page(page_to, map_valid, map_values) != 0) {
        return -1;
    }

    /* 擦除旧页 */
    if (erase_page(page_from) != 0) return -2;
    return 0;
}

/* 在活动页追加一条记录（addr=data 虚地址，data 为半字数据） */
static int append_record_to_active(uint16_t vaddr, uint16_t data)
{
    int active = find_active_page();
    int free_idx = find_first_free_record_index((uint8_t)active);
    if (free_idx >= 0) {
        uint32_t addr = record_addr_by_index((uint8_t)active, (uint32_t)free_idx);
        __disable_irq();
        FLASH_Unlock();
        FLASH_Status st;
        st = FLASH_ProgramHalfWord(addr, vaddr);
        if (st != FLASH_COMPLETE) { FLASH_Lock(); __enable_irq(); return -2; }
        st = FLASH_ProgramHalfWord(addr + 2, data);
        if (st != FLASH_COMPLETE) { FLASH_Lock(); __enable_irq(); return -3; }
        st = FLASH_ProgramHalfWord(addr + 4, rec_crc(vaddr, data));
        FLASH_Lock();
        __enable_irq();
        if (st != FLASH_COMPLETE) return -4;
        return 0;
    }
    return -1; /* 没空位 */
}

/* ========================= EEPROM 替代接口（对外） ========================= */

/* 读取一个半字（16-bit），参数 u32ByteAddr 支持偏移或绝对地址两种格式 */
uint16_t ReadEEPROM_Word_NoZone(uint32_t u32ByteAddr)
{
    uint32_t absAddr = normalize_eeprom_addr(u32ByteAddr);
    if (absAddr < EEPROM_FLASH_BASE) return 0xFFFF;
    uint32_t off = absAddr - EEPROM_FLASH_BASE;
    if (off & 1U) return 0xFFFF; /* 非半字对齐 */
    uint16_t vaddr = (uint16_t)(off / 2U);
    uint16_t val;
    if (read_latest(vaddr, &val) == 0) return val;
    return 0xFFFF;
}

/* 写一个半字（16-bit）：先比较现值，相同则跳过；否则追加记录到 active page。
   若当前页无空位则触发 page_transfer（把当前页数据搬到另一页）再追加。
*/
void WriteEEPROM_Word_NoZone(uint32_t u32ByteAddr, uint16_t u16Data)
{
    uint32_t absAddr = normalize_eeprom_addr(u32ByteAddr);
    if (absAddr < EEPROM_FLASH_BASE) return;
    uint32_t off = absAddr - EEPROM_FLASH_BASE;
    if (off & 1U) return;
    uint16_t vaddr = (uint16_t)(off / 2U);

    uint16_t cur = ReadEEPROM_Word_NoZone(u32ByteAddr);
    if (cur == u16Data) return;

    if (append_record_to_active(vaddr, u16Data) == 0) return;

    /* 如果追加失败（page 已满），则搬移到另一页 */
    int active = find_active_page();
    int other = active ^ 1;

    /* 擦空 other -> page_transfer(active->other) */
    if (erase_page((uint8_t)other) != 0) {
        return;
    }
    if (page_transfer((uint8_t)active, (uint8_t)other) != 0) {
        return;
    }

    /* 迁移完成后再追加（此时应该有空间） */
    append_record_to_active(vaddr, u16Data);
}

/* 可选：清格式化（擦两页并把 page0 置为 VALID）——仅调试用（未对外声明） */
static int flash_format_two_pages(void)
{
    if (erase_page(0) != 0) return -1;
    if (erase_page(1) != 0) return -2;
    if (write_page_status(0, PAGE_STATUS_VALID) != 0) return -3;
    return 0;
}

/* ========================= 原有日志功能（保持接口与语义） ========================= */

/* 写一条日志到 RAM + EEPROM（和你原来逻辑一致） */
void LogEvent_EEPROM(LogEventArray event, UINT32 *Time_S_Cnt)
{
    UINT16 temp = 0;

    if (BMS_LOG_POINT >= EVENT_RECORD_LENGTH)
        BMS_LOG_POINT = 0;
    BMS_LOG_RECORD[BMS_LOG_POINT][0] = (UINT8)event;
    BMS_LOG_RECORD[BMS_LOG_POINT][1] = LogTime_Map(Time_S_Cnt);
    if (event == BMS_START_UP)
        BMS_LOG_RECORD[BMS_LOG_POINT][1] = 0;
    ++BMS_LOG_POINT;

    temp = (UINT16)(BMS_LOG_RECORD[BMS_LOG_POINT - 1][0] + (BMS_LOG_RECORD[BMS_LOG_POINT - 1][1] << 8));

    /* 保存到仿 EEPROM（兼容原调用） */
    WriteEEPROM_Word_NoZone(E2P_ADDR_START_EVENT_RECORD + ((BMS_LOG_POINT - 1) << 1), temp);
    WriteEEPROM_Word_NoZone(E2P_ADDR_E2POS_EVENT_POINT, BMS_LOG_POINT);
}

/* 日志记录器：调用上层的逻辑（保留原有分支实现） */
void LogEvent_Record(UINT8 temp, LogEventArray event, UINT32 *Time_S_Cnt)
{
    static UINT8 su8_Event[EVENT_NUM] = {0};
    static UINT8 su8_CBC_Temp = 0;

    if (BMS_START_UP == event)
    {
        if (LogRecord_Flag.bits.Log_StartUp)
        { /* 直接使用，直接修改，别的不能自己修改 */
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
            Sleep_Mode.bits.b1_ToSleepFlag = 0; /* 释放，进入休眠 */
        }
    }
    else if (CBC_ERR == event)
    { /* 因为 CBC 不会被清除，只会一直叠加上去，所以用这个记录 */
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
            {
                su8_Event[event] = 0;
            }
            break;

        default:
            break;
        }
    }
}

/* 应用层周期调用（保留原实现） */
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
    temp = (UINT8)(System_ERROR_UserCallback(ERROR_STATUS_EEPROM_STORE) + System_ERROR_UserCallback(ERROR_STATUS_EEPROM_COM));
    LogEvent_Record(temp, EEPROM_ERR, &su32_Interval_S_Tcnt);

    LogEvent_Record(System_ERROR_UserCallback(ERROR_STATUS_CBC_DSG), CBC_ERR, &su32_Interval_S_Tcnt);
}

/* 读取寄存器响应：按你原实现把最近的 EVENT_RECORD_LENGTH 条以倒序填充到 t_u8BuffTemp[] */
void Sci_ACK_0x03_ReadRegs_EventRecord(UINT8 t_u8BuffTemp[])
{
    UINT16 i, j;
    INT8 k;

    i = 0; /* 初始化缓冲索引（你原代码里也加了这句） */

    for (j = 0; j < EVENT_RECORD_LENGTH; j++)
    {
        k = (INT8)BMS_LOG_POINT - 1 - (INT8)j;
        if (k < 0)
        {
            k = EVENT_RECORD_LENGTH + k;
        }
        t_u8BuffTemp[i++] = BMS_LOG_RECORD[k][0];
        t_u8BuffTemp[i++] = BMS_LOG_RECORD[k][1];
    }
}

/* 通过串口写寄存器（重置日志），保持原逻辑：若参数为 0x0001 则清 RAM 日志并设置 gu8_Reset_EventRecord */
void Sci_WrReg_0x06_Reset_EventRecord(struct RS485MSG *s)
{
    UINT8 i;

    UINT16 u16SciRegData = s->u16Buffer[5] + (s->u16Buffer[4] << 8);
    if (0x0001 == u16SciRegData)
    {
        /* 清 RAM */
        for (i = 0; i < EVENT_RECORD_LENGTH; ++i)
        {
            BMS_LOG_RECORD[i][0] = 0;
            BMS_LOG_RECORD[i][1] = 0;
        }
        BMS_LOG_POINT = 0;

        /* 将清除操作写回 EEPROM/Flash（异步或延后写入也可，但为兼容原逻辑这里直接写） */
        EEPROM_ResetData_EventRecord_ToDefault();

        gu8_Reset_EventRecord = EVENT_RECORD_LENGTH;
    }
    else
    {
        s->AckType = RS485_ACK_NEG;
        s->ErrorType = RS485_ERROR_DATA_INVALID;
    }
}

/* 将日志数据和指针恢复为默认（写回 flash） */
void EEPROM_ResetData_EventRecord_ToDefault(void)
{
    UINT8 i;

    for (i = 0; i < EVENT_RECORD_LENGTH; ++i)
    {
        BMS_LOG_RECORD[i][0] = 0;
        BMS_LOG_RECORD[i][1] = 0;
    }
    BMS_LOG_POINT = 0;

    /* 将每条记录写为 0（原来是写半字），并写指针为 0 */
    for (i = 0; i < EVENT_RECORD_LENGTH; ++i)
    {
        WriteEEPROM_Word_NoZone(E2P_ADDR_START_EVENT_RECORD + (i << 1), 0);
    }
    WriteEEPROM_Word_NoZone(E2P_ADDR_E2POS_EVENT_POINT, BMS_LOG_POINT);
}

/* 启动/上电读回 EEPROM EventRecord 数据并校验（保持你原来的校验逻辑） */
void ReadEEPROM_EventRecord_Parameters(void)
{
    UINT8 i;
    UINT16 t_u16RdTemp;

    /* 读取指针（偏移地址或绝对地址均被 normalize） */
    BMS_LOG_POINT = (UINT8)ReadEEPROM_Word_NoZone(E2P_ADDR_E2POS_EVENT_POINT);
    if (BMS_LOG_POINT >= 101)
    { /* 如果指针出问题，全部 Reset */
        System_ERROR_UserCallback(ERROR_EEPROM_STORE);
        EEPROM_ResetData_EventRecord_ToDefault();
    }

    for (i = 0; i < EVENT_RECORD_LENGTH; ++i)
    {
        t_u16RdTemp = ReadEEPROM_Word_NoZone(E2P_ADDR_START_EVENT_RECORD + (i << 1));
        /* 校验：低 8 bit 为事件编号，高 8 bit 为时间映射（<=171），并且事件编号在 EVENT_NUM 范围内 */
        if (((t_u16RdTemp & 0x00FF) <= EVENT_NUM) && ((t_u16RdTemp >> 8) <= 171))
        {
            BMS_LOG_RECORD[i][0] = (UINT8)(t_u16RdTemp & 0x00FF);
            BMS_LOG_RECORD[i][1] = (UINT8)(t_u16RdTemp >> 8);
        }
        else
        {
            if (0 == System_ErrFlag.u8ErrFlag_Com_EEPROM)
            {
                System_ERROR_UserCallback(ERROR_EEPROM_STORE);
            }
            BMS_LOG_RECORD[i][0] = 0;
            BMS_LOG_RECORD[i][1] = 0;
            /* 出现错误，日志可以尝试恢复为默认值 */
            WriteEEPROM_Word_NoZone(E2P_ADDR_START_EVENT_RECORD + (i << 1), 0);
        }
    }
}
