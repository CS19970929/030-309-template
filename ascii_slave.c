#include "main.h"
#include "stdio.h"
#include "ascii_slave.h"
#include "uart.h"
#include "modbus_host.h"
#include "CRC.h"
#include "string.h"
#include "led.h"


/************************* 全局变量定义 *************************/
// 串口接收缓冲区与状态机
uint8_t uart_rx_buf[MAX_FRAME_LEN] = {0};
uint16_t uart_rx_len = 0;
uint8_t frame_received_flag = 0;

void Ascii_slave(void)
{
    
}

/************************* 电池数据初始化（固定默认值） *************************/
Battery_Data_T g_battery_data = 
{
    // 基础信息
    .base_info = {
        
        .device_name = {'F', 'o', 'r', 'c', 'e', '_', 'L', 0, 0, 0},//Force_L
        .manufactory_name = {'P','y','l','o','n',0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},   
        .software_ver = {0, 9},//9
        .battery_num = CELL_MAX_NUM,
        .battery_barcode = {
            {0x30,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "0123456789abcdef"
            {0x31,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "1123456789abcdef"
            {0x32,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "2123456789abcdef"
            {0x33,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "3123456789abcdef"
            {0x34,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "4123456789abcdef"
            {0x35,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "5123456789abcdef"
            {0x36,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "6123456789abcdef"
            {0x37,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "7123456789abcdef"
            {0x38,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "8123456789abcdef"
            {0x39,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "9123456789abcdef"
            {0x61,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "a123456789abcdef"
            {0x62,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "b123456789abcdef"
            {0x63,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "c123456789abcdef"
            {0x64,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "d123456789abcdef"
            {0x65,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "e123456789abcdef"
            {0x66,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}  // "f123456789abcdef"
        }   
    },
    // 模拟量数据（匹配协议示例值）
    .analog_data = {
        // 电池组系统核心参数
        .pack_total_avg_voltage = 0x2E53,       // 电池组系统总平均电压
        .pack_total_current = 0x61A8,           // 电池组系统总电流
        .pack_soc = 0x62,                       // 电池组系统SOC (State of Charge)
        .pack_avg_cycle_count = 0x09D4,         // 平均循环次数
        .pack_max_cycle_count = 0x0B74,         // 最大循环次数
        .pack_avg_soh = 0x62,                   // 平均 SOH (State of Health)
        .pack_min_soh = 0x61,                   // 最小 SOH                                   .                        
        
        // 电芯电压监测 
        .cell_max_voltage = 0x0DB8,             // 单芯最高电压
        .cell_max_voltage_module = 0x0304,      // 单芯最高电压所在模块
        .cell_min_voltage = 0x0CBB,             // 单芯最低电压
        .cell_min_voltage_module = 0x0104,      // 单芯最低电压所在模块
                                       
        // 电芯温度监测                         
        .cell_avg_temp = 0x0BAA,                // 单芯平均温度
        .cell_max_temp = 0x0BB7,                // 单芯最高温度
        .cell_max_temp_module = 0x0305,         // 单芯最高温度所在模块
        .cell_min_temp = 0x0B9D,                // 单芯最低温度
        .cell_min_temp_module = 0x0105,         // 单芯最低温度所在模块
                                           
        // MOSFET 温度监测                     
        .mosfet_avg_temp = 0x0BAA,              // MOSFET 平均温度
        .mosfet_max_temp = 0x0BB8,              // MOSFET 最高温度
        .mosfet_max_temp_module = 0x0306,       // MOSFET 最高温度所在模块
        .mosfet_min_temp = 0x0B9C,              // MOSFET 最低温度
        .mosfet_min_temp_module = 0x0106,       // MOSFET 最低温度所在模块
                                            
        // BMS 板载温度监测                     
        .bms_avg_temp = 0x0BAA,                 // BMS 平均温度
        .bms_max_temp = 0x0BB6,                 // BMS 最高温度
        .bms_max_temp_module = 0x0307,          // BMS 最高温度所在模块
        .bms_min_temp = 0x0B9E,                 // BMS 最低温度
        .bms_min_temp_module = 0x0107           // BMS 最低温度所在模块
    },
    // 告警信息（默认全正常）
    .alarm_info = {
        .system_alarm1 = 0x0,  
        .system_alarm2 = 0x0,  
        .system_protect1 = 0x0,
        .system_protect2 = 0x0    
    },
    // 充放电管理信息
    .charge_dis_info = {
        .charge_volt_limit = 0xDCD3, // 56.531V
        .discharge_volt_limit = 0x5DC0, // 24.00V
        .max_charge_current = 0x09C4, // 25.0A
        .max_discharge_current = 0x07E4, // 20.2A
        .charge_dis_status = 0xC0 // 允许充放电
    }
};


/************************* 工具函数实现 *************************/
/**
 * @brief  HEX半字节转ASCII码
 * @param  hex: 0-15的半字节数据
 * @retval 转换后的ASCII码
 */
uint8_t Hex_To_Ascii(uint8_t hex)
{
    hex &= 0x0F;
    if(hex < 10) return hex + '0';
    else return hex - 10 + 'A';
}

/**
 * @brief  ASCII码转HEX半字节
 * @param  ascii: 0-9/A-F的ASCII码
 * @retval 转换后的HEX半字节（0-15），失败返回0xFF
 */
uint8_t Ascii_To_Hex(uint8_t ascii)
{
    if(ascii >= '0' && ascii <= '9') return ascii - '0';
    else if(ascii >= 'A' && ascii <= 'F') return ascii - 'A' + 10;
    else if(ascii >= 'a' && ascii <= 'f') return ascii - 'a' + 10;
    else return 0xFF;
}

/**
 * @brief  将2字节的版本号转成一字节HEX
 * @param  版本号
 * @retval 一字节HEX形式的版本号
 */
uint8_t VerToHex(uint8_t * ver)
{
    if(ver == NULL)
    {
        return 0;
    }
    
    uint8_t hex = 0;
    for(int i = 0; i < 2; i++)
    {
        hex = (16 * hex) + (ver[i] - 0x30); 
    }
    
    return hex;
}

/**
 * @brief  计算LCHKSUM长度校验码
 * @param  lenid: 12位的INFO ASCII字节数
 * @retval 4位LCHKSUM校验值
 */
uint8_t Calc_LCHKSUM(uint16_t lenid)
{
    uint8_t seg1 = (lenid >> 8) & 0x0F; // D11-D8
    uint8_t seg2 = (lenid >> 4) & 0x0F; // D7-D4
    uint8_t seg3 = lenid & 0x0F;         // D3-D0
    uint8_t sum = seg1 + seg2 + seg3;
    sum = sum % 16;
    return (~sum + 1) & 0x0F;
}

/**
 * @brief  计算CHKSUM整帧校验码
 * @param  data: 待校验的数据（除SOI、EOI、CHKSUM外的所有ASCII字符）
 * @param  len: 数据长度
 * @retval 16位CHKSUM校验值
 */
uint16_t Calc_CHKSUM(uint8_t *data, uint16_t len)
{
    uint32_t sum = 0;
    for(uint16_t i = 0; i < len; i++)
    {
        sum += data[i];
    }
    sum = sum % 65536;
    return (uint16_t)(~sum + 1);
}

/**
 * @brief  组装LENGTH字段（2字节HEX）
 * @param  lenid: INFO的ASCII字节数
 * @retval 16位LENGTH字段值
 */
uint16_t Build_LENGTH_Field(uint16_t lenid)
{
    uint8_t lchksum = Calc_LCHKSUM(lenid);
    return (uint16_t)((lchksum << 12) | (lenid & 0x0FFF));
}

/**
 * @brief  解析LENGTH字段，校验LCHKSUM
 * @param  length_field: 16位LENGTH字段
 * @param  lenid: 输出解析后的LENID
 * @retval true=校验通过，false=校验失败
 */
uint8_t Parse_LENGTH_Field(uint16_t length_field, uint16_t *lenid)
{
    uint8_t lchksum_rx = (length_field >> 12) & 0x0F;
    *lenid = length_field & 0x0FFF;
    uint8_t lchksum_calc = Calc_LCHKSUM(*lenid);
    return (lchksum_rx == lchksum_calc);
}

/************************* 命令处理函数实现 *************************/
/**
 * @brief  通用响应帧组装函数
 * @param  tx_buf: 发送缓冲区
 * @param  ver: 协议版本号
 * @param  adr: 从机地址
 * @param  rtn: 响应返回码
 * @param  info_data: INFO域的HEX数据
 * @param  info_hex_len: INFO域HEX数据长度（字节数）
 * @retval 组装完成的帧总长度
 */
uint16_t Build_Response_Frame(uint8_t *tx_buf, uint8_t ver, uint8_t adr, uint8_t rtn, uint8_t *info_data, uint16_t info_hex_len)
{
    uint16_t idx = 0;
    // 1. 帧起始SOI
    tx_buf[idx++] = SOI;
    // 2. VER和ADR（转ASCII）
    tx_buf[idx++] = Hex_To_Ascii(ver >> 4);
    tx_buf[idx++] = Hex_To_Ascii(ver & 0x0F);
    tx_buf[idx++] = Hex_To_Ascii(adr >> 4);
    tx_buf[idx++] = Hex_To_Ascii(adr & 0x0F);
    // 3. CID1固定0x46（转ASCII）
    tx_buf[idx++] = Hex_To_Ascii(CID1_BAT_DATA >> 4);
    tx_buf[idx++] = Hex_To_Ascii(CID1_BAT_DATA & 0x0F);
    // 4. RTN响应码（转ASCII）
    tx_buf[idx++] = Hex_To_Ascii(rtn >> 4);
    tx_buf[idx++] = Hex_To_Ascii(rtn & 0x0F);
    // 5. 计算LENGTH字段
    uint16_t lenid = info_hex_len * 2; // INFO的ASCII字节数=HEX长度*2
    uint16_t length_field = Build_LENGTH_Field(lenid);
    tx_buf[idx++] = Hex_To_Ascii(length_field >> 12);
    tx_buf[idx++] = Hex_To_Ascii((length_field >> 8) & 0x0F);
    tx_buf[idx++] = Hex_To_Ascii((length_field >> 4) & 0x0F);
    tx_buf[idx++] = Hex_To_Ascii(length_field & 0x0F);
    
    // 6. 填充INFO域（HEX转ASCII）
    for(uint16_t i = 0; i < info_hex_len; i++)
    {
        tx_buf[idx++] = Hex_To_Ascii((info_data[i] >> 4) & 0x0F);
        tx_buf[idx++] = Hex_To_Ascii(info_data[i] &0x0F);
    }
    
    // 7. 计算CHKSUM（SOI之后，CHKSUM之前的所有ASCII字符）
    uint16_t chksum = Calc_CHKSUM(&tx_buf[1], idx - 1);
    tx_buf[idx++] = Hex_To_Ascii(chksum >> 12);
    tx_buf[idx++] = Hex_To_Ascii((chksum >> 8) & 0x0F);
    tx_buf[idx++] = Hex_To_Ascii((chksum >> 4) & 0x0F);
    tx_buf[idx++] = Hex_To_Ascii(chksum & 0x0F);
    // 8. 帧结束EOI
    tx_buf[idx++] = EOI;
    return idx;
}

/**
 * @brief  0x60 获取厂商信息处理
 */
uint16_t Cmd_Handle_Manufactory_Info(uint8_t *tx_buf)
{
    uint8_t info_buf[MAX_FRAME_LEN] = {0};
    uint16_t idx = 0; 

    // 设备名称10字节
    memcpy(&info_buf[idx], g_battery_data.base_info.device_name, 10);
    idx += 10;
    // 厂商名称20字节
    memcpy(&info_buf[idx], g_battery_data.base_info.manufactory_name, 20);
    idx += 20;
    // 软件版本2字节
    memcpy(&info_buf[idx], g_battery_data.base_info.software_ver, 2);
    idx += 2;
    // 电池数量 
    info_buf[idx++] = g_battery_data.base_info.battery_num;
    // 条形码
    uint16_t i;
    for(i = 0; i < g_battery_data.base_info.battery_num; i++)
    {
        memcpy(&info_buf[idx], g_battery_data.base_info.battery_barcode[i], 16);
        idx += 16;
    }
    
    return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

/**
 * @brief  0x61 获取模拟量量化数据处理
 */
uint16_t Cmd_Handle_Analog_Value(uint8_t *tx_buf)
{
    uint8_t info_buf[100] = {0};
    uint16_t idx = 0;
    
    // 电池组系统总平均电压
    info_buf[idx++] = (g_battery_data.analog_data.pack_total_avg_voltage >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.pack_total_avg_voltage & 0xFF;
    // 电池组系统总电流
    info_buf[idx++] = (g_battery_data.analog_data.pack_total_current >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.pack_total_current & 0xFF;
    // 电池组系统 SOC
    info_buf[idx++] = g_battery_data.analog_data.pack_soc;
    // 平均循环次数
    info_buf[idx++] = (g_battery_data.analog_data.pack_avg_cycle_count >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.pack_avg_cycle_count & 0xFF;
    // 最大循环次数
    info_buf[idx++] = (g_battery_data.analog_data.pack_max_cycle_count >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.pack_max_cycle_count & 0xFF;
    // 平均 SOH (State of Health)
    info_buf[idx++] = g_battery_data.analog_data.pack_avg_soh;
    // 最小 SOH
    info_buf[idx++] = g_battery_data.analog_data.pack_min_soh;
    // 单芯最高电压
    info_buf[idx++] = (g_battery_data.analog_data.cell_max_voltage >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_max_voltage & 0xFF;
    // 单芯最高电压所在模块
    info_buf[idx++] = (g_battery_data.analog_data.cell_max_voltage_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_max_voltage_module & 0xFF;
    // 单芯最低电压
    info_buf[idx++] = (g_battery_data.analog_data.cell_min_voltage >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_min_voltage & 0xFF;
    // 单芯最低电压所在模块
    info_buf[idx++] = (g_battery_data.analog_data.cell_min_voltage_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_min_voltage_module & 0xFF;
    // 单芯平均温度
    info_buf[idx++] = (g_battery_data.analog_data.cell_avg_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_avg_temp & 0xFF;
    // 单芯最高温度
    info_buf[idx++] = (g_battery_data.analog_data.cell_max_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_max_temp & 0xFF;
    // 单芯最高温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data.cell_max_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_max_temp_module & 0xFF;
    // 单芯最低温度
    info_buf[idx++] = (g_battery_data.analog_data.cell_min_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_min_temp & 0xFF;
    // 单芯最低温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data.cell_min_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.cell_min_temp_module & 0xFF;
    // MOSFET 平均温度
    info_buf[idx++] = (g_battery_data.analog_data.mosfet_avg_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.mosfet_avg_temp & 0xFF;
    // MOSFET 最高温度
    info_buf[idx++] = (g_battery_data.analog_data.mosfet_max_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.mosfet_max_temp & 0xFF;
    // MOSFET 最高温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data.mosfet_max_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.mosfet_max_temp_module & 0xFF;
    // MOSFET 最低温度
    info_buf[idx++] = (g_battery_data.analog_data.mosfet_min_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.mosfet_min_temp & 0xFF;
    // MOSFET 最低温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data.mosfet_min_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.mosfet_min_temp_module & 0xFF;
    // BMS 平均温度
    info_buf[idx++] = (g_battery_data.analog_data.bms_avg_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.bms_avg_temp & 0xFF;
    // BMS 最高温度
    info_buf[idx++] = (g_battery_data.analog_data.bms_max_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.bms_max_temp & 0xFF;
    // BMS 最高温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data.bms_max_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.bms_max_temp_module & 0xFF;
    // BMS 最低温度
    info_buf[idx++] = (g_battery_data.analog_data.bms_min_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.bms_min_temp & 0xFF;
    // BMS 最低温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data.bms_min_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data.bms_min_temp_module & 0xFF;
    
    return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

/**
 * @brief  0x62 获取电池组系统状态告警量信息
 */
uint16_t Cmd_Handle_Alarm_Info(uint8_t *tx_buf)
{
    uint8_t info_buf[16] = {0};
    uint16_t idx = 0;
    Battery_Alarm_T *p = &g_battery_data.alarm_info;
    
    // 系统告警状态 1
    info_buf[idx++] = p->system_alarm1;
    // 系统告警状态 2
    info_buf[idx++] = p->system_alarm2;
    // 系统保护状态 1
    info_buf[idx++] = p->system_protect1;
    // 系统保护状态 2
    info_buf[idx++] = p->system_protect2;
    
    return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

/**
 * @brief  0x63 获取电池组系统充放电管理交互信息
 */
uint16_t Cmd_Handle_Charge_Dis_Info(uint8_t *tx_buf)
{
    uint8_t info_buf[16] = {0};
    uint16_t idx = 0;
    Battery_Charge_Dis_Info_T *p = &g_battery_data.charge_dis_info;
    
    // 充电电压建议上限
    info_buf[idx++] = (p->charge_volt_limit >> 8) & 0xFF;
    info_buf[idx++] = p->charge_volt_limit & 0xFF;
    // 放电电压建议下限
    info_buf[idx++] = (p->discharge_volt_limit >> 8) & 0xFF;
    info_buf[idx++] = p->discharge_volt_limit & 0xFF;
    // 最大充电电流
    info_buf[idx++] = (p->max_charge_current >> 8) & 0xFF;
    info_buf[idx++] = p->max_charge_current & 0xFF;
    // 最大放电电流
    info_buf[idx++] = (p->max_discharge_current >> 8) & 0xFF;
    info_buf[idx++] = p->max_discharge_current & 0xFF;
    // 充放电状态
    info_buf[idx++] = p->charge_dis_status;
    return Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_OK, info_buf, idx);
}

/**
 * @brief  接收帧解析与命令分发
 */
void Frame_Parse_Process(void)
{
    if(!frame_received_flag) return;
    frame_received_flag = 0;
    uint8_t *rx_buf = uart_rx_buf;
    uint16_t rx_len = uart_rx_len;
    uint8_t tx_buf[MAX_FRAME_LEN] = {0};
    uint16_t tx_len = 0;
    // 最小帧长度校验（SOI+VER+ADR+CID1+CID2+LENGTH+CHKSUM+EOI = 1+2+2+2+2+4+4+1=18字节）
    if(rx_len < 18)
    {
        return;
    }
    // 1. 解析基础字段（ASCII转HEX）
    uint8_t ver = (Ascii_To_Hex(rx_buf[1]) << 4) | Ascii_To_Hex(rx_buf[2]);
    uint8_t adr = (Ascii_To_Hex(rx_buf[3]) << 4) | Ascii_To_Hex(rx_buf[4]);
    uint8_t cid1 = (Ascii_To_Hex(rx_buf[5]) << 4) | Ascii_To_Hex(rx_buf[6]);
    uint8_t cid2 = (Ascii_To_Hex(rx_buf[7]) << 4) | Ascii_To_Hex(rx_buf[8]);
    uint16_t length_field = (Ascii_To_Hex(rx_buf[9]) << 12) | (Ascii_To_Hex(rx_buf[10]) << 8) | (Ascii_To_Hex(rx_buf[11]) << 4) | Ascii_To_Hex(rx_buf[12]);
    // 2. 地址校验：只处理本机地址
    if(adr != SLAVE_ADDRESS)
    {
        tx_len = Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_ADR_ERROR, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 3. CID1校验
    if(cid1 != CID1_BAT_DATA)
    {
        tx_len = Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_CID2_INVALID, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 4. LENGTH字段校验
    uint16_t lenid = 0;
    if(!Parse_LENGTH_Field(length_field, &lenid))
    {
        tx_len = Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_LCHKSUM_ERROR, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 5. 帧长度校验
    uint16_t expect_len = 13 + lenid + 4 + 1; // SOI+基础字段+INFO+CHKSUM+EOI
    if(rx_len != expect_len)
    {
        tx_len = Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_FORMAT_ERROR, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 6. CHKSUM整帧校验
    uint16_t chksum_rx = (Ascii_To_Hex(rx_buf[13+lenid]) << 12) | (Ascii_To_Hex(rx_buf[13+lenid+1]) << 8) | (Ascii_To_Hex(rx_buf[13+lenid+2]) << 4) | Ascii_To_Hex(rx_buf[13+lenid+3]);
    uint16_t chksum_calc = Calc_CHKSUM(&rx_buf[1], 12 + lenid);
    if(chksum_rx != chksum_calc)
    {
        tx_len = Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_CHKSUM_ERROR, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 7. 命令分发处理
    switch(cid2)
    {
        case CMD_GET_BATTERY_INFO:
            tx_len = Cmd_Handle_Manufactory_Info(tx_buf);
            break;
        case CMD_GET_ANALOG_DATA:
            tx_len = Cmd_Handle_Analog_Value(tx_buf);
            break;
        case CMD_GET_ALARM_INFO:
            tx_len = Cmd_Handle_Alarm_Info(tx_buf);
            break;
        case CMD_GET_CHARGE_DIS_INFO:
            tx_len = Cmd_Handle_Charge_Dis_Info(tx_buf);
            break;
        default:
            tx_len = Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_CID2_INVALID, NULL, 0);
            break;
    }

    // 8. 发送响应帧
    if(tx_len > 0)
    {
        LED_L4851_ON();
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
    }
    // 清空接收缓冲区
    memset(uart_rx_buf, 0, MAX_FRAME_LEN);
    uart_rx_len = 0;
}

//Ascii串口发送一个字节数据
void Ascii_Send_Byte(uint8_t Modbus_byte,UART_TypeDef *UARTx)
{  
	LL_UART_TransmitData8(UARTx, Modbus_byte);   
	while (!LL_UART_IsActiveFlag_TXCF(UARTx));
	LL_UART_ClearFlag_TXCF(UARTx);                 
}     


//Ascii串口发送N个字节数据
void Ascii_Send_NByte(uint8_t *buff,uint16_t len,UART_TypeDef *UARTx)
{    
	uint16_t t;
    RS485_TX_ENABLE();
	LL_mDelay(1); // 确保DE引脚稳定
	for(t=0;t<len;t++)
	{
		while (!LL_UART_IsActiveFlag_TXEF(UARTx));
		LL_UART_TransmitData8(UARTx, buff[t]);   
		while (!LL_UART_IsActiveFlag_TXCF(UARTx));
		LL_UART_ClearFlag_TXCF(UARTx);
	}		
	LL_mDelay(1); // 确保DE引脚稳定
    RS485_RX_ENABLE();
}


