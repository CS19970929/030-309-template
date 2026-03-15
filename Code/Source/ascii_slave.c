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

uint16_t Build_Response_Frame(uint8_t *tx_buf, uint8_t ver, uint8_t adr, uint8_t rtn, uint8_t *info_data, uint16_t info_hex_len)
{
    uint16_t idx;
    uint16_t lenid;
    uint16_t length_field;
    uint16_t chksum;
    uint16_t i;

    idx = 0;
    tx_buf[idx++] = SOI;
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)(ver >> 4));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)(ver & 0x0F));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)(adr >> 4));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)(adr & 0x0F));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)(CID1_BAT_DATA >> 4));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)(CID1_BAT_DATA & 0x0F));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)(rtn >> 4));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)(rtn & 0x0F));

    lenid = (uint16_t)(info_hex_len * 2);
    length_field = Build_LENGTH_Field(lenid);
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)(length_field >> 12));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)((length_field >> 8) & 0x0F));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)((length_field >> 4) & 0x0F));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)(length_field & 0x0F));

    for (i = 0; i < info_hex_len; ++i)
    {
        tx_buf[idx++] = Hex_To_Ascii((uint8_t)(info_data[i] >> 4));
        tx_buf[idx++] = Hex_To_Ascii((uint8_t)(info_data[i] & 0x0F));
    }

    chksum = Calc_CHKSUM(&tx_buf[1], (uint16_t)(idx - 1));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)(chksum >> 12));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)((chksum >> 8) & 0x0F));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)((chksum >> 4) & 0x0F));
    tx_buf[idx++] = Hex_To_Ascii((uint8_t)(chksum & 0x0F));
    tx_buf[idx++] = EOI;
    return idx;
}

