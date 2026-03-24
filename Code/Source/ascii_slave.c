#include "main.h"
#include "ascii_slave.h"

#define ASCII_DEVICE_NAME_LEN        10u
#define ASCII_FACTORY_NAME_LEN       20u
#define ASCII_BARCODE_LEN            16u
#define ASCII_CELL_MAX_NUM           16u
#define ASCII_TEMP_MAX_NUM           6u

typedef struct
{
    uint8_t device_name[ASCII_DEVICE_NAME_LEN];
    uint8_t manufactory_name[ASCII_FACTORY_NAME_LEN];
    uint8_t software_ver[2];
    uint8_t battery_num;
    uint8_t battery_barcode[ASCII_CELL_MAX_NUM][ASCII_BARCODE_LEN];
} ASCII_BASE_INFO_T;

typedef struct
{
    uint16_t pack_total_avg_voltage;
    int16_t pack_total_current;
    uint8_t pack_soc;
    uint16_t pack_avg_cycle_count;
    uint16_t pack_max_cycle_count;
    uint8_t pack_avg_soh;
    uint8_t pack_min_soh;
    uint16_t cell_max_voltage;
    uint16_t cell_max_voltage_module;
    uint16_t cell_min_voltage;
    uint16_t cell_min_voltage_module;
    int16_t cell_avg_temp;
    int16_t cell_max_temp;
    uint16_t cell_max_temp_module;
    int16_t cell_min_temp;
    uint16_t cell_min_temp_module;
    int16_t mosfet_avg_temp;
    int16_t mosfet_max_temp;
    uint16_t mosfet_max_temp_module;
    int16_t mosfet_min_temp;
    uint16_t mosfet_min_temp_module;
    int16_t bms_avg_temp;
    int16_t bms_max_temp;
    uint16_t bms_max_temp_module;
    int16_t bms_min_temp;
    uint16_t bms_min_temp_module;
} ASCII_ANALOG1_T;

typedef struct
{
    uint8_t cell_num;
    uint16_t cell_voltage[ASCII_CELL_MAX_NUM];
    uint8_t temp_num;
    uint16_t temp_value[ASCII_TEMP_MAX_NUM];
    int16_t current;
    uint16_t module_voltage;
    uint16_t remain_capacity;
    uint8_t user_define_num;
    uint16_t total_capacity;
    uint16_t cycle_num;
    uint16_t full_charge_capacity;
} ASCII_ANALOG2_T;

typedef struct
{
    uint8_t system_alarm1;
    uint8_t system_alarm2;
    uint8_t system_protect1;
    uint8_t system_protect2;
} ASCII_ALARM_T;

typedef struct
{
    uint16_t charge_volt_limit;
    uint16_t discharge_volt_limit;
    int16_t max_charge_current;
    int16_t max_discharge_current;
    uint8_t charge_dis_status;
} ASCII_CHARGE_INFO_T;

static uint8_t Ascii_HexToChar(uint8_t hex);
static uint8_t Ascii_CharToHex(uint8_t ascii);
static uint8_t Ascii_CalcLengthChecksum(uint16_t lenid);
static uint16_t Ascii_CalcChecksum(const uint8_t *data, uint16_t len);
static uint16_t Ascii_BuildLengthField(uint16_t lenid);
static uint8_t Ascii_ParseLengthField(uint16_t length_field, uint16_t *lenid);
static uint16_t Ascii_BuildResponse(uint8_t *tx_buf, uint8_t ver, uint8_t adr, uint8_t rtn, const uint8_t *info_data, uint16_t info_len);
static uint16_t Ascii_HandleBatteryInfo(uint8_t *tx_buf);
static uint16_t Ascii_HandleAnalog1(uint8_t *tx_buf);
static uint16_t Ascii_HandleAnalog2(uint8_t *tx_buf, uint8_t cmd);
static uint16_t Ascii_HandleAlarm(uint8_t *tx_buf);
static uint16_t Ascii_HandleChargeInfo(uint8_t *tx_buf);
static void Ascii_LoadBaseInfo(ASCII_BASE_INFO_T *info);
static void Ascii_LoadAnalog1(ASCII_ANALOG1_T *info);
static void Ascii_LoadAnalog2(ASCII_ANALOG2_T *info);
static void Ascii_LoadAlarm(ASCII_ALARM_T *info);
static void Ascii_LoadChargeInfo(ASCII_CHARGE_INFO_T *info);
static uint16_t Ascii_GetPackVoltageMv(void);
static int16_t Ascii_GetPackCurrent10mA(void);
static uint16_t Ascii_GetTempDeciKelvin(uint16_t raw_temp);
static uint8_t Ascii_GetSeriesCount(void);
static uint8_t Ascii_ClampU8(uint16_t value);
static uint16_t Ascii_ClampU16(uint32_t value);
static void Ascii_CopyPadded(uint8_t *dst, uint16_t dst_len, const uint8_t *src, uint16_t src_len);

