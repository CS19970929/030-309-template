#include "ascii_slave.h"

#include "bms_comm_data_adapter.h"

void AsciiParser_Reset(AsciiParser *parser)
{
    parser->length = 0;
    parser->expected_length = 0;
}

uint8_t Hex_To_Ascii(uint8_t hex)
{
    hex &= 0x0F;
    if (hex < 10)
    {
        return (uint8_t)(hex + '0');
    }
    return (uint8_t)(hex - 10 + 'A');
}

uint8_t Ascii_To_Hex(uint8_t ascii)
{
    if ((ascii >= '0') && (ascii <= '9'))
    {
        return (uint8_t)(ascii - '0');
    }
    if ((ascii >= 'A') && (ascii <= 'F'))
    {
        return (uint8_t)(ascii - 'A' + 10);
    }
    if ((ascii >= 'a') && (ascii <= 'f'))
    {
        return (uint8_t)(ascii - 'a' + 10);
    }
    return 0xFF;
}

uint8_t VerToHex(uint8_t *ver)
{
    uint8_t hex;
    uint8_t i;

    if (ver == NULL)
    {
        return 0;
    }

    hex = 0;
    for (i = 0; i < 2; ++i)
    {
        if ((ver[i] < '0') || (ver[i] > '9'))
        {
            return 0;
        }
        hex = (uint8_t)((hex * 16) + (ver[i] - '0'));
    }
    return hex;
}

uint8_t Calc_LCHKSUM(uint16_t lenid)
{
    uint8_t sum;

    sum = (uint8_t)(((lenid >> 8) & 0x0F) + ((lenid >> 4) & 0x0F) + (lenid & 0x0F));
    sum = (uint8_t)(sum % 16);
    return (uint8_t)((~sum + 1) & 0x0F);
}

uint16_t Calc_CHKSUM(uint8_t *data, uint16_t len)
{
    uint32_t sum;
    uint16_t i;

    sum = 0;
    for (i = 0; i < len; ++i)
    {
        sum += data[i];
    }
    sum %= 65536UL;
    return (uint16_t)(~sum + 1);
}

uint16_t Build_LENGTH_Field(uint16_t lenid)
{
    return (uint16_t)((Calc_LCHKSUM(lenid) << 12) | (lenid & 0x0FFF));
}

uint8_t Parse_LENGTH_Field(uint16_t length_field, uint16_t *lenid)
{
    uint8_t lchksum_rx;

    lchksum_rx = (uint8_t)((length_field >> 12) & 0x0F);
    *lenid = (uint16_t)(length_field & 0x0FFF);
    return (uint8_t)(lchksum_rx == Calc_LCHKSUM(*lenid));
}

static uint8_t Parse_Hex_Byte(const uint8_t *buf, uint16_t idx, uint8_t *value)
{
    uint8_t high;
    uint8_t low;

    high = Ascii_To_Hex(buf[idx]);
    low = Ascii_To_Hex(buf[idx + 1U]);
    if ((high == 0xFFU) || (low == 0xFFU))
    {
        return 0U;
    }

    *value = (uint8_t)((high << 4) | low);
    return 1U;
}

static uint8_t Parse_Hex_U16(const uint8_t *buf, uint16_t idx, uint16_t *value)
{
    uint8_t nibble0;
    uint8_t nibble1;
    uint8_t nibble2;
    uint8_t nibble3;

    nibble0 = Ascii_To_Hex(buf[idx]);
    nibble1 = Ascii_To_Hex(buf[idx + 1U]);
    nibble2 = Ascii_To_Hex(buf[idx + 2U]);
    nibble3 = Ascii_To_Hex(buf[idx + 3U]);
    if ((nibble0 == 0xFFU) || (nibble1 == 0xFFU) || (nibble2 == 0xFFU) || (nibble3 == 0xFFU))
    {
        return 0U;
    }

    *value = (uint16_t)((nibble0 << 12) | (nibble1 << 8) | (nibble2 << 4) | nibble3);
    return 1U;
}

