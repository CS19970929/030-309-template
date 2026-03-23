#ifndef _ascii_slave_h_
#define _ascii_slave_h_
#include "main.h"

/************************* 协议固定宏定义 *************************/
// 帧首尾固定值
#define SOI                     0x7E       // 帧起始标志
#define EOI                     0x0D       // 帧结束标志
// 固定CID1  
#define CID1_BAT_DATA           0x46       // 电池数据类固定CID1
// 4种核心命令CID2定义
#define CMD_GET_BATTERY_INFO    0x60       // 获取电池组系统基本信息
#define CMD_GET_ANALOG_DATA     0x61       // 获取电池系统运行模拟量信息
#define CMD_GET_ALARM_INFO      0x62       // 获取电池组系统状态告警量信息
#define CMD_GET_CHARGE_DIS_INFO 0x63       // 获取电池组系统充放电管理交互信息
   
// 响应返回码定义 
#define RTN_OK                  0x00       // 正常响应
#define RTN_VER_ERROR           0x01       // 版本错误
#define RTN_CHKSUM_ERROR        0x02       // 整帧校验错误
#define RTN_LCHKSUM_ERROR       0x03       // 长度校验错误
#define RTN_CID2_INVALID        0x04       // CID2命令码无效
#define RTN_FORMAT_ERROR        0x05       // 命令格式错误
#define RTN_DATA_INVALID        0x06       // 数据无效
#define RTN_ADR_ERROR           0x90       // 地址错误
#define RTN_COMM_ERROR          0x91       // 内部通信错误
// 从机配置
#define PROTOCOL_VERSION        0x20       //协议版本号
#define SLAVE_ADDRESS           0x12       // 本机从机地址（协议要求从2开始）
#define MAX_FRAME_LEN           600        // 最大帧长度
#define CELL_MAX_NUM            16         // 最大电芯数量（48V电池16串）
// RS485控制引脚定义
#define RS485_CTRL_PORT         GPIOA
#define RS485_CTRL_PIN          LL_GPIO_PIN_8
#define RS485_TX_ENABLE()       LL_GPIO_SetOutputPin(RS485_CTRL_PORT, RS485_CTRL_PIN)
#define RS485_RX_ENABLE()       LL_GPIO_ResetOutputPin(RS485_CTRL_PORT, RS485_CTRL_PIN)

/************************* 数据结构体定义 *************************/

// 设备基础信息
typedef struct {
    uint8_t  device_name[10];              // 主机设备名称，10字节ASCII
    uint8_t  manufactory_name[20];         // 主机厂商名称，20字节ASCII
    uint8_t  software_ver[2];              // 主机软件版本，2字节
    uint8_t  battery_num;                  // 电池数量
    uint8_t  battery_barcode[CELL_MAX_NUM][16];// 电池1~16条形码
} Battery_Base_Info_T;

// 模拟量数据
typedef struct {
    // 电池组系统核心参数
    uint16_t pack_total_avg_voltage;       // 电池组系统总平均电压
    int16_t pack_total_current;            // 电池组系统总电流
    uint8_t pack_soc;                      // 电池组系统SOC (State of Charge)
    uint16_t pack_avg_cycle_count;         // 平均循环次数
    uint16_t pack_max_cycle_count;         // 最大循环次数
    uint8_t pack_avg_soh;                  // 平均 SOH (State of Health)
    uint8_t pack_min_soh;                  // 最小 SOH
                                           
    // 电芯电压监测                         
    uint16_t cell_max_voltage;             // 单芯最高电压
    uint16_t cell_max_voltage_module;      // 单芯最高电压所在模块
    uint16_t cell_min_voltage;             // 单芯最低电压
    uint16_t cell_min_voltage_module;      // 单芯最低电压所在模块
                                           
    // 电芯温度监测                         
    int16_t cell_avg_temp;                 // 单芯平均温度
    int16_t cell_max_temp;                 // 单芯最高温度
    uint16_t cell_max_temp_module;         // 单芯最高温度所在模块
    int16_t cell_min_temp;                 // 单芯最低温度
    uint16_t cell_min_temp_module;         // 单芯最低温度所在模块
                                           
    // MOSFET 温度监测                     
    int16_t mosfet_avg_temp;               // MOSFET 平均温度
    int16_t mosfet_max_temp;               // MOSFET 最高温度
    uint16_t mosfet_max_temp_module;       // MOSFET 最高温度所在模块
    int16_t mosfet_min_temp;               // MOSFET 最低温度
    uint16_t mosfet_min_temp_module;       // MOSFET 最低温度所在模块
                                           
    // BMS 板载温度监测                     
    int16_t bms_avg_temp;                  // BMS 平均温度
    int16_t bms_max_temp;                  // BMS 最高温度
    uint16_t bms_max_temp_module;          // BMS 最高温度所在模块
    int16_t bms_min_temp;                  // BMS 最低温度
    uint16_t bms_min_temp_module;          // BMS 最低温度所在模块
} Battery_Analog_T;   
   
// 告警状态信息  
typedef struct {   
    uint8_t system_alarm1;                 // 系统告警状态 1         
    uint8_t system_alarm2;                 // 系统告警状态 2     
    uint8_t system_protect1;               // 系统保护状态 1    
    uint8_t system_protect2;               // 系统保护状态 2                             
} Battery_Alarm_T; 
   
// 充放电管理信息 
typedef struct {   
    uint16_t charge_volt_limit;            // 充电电压建议上限，单位mV
    uint16_t discharge_volt_limit;         // 放电电压建议下限，单位mV
    int16_t  max_charge_current;           // 最大充电电流，单位0.01A
    int16_t  max_discharge_current;        // 最大放电电流，单位0.01A
    uint8_t  charge_dis_status;            // 充放电状态
} Battery_Charge_Dis_Info_T;

// 电池全局数据结构体
typedef struct {
    Battery_Base_Info_T     base_info;            // 设备基础信息
    Battery_Analog_T        analog_data;          // 电芯与模拟量数据
    Battery_Alarm_T         alarm_info;           // 告警状态信息
    Battery_Charge_Dis_Info_T charge_dis_info;    // 充放电管理信息
} Battery_Data_T;


extern Battery_Data_T g_battery_data;

uint8_t Hex_To_Ascii(uint8_t hex);
uint8_t Ascii_To_Hex(uint8_t ascii);
uint8_t VerToHex(uint8_t * ver);
uint8_t Calc_LCHKSUM(uint16_t lenid);
uint16_t Calc_CHKSUM(uint8_t *data, uint16_t len);
uint16_t Build_LENGTH_Field(uint16_t lenid);
uint8_t Parse_LENGTH_Field(uint16_t length_field, uint16_t *lenid);
uint16_t Build_Response_Frame(uint8_t *tx_buf, uint8_t ver, uint8_t adr, uint8_t rtn, uint8_t *info_data, uint16_t info_hex_len);
uint16_t Cmd_Handle_Manufactory_Info(uint8_t *tx_buf);
uint16_t Cmd_Handle_Analog_Value(uint8_t *tx_buf);
uint16_t Cmd_Handle_Alarm_Info(uint8_t *tx_buf);
uint16_t Cmd_Handle_Charge_Dis_Info(uint8_t *tx_buf);
void Frame_Parse_Process(uint8_t *rx_buf, uint16_t rx_len, UART_TypeDef *UARTx);

void Ascii_Send_Byte(uint8_t Modbus_byte,UART_TypeDef *UARTx);
void Ascii_Send_NByte(uint8_t *buff,uint16_t len,UART_TypeDef *UARTx);


#endif