void AsciiSlave_Init(ASCII_SLAVE_CTX *ctx)
{
    if (ctx == NULL)
    {
        return;
    }

    AsciiSlave_Reset(ctx);
}

void AsciiSlave_Reset(ASCII_SLAVE_CTX *ctx)
{
    if (ctx == NULL)
    {
        return;
    }

    memset(ctx->rx_buf, 0, sizeof(ctx->rx_buf));
    memset(ctx->tx_buf, 0, sizeof(ctx->tx_buf));
    ctx->rx_len = 0;
    ctx->tx_len = 0;
    ctx->frame_ready = 0;
    ctx->receiving = 0;
    ctx->last_rx_tick = 0;
}

uint8_t AsciiSlave_InputByte(ASCII_SLAVE_CTX *ctx, uint8_t data, int32_t now_ms)
{
    if (ctx == NULL)
    {
        return 0;
    }

    if (data == ASCII_SLAVE_SOF)
    {
        ctx->receiving = 1;
        ctx->frame_ready = 0;
        ctx->rx_len = 0;
    }

    if (!ctx->receiving)
    {
        return 0;
    }

    if (ctx->rx_len >= ASCII_SLAVE_MAX_FRAME_LEN)
    {
        AsciiSlave_Reset(ctx);
        return 0;
    }

    ctx->rx_buf[ctx->rx_len++] = data;
    ctx->last_rx_tick = now_ms;

    if (data == ASCII_SLAVE_EOF)
    {
        ctx->frame_ready = 1;
        ctx->receiving = 0;
        return 1;
    }

    return 0;
}

void AsciiSlave_CheckTimeout(ASCII_SLAVE_CTX *ctx, int32_t now_ms, uint16_t timeout_ms)
{
    if ((ctx == NULL) || (!ctx->receiving))
    {
        return;
    }

    if ((now_ms - ctx->last_rx_tick) > timeout_ms)
    {
        AsciiSlave_Reset(ctx);
    }
}

