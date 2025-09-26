#include "LogRecord.h"
#include "stm32f0xx_flash.h"
#include <string.h>

/* ========================= Flash EEPROM 配置 ========================= */

/* 注意：必须根据芯片 flash 容量修改此地址，确保这两页没被代码占用 */
#define EEPROM_FLASH_BASE ((uint32_t)0x0800F800) /* Page0 起始地址 */
#define EEPROM_FLASH_PAGE_SIZE ((uint32_t)1024)  /* 每页大小 (字节) */

#define PAGE0_BASE (EEPROM_FLASH_BASE)
#define PAGE1_BASE (EEPROM_FLASH_BASE + EEPROM_FLASH_PAGE_SIZE)

/* Page 状态标志 */
#define PAGE_STATUS_ERASED 0xFFFF
#define PAGE_STATUS_RECEIVE 0xEEEE
#define PAGE_STATUS_VALID 0x0000

/* 单条记录结构: 3 半字 (地址, 数据, CRC) */
#define RECORD_HALFWORDS 3
#define PAGE_HALFWORDS_TOTAL (EEPROM_FLASH_PAGE_SIZE / 2)
#define PAGE_DATA_HALFWORDS (PAGE_HALFWORDS_TOTAL - 1)
#define PAGE_RECORD_CAPACITY (PAGE_DATA_HALFWORDS / RECORD_HALFWORDS)

/* E2P 地址映射（保持和原文件一致） */
#define E2P_ADDR_LAST_INDEX (EEPROM_FLASH_BASE + 0x0004)
#define E2P_ADDR_START_EVENT_RECORD (EEPROM_FLASH_BASE + 0x0100)

/* ========================= Flash 底层工具函数 ========================= */

static inline uint16_t rec_crc(uint16_t addr, uint16_t data)
{
    return addr ^ data;
}

static inline uint32_t page_base_addr(uint8_t page_index)
{
    return (page_index == 0) ? PAGE0_BASE : PAGE1_BASE;
}

static uint16_t read_page_status(uint8_t page_index)
{
    return *(uint16_t *)page_base_addr(page_index);
}

static int write_page_status(uint8_t page_index, uint16_t status)
{
    uint32_t addr = page_base_addr(page_index);
    FLASH_Unlock();
    FLASH_Status st = FLASH_ProgramHalfWord(addr, status);
    FLASH_Lock();
    return (st == FLASH_COMPLETE) ? 0 : -1;
}

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

/* 找到当前活动页 */
static int find_active_page(void)
{
    uint16_t s0 = read_page_status(0);
    uint16_t s1 = read_page_status(1);

    if (s0 == PAGE_STATUS_VALID && s1 != PAGE_STATUS_VALID)
        return 0;
    if (s1 == PAGE_STATUS_VALID && s0 != PAGE_STATUS_VALID)
        return 1;

    if (s0 == PAGE_STATUS_ERASED && s1 == PAGE_STATUS_ERASED)
    {
        erase_page(0);
        write_page_status(0, PAGE_STATUS_VALID);
        return 0;
    }
    return (s0 == PAGE_STATUS_VALID) ? 0 : 1;
}

static int find_first_free_record_index(uint8_t page_index)
{
    uint16_t *p = (uint16_t *)page_base_addr(page_index);
    for (uint32_t hw = 1; hw + 2 < PAGE_HALFWORDS_TOTAL; hw += RECORD_HALFWORDS)
    {
        if (p[hw] == 0xFFFF && p[hw + 1] == 0xFFFF && p[hw + 2] == 0xFFFF)
        {
            return (int)((hw - 1) / RECORD_HALFWORDS);
        }
    }
    return -1;
}

static uint32_t record_addr_by_index(uint8_t page_index, uint32_t rec_index)
{
    uint32_t base_hw = 1 + rec_index * RECORD_HALFWORDS;
    return page_base_addr(page_index) + (base_hw * 2U);
}

static int search_latest_in_page(uint8_t page_index, uint16_t vaddr, uint16_t *out_data)
{
    uint16_t *p = (uint16_t *)page_base_addr(page_index);
    for (int hw = PAGE_HALFWORDS_TOTAL - RECORD_HALFWORDS; hw >= 1; hw -= RECORD_HALFWORDS)
    {
        uint16_t a = p[hw];
        uint16_t d = p[hw + 1];
        uint16_t c = p[hw + 2];
        if (a == 0xFFFF && d == 0xFFFF && c == 0xFFFF)
            continue;
        if (rec_crc(a, d) == c && a == vaddr)
        {
            *out_data = d;
            return 0;
        }
    }
    return -1;
}

