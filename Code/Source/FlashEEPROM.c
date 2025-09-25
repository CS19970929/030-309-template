#include "FlashEEPROM.h"
#include "stm32f0xx_flash.h"
#include <string.h>

/*
  实现说明（简短）：
  - 使用两页（PAGE0 在 EEPROM_FLASH_BASE，PAGE1 在 EEPROM_FLASH_BASE + PAGE_SIZE）
  - 每页开头一个状态半字（16-bit）：
      0xFFFF = ERASED
      0xEEEE = RECEIVE (正在接收搬移数据)
      0x0000 = VALID (活动页)
  - 记录格式（追加写入，每条记录 3 * 半字 = 6 字节）：
      [addr (16-bit)][data (16-bit)][crc (16-bit)]
    crc = addr ^ data (简单异或校验)
  - 读：从活动页末尾向前搜索第一个校验通过且地址匹配的记录
  - 写：追加记录；若页空间不足则触发 page transfer（搬移）然后在新活动页追加
*/

/* page 地址 */
#define PAGE0_BASE (EEPROM_FLASH_BASE)
#define PAGE1_BASE (EEPROM_FLASH_BASE + EEPROM_FLASH_PAGE_SIZE)

/* page status values */
#define PAGE_STATUS_ERASED ((uint16_t)0xFFFF)
#define PAGE_STATUS_RECEIVE ((uint16_t)0xEEEE)
#define PAGE_STATUS_VALID ((uint16_t)0x0000)

/* record 定义：3 半字（addr, data, crc）*/
#define RECORD_HALFWORDS 3U
#define RECORD_BYTES (RECORD_HALFWORDS * 2U)

/* page 可用用于记录的最大半字数（减去首状态半字） */
#define PAGE_HALFWORDS_TOTAL (EEPROM_FLASH_PAGE_SIZE / 2U)
#define PAGE_DATA_HALFWORDS (PAGE_HALFWORDS_TOTAL - 1U) /* 第一半字给 page 状态 */

/* 记录容量（每页能存多少条） */
#define PAGE_RECORD_CAPACITY (PAGE_DATA_HALFWORDS / RECORD_HALFWORDS)

/* 内部 helper */
static inline uint32_t page_base_addr(uint8_t page_index)
{
    return (page_index == 0) ? PAGE0_BASE : PAGE1_BASE;
}

/* 计算记录 CRC（简单 xor，快速） */
static inline uint16_t rec_crc(uint16_t addr, uint16_t data)
{
    return (uint16_t)(addr ^ data);
}

/* 读 page 状态 */
static uint16_t read_page_status(uint8_t page_index)
{
    uint16_t *p = (uint16_t *)page_base_addr(page_index);
    return p[0];
}

/* 将 page 状态写为某值（仅用在初始化/搬移流程里） */
static int write_page_status(uint8_t page_index, uint16_t status)
{
    uint32_t addr = page_base_addr(page_index);
    FLASH_Unlock();
    FLASH_Status st = FLASH_ProgramHalfWord(addr, status);
    FLASH_Lock();
    return (st == FLASH_COMPLETE) ? 0 : -1;
}