uint16_t AsciiSlave_ProcessFrame(ASCII_SLAVE_CTX *ctx)
{
    uint8_t ver;
    uint8_t adr;
    uint8_t cid1;
    uint8_t cid2;
    uint8_t cmd;
    uint8_t hi;
    uint8_t lo;
    uint16_t lenid;
    uint16_t rx_len;
    uint16_t length_field;
    uint16_t expect_len;
    uint16_t chksum_rx;
    uint16_t chksum_calc;
    uint16_t i;
    uint16_t info_hex_len;
    uint8_t info_hex_buf[256];

    if ((ctx == NULL) || (!ctx->frame_ready))
    {
        return 0;
    }

    rx_len = ctx->rx_len;
    ctx->frame_ready = 0;
    ctx->tx_len = 0;

    if ((rx_len < 18u) || (ctx->rx_buf[0] != ASCII_SLAVE_SOF) || (ctx->rx_buf[rx_len - 1u] != ASCII_SLAVE_EOF))
    {
        AsciiSlave_Reset(ctx);
        return 0;
    }

    hi = Ascii_CharToHex(ctx->rx_buf[1]);
    lo = Ascii_CharToHex(ctx->rx_buf[2]);
    if ((hi > 0x0Fu) || (lo > 0x0Fu))
    {
        ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_FORMAT_ERROR, NULL, 0);
        return ctx->tx_len;
    }
    ver = (uint8_t)((hi << 4) | lo);

    hi = Ascii_CharToHex(ctx->rx_buf[3]);
    lo = Ascii_CharToHex(ctx->rx_buf[4]);
    if ((hi > 0x0Fu) || (lo > 0x0Fu))
    {
        ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_FORMAT_ERROR, NULL, 0);
        return ctx->tx_len;
    }
    adr = (uint8_t)((hi << 4) | lo);

    hi = Ascii_CharToHex(ctx->rx_buf[5]);
    lo = Ascii_CharToHex(ctx->rx_buf[6]);
    if ((hi > 0x0Fu) || (lo > 0x0Fu))
    {
        ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_FORMAT_ERROR, NULL, 0);
        return ctx->tx_len;
    }
    cid1 = (uint8_t)((hi << 4) | lo);

    hi = Ascii_CharToHex(ctx->rx_buf[7]);
    lo = Ascii_CharToHex(ctx->rx_buf[8]);
    if ((hi > 0x0Fu) || (lo > 0x0Fu))
    {
        ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_FORMAT_ERROR, NULL, 0);
        return ctx->tx_len;
    }
    cid2 = (uint8_t)((hi << 4) | lo);

    length_field = 0;
    for (i = 0; i < 4u; i++)
    {
        hi = Ascii_CharToHex(ctx->rx_buf[9u + i]);
        if (hi > 0x0Fu)
        {
            ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_FORMAT_ERROR, NULL, 0);
            return ctx->tx_len;
        }
        length_field = (uint16_t)((length_field << 4) | hi);
    }

    if (ver != ASCII_SLAVE_PROTOCOL_VER)
    {
        ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_VER_ERROR, NULL, 0);
        return ctx->tx_len;
    }

    if (adr != ASCII_SLAVE_ADDR)
    {
        ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_ADDR_ERROR, NULL, 0);
        return ctx->tx_len;
    }

    if (cid1 != ASCII_SLAVE_CID1_BATTERY)
    {
        ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_CID2_INVALID, NULL, 0);
        return ctx->tx_len;
    }

    if (!Ascii_ParseLengthField(length_field, &lenid))
    {
        ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_LCHKSUM_ERR, NULL, 0);
        return ctx->tx_len;
    }

    expect_len = (uint16_t)(13u + lenid + 4u + 1u);
    if (rx_len != expect_len)
    {
        ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_FORMAT_ERROR, NULL, 0);
        return ctx->tx_len;
    }

    chksum_rx = 0;
    for (i = 0; i < 4u; i++)
    {
        hi = Ascii_CharToHex(ctx->rx_buf[13u + lenid + i]);
        if (hi > 0x0Fu)
        {
            ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_FORMAT_ERROR, NULL, 0);
            return ctx->tx_len;
        }
        chksum_rx = (uint16_t)((chksum_rx << 4) | hi);
    }

    chksum_calc = Ascii_CalcChecksum(&ctx->rx_buf[1], (uint16_t)(12u + lenid));
    if (chksum_rx != chksum_calc)
    {
        ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_CHKSUM_ERROR, NULL, 0);
        return ctx->tx_len;
    }

    if ((lenid & 0x0001u) != 0u)
    {
        ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_FORMAT_ERROR, NULL, 0);
        return ctx->tx_len;
    }

    info_hex_len = (uint16_t)(lenid >> 1);
    if ((info_hex_len == 0u) || (info_hex_len > sizeof(info_hex_buf)))
    {
        ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_DATA_INVALID, NULL, 0);
        return ctx->tx_len;
    }

    for (i = 0; i < info_hex_len; i++)
    {
        hi = Ascii_CharToHex(ctx->rx_buf[13u + (i << 1)]);
        lo = Ascii_CharToHex(ctx->rx_buf[14u + (i << 1)]);
        if ((hi > 0x0Fu) || (lo > 0x0Fu))
        {
            ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_FORMAT_ERROR, NULL, 0);
            return ctx->tx_len;
        }
        info_hex_buf[i] = (uint8_t)((hi << 4) | lo);
    }

    cmd = info_hex_buf[0];
    switch (cid2)
    {
    case ASCII_SLAVE_CMD_BATTERY_INFO:
        ctx->tx_len = Ascii_HandleBatteryInfo(ctx->tx_buf);
        break;
    case ASCII_SLAVE_CMD_ANALOG1_DATA:
        ctx->tx_len = Ascii_HandleAnalog1(ctx->tx_buf);
        break;
    case ASCII_SLAVE_CMD_ANALOG2_DATA:
        ctx->tx_len = Ascii_HandleAnalog2(ctx->tx_buf, cmd);
        break;
    case ASCII_SLAVE_CMD_ALARM_INFO:
        ctx->tx_len = Ascii_HandleAlarm(ctx->tx_buf);
        break;
    case ASCII_SLAVE_CMD_CHARGE_DIS_INFO:
        ctx->tx_len = Ascii_HandleChargeInfo(ctx->tx_buf);
        break;
    default:
        ctx->tx_len = Ascii_BuildResponse(ctx->tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_CID2_INVALID, NULL, 0);
        break;
    }

    return ctx->tx_len;
}

void AsciiSlave_SendBuffer(USART_TypeDef *uart, uint8_t *buf, uint16_t len, uint8_t use_rs485_dir)
{
    uint16_t i;

    if ((uart == NULL) || (buf == NULL) || (len == 0u))
    {
        return;
    }

    if (use_rs485_dir)
    {
        TRANS_EN_485();
        __delay_ms(1);
    }

    for (i = 0; i < len; i++)
    {
        while (USART_GetFlagStatus(uart, USART_FLAG_TXE) == RESET)
        {
        }
        USART_SendData(uart, buf[i]);
        while (USART_GetFlagStatus(uart, USART_FLAG_TC) == RESET)
        {
        }
    }

    if (use_rs485_dir)
    {
        __delay_ms(1);
        RECV_EN_485();
    }
}

