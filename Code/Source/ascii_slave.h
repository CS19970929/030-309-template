#ifndef ASCII_SLAVE_H
#define ASCII_SLAVE_H

#include "stm32f0xx.h"
#include "modbus_rtu_parser.h"

#define SOI                     0x7E
#define EOI                     0x0D
#define CID1_BAT_DATA           0x46
#define CMD_GET_BATTERY_INFO    0x60
#define CMD_GET_ANALOG_DATA     0x61
#define CMD_GET_ALARM_INFO      0x62
#define CMD_GET_CHARGE_DIS_INFO 0x63

#define RTN_OK                  0x00
#define RTN_VER_ERROR           0x01
#define RTN_CHKSUM_ERROR        0x02
#define RTN_LCHKSUM_ERROR       0x03
#define RTN_CID2_INVALID        0x04
#define RTN_FORMAT_ERROR        0x05
#define RTN_DATA_INVALID        0x06
#define RTN_ADR_ERROR           0x90
#define RTN_COMM_ERROR          0x91

#define PROTOCOL_VERSION        0x20
#define SLAVE_ADDRESS           0x12
#define MAX_FRAME_LEN           600
#define CELL_MAX_NUM            16

typedef struct {
    uint8_t device_name[10];
    uint8_t manufactory_name[20];
    uint8_t software_ver[2];
    uint8_t battery_num;
    uint8_t battery_barcode[CELL_MAX_NUM][16];
} Battery_Base_Info_T;

typedef struct {
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
} Battery_Analog_T;

typedef struct {
    uint8_t system_alarm1;
    uint8_t system_alarm2;
    uint8_t system_protect1;
    uint8_t system_protect2;
} Battery_Alarm_T;

typedef struct {
    uint16_t charge_volt_limit;
    uint16_t discharge_volt_limit;
    int16_t max_charge_current;
    int16_t max_discharge_current;
    uint8_t charge_dis_status;
} Battery_Charge_Dis_Info_T;

typedef struct {
    uint8_t buffer[MAX_FRAME_LEN];
    uint16_t length;
    uint16_t expected_length;
} AsciiParser;

void AsciiParser_Reset(AsciiParser *parser);
ProtocolParseResult AsciiParser_ConsumeByte(AsciiParser *parser, uint8_t byte);
uint16_t Ascii_HandleFrame(const uint8_t *rx_buf, uint16_t rx_len, uint8_t *tx_buf);

uint8_t Hex_To_Ascii(uint8_t hex);
uint8_t Ascii_To_Hex(uint8_t ascii);
uint8_t VerToHex(uint8_t *ver);
uint8_t Calc_LCHKSUM(uint16_t lenid);
uint16_t Calc_CHKSUM(uint8_t *data, uint16_t len);
uint16_t Build_LENGTH_Field(uint16_t lenid);
uint8_t Parse_LENGTH_Field(uint16_t length_field, uint16_t *lenid);
uint16_t Build_Response_Frame(uint8_t *tx_buf, uint8_t ver, uint8_t adr, uint8_t rtn, uint8_t *info_data, uint16_t info_hex_len);

#endif
