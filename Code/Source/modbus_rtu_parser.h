#ifndef MODBUS_RTU_PARSER_H
#define MODBUS_RTU_PARSER_H

#include "stm32f0xx.h"
#include "Sci_Upper.h"

typedef enum {
    PROTO_PARSE_IN_PROGRESS = 0,
    PROTO_PARSE_FRAME_READY,
    PROTO_PARSE_FRAME_INVALID
} ProtocolParseResult;

typedef struct {
    uint8_t buffer[RS485_MAX_BUFFER_SIZE];
    uint16_t length;
    uint16_t expected_length;
} ModbusRtuParser;

void ModbusRtuParser_Reset(ModbusRtuParser *parser);
ProtocolParseResult ModbusRtuParser_ConsumeByte(ModbusRtuParser *parser, uint8_t byte);

#endif