static uint8_t Ascii_HexToChar(uint8_t hex)
{
    hex &= 0x0Fu;
    return (hex < 10u) ? (uint8_t)(hex + '0') : (uint8_t)(hex - 10u + 'A');
}

static uint8_t Ascii_CharToHex(uint8_t ascii)
{
    if ((ascii >= '0') && (ascii <= '9'))
    {
        return (uint8_t)(ascii - '0');
    }
    if ((ascii >= 'A') && (ascii <= 'F'))
    {
        return (uint8_t)(ascii - 'A' + 10u);
    }
    if ((ascii >= 'a') && (ascii <= 'f'))
    {
        return (uint8_t)(ascii - 'a' + 10u);
    }
    return 0xFFu;
}

static uint8_t Ascii_CalcLengthChecksum(uint16_t lenid)
{
    uint8_t seg1;
    uint8_t seg2;
    uint8_t seg3;
    uint8_t sum;

    seg1 = (uint8_t)((lenid >> 8) & 0x0Fu);
    seg2 = (uint8_t)((lenid >> 4) & 0x0Fu);
    seg3 = (uint8_t)(lenid & 0x0Fu);
    sum = (uint8_t)((seg1 + seg2 + seg3) & 0x0Fu);

    return (uint8_t)((~sum + 1u) & 0x0Fu);
}

static uint16_t Ascii_CalcChecksum(const uint8_t *data, uint16_t len)
{
    uint32_t sum;
    uint16_t i;

    sum = 0u;
    for (i = 0; i < len; i++)
    {
        sum += data[i];
    }

    sum &= 0xFFFFu;
    return (uint16_t)(~sum + 1u);
}

static uint16_t Ascii_BuildLengthField(uint16_t lenid)
{
    uint8_t lchksum;

    lchksum = Ascii_CalcLengthChecksum(lenid);
    return (uint16_t)(((uint16_t)lchksum << 12) | (lenid & 0x0FFFu));
}

static uint8_t Ascii_ParseLengthField(uint16_t length_field, uint16_t *lenid)
{
    uint8_t lchksum_rx;

    if (lenid == NULL)
    {
        return 0;
    }

    lchksum_rx = (uint8_t)((length_field >> 12) & 0x0Fu);
    *lenid = (uint16_t)(length_field & 0x0FFFu);

    return (lchksum_rx == Ascii_CalcLengthChecksum(*lenid)) ? 1u : 0u;
}

static uint16_t Ascii_BuildResponse(uint8_t *tx_buf, uint8_t ver, uint8_t adr, uint8_t rtn, const uint8_t *info_data, uint16_t info_len)
{
    uint16_t idx;
    uint16_t length_field;
    uint16_t chksum;
    uint16_t i;

    if (tx_buf == NULL)
    {
        return 0;
    }

    idx = 0;
    tx_buf[idx++] = ASCII_SLAVE_SOF;
    tx_buf[idx++] = Ascii_HexToChar((uint8_t)(ver >> 4));
    tx_buf[idx++] = Ascii_HexToChar(ver);
    tx_buf[idx++] = Ascii_HexToChar((uint8_t)(adr >> 4));
    tx_buf[idx++] = Ascii_HexToChar(adr);
    tx_buf[idx++] = Ascii_HexToChar((uint8_t)(ASCII_SLAVE_CID1_BATTERY >> 4));
    tx_buf[idx++] = Ascii_HexToChar(ASCII_SLAVE_CID1_BATTERY);
    tx_buf[idx++] = Ascii_HexToChar((uint8_t)(rtn >> 4));
    tx_buf[idx++] = Ascii_HexToChar(rtn);

    length_field = Ascii_BuildLengthField((uint16_t)(info_len << 1));
    tx_buf[idx++] = Ascii_HexToChar((uint8_t)(length_field >> 12));
    tx_buf[idx++] = Ascii_HexToChar((uint8_t)(length_field >> 8));
    tx_buf[idx++] = Ascii_HexToChar((uint8_t)(length_field >> 4));
    tx_buf[idx++] = Ascii_HexToChar((uint8_t)length_field);

    for (i = 0; i < info_len; i++)
    {
        tx_buf[idx++] = Ascii_HexToChar((uint8_t)(info_data[i] >> 4));
        tx_buf[idx++] = Ascii_HexToChar(info_data[i]);
    }

    chksum = Ascii_CalcChecksum(&tx_buf[1], (uint16_t)(idx - 1u));
    tx_buf[idx++] = Ascii_HexToChar((uint8_t)(chksum >> 12));
    tx_buf[idx++] = Ascii_HexToChar((uint8_t)(chksum >> 8));
    tx_buf[idx++] = Ascii_HexToChar((uint8_t)(chksum >> 4));
    tx_buf[idx++] = Ascii_HexToChar((uint8_t)chksum);
    tx_buf[idx++] = ASCII_SLAVE_EOF;

    return idx;
}