static int read_latest(uint16_t vaddr, uint16_t *out_data)
{
    int active = find_active_page();
    return search_latest_in_page(active, vaddr, out_data);
}

static int append_record_to_active(uint16_t vaddr, uint16_t data)
{
    int active = find_active_page();
    int free_idx = find_first_free_record_index((uint8_t)active);
    if (free_idx < 0)
        return -1;
    uint32_t addr = record_addr_by_index((uint8_t)active, (uint32_t)free_idx);
    __disable_irq();
    FLASH_Unlock();
    FLASH_Status st;
    st = FLASH_ProgramHalfWord(addr, vaddr);
    if (st != FLASH_COMPLETE)
    {
        FLASH_Lock();
        __enable_irq();
        return -2;
    }
    st = FLASH_ProgramHalfWord(addr + 2, data);
    if (st != FLASH_COMPLETE)
    {
        FLASH_Lock();
        __enable_irq();
        return -3;
    }
    st = FLASH_ProgramHalfWord(addr + 4, rec_crc(vaddr, data));
    FLASH_Lock();
    __enable_irq();
    return 0;
}

/* ========================= EEPROM 接口替代实现 ========================= */

uint16_t ReadEEPROM_Word_NoZone(uint32_t u32ByteAddr)
{
    uint32_t off = u32ByteAddr - EEPROM_FLASH_BASE;
    if (off & 1U)
        return 0xFFFF;
    uint16_t vaddr = off / 2U;
    uint16_t val;
    if (read_latest(vaddr, &val) == 0)
        return val;
    return 0xFFFF;
}

void WriteEEPROM_Word_NoZone(uint32_t u32ByteAddr, uint16_t u16Data)
{
    uint32_t off = u32ByteAddr - EEPROM_FLASH_BASE;
    if (off & 1U)
        return;
    uint16_t vaddr = off / 2U;
    uint16_t cur = ReadEEPROM_Word_NoZone(u32ByteAddr);
    if (cur == u16Data)
        return;
    if (append_record_to_active(vaddr, u16Data) == 0)
        return;

    /* 如果页满了，擦另一页并写入 */
    int active = find_active_page();
    int other = active ^ 1;
    erase_page(other);
    write_page_status(other, PAGE_STATUS_VALID);
    erase_page(active);
    append_record_to_active(vaddr, u16Data);
}

/* ========================= 日志逻辑（应用层接口保持不变） ========================= */

void LogRecord_Init(void)
{
    find_active_page();
    uint16_t idx = ReadEEPROM_Word_NoZone(E2P_ADDR_LAST_INDEX);
    if (idx == 0xFFFF)
    {
        WriteEEPROM_Word_NoZone(E2P_ADDR_LAST_INDEX, 0);
    }
}

void LogRecord_AddEvent(uint16_t event)
{
    uint16_t lastIdx = ReadEEPROM_Word_NoZone(E2P_ADDR_LAST_INDEX);
    if (lastIdx == 0xFFFF)
        lastIdx = 0;
    lastIdx++;
    if (lastIdx >= BMS_LOG_POINT)
        lastIdx = 0;
    uint32_t addr = E2P_ADDR_START_EVENT_RECORD + (lastIdx * 4);
    WriteEEPROM_Word_NoZone(addr, lastIdx);
    WriteEEPROM_Word_NoZone(addr + 2, event);
    WriteEEPROM_Word_NoZone(E2P_ADDR_LAST_INDEX, lastIdx);
}

BMS_LOG_ITEM LogRecord_Read(uint16_t index)
{
    BMS_LOG_ITEM item;
    item.u16Index = BMS_LOG_INDEX_NULL;
    item.u16Event = 0xFFFF;
    uint32_t addr = E2P_ADDR_START_EVENT_RECORD + (index * 4);
    item.u16Index = ReadEEPROM_Word_NoZone(addr);
    item.u16Event = ReadEEPROM_Word_NoZone(addr + 2);
    return item;
}

uint16_t LogRecord_GetLastIndex(void)
{
    uint16_t idx = ReadEEPROM_Word_NoZone(E2P_ADDR_LAST_INDEX);
    return (idx == 0xFFFF) ? 0 : idx;
}