/* 查找活动页（0 或 1）。如果两页都 ERASED，则格式化 page0 为 VALID 并返回 0。*/
static int find_active_page(void)
{
    uint16_t s0 = read_page_status(0);
    uint16_t s1 = read_page_status(1);

    if (s0 == PAGE_STATUS_VALID && s1 != PAGE_STATUS_VALID)
        return 0;
    if (s1 == PAGE_STATUS_VALID && s0 != PAGE_STATUS_VALID)
        return 1;

    /* 若存在 RECEIVE 的页，需处理完成 */
    if (s0 == PAGE_STATUS_RECEIVE && s1 == PAGE_STATUS_VALID)
    {
        /* 完成接收：将 page0 标记为 VALID（理论上搬移已完成） */
        write_page_status(0, PAGE_STATUS_VALID);
        return 0;
    }
    if (s1 == PAGE_STATUS_RECEIVE && s0 == PAGE_STATUS_VALID)
    {
        write_page_status(1, PAGE_STATUS_VALID);
        return 1;
    }

    /* 两页都 ERASED 或不明情况 -> 格式化 page0 为 VALID */
    if (s0 == PAGE_STATUS_ERASED && s1 == PAGE_STATUS_ERASED)
    {
        /* write page0 status to VALID (must program halfword) */
        if (write_page_status(0, PAGE_STATUS_VALID) != 0)
        {
            /* 若写 status 失败，尽量返回 page0 */
            return 0;
        }
        return 0;
    }

    /* 兜底：如果两页都有 VALID（异常），选记录更多的那一页为活动页 */
    if (s0 == PAGE_STATUS_VALID && s1 == PAGE_STATUS_VALID)
    {
        /* 比较有效记录数，返回较多的一页 */
        uint32_t count0 = 0, count1 = 0;
        /* count records by scanning */
        uint16_t *p0 = (uint16_t *)page_base_addr(0);
        uint16_t *p1 = (uint16_t *)page_base_addr(1);
        /* scan p0 */
        for (uint32_t i = 1; i + 2 < PAGE_HALFWORDS_TOTAL; i += RECORD_HALFWORDS)
        {
            if (p0[i] == 0xFFFF && p0[i + 1] == 0xFFFF && p0[i + 2] == 0xFFFF)
                break;
            count0++;
        }
        for (uint32_t i = 1; i + 2 < PAGE_HALFWORDS_TOTAL; i += RECORD_HALFWORDS)
        {
            if (p1[i] == 0xFFFF && p1[i + 1] == 0xFFFF && p1[i + 2] == 0xFFFF)
                break;
            count1++;
        }
        return (count0 >= count1) ? 0 : 1;
    }

    /* 其他复杂情况，优先选 VALID 的页，否则 page0 */
    if (s0 == PAGE_STATUS_VALID)
        return 0;
    if (s1 == PAGE_STATUS_VALID)
        return 1;
    return 0;
}

/* 在 page 中查找第一个空闲记录位置（用于追加写） - 返回 index of record (0..PAGE_RECORD_CAPACITY-1)
   若满则返回 -1 */
static int find_first_free_record_index(uint8_t page_index)
{
    uint16_t *p = (uint16_t *)page_base_addr(page_index);
    /* 数据区从半字索引 1 开始（因为 0 用作 status） */
    uint32_t base = 1;
    uint32_t end = PAGE_HALFWORDS_TOTAL;
    for (uint32_t hw = base; hw + 2 < end; hw += RECORD_HALFWORDS)
    {
        /* 如果三半字都为 0xFFFF，说明空闲（未写） */
        if (p[hw] == 0xFFFF && p[hw + 1] == 0xFFFF && p[hw + 2] == 0xFFFF)
        {
            return (int)((hw - base) / RECORD_HALFWORDS);
        }
    }
    return -1;
}

/* 计算页中某条记录的半字起始地址（绝对地址） */
static uint32_t record_addr_by_index(uint8_t page_index, uint32_t rec_index)
{
    uint32_t base_hw = 1 + rec_index * RECORD_HALFWORDS; /* 半字索引 */
    return page_base_addr(page_index) + (base_hw * 2U);
}

/* 从活动页末尾向前搜索某地址的最新记录。找到返回 0 并把值放到 *out_data；否则返回 -1 */
static int search_latest_in_page(uint8_t page_index, uint16_t vaddr, uint16_t *out_data)
{
    uint16_t *p = (uint16_t *)page_base_addr(page_index);
    uint32_t base = 1;
    uint32_t end = PAGE_HALFWORDS_TOTAL;

    /* 从最后写入位置向前扫描。找到第一个校验通过并地址匹配的记录即返回 */
    for (int hw = (int)(end - RECORD_HALFWORDS); hw >= (int)base; hw -= RECORD_HALFWORDS)
    {
        uint16_t a = p[hw];
        uint16_t d = p[hw + 1];
        uint16_t c = p[hw + 2];
        if (a == 0xFFFF && d == 0xFFFF && c == 0xFFFF)
        {
            /* 空位，继续向前 */
            continue;
        }
        if (rec_crc(a, d) == c && a == vaddr)
        {
            *out_data = d;
            return 0;
        }
    }
    return -1;
}