uint16_t Build_Response_Frame(uint8_t *tx_buf, uint16_t tx_capacity, uint8_t ver, uint8_t adr, uint8_t rtn, const uint8_t *info_data, uint16_t info_hex_len)
{
    uint16_t idx;
    uint16_t lenid;
    uint16_t length_field;
    uint16_t chksum;
    uint16_t i;
    uint16_t frame_len;
    uint8_t value;

#define APPEND_HEX_BYTE(v)                                     \
    do                                                         \
    {                                                          \
        value = (uint8_t)(v);                                  \
        tx_buf[idx++] = Hex_To_Ascii((uint8_t)(value >> 4));   \
        tx_buf[idx++] = Hex_To_Ascii((uint8_t)(value & 0x0F)); \
    } while (0)

#define APPEND_HEX_U16(v)                                              \
    do                                                                 \
    {                                                                  \
        chksum = (uint16_t)(v);                                        \
        tx_buf[idx++] = Hex_To_Ascii((uint8_t)(chksum >> 12));         \
        tx_buf[idx++] = Hex_To_Ascii((uint8_t)((chksum >> 8) & 0x0F)); \
        tx_buf[idx++] = Hex_To_Ascii((uint8_t)((chksum >> 4) & 0x0F)); \
        tx_buf[idx++] = Hex_To_Ascii((uint8_t)(chksum & 0x0F));        \
    } while (0)

    frame_len = (uint16_t)(18U + (uint16_t)(info_hex_len * 2U));
    if ((tx_buf == NULL) || (frame_len > tx_capacity))
    {
        return 0;
    }

    idx = 0;
    tx_buf[idx++] = SOI;
    APPEND_HEX_BYTE(ver);
    APPEND_HEX_BYTE(adr);
    APPEND_HEX_BYTE(CID1_BAT_DATA);
    APPEND_HEX_BYTE(rtn);

    lenid = (uint16_t)(info_hex_len * 2);
    length_field = Build_LENGTH_Field(lenid);
    APPEND_HEX_U16(length_field);

    for (i = 0; i < info_hex_len; ++i)
    {
        APPEND_HEX_BYTE(info_data[i]);
    }

    chksum = Calc_CHKSUM(&tx_buf[1], (uint16_t)(idx - 1));
    APPEND_HEX_U16(chksum);
    tx_buf[idx++] = EOI;

#undef APPEND_HEX_U16
#undef APPEND_HEX_BYTE

    return idx;
}