static uint16_t Ascii_BuildBaseInfo(uint8_t *tx_buf)
{
    Battery_Base_Info_T info;
    uint8_t info_buf[MAX_FRAME_LEN];
    uint16_t idx;
    uint16_t i;

    BmsComm_GetBaseInfo(&info);

    idx = 0;
    memcpy(&info_buf[idx], info.device_name, sizeof(info.device_name));
    idx += sizeof(info.device_name);
    memcpy(&info_buf[idx], info.manufactory_name, sizeof(info.manufactory_name));
    idx += sizeof(info.manufactory_name);
    memcpy(&info_buf[idx], info.software_ver, sizeof(info.software_ver));
    idx += sizeof(info.software_ver);
    info_buf[idx++] = info.battery_num;

    for (i = 0; i < info.battery_num; ++i)
    {
        memcpy(&info_buf[idx], info.battery_barcode[i], sizeof(info.battery_barcode[i]));
        idx += sizeof(info.battery_barcode[i]);
    }

    return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

static uint16_t Ascii_BuildAnalogData(uint8_t *tx_buf)
{
    Battery_Analog_T data;
    uint8_t info_buf[100];
    uint16_t idx;

    BmsComm_GetAnalogData(&data);

    idx = 0;
#define APPEND_U16(v)                      \
    do                                     \
    {                                      \
        info_buf[idx++] = (uint8_t)((v) >> 8); \
        info_buf[idx++] = (uint8_t)(v);         \
    } while (0)

    APPEND_U16(data.pack_total_avg_voltage);
    APPEND_U16((uint16_t)data.pack_total_current);
    info_buf[idx++] = data.pack_soc;
    APPEND_U16(data.pack_avg_cycle_count);
    APPEND_U16(data.pack_max_cycle_count);
    info_buf[idx++] = data.pack_avg_soh;
    info_buf[idx++] = data.pack_min_soh;
    APPEND_U16(data.cell_max_voltage);
    APPEND_U16(data.cell_max_voltage_module);
    APPEND_U16(data.cell_min_voltage);
    APPEND_U16(data.cell_min_voltage_module);
    APPEND_U16((uint16_t)data.cell_avg_temp);
    APPEND_U16((uint16_t)data.cell_max_temp);
    APPEND_U16(data.cell_max_temp_module);
    APPEND_U16((uint16_t)data.cell_min_temp);
    APPEND_U16(data.cell_min_temp_module);
    APPEND_U16((uint16_t)data.mosfet_avg_temp);
    APPEND_U16((uint16_t)data.mosfet_max_temp);
    APPEND_U16(data.mosfet_max_temp_module);
    APPEND_U16((uint16_t)data.mosfet_min_temp);
    APPEND_U16(data.mosfet_min_temp_module);
    APPEND_U16((uint16_t)data.bms_avg_temp);
    APPEND_U16((uint16_t)data.bms_max_temp);
    APPEND_U16(data.bms_max_temp_module);
    APPEND_U16((uint16_t)data.bms_min_temp);
    APPEND_U16(data.bms_min_temp_module);

#undef APPEND_U16

    return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

static uint16_t Ascii_BuildAlarmInfo(uint8_t *tx_buf)
{
    Battery_Alarm_T data;
    uint8_t info_buf[4];

    BmsComm_GetAlarmData(&data);
    info_buf[0] = data.system_alarm1;
    info_buf[1] = data.system_alarm2;
    info_buf[2] = data.system_protect1;
    info_buf[3] = data.system_protect2;

    return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, sizeof(info_buf));
}

static uint16_t Ascii_BuildChargeDisInfo(uint8_t *tx_buf)
{
    Battery_Charge_Dis_Info_T data;
    uint8_t info_buf[9];
    uint16_t idx;

    BmsComm_GetChargeDischargeInfo(&data);

    idx = 0;
    info_buf[idx++] = (uint8_t)(data.charge_volt_limit >> 8);
    info_buf[idx++] = (uint8_t)data.charge_volt_limit;
    info_buf[idx++] = (uint8_t)(data.discharge_volt_limit >> 8);
    info_buf[idx++] = (uint8_t)data.discharge_volt_limit;
    info_buf[idx++] = (uint8_t)((uint16_t)data.max_charge_current >> 8);
    info_buf[idx++] = (uint8_t)data.max_charge_current;
    info_buf[idx++] = (uint8_t)((uint16_t)data.max_discharge_current >> 8);
    info_buf[idx++] = (uint8_t)data.max_discharge_current;
    info_buf[idx++] = data.charge_dis_status;

    return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

ProtocolParseResult AsciiParser_ConsumeByte(AsciiParser *parser, uint8_t byte)
{
    uint16_t length_field;
    uint16_t lenid;

    if (parser->length >= MAX_FRAME_LEN)
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
        if ((Ascii_To_Hex(parser->buffer[9]) == 0xFF) ||
            (Ascii_To_Hex(parser->buffer[10]) == 0xFF) ||
            (Ascii_To_Hex(parser->buffer[11]) == 0xFF) ||
            (Ascii_To_Hex(parser->buffer[12]) == 0xFF))
        {
            AsciiParser_Reset(parser);
            return PROTO_PARSE_FRAME_INVALID;
        }

        length_field = (uint16_t)((Ascii_To_Hex(parser->buffer[9]) << 12) |
                                  (Ascii_To_Hex(parser->buffer[10]) << 8) |
                                  (Ascii_To_Hex(parser->buffer[11]) << 4) |
                                  Ascii_To_Hex(parser->buffer[12]));
        if (Parse_LENGTH_Field(length_field, &lenid) == 0)
        {
            AsciiParser_Reset(parser);
            return PROTO_PARSE_FRAME_INVALID;
        }

        parser->expected_length = (uint16_t)(18 + lenid);
        if (parser->expected_length > MAX_FRAME_LEN)
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

uint16_t Ascii_HandleFrame(const uint8_t *rx_buf, uint16_t rx_len, uint8_t *tx_buf)
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

    if ((Ascii_To_Hex(rx_buf[1]) == 0xFF) || (Ascii_To_Hex(rx_buf[2]) == 0xFF) ||
        (Ascii_To_Hex(rx_buf[3]) == 0xFF) || (Ascii_To_Hex(rx_buf[4]) == 0xFF) ||
        (Ascii_To_Hex(rx_buf[5]) == 0xFF) || (Ascii_To_Hex(rx_buf[6]) == 0xFF) ||
        (Ascii_To_Hex(rx_buf[7]) == 0xFF) || (Ascii_To_Hex(rx_buf[8]) == 0xFF))
    {
        return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_FORMAT_ERROR, NULL, 0);
    }

    ver = (uint8_t)((Ascii_To_Hex(rx_buf[1]) << 4) | Ascii_To_Hex(rx_buf[2]));
    adr = (uint8_t)((Ascii_To_Hex(rx_buf[3]) << 4) | Ascii_To_Hex(rx_buf[4]));
    cid1 = (uint8_t)((Ascii_To_Hex(rx_buf[5]) << 4) | Ascii_To_Hex(rx_buf[6]));
    cid2 = (uint8_t)((Ascii_To_Hex(rx_buf[7]) << 4) | Ascii_To_Hex(rx_buf[8]));
    length_field = (uint16_t)((Ascii_To_Hex(rx_buf[9]) << 12) |
                              (Ascii_To_Hex(rx_buf[10]) << 8) |
                              (Ascii_To_Hex(rx_buf[11]) << 4) |
                              Ascii_To_Hex(rx_buf[12]));

    if (ver != PROTOCOL_VERSION)
    {
        return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_VER_ERROR, NULL, 0);
    }

    if (adr != SLAVE_ADDRESS)
    {
        return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_ADR_ERROR, NULL, 0);
    }

    if (cid1 != CID1_BAT_DATA)
    {
        return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_CID2_INVALID, NULL, 0);
    }

    if (Parse_LENGTH_Field(length_field, &lenid) == 0)
    {
        return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_LCHKSUM_ERROR, NULL, 0);
    }

    expect_len = (uint16_t)(18 + lenid);
    if (rx_len != expect_len)
    {
        return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_FORMAT_ERROR, NULL, 0);
    }

    chksum_rx = (uint16_t)((Ascii_To_Hex(rx_buf[13 + lenid]) << 12) |
                           (Ascii_To_Hex(rx_buf[14 + lenid]) << 8) |
                           (Ascii_To_Hex(rx_buf[15 + lenid]) << 4) |
                           Ascii_To_Hex(rx_buf[16 + lenid]));
    chksum_calc = Calc_CHKSUM((uint8_t *)&rx_buf[1], (uint16_t)(12 + lenid));
    if (chksum_rx != chksum_calc)
    {
        return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_CHKSUM_ERROR, NULL, 0);
    }

    switch (cid2)
    {
    case CMD_GET_BATTERY_INFO:
        return Ascii_BuildBaseInfo(tx_buf);
    case CMD_GET_ANALOG_DATA:
        return Ascii_BuildAnalogData(tx_buf);
    case CMD_GET_ALARM_INFO:
        return Ascii_BuildAlarmInfo(tx_buf);
    case CMD_GET_CHARGE_DIS_INFO:
        return Ascii_BuildChargeDisInfo(tx_buf);
    default:
        return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_CID2_INVALID, NULL, 0);
    }
}