/* 读取最后已写的 value（先在 active page 查找，再在另一个页查找） */
static int read_latest(uint16_t vaddr, uint16_t *out_data)
{
    int active = find_active_page();
    if (search_latest_in_page((uint8_t)active, vaddr, out_data) == 0)
        return 0;
    int other = active ^ 1;
    if (search_latest_in_page((uint8_t)other, vaddr, out_data) == 0)
        return 0;
    return -1;
}

/* 擦除指定 page（调用 FLASH_ErasePage） */
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

/* 将 page_index_from 的最新数据搬移到 page_index_to（page_to 必须已被擦空）：
   步骤：
   - 在 page_to 写入 PAGE_STATUS_RECEIVE
   - 收集 page_from 中每个虚拟地址的最新值（RAM 临时表）
   - 在 page_to 依序追加写入这些最新值（按 vaddr 升序以便 deterministic）
   - 将 page_to 状态写为 PAGE_STATUS_VALID
   - 擦除 page_from
*/
static int page_transfer(uint8_t page_from, uint8_t page_to)
{
    /* 构建 RAM 映射表，范围为 0 .. (PAGE_RECORD_CAPACITY * ?)。
       为简单起见，允许虚地址空间为 0 .. (EEPROM_WORD_COUNT-1)（也就是和页容量相关）
       这保证内存开销可控（EEPROM_WORD_COUNT 通常不大）。 */
    uint32_t max_addr_space = EEPROM_WORD_COUNT; /* 一般 <= 512 */
    uint16_t *map_values = (uint16_t *)malloc(sizeof(uint16_t) * max_addr_space);
    uint8_t *map_valid = (uint8_t *)malloc(sizeof(uint8_t) * max_addr_space);
    if (!map_values || !map_valid)
    {
        if (map_values)
            free(map_values);
        if (map_valid)
            free(map_valid);
        return -1;
    }
    memset(map_valid, 0, max_addr_space);
    /* 扫描 page_from，从头到尾覆盖，记住最新值（后写的覆盖前写的） */
    uint16_t *p = (uint16_t *)page_base_addr(page_from);
    uint32_t base = 1;
    uint32_t end = PAGE_HALFWORDS_TOTAL;
    for (uint32_t hw = base; hw + 2 < end; hw += RECORD_HALFWORDS)
    {
        uint16_t a = p[hw];
        uint16_t d = p[hw + 1];
        uint16_t c = p[hw + 2];
        if (a == 0xFFFF && d == 0xFFFF && c == 0xFFFF)
            continue;
        if (rec_crc(a, d) == c)
        {
            /* 合法记录，记录到 map */
            if (a < max_addr_space)
            {
                map_values[a] = d;
                map_valid[a] = 1;
            }
        }
    }

    /* 准备写入 page_to：先设为 RECEIVE */
    if (write_page_status(page_to, PAGE_STATUS_RECEIVE) != 0)
    {
        free(map_values);
        free(map_valid);
        return -2;
    }

    /* 将 map 中所有有效项按地址升序写入 page_to */
    /* 找出第一个可写位置（应为 index 0） */
    int rec_index = 0;
    for (uint32_t a = 0; a < max_addr_space; ++a)
    {
        if (!map_valid[a])
            continue;
        if (rec_index >= (int)PAGE_RECORD_CAPACITY)
        {
            /* 空间不够（理论上不应该），中止并回滚 */
            free(map_values);
            free(map_valid);
            return -3;
        }
        uint32_t addr_word = record_addr_by_index(page_to, rec_index);
        /* 依次编程三个半字：addr, data, crc */
        __disable_irq();
        FLASH_Unlock();
        FLASH_Status st;
        st = FLASH_ProgramHalfWord(addr_word, (uint16_t)a);
        if (st != FLASH_COMPLETE)
        {
            FLASH_Lock();
            __enable_irq();
            free(map_values);
            free(map_valid);
            return -4;
        }
        st = FLASH_ProgramHalfWord(addr_word + 2, map_values[a]);
        if (st != FLASH_COMPLETE)
        {
            FLASH_Lock();
            __enable_irq();
            free(map_values);
            free(map_valid);
            return -5;
        }
        st = FLASH_ProgramHalfWord(addr_word + 4, rec_crc((uint16_t)a, map_values[a]));
        FLASH_Lock();
        __enable_irq();
        if (st != FLASH_COMPLETE)
        {
            free(map_values);
            free(map_valid);
            return -6;
        }
        rec_index++;
    }

    /* 写完后把 page_to 状态改为 VALID */
    if (write_page_status(page_to, PAGE_STATUS_VALID) != 0)
    {
        free(map_values);
        free(map_valid);
        return -7;
    }

    /* 擦除旧页 */
    if (erase_page(page_from) != 0)
    {
        free(map_values);
        free(map_valid);
        return -8;
    }

    free(map_values);
    free(map_valid);
    return 0;
}