static uint16_t Ascii_BuildBaseInfo(uint8_t *tx_buf)
{
    uint8_t info_buf[MAX_FRAME_LEN];
    uint16_t idx;

    idx = BmsComm_BuildBaseInfoPayload(info_buf, sizeof(info_buf));

    return Build_Response_Frame(tx_buf, MAX_FRAME_LEN, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

static uint16_t Ascii_BuildAnalogData(uint8_t *tx_buf)
{
    uint8_t info_buf[100];
    uint16_t idx;

    idx = BmsComm_BuildAnalogPayload(info_buf, sizeof(info_buf));

    return Build_Response_Frame(tx_buf, MAX_FRAME_LEN, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

extern uint16_t BmsComm_EncodePylonTemperature(int16_t temp_c_x10);

uint16_t Cmd_Handle_Analog2_Value(uint8_t *tx_buf, uint8_t cmd)
{
    // 校验Command与本机地址匹配
    if (cmd != SLAVE_ADDRESS)
    {
        return Build_Response_Frame(tx_buf, MAX_FRAME_LEN ,PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_ADR_ERROR, NULL, 0);
    }
    uint8_t info_buf[128] = {0};
    uint16_t idx = 0;
    // INFOFLAG固定值
    info_buf[idx++] = 0x00;
    // Command值
    info_buf[idx++] = cmd;
    // 电芯节数
    uint8_t cell_num = SNum;
    info_buf[idx++] = cell_num;
    // 电芯电压
    for (uint8_t i = 0; i < cell_num; i++)
    {
        info_buf[idx++] = (g_stCellInfoReport.u16VCell[i] >> 8) & 0xFF;
        info_buf[idx++] = g_stCellInfoReport.u16VCell[i] & 0xFF;
    }
    // 温度点数量
    uint8_t temp_num = 2;
    info_buf[idx++] = temp_num;
    // 温度值
    for (uint8_t i = 0; i < temp_num; i++)
    {
        uint16_t temp = BmsComm_EncodePylonTemperature((int16_t)g_stCellInfoReport.u16Temperature[i] - 400);
        ;
        info_buf[idx++] = (temp >> 8) & 0xFF;
        info_buf[idx++] = temp & 0xFF;
    }
    // 电流
    int16_t pack_current = (g_stCellInfoReport.u16Ichg > 0U) ? (int16_t)g_stCellInfoReport.u16Ichg : (int16_t)(-((int16_t)g_stCellInfoReport.u16IDischg));
    info_buf[idx++] = (pack_current >> 8) & 0xFF;
    info_buf[idx++] = pack_current & 0xFF;
    // 模块电压
    uint16_t vtotle = g_stCellInfoReport.u16VCellTotle * 10;
    info_buf[idx++] = (vtotle >> 8) & 0xFF;
    info_buf[idx++] = vtotle & 0xFF;
    // 剩余容量
    uint32_t cap_res = g_stCellInfoReport.SocElement.u16CapacityNow * 10;
    uint32_t cap_full = g_stCellInfoReport.SocElement.u16CapacityFactory * 10;
    if (cap_full <= 65000)
    {
        info_buf[idx++] = (cap_res >> 8) & 0xFF;
        info_buf[idx++] = cap_res & 0xFF;
        // 用户自定义个数
        info_buf[idx++] = 2;
        // 总容量
        info_buf[idx++] = (cap_full >> 8) & 0xFF;
        info_buf[idx++] = cap_full & 0xFF;
        // 循环次数
        info_buf[idx++] = (g_stCellInfoReport.SocElement.u16Cycle_times >> 8) & 0xFF;
        info_buf[idx++] = g_stCellInfoReport.SocElement.u16Cycle_times & 0xFF;
        // 满充容量
        info_buf[idx++] = 0xff;
        info_buf[idx++] = 0xff;
        info_buf[idx++] = 0xff;

        info_buf[idx++] = 0xff;
        info_buf[idx++] = 0xff;
        info_buf[idx++] = 0xff;
    }
    else
    {
        info_buf[idx++] = 0xff;
        info_buf[idx++] = 0xff;
        // 用户自定义个数
        info_buf[idx++] = 4;
        // 总容量
        info_buf[idx++] = 0xff;
        info_buf[idx++] = 0xff;
        // 循环次数
        info_buf[idx++] = (g_stCellInfoReport.SocElement.u16Cycle_times >> 8) & 0xFF;
        info_buf[idx++] = g_stCellInfoReport.SocElement.u16Cycle_times & 0xFF;
        // 满充容量
        info_buf[idx++] = (cap_res >> 16) & 0xff;
        info_buf[idx++] = (cap_res >> 8) & 0xff;
        info_buf[idx++] = cap_res & 0xff;

        info_buf[idx++] = (cap_full >> 16) & 0xff;
        info_buf[idx++] = (cap_full >> 8) & 0xff;
        info_buf[idx++] = cap_full & 0xff;
    }

    return Build_Response_Frame(tx_buf, MAX_FRAME_LEN, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

static uint16_t Ascii_BuildAlarmInfo(uint8_t *tx_buf)
{
    uint8_t info_buf[4];
    uint16_t idx;

    idx = BmsComm_BuildAlarmPayload(info_buf, sizeof(info_buf));

    return Build_Response_Frame(tx_buf, MAX_FRAME_LEN, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

static uint16_t Ascii_BuildChargeDisInfo(uint8_t *tx_buf)
{
    uint8_t info_buf[9];
    uint16_t idx;

    idx = BmsComm_BuildChargeDischargePayload(info_buf, sizeof(info_buf));

    return Build_Response_Frame(tx_buf, MAX_FRAME_LEN, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

ProtocolParseResult AsciiParser_ConsumeByte(AsciiParser *parser, uint8_t byte)
{
    uint16_t length_field;
    uint16_t lenid;

    if (parser->length >= ASCII_RX_FRAME_LEN)
    {
        AsciiParser_Reset(parser);
        return PROTO_PARSE_FRAME_INVALID;
    }

    if ((parser->length == 0) && (byte != SOI))
    {
        return PROTO_PARSE_FRAME_INVALID;
    }

    parser->buffer[parser->length++] = byte;

    if (parser->length < 13)
    {
        return PROTO_PARSE_IN_PROGRESS;
    }

    if (parser->expected_length == 0)
    {
        if (Parse_Hex_U16(parser->buffer, 9U, &length_field) == 0U)
        {
            AsciiParser_Reset(parser);
            return PROTO_PARSE_FRAME_INVALID;
        }
        if (Parse_LENGTH_Field(length_field, &lenid) == 0)
        {
            AsciiParser_Reset(parser);
            return PROTO_PARSE_FRAME_INVALID;
        }

        parser->expected_length = (uint16_t)(18 + lenid);
        if (parser->expected_length > ASCII_RX_FRAME_LEN)
        {
            AsciiParser_Reset(parser);
            return PROTO_PARSE_FRAME_INVALID;
        }
    }

    if (parser->length == parser->expected_length)
    {
        if (parser->buffer[parser->length - 1] != EOI)
        {
            AsciiParser_Reset(parser);
            return PROTO_PARSE_FRAME_INVALID;
        }
        return PROTO_PARSE_FRAME_READY;
    }

    if (parser->length > parser->expected_length)
    {
        AsciiParser_Reset(parser);
        return PROTO_PARSE_FRAME_INVALID;
    }

    return PROTO_PARSE_IN_PROGRESS;
}

uint16_t Ascii_HandleFrame(const uint8_t *rx_buf, uint16_t rx_len, uint8_t *tx_buf, uint16_t tx_capacity)
{
    uint8_t ver;
    uint8_t adr;
    uint8_t cid1;
    uint8_t cid2;
    uint16_t length_field;
    uint16_t lenid;
    uint16_t expect_len;
    uint16_t chksum_rx;
    uint16_t chksum_calc;

    if (rx_len < 18)
    {
        return 0;
    }

    if ((Parse_Hex_Byte(rx_buf, 1U, &ver) == 0U) ||
        (Parse_Hex_Byte(rx_buf, 3U, &adr) == 0U) ||
        (Parse_Hex_Byte(rx_buf, 5U, &cid1) == 0U) ||
        (Parse_Hex_Byte(rx_buf, 7U, &cid2) == 0U) ||
        (Parse_Hex_U16(rx_buf, 9U, &length_field) == 0U))
    {
        return Build_Response_Frame(tx_buf, tx_capacity, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_FORMAT_ERROR, NULL, 0);
    }

    if (ver != PROTOCOL_VERSION)
    {
        return Build_Response_Frame(tx_buf, tx_capacity, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_VER_ERROR, NULL, 0);
    }

    if (adr != SLAVE_ADDRESS)
    {
        return Build_Response_Frame(tx_buf, tx_capacity, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_ADR_ERROR, NULL, 0);
    }

    if (cid1 != CID1_BAT_DATA)
    {
        return Build_Response_Frame(tx_buf, tx_capacity, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_CID2_INVALID, NULL, 0);
    }

    if (Parse_LENGTH_Field(length_field, &lenid) == 0)
    {
        return Build_Response_Frame(tx_buf, tx_capacity, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_LCHKSUM_ERROR, NULL, 0);
    }

    expect_len = (uint16_t)(18 + lenid);
    if (rx_len != expect_len)
    {
        return Build_Response_Frame(tx_buf, tx_capacity, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_FORMAT_ERROR, NULL, 0);
    }

    if (Parse_Hex_U16(rx_buf, (uint16_t)(13U + lenid), &chksum_rx) == 0U)
    {
        return Build_Response_Frame(tx_buf, tx_capacity, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_FORMAT_ERROR, NULL, 0);
    }
    chksum_calc = Calc_CHKSUM((uint8_t *)&rx_buf[1], (uint16_t)(12 + lenid));
    if (chksum_rx != chksum_calc)
    {
        return Build_Response_Frame(tx_buf, tx_capacity, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_CHKSUM_ERROR, NULL, 0);
    }

    // 7. 解析INFO域（ASCII转HEX）
    uint8_t info_hex_buf[256] = {0};
    uint16_t info_hex_len = lenid / 2;
    for (uint16_t i = 0; i < info_hex_len; i++)
    {
        info_hex_buf[i] = (Ascii_To_Hex(rx_buf[13 + i * 2]) << 4) | Ascii_To_Hex(rx_buf[13 + i * 2 + 1]);
    }
    uint8_t cmd = info_hex_buf[0]; // INFO第一个字节为Command

#if 1
    switch (cid2)
    {
    case CMD_GET_BATTERY_INFO:
        return Ascii_BuildBaseInfo(tx_buf);
    case CMD_GET_ANALOG1_DATA:
        return Ascii_BuildAnalogData(tx_buf);
    case CMD_GET_ANALOG2_DATA:
        return Cmd_Handle_Analog2_Value(tx_buf, cmd);
        break;
    case CMD_GET_ALARM_INFO:
        return Ascii_BuildAlarmInfo(tx_buf);
    case CMD_GET_CHARGE_DIS_INFO:
        return Ascii_BuildChargeDisInfo(tx_buf);
    default:
        return Build_Response_Frame(tx_buf, tx_capacity, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_CID2_INVALID, NULL, 0);
    }
#endif
    // return Build_Response_Frame(tx_buf, tx_capacity, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_CID2_INVALID, NULL, 0);
}
