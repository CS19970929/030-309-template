#ifndef FLASH_EEPROM_H
#define FLASH_EEPROM_H

#include "stm32f0xx.h"
#include <stdint.h>

/*
  配置（务必根据目标芯片/闪存布局调整）：
  - EEPROM_FLASH_BASE: 模拟 EEPROM 区域起始地址（必须页对齐）
  - EEPROM_FLASH_PAGE_SIZE: 选定页大小（字节），例如 1024 / 2048 等，取决于芯片型号
    注意：实现使用两个连续页（Page0 和 Page1），所以请确保 EEPROM_FLASH_BASE + 2*EEPROM_FLASH_PAGE_SIZE
    在 flash 可用地址范围内。
*/
#ifndef EEPROM_FLASH_BASE
/* 默认示例：请根据你的芯片修改地址为 flash 尾部可用页 */
#define EEPROM_FLASH_BASE    ((uint32_t)0x0800F800)
#endif

#ifndef EEPROM_FLASH_PAGE_SIZE
#define EEPROM_FLASH_PAGE_SIZE   ((uint32_t)1024) /* 请按芯片调整 */
#endif

/* 以半字（16-bit）为基本单元的容量 */
#define EEPROM_WORD_COUNT   ((EEPROM_FLASH_PAGE_SIZE) / 2U)

/* 初始化（系统启动时调用一次） */
void FlashEEPROM_Init(void);

/* 与原接口完全兼容（参数为 flash 区的字节地址） */
uint16_t ReadEEPROM_Word_NoZone(uint32_t u32ByteAddr);
void WriteEEPROM_Word_NoZone(uint32_t u32ByteAddr, uint16_t u16Data);

/* 额外的工具接口（可选） */
int FlashEEPROM_Format(void); /* 清空两页并初始化（危险，谨慎使用） */

#endif /* FLASH_EEPROM_H */