/* 在活动页追加一条记录（addr, data） - 若空间不足返回 -1 */
static int append_record_to_active(uint16_t vaddr, uint16_t data)
{
    int active = find_active_page();
    int free_idx = find_first_free_record_index((uint8_t)active);
    if (free_idx >= 0)
    {
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
        if (st != FLASH_COMPLETE)
            return -4;
        return 0;
    }
    else
    {
        return -1; /* full */
    }
}

/* 公共 API 实现 */

/* 初始化：确保两页有一个 VALID，处理异常状态 */
void FlashEEPROM_Init(void)
{
    /* 简单调用 find_active_page 以修正状态（find_active_page 内部会在两页都 ERASED 时把 page0 置为 VALID） */
    (void)find_active_page();
}

/* 读取：参数为绝对字节地址（与之前 EEPROM 地址兼容） */
uint16_t ReadEEPROM_Word_NoZone(uint32_t u32ByteAddr)
{
    /* 将 u32ByteAddr 映射到虚拟地址 vaddr（以半字为单位，从 0 开始计数） */
    if (u32ByteAddr < EEPROM_FLASH_BASE)
    {
        /* 也允许调用者传入以 page-relative 0 开始的偏移（常见），如果是这种情况，会错位 */
        /* 我这里直接把超出范围的返回 0xFFFF */
        return 0xFFFF;
    }
    uint32_t off = u32ByteAddr - EEPROM_FLASH_BASE;
    if (off & 1U)
        return 0xFFFF;         /* 非半字对齐 */
    uint32_t vaddr = off / 2U; /* 虚拟地址（16-bit 索引） */
    if (vaddr >= EEPROM_WORD_COUNT)
        return 0xFFFF;

    uint16_t val;
    if (read_latest((uint16_t)vaddr, &val) == 0)
        return val;
    return 0xFFFF;
}

/* 写入：参数为绝对字节地址（与之前 EEPROM 地址兼容） */
void WriteEEPROM_Word_NoZone(uint32_t u32ByteAddr, uint16_t u16Data)
{
    if (u32ByteAddr < EEPROM_FLASH_BASE)
        return;
    uint32_t off = u32ByteAddr - EEPROM_FLASH_BASE;
    if (off & 1U)
        return;
    uint32_t vaddr = off / 2U;
    if (vaddr >= EEPROM_WORD_COUNT)
        return;

    /* 先读当前值，若相同则跳过以减少写次数 */
    uint16_t cur = ReadEEPROM_Word_NoZone(u32ByteAddr);
    if (cur == u16Data)
        return;

    /* 尝试追加记录到活动页 */
    int ret = append_record_to_active((uint16_t)vaddr, u16Data);
    if (ret == 0)
        return;

    /* 若空间不足（ret < 0），触发 page transfer：把活动页搬到另一页 */
    int active = find_active_page();
    int other = active ^ 1;
    /* 擦除目标页以保证干净 */
    if (erase_page((uint8_t)other) != 0)
    {
        /* 擦除失败，直接返回（不能保证写入） */
        return;
    }
    /* page transfer */
    if (page_transfer((uint8_t)active, (uint8_t)other) != 0)
    {
        /* 迁移失败，返回 */
        return;
    }
    /* 迁移完成后，active 页变为 other */
    /* 现在再追加（此时应有空位） */
    (void)append_record_to_active((uint16_t)vaddr, u16Data);
}

/* 格式化：擦除两页并初始化 page0 为 VALID */
int FlashEEPROM_Format(void)
{
    if (erase_page(0) != 0)
        return -1;
    if (erase_page(1) != 0)
        return -2;
    if (write_page_status(0, PAGE_STATUS_VALID) != 0)
        return -3;
    return 0;
}