static uint16_t Ascii_HandleBatteryInfo(uint8_t *tx_buf)
{
    ASCII_BASE_INFO_T info;
    uint8_t payload[ASCII_DEVICE_NAME_LEN + ASCII_FACTORY_NAME_LEN + 2u + 1u + (ASCII_CELL_MAX_NUM * ASCII_BARCODE_LEN)];
    uint16_t idx;
    uint8_t i;

    Ascii_LoadBaseInfo(&info);

    idx = 0;
    memcpy(&payload[idx], info.device_name, ASCII_DEVICE_NAME_LEN);
    idx += ASCII_DEVICE_NAME_LEN;
    memcpy(&payload[idx], info.manufactory_name, ASCII_FACTORY_NAME_LEN);
    idx += ASCII_FACTORY_NAME_LEN;
    memcpy(&payload[idx], info.software_ver, 2u);
    idx += 2u;
    payload[idx++] = info.battery_num;

    for (i = 0; i < info.battery_num; i++)
    {
        memcpy(&payload[idx], info.battery_barcode[i], ASCII_BARCODE_LEN);
        idx += ASCII_BARCODE_LEN;
    }

    return Ascii_BuildResponse(tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_OK, payload, idx);
}

static uint16_t Ascii_HandleAnalog1(uint8_t *tx_buf)
{
    ASCII_ANALOG1_T info;
    uint8_t payload[64];
    uint16_t idx;

    Ascii_LoadAnalog1(&info);
    idx = 0;

#define ASCII_PUSH_U16(v)                \
    do                                   \
    {                                    \
        payload[idx++] = (uint8_t)((v) >> 8); \
        payload[idx++] = (uint8_t)(v);        \
    } while (0)

    ASCII_PUSH_U16(info.pack_total_avg_voltage);
    ASCII_PUSH_U16((uint16_t)info.pack_total_current);
    payload[idx++] = info.pack_soc;
    ASCII_PUSH_U16(info.pack_avg_cycle_count);
    ASCII_PUSH_U16(info.pack_max_cycle_count);
    payload[idx++] = info.pack_avg_soh;
    payload[idx++] = info.pack_min_soh;
    ASCII_PUSH_U16(info.cell_max_voltage);
    ASCII_PUSH_U16(info.cell_max_voltage_module);
    ASCII_PUSH_U16(info.cell_min_voltage);
    ASCII_PUSH_U16(info.cell_min_voltage_module);
    ASCII_PUSH_U16((uint16_t)info.cell_avg_temp);
    ASCII_PUSH_U16((uint16_t)info.cell_max_temp);
    ASCII_PUSH_U16(info.cell_max_temp_module);
    ASCII_PUSH_U16((uint16_t)info.cell_min_temp);
    ASCII_PUSH_U16(info.cell_min_temp_module);
    ASCII_PUSH_U16((uint16_t)info.mosfet_avg_temp);
    ASCII_PUSH_U16((uint16_t)info.mosfet_max_temp);
    ASCII_PUSH_U16(info.mosfet_max_temp_module);
    ASCII_PUSH_U16((uint16_t)info.mosfet_min_temp);
    ASCII_PUSH_U16(info.mosfet_min_temp_module);
    ASCII_PUSH_U16((uint16_t)info.bms_avg_temp);
    ASCII_PUSH_U16((uint16_t)info.bms_max_temp);
    ASCII_PUSH_U16(info.bms_max_temp_module);
    ASCII_PUSH_U16((uint16_t)info.bms_min_temp);
    ASCII_PUSH_U16(info.bms_min_temp_module);

#undef ASCII_PUSH_U16

    return Ascii_BuildResponse(tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_OK, payload, idx);
}

