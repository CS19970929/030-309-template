#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "modbus_rtu_parser.h"

static int run_case(const char *name, const UINT8 *data, UINT16 len, ProtocolParseResult expected)
{
    ModbusRtuParser parser;
    ProtocolParseResult result;
    UINT16 i;

    ModbusRtuParser_Reset(&parser);
    result = PROTO_PARSE_IN_PROGRESS;

    for (i = 0; i < len; ++i)
    {
        result = ModbusRtuParser_ConsumeByte(&parser, data[i]);
        if (result == PROTO_PARSE_FRAME_INVALID)
        {
            break;
        }
    }

    if (result != expected)
    {
        fprintf(stderr, "[FAIL] %s: expected %d, got %d\n", name, expected, result);
        return 1;
    }

    printf("[PASS] %s\n", name);
    return 0;
}

int main(void)
{
    int failed = 0;

    const UINT8 read_regs[] = {0x01, 0x03, 0x00, 0x10, 0x00, 0x02, 0x44, 0x09};
    const UINT8 write_reg[] = {0x01, 0x06, 0x00, 0x05, 0x12, 0x34, 0x95, 0x78};
    const UINT8 write_regs[] = {0x01, 0x10, 0x00, 0x20, 0x00, 0x02, 0x04, 0x00, 0x01, 0x00, 0x02, 0x30, 0x39};
    const UINT8 invalid_addr[] = {0x02, 0x03, 0x00, 0x10, 0x00, 0x02, 0x44, 0x09};
    const UINT8 invalid_fc[] = {0x01, 0x99, 0x00, 0x10};

    failed |= run_case("read holding registers", read_regs, (UINT16)sizeof(read_regs), PROTO_PARSE_FRAME_READY);
    failed |= run_case("write single register", write_reg, (UINT16)sizeof(write_reg), PROTO_PARSE_FRAME_READY);
    failed |= run_case("write multiple registers", write_regs, (UINT16)sizeof(write_regs), PROTO_PARSE_FRAME_READY);
    failed |= run_case("invalid slave address", invalid_addr, (UINT16)sizeof(invalid_addr), PROTO_PARSE_FRAME_INVALID);
    failed |= run_case("invalid function code", invalid_fc, (UINT16)sizeof(invalid_fc), PROTO_PARSE_FRAME_INVALID);

    if (failed != 0)
    {
        return EXIT_FAILURE;
    }

    printf("All host-side Modbus parser checks passed.\n");
    return EXIT_SUCCESS;
}
