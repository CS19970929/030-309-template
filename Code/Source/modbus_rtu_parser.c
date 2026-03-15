#include "modbus_rtu_parser.h"

void ModbusRtuParser_Reset(ModbusRtuParser *parser)
{
    parser->length = 0;
    parser->expected_length = 0;
}

ProtocolParseResult ModbusRtuParser_ConsumeByte(ModbusRtuParser *parser, uint8_t byte)
{
    if (parser->length >= RS485_MAX_BUFFER_SIZE)
    {
        ModbusRtuParser_Reset(parser);
        return PROTO_PARSE_FRAME_INVALID;
    }

    parser->buffer[parser->length++] = byte;

    if (parser->length == 1)
    {
        if ((byte != RS485_SLAVE_ADDR) && (byte != RS485_BROADCAST_ADDR))
        {
            ModbusRtuParser_Reset(parser);
            return PROTO_PARSE_FRAME_INVALID;
        }
        return PROTO_PARSE_IN_PROGRESS;
    }

    if (parser->length == 2)
    {
        switch (parser->buffer[1])
        {
        case RS485_CMD_READ_REGS:
        case RS485_CMD_WRITE_REG:
            parser->expected_length = 8;
            break;
        case RS485_CMD_WRITE_REGS:
            parser->expected_length = 0;
            break;
        default:
            ModbusRtuParser_Reset(parser);
            return PROTO_PARSE_FRAME_INVALID;
        }
    }

    if ((parser->buffer[1] == RS485_CMD_WRITE_REGS) && (parser->length == 7))
    {
        parser->expected_length = (uint16_t)(9 + parser->buffer[6]);
    }

    if ((parser->expected_length > 0) && (parser->length == parser->expected_length))
    {
        return PROTO_PARSE_FRAME_READY;
    }

    if ((parser->expected_length > 0) && (parser->length > parser->expected_length))
    {
        ModbusRtuParser_Reset(parser);
        return PROTO_PARSE_FRAME_INVALID;
    }

    return PROTO_PARSE_IN_PROGRESS;
}