static uint16_t Ascii_HandleAnalog2(uint8_t *tx_buf, uint8_t cmd)
{
    ASCII_ANALOG2_T info;
    uint8_t payload[128];
    uint16_t idx;
    uint8_t i;

    if (cmd != ASCII_SLAVE_ADDR)
    {
        return Ascii_BuildResponse(tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_ADDR_ERROR, NULL, 0);
    }

    Ascii_LoadAnalog2(&info);
    idx = 0;

    payload[idx++] = 0x00u;
    payload[idx++] = cmd;
    payload[idx++] = info.cell_num;

    for (i = 0; i < info.cell_num; i++)
    {
        payload[idx++] = (uint8_t)(info.cell_voltage[i] >> 8);
        payload[idx++] = (uint8_t)info.cell_voltage[i];
    }

    payload[idx++] = info.temp_num;
    for (i = 0; i < info.temp_num; i++)
    {
        payload[idx++] = (uint8_t)(info.temp_value[i] >> 8);
        payload[idx++] = (uint8_t)info.temp_value[i];
    }

    payload[idx++] = (uint8_t)(((uint16_t)info.current) >> 8);
    payload[idx++] = (uint8_t)info.current;
    payload[idx++] = (uint8_t)(info.module_voltage >> 8);
    payload[idx++] = (uint8_t)info.module_voltage;
    payload[idx++] = (uint8_t)(info.remain_capacity >> 8);
    payload[idx++] = (uint8_t)info.remain_capacity;
    payload[idx++] = info.user_define_num;
    payload[idx++] = (uint8_t)(info.total_capacity >> 8);
    payload[idx++] = (uint8_t)info.total_capacity;
    payload[idx++] = (uint8_t)(info.cycle_num >> 8);
    payload[idx++] = (uint8_t)info.cycle_num;
    payload[idx++] = (uint8_t)(info.full_charge_capacity >> 8);
    payload[idx++] = (uint8_t)info.full_charge_capacity;

    return Ascii_BuildResponse(tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_OK, payload, idx);
}

static uint16_t Ascii_HandleAlarm(uint8_t *tx_buf)
{
    ASCII_ALARM_T info;
    uint8_t payload[4];

    Ascii_LoadAlarm(&info);
    payload[0] = info.system_alarm1;
    payload[1] = info.system_alarm2;
    payload[2] = info.system_protect1;
    payload[3] = info.system_protect2;

    return Ascii_BuildResponse(tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_OK, payload, sizeof(payload));
}

static uint16_t Ascii_HandleChargeInfo(uint8_t *tx_buf)
{
    ASCII_CHARGE_INFO_T info;
    uint8_t payload[9];

    Ascii_LoadChargeInfo(&info);
    payload[0] = (uint8_t)(info.charge_volt_limit >> 8);
    payload[1] = (uint8_t)info.charge_volt_limit;
    payload[2] = (uint8_t)(info.discharge_volt_limit >> 8);
    payload[3] = (uint8_t)info.discharge_volt_limit;
    payload[4] = (uint8_t)(((uint16_t)info.max_charge_current) >> 8);
    payload[5] = (uint8_t)info.max_charge_current;
    payload[6] = (uint8_t)(((uint16_t)info.max_discharge_current) >> 8);
    payload[7] = (uint8_t)info.max_discharge_current;
    payload[8] = info.charge_dis_status;

    return Ascii_BuildResponse(tx_buf, ASCII_SLAVE_PROTOCOL_VER, ASCII_SLAVE_ADDR, ASCII_SLAVE_RTN_OK, payload, sizeof(payload));
}

static void Ascii_LoadBaseInfo(ASCII_BASE_INFO_T *info)
{
    uint8_t i;
    uint16_t serial_len;

    if (info == NULL)
    {
        return;
    }

    memset(info, 0, sizeof(*info));

    Ascii_CopyPadded(info->device_name, ASCII_DEVICE_NAME_LEN, (const uint8_t *)"SH367309", 8u);
    Ascii_CopyPadded(info->manufactory_name, ASCII_FACTORY_NAME_LEN, (const uint8_t *)"SEANARY", 7u);

    if (ProductionInfor.BMS_SoftWareVersionLength >= 2u)
    {
        info->software_ver[0] = ProductionInfor.BMS_SoftWareVersion[0];
        info->software_ver[1] = ProductionInfor.BMS_SoftWareVersion[1];
    }
    else
    {
        info->software_ver[0] = '0';
        info->software_ver[1] = (uint8_t)('0' + (VERSION % 10));
    }

    info->battery_num = Ascii_GetSeriesCount();
    if (info->battery_num > ASCII_CELL_MAX_NUM)
    {
        info->battery_num = ASCII_CELL_MAX_NUM;
    }

    serial_len = ProductionInfor.BMS_SerialNumberLength;
    if (serial_len > PRODUCT_ID_LENGTH_MAX)
    {
        serial_len = PRODUCT_ID_LENGTH_MAX;
    }

    for (i = 0; i < info->battery_num; i++)
    {
        if (serial_len > 0u)
        {
            Ascii_CopyPadded(info->battery_barcode[i], ASCII_BARCODE_LEN, ProductionInfor.BMS_SerialNumber, serial_len);
        }
        else
        {
            Ascii_CopyPadded(info->battery_barcode[i], ASCII_BARCODE_LEN, (const uint8_t *)"0000000000000000", ASCII_BARCODE_LEN);
        }
    }
}

