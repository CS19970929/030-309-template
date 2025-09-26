#ifndef __LOGRECORD_H
#define __LOGRECORD_H

#include "stm32f0xx.h"
#include <stdint.h>

/* 日志数量上限 */
#define BMS_LOG_POINT       100
#define BMS_LOG_INDEX_NULL  0xFFFF

typedef struct
{
    uint16_t u16Index;
    uint16_t u16Event;
} BMS_LOG_ITEM;

/* API：应用层调用保持不变 */
void LogRecord_Init(void);
void LogRecord_AddEvent(uint16_t event);
BMS_LOG_ITEM LogRecord_Read(uint16_t index);
uint16_t LogRecord_GetLastIndex(void);

/* 提供的 EEPROM 接口（实际由 Flash 模拟实现） */
uint16_t ReadEEPROM_Word_NoZone(uint32_t u32ByteAddr);
void WriteEEPROM_Word_NoZone(uint32_t u32ByteAddr, uint16_t u16Data);

#endif /* __LOGRECORD_H */
