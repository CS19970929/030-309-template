#ifndef ASCII_SLAVE_H
#define ASCII_SLAVE_H

#include "stm32f0xx.h"
#include "modbus_rtu_parser.h"

/************************* 协�??固定宏定�? *************************/
// 帧�?�尾固定�?
#define SOI                     0x7E       // 帧起始标�?
#define EOI                     0x0D       // 帧结束标�?
// 固定CID1  
#define CID1_BAT_DATA           0x46       // 电池数据类固定CID1
// 4种核心命�?CID2定义
#define CMD_GET_BATTERY_INFO    0x60       // 获取电池组系统基�?信息
#define CMD_GET_ANALOG1_DATA    0x61       // 获取电池系统运�?�模拟量信息
#define CMD_GET_ANALOG2_DATA    0x42       // 获取电池系统运�?�模拟量信息
#define CMD_GET_ALARM_INFO      0x62       // 获取电池组系统状态告警量信息
#define CMD_GET_CHARGE_DIS_INFO 0x63       // 获取电池组系统充放电管理交互信息
   
// 响应返回码定�? 
#define RTN_OK                  0x00       // 正常响应
#define RTN_VER_ERROR           0x01       // 版本错�??
#define RTN_CHKSUM_ERROR        0x02       // 整帧校验错�??
#define RTN_LCHKSUM_ERROR       0x03       // 长度校验错�??
#define RTN_CID2_INVALID        0x04       // CID2命令码无�?
#define RTN_FORMAT_ERROR        0x05       // 命令格式错�??
#define RTN_DATA_INVALID        0x06       // 数据无效
#define RTN_ADR_ERROR           0x90       // 地址错�??
#define RTN_COMM_ERROR          0x91       // 内部通信错�??
// 从机配置
#define PROTOCOL_VERSION        0x20       //协�??版本�?
#define SLAVE_ADDRESS           0x12       // �?机从机地址（协�?要求�?2开始）
// #define SLAVE_ADDRESS           0x02       // �?机从机地址（协�?要求�?2开始）
#define MAX_FRAME_LEN           600
//#define MAX_FRAME_LEN           350        // 最大帧长度
#define ASCII_RX_FRAME_LEN      64
#define CELL_MAX_NUM            16         // 最大电�?数量�?48V电池16串）
// RS485控制引脚定义
//#define RS485_CTRL_PORT         GPIOA
//#define RS485_CTRL_PIN          LL_GPIO_PIN_8
//#define RS485_TX_ENABLE()       LL_GPIO_SetOutputPin(RS485_CTRL_PORT, RS485_CTRL_PIN)
//#define RS485_RX_ENABLE()       LL_GPIO_ResetOutputPin(RS485_CTRL_PORT, RS485_CTRL_PIN)

/************************* 数据结构体定�? *************************/

// 设�?�基础信息
// typedef struct {
//     uint8_t  device_name[10];              // 主机设�?�名称，10字节ASCII
//     uint8_t  manufactory_name[20];         // 主机厂商名称�?20字节ASCII
//     uint8_t  software_ver[2];              // 主机�?件版�?�?2字节
//     uint8_t  battery_num;                  // 电池数量
//     uint8_t  battery_barcode[CELL_MAX_NUM][16];// 电池1~16条形�?
// } Battery_Base_Info_T;

// 模拟量数�?
typedef struct {
    // 电池组系统核心参�?
    uint16_t pack_total_avg_voltage;       // 电池组系统总平均电�?
    int16_t pack_total_current;            // 电池组系统总电�?
    uint8_t pack_soc;                      // 电池组系统SOC (State of Charge)
    uint16_t pack_avg_cycle_count;         // 平均�?�?次数
    uint16_t pack_max_cycle_count;         // 最大循�?次数
    uint8_t pack_avg_soh;                  // 平均 SOH (State of Health)
    uint8_t pack_min_soh;                  // 最�? SOH
                                           
    // 电芯电压监测                         
    uint16_t cell_max_voltage;             // 单芯最高电�?
    uint16_t cell_max_voltage_module;      // 单芯最高电压所在模�?
    uint16_t cell_min_voltage;             // 单芯最低电�?
    uint16_t cell_min_voltage_module;      // 单芯最低电压所在模�?
                                           
    // 电芯温度监测                         
    int16_t cell_avg_temp;                 // 单芯平均温度
    int16_t cell_max_temp;                 // 单芯最高温�?
    uint16_t cell_max_temp_module;         // 单芯最高温度所在模�?
    int16_t cell_min_temp;                 // 单芯最低温�?
    uint16_t cell_min_temp_module;         // 单芯最低温度所在模�?
                                           
    // MOSFET 温度监测                     
    int16_t mosfet_avg_temp;               // MOSFET 平均温度
    int16_t mosfet_max_temp;               // MOSFET 最高温�?
    uint16_t mosfet_max_temp_module;       // MOSFET 最高温度所在模�?
    int16_t mosfet_min_temp;               // MOSFET 最低温�?
    uint16_t mosfet_min_temp_module;       // MOSFET 最低温度所在模�?
                                           
    // BMS 板载温度监测                     
    int16_t bms_avg_temp;                  // BMS 平均温度
    int16_t bms_max_temp;                  // BMS 最高温�?
    uint16_t bms_max_temp_module;          // BMS 最高温度所在模�?
    int16_t bms_min_temp;                  // BMS 最低温�?
    uint16_t bms_min_temp_module;          // BMS 最低温度所在模�?
} Battery_Analog_T;   
   
// 告�?�状态信�?  
typedef struct {   
    uint8_t system_alarm1;                 // 系统告�?�状�? 1         
    uint8_t system_alarm2;                 // 系统告�?�状�? 2     
    uint8_t system_protect1;               // 系统保护状�? 1    
    uint8_t system_protect2;               // 系统保护状�? 2                             
} Battery_Alarm_T; 
   
// 充放电�?�理信息 
typedef struct {   
    uint16_t charge_volt_limit;            // 充电电压建�??上限，单位mV
    uint16_t discharge_volt_limit;         // 放电电压建�??下限，单位mV
    int16_t  max_charge_current;           // 最大充电电流，单位0.01A
    int16_t  max_discharge_current;        // 最大放电电流，单位0.01A
    uint8_t  charge_dis_status;            // 充放电状�?
} Battery_Charge_Dis_Info_T;

typedef struct {
    uint8_t buffer[ASCII_RX_FRAME_LEN];
    uint16_t length;
    uint16_t expected_length;
} AsciiParser;

void AsciiParser_Reset(AsciiParser *parser);
ProtocolParseResult AsciiParser_ConsumeByte(AsciiParser *parser, uint8_t byte);
uint16_t Ascii_HandleFrame(const uint8_t *rx_buf, uint16_t rx_len, uint8_t *tx_buf, uint16_t tx_capacity);

uint8_t Hex_To_Ascii(uint8_t hex);
uint8_t Ascii_To_Hex(uint8_t ascii);
uint8_t VerToHex(uint8_t *ver);
uint8_t Calc_LCHKSUM(uint16_t lenid);
uint16_t Calc_CHKSUM(uint8_t *data, uint16_t len);
uint16_t Build_LENGTH_Field(uint16_t lenid);
uint8_t Parse_LENGTH_Field(uint16_t length_field, uint16_t *lenid);
uint16_t Build_Response_Frame(uint8_t *tx_buf, uint16_t tx_capacity, uint8_t ver, uint8_t adr, uint8_t rtn, const uint8_t *info_data, uint16_t info_hex_len);

#endif