static void Ascii_LoadAnalog1(ASCII_ANALOG1_T *info)
{
    uint8_t temp_cnt;
    uint32_t temp_sum;
    uint8_t i;
    uint16_t max_temp;
    uint16_t min_temp;
    uint8_t max_temp_pos;
    uint8_t min_temp_pos;

    if (info == NULL)
    {
        return;
    }

    memset(info, 0, sizeof(*info));

    info->pack_total_avg_voltage = Ascii_GetPackVoltageMv();
    info->pack_total_current = Ascii_GetPackCurrent10mA();
    info->pack_soc = Ascii_ClampU8(g_stCellInfoReport.SocElement.u16Soc);
    info->pack_avg_cycle_count = g_stCellInfoReport.SocElement.u16Cycle_times;
    info->pack_max_cycle_count = g_stCellInfoReport.SocElement.u16Cycle_times;
    info->pack_avg_soh = Ascii_ClampU8(g_stCellInfoReport.SocElement.u16Soh);
    info->pack_min_soh = Ascii_ClampU8(g_stCellInfoReport.SocElement.u16Soh);
    info->cell_max_voltage = g_stCellInfoReport.u16VCellMax;
    info->cell_max_voltage_module = (uint16_t)(g_stCellInfoReport.u16VCellMaxPosition + 1u);
    info->cell_min_voltage = g_stCellInfoReport.u16VCellMin;
    info->cell_min_voltage_module = (uint16_t)(g_stCellInfoReport.u16VCellMinPosition + 1u);

    temp_cnt = TEMP_NUM;
    if (temp_cnt > ASCII_TEMP_MAX_NUM)
    {
        temp_cnt = ASCII_TEMP_MAX_NUM;
    }

    if (temp_cnt == 0u)
    {
        return;
    }

    temp_sum = 0u;
    max_temp = g_stCellInfoReport.u16Temperature[0];
    min_temp = g_stCellInfoReport.u16Temperature[0];
    max_temp_pos = 0u;
    min_temp_pos = 0u;

    for (i = 0; i < temp_cnt; i++)
    {
        temp_sum += g_stCellInfoReport.u16Temperature[i];
        if (g_stCellInfoReport.u16Temperature[i] > max_temp)
        {
            max_temp = g_stCellInfoReport.u16Temperature[i];
            max_temp_pos = i;
        }
        if (g_stCellInfoReport.u16Temperature[i] < min_temp)
        {
            min_temp = g_stCellInfoReport.u16Temperature[i];
            min_temp_pos = i;
        }
    }

    info->cell_avg_temp = (int16_t)Ascii_GetTempDeciKelvin((uint16_t)(temp_sum / temp_cnt));
    info->cell_max_temp = (int16_t)Ascii_GetTempDeciKelvin(max_temp);
    info->cell_max_temp_module = (uint16_t)(max_temp_pos + 1u);
    info->cell_min_temp = (int16_t)Ascii_GetTempDeciKelvin(min_temp);
    info->cell_min_temp_module = (uint16_t)(min_temp_pos + 1u);

    info->mosfet_avg_temp = info->cell_avg_temp;
    info->mosfet_max_temp = info->cell_max_temp;
    info->mosfet_max_temp_module = info->cell_max_temp_module;
    info->mosfet_min_temp = info->cell_min_temp;
    info->mosfet_min_temp_module = info->cell_min_temp_module;
    info->bms_avg_temp = info->cell_avg_temp;
    info->bms_max_temp = info->cell_max_temp;
    info->bms_max_temp_module = info->cell_max_temp_module;
    info->bms_min_temp = info->cell_min_temp;
    info->bms_min_temp_module = info->cell_min_temp_module;
}

static void Ascii_LoadAnalog2(ASCII_ANALOG2_T *info)
{
    uint8_t i;
    uint8_t cell_cnt;
    uint8_t temp_cnt;

    if (info == NULL)
    {
        return;
    }

    memset(info, 0, sizeof(*info));

    cell_cnt = Ascii_GetSeriesCount();
    if (cell_cnt > ASCII_CELL_MAX_NUM)
    {
        cell_cnt = ASCII_CELL_MAX_NUM;
    }
    info->cell_num = cell_cnt;
    for (i = 0; i < cell_cnt; i++)
    {
        info->cell_voltage[i] = g_stCellInfoReport.u16VCell[i];
    }

    temp_cnt = TEMP_NUM;
    if (temp_cnt > ASCII_TEMP_MAX_NUM)
    {
        temp_cnt = ASCII_TEMP_MAX_NUM;
    }
    info->temp_num = temp_cnt;
    for (i = 0; i < temp_cnt; i++)
    {
        info->temp_value[i] = Ascii_GetTempDeciKelvin(g_stCellInfoReport.u16Temperature[i]);
    }

    info->current = Ascii_GetPackCurrent10mA();
    info->module_voltage = Ascii_GetPackVoltageMv();
    info->remain_capacity = Ascii_ClampU16((uint32_t)g_stCellInfoReport.SocElement.u16CapacityNow * 10u);
    info->user_define_num = 0u;
    info->total_capacity = Ascii_ClampU16((uint32_t)g_stCellInfoReport.SocElement.u16CapacityFactory * 10u);
    info->cycle_num = g_stCellInfoReport.SocElement.u16Cycle_times;
    info->full_charge_capacity = Ascii_ClampU16((uint32_t)g_stCellInfoReport.SocElement.u16CapacityFull * 10u);
}

static void Ascii_LoadAlarm(ASCII_ALARM_T *info)
{
    if (info == NULL)
    {
        return;
    }

    memset(info, 0, sizeof(*info));

    info->system_alarm1 = (uint8_t)(g_stCellInfoReport.unMdlFault_First.all & 0x00FFu);
    info->system_alarm2 = (uint8_t)((g_stCellInfoReport.unMdlFault_First.all >> 8) & 0x00FFu);
    info->system_protect1 = (uint8_t)(g_stCellInfoReport.unMdlFault_Second.all & 0x00FFu);
    info->system_protect2 = (uint8_t)((g_stCellInfoReport.unMdlFault_Third.all >> 8) & 0x00FFu);
}

static void Ascii_LoadChargeInfo(ASCII_CHARGE_INFO_T *info)
{
    uint16_t pack_voltage;

    if (info == NULL)
    {
        return;
    }

    memset(info, 0, sizeof(*info));

    pack_voltage = Ascii_GetPackVoltageMv();
    info->charge_volt_limit = (uint16_t)(pack_voltage + 1600u);
    info->discharge_volt_limit = (pack_voltage > 6400u) ? (uint16_t)(pack_voltage - 6400u) : pack_voltage;
    info->max_charge_current = 2500;
    info->max_discharge_current = 5000;
    info->charge_dis_status = 0u;
    if (SystemStatus.bits.b1Status_MOS_CHG || SystemStatus.bits.b1Status_Relay_CHG)
    {
        info->charge_dis_status |= 0x80u;
    }
    if (SystemStatus.bits.b1Status_MOS_DSG || SystemStatus.bits.b1Status_Relay_DSG)
    {
        info->charge_dis_status |= 0x40u;
    }
}

static uint16_t Ascii_GetPackVoltageMv(void)
{
    uint32_t total_mv;
    uint8_t i;
    uint8_t cell_cnt;

    total_mv = 0u;
    cell_cnt = Ascii_GetSeriesCount();
    if (cell_cnt > ASCII_CELL_MAX_NUM)
    {
        cell_cnt = ASCII_CELL_MAX_NUM;
    }

    for (i = 0; i < cell_cnt; i++)
    {
        total_mv += g_stCellInfoReport.u16VCell[i];
    }

    if (total_mv == 0u)
    {
        total_mv = (uint32_t)g_stCellInfoReport.u16VCellTotle * 10u;
    }

    return Ascii_ClampU16(total_mv);
}

static int16_t Ascii_GetPackCurrent10mA(void)
{
    if (g_stCellInfoReport.u16Ichg > 0u)
    {
        return (int16_t)Ascii_ClampU16((uint32_t)g_stCellInfoReport.u16Ichg * 100u);
    }

    if (g_stCellInfoReport.u16IDischg > 0u)
    {
        return (int16_t)(-((int16_t)Ascii_ClampU16((uint32_t)g_stCellInfoReport.u16IDischg * 100u)));
    }

    return 0;
}

static uint16_t Ascii_GetTempDeciKelvin(uint16_t raw_temp)
{
    return (uint16_t)(raw_temp + 2331u);
}

static uint8_t Ascii_GetSeriesCount(void)
{
    if (SeriesNum == 0u)
    {
        return ASCII_CELL_MAX_NUM;
    }
    return SeriesNum;
}

static uint8_t Ascii_ClampU8(uint16_t value)
{
    return (value > 255u) ? 255u : (uint8_t)value;
}

static uint16_t Ascii_ClampU16(uint32_t value)
{
    return (value > 65535u) ? 65535u : (uint16_t)value;
}

static void Ascii_CopyPadded(uint8_t *dst, uint16_t dst_len, const uint8_t *src, uint16_t src_len)
{
    uint16_t copy_len;

    if ((dst == NULL) || (dst_len == 0u))
    {
        return;
    }

    memset(dst, 0, dst_len);
    if ((src == NULL) || (src_len == 0u))
    {
        return;
    }

    copy_len = (src_len < dst_len) ? src_len : dst_len;
    memcpy(dst, src, copy_len);
}
