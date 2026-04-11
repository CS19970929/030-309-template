#include "main.h"
#include "ascii_slave.h"
#include "string.h"

uint8_t Adress;
uint8_t Version;

/************************* 全局变量定义 *************************/
// 串口接收缓冲区与状态机
uint8_t uart_rx_buf[MAX_FRAME_LEN] = {0};
uint16_t uart_rx_len = 0;
uint8_t frame_received_flag = 0;
uint8_t tx_buf[MAX_FRAME_LEN] = {0};
static uint16_t ascii_expect_len = 0;
static int32_t ascii_rx_last_byte_time = 0;

#define ASCII_RX_INTER_BYTE_TIMEOUT_MS    50U

/************************* 电池数据初始化（固定默认值） *************************/
Battery_Data_T g_battery_data = 
{
//    // 基础信息
//    .base_info = {
//        
//        .device_name = {'F', 'o', 'r', 'c', 'e', '_', 'L', 0, 0, 0},//Force_L
//        .manufactory_name = {'P','y','l','o','n',0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},   
//        .software_ver = {0, 9},//9
//        .battery_num = CELL_MAX_NUM,
//        .battery_barcode = {
//            {0x30,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "0123456789abcdef"
//            {0x31,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "1123456789abcdef"
//            {0x32,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "2123456789abcdef"
//            {0x33,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "3123456789abcdef"
//            {0x34,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "4123456789abcdef"
//            {0x35,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "5123456789abcdef"
//            {0x36,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "6123456789abcdef"
//            {0x37,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "7123456789abcdef"
//            {0x38,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "8123456789abcdef"
//            {0x39,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "9123456789abcdef"
//            {0x61,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "a123456789abcdef"
//            {0x62,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "b123456789abcdef"
//            {0x63,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "c123456789abcdef"
//            {0x64,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "d123456789abcdef"
//            {0x65,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}, // "e123456789abcdef"
//            {0x66,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x61,0x62,0x63,0x64,0x65,0x66}  // "f123456789abcdef"
//        }   
//    },
    // 模拟量数据（匹配协议示例值）
    .analog_data1 = {
        // 电池组系统核心参数
        .pack_total_avg_voltage = 0xCF08,       // 电池组系统总平均电压
        .pack_total_current = 0x00C9,           // 电池组系统总电流
        .pack_soc = 0x59,                       // 电池组系统SOC (State of Charge)
        .pack_avg_cycle_count = 0x0,            // 平均循环次数
        .pack_max_cycle_count = 0x07D0,         // 最大循环次数
        .pack_avg_soh = 0x64,                   // 平均 SOH (State of Health)
        .pack_min_soh = 0x64,                   // 最小 SOH                                   .                        
        
        // 电芯电压监测 
        .cell_max_voltage = 0x0CF4,             // 单芯最高电压
        .cell_max_voltage_module = 0x0102,      // 单芯最高电压所在模块
        .cell_min_voltage = 0x0CEF,             // 单芯最低电压
        .cell_min_voltage_module = 0x0301,      // 单芯最低电压所在模块
                                       
        // 电芯温度监测                         
        .cell_avg_temp = 0x0BDA,                // 单芯平均温度
        .cell_max_temp = 0x0BDB,                // 单芯最高温度
        .cell_max_temp_module = 0x0101,         // 单芯最高温度所在模块
        .cell_min_temp = 0x0BD9,                // 单芯最低温度
        .cell_min_temp_module = 0x0201,         // 单芯最低温度所在模块
                                           
        // MOSFET 温度监测                     
        .mosfet_avg_temp = 0x0C0A,              // MOSFET 平均温度
        .mosfet_max_temp = 0x0000,              // MOSFET 最高温度
        .mosfet_max_temp_module = 0x0000,       // MOSFET 最高温度所在模块
        .mosfet_min_temp = 0x0000,              // MOSFET 最低温度
        .mosfet_min_temp_module = 0x0000,       // MOSFET 最低温度所在模块
                                            
        // BMS 板载温度监测                     
        .bms_avg_temp = 0x0000,                 // BMS 平均温度
        .bms_max_temp = 0x0000,                 // BMS 最高温度
        .bms_max_temp_module = 0x0000,          // BMS 最高温度所在模块
        .bms_min_temp = 0x0000,                 // BMS 最低温度
        .bms_min_temp_module = 0x0000           // BMS 最低温度所在模块
    },
    // 模拟量数据（匹配协议示例值）
    .analog_data2 = {
        .cell_num = CELL_MAX_NUM,
        .cell_voltage = {3397, 3396, 3397, 3396, 3397, 3396, 3397, 3396, 3400, 3400, 3402, 3400, 3400, 3400, 3400, 3399},
        .temp_num = TEMP_MAX_NUM,
        .temp_value = {2986, 2986, 2986, 2986, 3021, 2996}, // 25.5℃、25.5℃、25.5℃、25.5℃、29℃、26.5℃
        .current = 0, // 0A
        .module_voltage = 50981, // 50.981V
        .remain_capacity = 49000, // 49Ah
        .user_define_num = 3,    // 用户自定义个数
        .total_capacity = 50000, // 50Ah
        .cycle_num = 2,
        .full_charge_capacity = 50000  //  5Ah
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
        .charge_volt_limit = 0xEA60, // 60.000V
        .discharge_volt_limit = 0x8980, // 35.200V
        .max_charge_current = 0x55F0, // 22.000A
        .max_discharge_current = 0x55F0, // 22.000A
        .charge_dis_status = 0xC0 // 允许充放电
    }
};

static uint16_t Ascii_Temp_To_TenthKelvin(uint16_t temp_raw)
{
    if(temp_raw == 0U)
    {
        return 0U;
    }

    return (uint16_t)(temp_raw + 2331U);
}

static int16_t Ascii_GetSignedPackCurrentA10(void)
{
    if(g_stCellInfoReport.u16Ichg > 0U)
    {
        return (int16_t)g_stCellInfoReport.u16Ichg;
    }

    return -(int16_t)g_stCellInfoReport.u16IDischg;
}

static uint16_t Ascii_BuildModulePosition(uint16_t position)
{
    if(position == 0U)
    {
        return 0U;
    }

    return (uint16_t)(0x0100U | (position & 0x00FFU));
}

static uint8_t Ascii_ParseHexByte(uint8_t high_ascii, uint8_t low_ascii, uint8_t *value)
{
    uint8_t high = Ascii_To_Hex(high_ascii);
    uint8_t low = Ascii_To_Hex(low_ascii);

    if((high == 0xFFU) || (low == 0xFFU) || (value == NULL))
    {
        return 0U;
    }

    *value = (uint8_t)((high << 4) | low);
    return 1U;
}

static uint8_t Ascii_Slave_IsRxTimeout(void)
{
    if((uart_rx_len == 0U) || (frame_received_flag != 0U))
    {
        return 0U;
    }

    if(bsp_CheckRunTime(ascii_rx_last_byte_time) > (int32_t)ASCII_RX_INTER_BYTE_TIMEOUT_MS)
    {
        return 1U;
    }

    return 0U;
}

static void Ascii_RefreshAnalog1Temps(void)
{
    uint32_t sum = 0U;
    uint16_t count = 0U;
    uint16_t max_temp = 0U;
    uint16_t min_temp = 0xFFFFU;
    uint16_t max_index = 0U;
    uint16_t min_index = 0U;
    uint16_t i;

    for(i = 0U; i < TEMP_MAX_NUM; i++)
    {
        uint16_t temp = Ascii_Temp_To_TenthKelvin(g_stCellInfoReport.u16Temperature[i]);
        g_battery_data.analog_data2.temp_value[i] = temp;
        if(temp == 0U)
        {
            continue;
        }

        sum += temp;
        count++;
        if(temp > max_temp)
        {
            max_temp = temp;
            max_index = (uint16_t)(i + 1U);
        }
        if(temp < min_temp)
        {
            min_temp = temp;
            min_index = (uint16_t)(i + 1U);
        }
    }

    if(count == 0U)
    {
        g_battery_data.analog_data1.cell_avg_temp = 0;
        g_battery_data.analog_data1.cell_max_temp = 0;
        g_battery_data.analog_data1.cell_min_temp = 0;
        g_battery_data.analog_data1.cell_max_temp_module = 0;
        g_battery_data.analog_data1.cell_min_temp_module = 0;
    }
    else
    {
        g_battery_data.analog_data1.cell_avg_temp = (int16_t)(sum / count);
        g_battery_data.analog_data1.cell_max_temp = (int16_t)max_temp;
        g_battery_data.analog_data1.cell_min_temp = (int16_t)min_temp;
        g_battery_data.analog_data1.cell_max_temp_module = Ascii_BuildModulePosition(max_index);
        g_battery_data.analog_data1.cell_min_temp_module = Ascii_BuildModulePosition(min_index);
    }
}

static void Ascii_RefreshEnvTemps(void)
{
    uint16_t env_indexes[3] = {ENV_TEMP1, ENV_TEMP2, ENV_TEMP3};
    uint32_t sum = 0U;
    uint16_t count = 0U;
    uint16_t max_temp = 0U;
    uint16_t min_temp = 0xFFFFU;
    uint16_t max_index = 0U;
    uint16_t min_index = 0U;
    uint16_t i;

    for(i = 0U; i < 3U; i++)
    {
        uint16_t temp = Ascii_Temp_To_TenthKelvin(g_stCellInfoReport.u16Temperature[env_indexes[i]]);
        if(temp == 0U)
        {
            continue;
        }

        sum += temp;
        count++;
        if(temp > max_temp)
        {
            max_temp = temp;
            max_index = (uint16_t)(i + 1U);
        }
        if(temp < min_temp)
        {
            min_temp = temp;
            min_index = (uint16_t)(i + 1U);
        }
    }

    if(count == 0U)
    {
        g_battery_data.analog_data1.bms_avg_temp = 0;
        g_battery_data.analog_data1.bms_max_temp = 0;
        g_battery_data.analog_data1.bms_min_temp = 0;
        g_battery_data.analog_data1.bms_max_temp_module = 0;
        g_battery_data.analog_data1.bms_min_temp_module = 0;
    }
    else
    {
        g_battery_data.analog_data1.bms_avg_temp = (int16_t)(sum / count);
        g_battery_data.analog_data1.bms_max_temp = (int16_t)max_temp;
        g_battery_data.analog_data1.bms_min_temp = (int16_t)min_temp;
        g_battery_data.analog_data1.bms_max_temp_module = Ascii_BuildModulePosition(max_index);
        g_battery_data.analog_data1.bms_min_temp_module = Ascii_BuildModulePosition(min_index);
    }

    {
        uint16_t mos_temp = Ascii_Temp_To_TenthKelvin(g_stCellInfoReport.u16Temperature[MOS_TEMP1]);
        g_battery_data.analog_data1.mosfet_avg_temp = (int16_t)mos_temp;
        g_battery_data.analog_data1.mosfet_max_temp = (int16_t)mos_temp;
        g_battery_data.analog_data1.mosfet_min_temp = (int16_t)mos_temp;
        g_battery_data.analog_data1.mosfet_max_temp_module = (mos_temp == 0U) ? 0U : Ascii_BuildModulePosition(1U);
        g_battery_data.analog_data1.mosfet_min_temp_module = (mos_temp == 0U) ? 0U : Ascii_BuildModulePosition(1U);
    }
}

static void Ascii_Slave_RefreshBatteryData(void)
{
    uint16_t cell_num;
    int16_t pack_current_a10;
    uint16_t i;

    cell_num = OtherElement.u16Sys_SeriesNum;
    if(cell_num == 0U)
    {
        cell_num = CELL_MAX_NUM;
    }
    if(cell_num > CELL_MAX_NUM)
    {
        cell_num = CELL_MAX_NUM;
    }

    pack_current_a10 = Ascii_GetSignedPackCurrentA10();

    g_battery_data.analog_data1.pack_total_avg_voltage = (uint16_t)(g_stCellInfoReport.u16VCellTotle * 10U);
    g_battery_data.analog_data1.pack_total_current = pack_current_a10;
    g_battery_data.analog_data1.pack_soc = (uint8_t)g_stCellInfoReport.SocElement.u16Soc;
    g_battery_data.analog_data1.pack_avg_cycle_count = g_stCellInfoReport.SocElement.u16Cycle_times;
    g_battery_data.analog_data1.pack_max_cycle_count = g_stCellInfoReport.SocElement.u16Cycle_times;
    g_battery_data.analog_data1.pack_avg_soh = (uint8_t)g_stCellInfoReport.SocElement.u16Soh;
    g_battery_data.analog_data1.pack_min_soh = (uint8_t)g_stCellInfoReport.SocElement.u16Soh;
    g_battery_data.analog_data1.cell_max_voltage = g_stCellInfoReport.u16VCellMax;
    g_battery_data.analog_data1.cell_max_voltage_module = Ascii_BuildModulePosition(g_stCellInfoReport.u16VCellMaxPosition);
    g_battery_data.analog_data1.cell_min_voltage = g_stCellInfoReport.u16VCellMin;
    g_battery_data.analog_data1.cell_min_voltage_module = Ascii_BuildModulePosition(g_stCellInfoReport.u16VCellMinPosition);

    g_battery_data.analog_data2.cell_num = (uint8_t)cell_num;
    g_battery_data.analog_data2.temp_num = TEMP_MAX_NUM;
    g_battery_data.analog_data2.current = (int16_t)(pack_current_a10 * 10);
    g_battery_data.analog_data2.module_voltage = (uint16_t)(g_stCellInfoReport.u16VCellTotle * 10U);
    g_battery_data.analog_data2.remain_capacity = (uint16_t)(g_stCellInfoReport.SocElement.u16CapacityNow * 10U);
    g_battery_data.analog_data2.user_define_num = 3U;
    g_battery_data.analog_data2.total_capacity = (uint16_t)(g_stCellInfoReport.SocElement.u16CapacityFactory * 10U);
    g_battery_data.analog_data2.cycle_num = g_stCellInfoReport.SocElement.u16Cycle_times;
    g_battery_data.analog_data2.full_charge_capacity = (uint16_t)(g_stCellInfoReport.SocElement.u16CapacityFull * 10U);

    for(i = 0U; i < CELL_MAX_NUM; i++)
    {
        g_battery_data.analog_data2.cell_voltage[i] = (i < cell_num) ? g_stCellInfoReport.u16VCell[i] : 0U;
    }

    Ascii_RefreshAnalog1Temps();
    Ascii_RefreshEnvTemps();

    g_battery_data.alarm_info.system_alarm1 = (uint8_t)(g_stCellInfoReport.unMdlFault_Second.all & 0x00FFU);
    g_battery_data.alarm_info.system_alarm2 = (uint8_t)((g_stCellInfoReport.unMdlFault_Second.all >> 8) & 0x00FFU);
    g_battery_data.alarm_info.system_protect1 = (uint8_t)(g_stCellInfoReport.unMdlFault_Third.all & 0x00FFU);
    g_battery_data.alarm_info.system_protect2 = (uint8_t)((g_stCellInfoReport.unMdlFault_Third.all >> 8) & 0x00FFU);

    g_battery_data.charge_dis_info.charge_volt_limit = (uint16_t)(PRT_E2ROMParas.u16VbusOvp_Third * 10U);
    g_battery_data.charge_dis_info.discharge_volt_limit = (uint16_t)(PRT_E2ROMParas.u16VbusUvp_Third * 10U);
    g_battery_data.charge_dis_info.max_charge_current = (int16_t)OtherElement.u16CS_Cur_CHGmax;
    g_battery_data.charge_dis_info.max_discharge_current = (int16_t)OtherElement.u16CS_Cur_DSGmax;
    g_battery_data.charge_dis_info.charge_dis_status = 0U;
    if(SystemStatus.bits.b1Status_MOS_CHG != 0U)
    {
        g_battery_data.charge_dis_info.charge_dis_status |= 0x80U;
    }
    if(SystemStatus.bits.b1Status_MOS_DSG != 0U)
    {
        g_battery_data.charge_dis_info.charge_dis_status |= 0x40U;
    }
}


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
        tx_buf[idx++] = Hex_To_Ascii(info_data[i] & 0x0F);
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
//uint16_t Cmd_Handle_Manufactory_Info(uint8_t *tx_buf)
//{
//    uint8_t info_buf[MAX_FRAME_LEN] = {0};
//    uint16_t idx = 0; 

//    // 设备名称10字节
//    memcpy(&info_buf[idx], g_battery_data.base_info.device_name, 10);
//    idx += 10;
//    // 厂商名称20字节
//    memcpy(&info_buf[idx], g_battery_data.base_info.manufactory_name, 20);
//    idx += 20;
//    // 软件版本2字节
//    memcpy(&info_buf[idx], g_battery_data.base_info.software_ver, 2);
//    idx += 2;
//    // 电池数量 
//    info_buf[idx++] = g_battery_data.base_info.battery_num;
//    // 条形码
//    uint16_t i;
//    for(i = 0; i < g_battery_data.base_info.battery_num; i++)
//    {
//        memcpy(&info_buf[idx], g_battery_data.base_info.battery_barcode[i], 16);
//        idx += 16;
//    }
//    
//    return Build_Response_Frame(tx_buf, Version, Adress, RTN_OK, info_buf, idx);
//}

/**
 * @brief  0x61 获取模拟量量化数据处理
 */
uint16_t Cmd_Handle_Analog1_Value(uint8_t *tx_buf)
{
    uint8_t info_buf[100] = {0};
    uint16_t idx = 0;
    
    // 电池组系统总平均电压
    info_buf[idx++] = (g_battery_data.analog_data1.pack_total_avg_voltage >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.pack_total_avg_voltage & 0xFF;
    // 电池组系统总电流
    info_buf[idx++] = (g_battery_data.analog_data1.pack_total_current >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.pack_total_current & 0xFF;
    // 电池组系统 SOC
    info_buf[idx++] = g_battery_data.analog_data1.pack_soc;
    // 平均循环次数
    info_buf[idx++] = (g_battery_data.analog_data1.pack_avg_cycle_count >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.pack_avg_cycle_count & 0xFF;
    // 最大循环次数
    info_buf[idx++] = (g_battery_data.analog_data1.pack_max_cycle_count >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.pack_max_cycle_count & 0xFF;
    // 平均 SOH (State of Health)
    info_buf[idx++] = g_battery_data.analog_data1.pack_avg_soh;
    // 最小 SOH
    info_buf[idx++] = g_battery_data.analog_data1.pack_min_soh;
    // 单芯最高电压
    info_buf[idx++] = (g_battery_data.analog_data1.cell_max_voltage >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.cell_max_voltage & 0xFF;
    // 单芯最高电压所在模块
    info_buf[idx++] = (g_battery_data.analog_data1.cell_max_voltage_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.cell_max_voltage_module & 0xFF;
    // 单芯最低电压
    info_buf[idx++] = (g_battery_data.analog_data1.cell_min_voltage >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.cell_min_voltage & 0xFF;
    // 单芯最低电压所在模块
    info_buf[idx++] = (g_battery_data.analog_data1.cell_min_voltage_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.cell_min_voltage_module & 0xFF;
    // 单芯平均温度
    info_buf[idx++] = (g_battery_data.analog_data1.cell_avg_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.cell_avg_temp & 0xFF;
    // 单芯最高温度
    info_buf[idx++] = (g_battery_data.analog_data1.cell_max_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.cell_max_temp & 0xFF;
    // 单芯最高温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data1.cell_max_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.cell_max_temp_module & 0xFF;
    // 单芯最低温度
    info_buf[idx++] = (g_battery_data.analog_data1.cell_min_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.cell_min_temp & 0xFF;
    // 单芯最低温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data1.cell_min_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.cell_min_temp_module & 0xFF;
    // MOSFET 平均温度
    info_buf[idx++] = (g_battery_data.analog_data1.mosfet_avg_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.mosfet_avg_temp & 0xFF;
    // MOSFET 最高温度
    info_buf[idx++] = (g_battery_data.analog_data1.mosfet_max_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.mosfet_max_temp & 0xFF;
    // MOSFET 最高温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data1.mosfet_max_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.mosfet_max_temp_module & 0xFF;
    // MOSFET 最低温度
    info_buf[idx++] = (g_battery_data.analog_data1.mosfet_min_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.mosfet_min_temp & 0xFF;
    // MOSFET 最低温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data1.mosfet_min_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.mosfet_min_temp_module & 0xFF;
    // BMS 平均温度
    info_buf[idx++] = (g_battery_data.analog_data1.bms_avg_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.bms_avg_temp & 0xFF;
    // BMS 最高温度
    info_buf[idx++] = (g_battery_data.analog_data1.bms_max_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.bms_max_temp & 0xFF;
    // BMS 最高温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data1.bms_max_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.bms_max_temp_module & 0xFF;
    // BMS 最低温度
    info_buf[idx++] = (g_battery_data.analog_data1.bms_min_temp >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.bms_min_temp & 0xFF;
    // BMS 最低温度所在模块
    info_buf[idx++] = (g_battery_data.analog_data1.bms_min_temp_module >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data1.bms_min_temp_module & 0xFF;
    
    return Build_Response_Frame(tx_buf, Version, Adress, RTN_OK, info_buf, idx);
}

/**
 * @brief  0x42 获取模拟量量化数据处理
 */
uint16_t Cmd_Handle_Analog2_Value(uint8_t *tx_buf, uint8_t cmd)
{
    // 校验Command与本机地址匹配
    if(cmd != Adress)
    {
        return Build_Response_Frame(tx_buf, Version, Adress, RTN_ADR_ERROR, NULL, 0);
    }
    uint8_t info_buf[128] = {0};
    uint16_t idx = 0;
    // INFOFLAG固定值
    info_buf[idx++] = 0x00;
    // Command值
    info_buf[idx++] = cmd;
    // 电芯节数
    uint8_t cell_num = g_battery_data.analog_data2.cell_num;
    info_buf[idx++] = cell_num;
    // 电芯电压
    for(uint8_t i = 0; i < cell_num; i++)
    {
        info_buf[idx++] = (g_battery_data.analog_data2.cell_voltage[i] >> 8) & 0xFF;
        info_buf[idx++] = g_battery_data.analog_data2.cell_voltage[i] & 0xFF;
    }
    // 温度点数量
    uint8_t temp_num = g_battery_data.analog_data2.temp_num;
    info_buf[idx++] = temp_num;
    // 温度值
    for(uint8_t i = 0; i < temp_num; i++)
    {
        info_buf[idx++] = (g_battery_data.analog_data2.temp_value[i] >> 8) & 0xFF;
        info_buf[idx++] = g_battery_data.analog_data2.temp_value[i] & 0xFF;
    }
    // 电流
    info_buf[idx++] = (g_battery_data.analog_data2.current >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data2.current & 0xFF;
    // 模块电压
    info_buf[idx++] = (g_battery_data.analog_data2.module_voltage >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data2.module_voltage & 0xFF;
    // 剩余容量
    info_buf[idx++] = (g_battery_data.analog_data2.remain_capacity >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data2.remain_capacity & 0xFF;
    // 用户自定义个数
    info_buf[idx++] = g_battery_data.analog_data2.user_define_num;
    // 总容量
    info_buf[idx++] = (g_battery_data.analog_data2.total_capacity >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data2.total_capacity & 0xFF;
    // 循环次数
    info_buf[idx++] = (g_battery_data.analog_data2.cycle_num >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data2.cycle_num & 0xFF;
    // 满充容量
    info_buf[idx++] = (g_battery_data.analog_data2.full_charge_capacity >> 8) & 0xFF;
    info_buf[idx++] = g_battery_data.analog_data2.full_charge_capacity & 0xFF;
    return Build_Response_Frame(tx_buf, Version, Adress, RTN_OK, info_buf, idx);
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
    
    return Build_Response_Frame(tx_buf, Version, Adress, RTN_OK, info_buf, idx);
}

/**
 * @brief  0x63 获取电池组系统充放电管理交互信息
 */
uint16_t Cmd_Handle_Charge_Dis_Info(uint8_t *tx_buf)
{
    uint8_t info_buf[16] = {0};
    uint16_t idx = 0;
    Battery_Charge_Dis_Info_T *p = &g_battery_data.charge_dis_info;
    uint16_t max_charge_current = (uint16_t)((uint16_t)p->max_charge_current * 100U);
    uint16_t max_discharge_current = (uint16_t)((uint16_t)p->max_discharge_current * 100U);
    
    // 充电电压建议上限
    info_buf[idx++] = (p->charge_volt_limit >> 8) & 0xFF;
    info_buf[idx++] = p->charge_volt_limit & 0xFF;
    // 放电电压建议下限
    info_buf[idx++] = (p->discharge_volt_limit >> 8) & 0xFF;
    info_buf[idx++] = p->discharge_volt_limit & 0xFF;
    // 最大充电电流
    info_buf[idx++] = (max_charge_current >> 8) & 0xFF;
    info_buf[idx++] = max_charge_current & 0xFF;
    // 最大放电电流
    info_buf[idx++] = (max_discharge_current >> 8) & 0xFF;
    info_buf[idx++] = max_discharge_current & 0xFF;
    // 充放电状态
    info_buf[idx++] = p->charge_dis_status;
    return Build_Response_Frame(tx_buf, Version, Adress, RTN_OK, info_buf, idx);
}

/**
 * @brief  接收帧解析与命令分发
 */
void Ascii_Slave_ResetRx(void)
{
    uart_rx_len = 0U;
    ascii_expect_len = 0U;
    frame_received_flag = 0U;
    ascii_rx_last_byte_time = 0;
    memset(uart_rx_buf, 0, sizeof(uart_rx_buf));
}

void Ascii_Slave_PollTimeout(void)
{
    if(Ascii_Slave_IsRxTimeout() != 0U)
    {
        Ascii_Slave_ResetRx();
    }
}

uint8_t Ascii_Slave_ConsumeByte(uint8_t rx_byte)
{
    uint16_t lenid = 0U;
    uint8_t length_high;
    uint8_t length_low;

    if(Ascii_Slave_IsRxTimeout() != 0U)
    {
        Ascii_Slave_ResetRx();
    }

    if(rx_byte == SOI)
    {
        Ascii_Slave_ResetRx();
        uart_rx_buf[uart_rx_len++] = rx_byte;
        ascii_rx_last_byte_time = bsp_GetRunTime();
        return 1U;
    }

    if(uart_rx_len == 0U)
    {
        return 0U;
    }

    if(uart_rx_len >= MAX_FRAME_LEN)
    {
        Ascii_Slave_ResetRx();
        return 1U;
    }

    uart_rx_buf[uart_rx_len++] = rx_byte;
    ascii_rx_last_byte_time = bsp_GetRunTime();

    if(uart_rx_len == 13U)
    {
        if(!Ascii_ParseHexByte(uart_rx_buf[9], uart_rx_buf[10], &length_high) ||
           !Ascii_ParseHexByte(uart_rx_buf[11], uart_rx_buf[12], &length_low))
        {
            Ascii_Slave_ResetRx();
            return 1U;
        }

        if(!Parse_LENGTH_Field((uint16_t)(((uint16_t)length_high << 8) | length_low), &lenid) ||
           ((lenid & 0x0001U) != 0U))
        {
            Ascii_Slave_ResetRx();
            return 1U;
        }

        ascii_expect_len = (uint16_t)(13U + lenid + 4U + 1U);
        if((ascii_expect_len > MAX_FRAME_LEN) || (ascii_expect_len < 18U))
        {
            Ascii_Slave_ResetRx();
            return 1U;
        }
    }

    if((ascii_expect_len != 0U) && (uart_rx_len >= ascii_expect_len))
    {
        if(uart_rx_len == ascii_expect_len)
        {
            frame_received_flag = 1U;
        }
        else
        {
            Ascii_Slave_ResetRx();
        }
    }

    return 1U;
}

#if 0
void Frame_Parse_Process(void)
{
    if(!frame_received_flag) return;
    frame_received_flag = 0;
    uint8_t *rx_buf = uart_rx_buf;
    uint16_t rx_len = uart_rx_len;
    //uint8_t tx_buf[MAX_FRAME_LEN] = {0};
    uint16_t tx_len = 0;
    // 最小帧长度校验（SOI+VER+ADR+CID1+CID2+LENGTH+CHKSUM+EOI = 1+2+2+2+2+4+4+1=18字节）
    if(rx_len < 18)
    {
        return;
    }
    // 1. 解析基础字段（ASCII转HEX）
    Version = (Ascii_To_Hex(rx_buf[1]) << 4) | Ascii_To_Hex(rx_buf[2]);
    Adress = (Ascii_To_Hex(rx_buf[3]) << 4) | Ascii_To_Hex(rx_buf[4]);
    uint8_t cid1 = (Ascii_To_Hex(rx_buf[5]) << 4) | Ascii_To_Hex(rx_buf[6]);
    uint8_t cid2 = (Ascii_To_Hex(rx_buf[7]) << 4) | Ascii_To_Hex(rx_buf[8]);
    uint16_t length_field = (Ascii_To_Hex(rx_buf[9]) << 12) | (Ascii_To_Hex(rx_buf[10]) << 8) | (Ascii_To_Hex(rx_buf[11]) << 4) | Ascii_To_Hex(rx_buf[12]);
    // 2. 地址校验：只处理本机地址
    if(Adress != SLAVE_ADDRESS)
    {
        //tx_len = Build_Response_Frame(tx_buf, PROTOCOL_VERSION, SLAVE_ADDRESS, RTN_ADR_ERROR, NULL, 0);
        //Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 3. CID1校验
    if(cid1 != CID1_BAT_DATA)
    {
        tx_len = Build_Response_Frame(tx_buf, Version, Adress, RTN_CID2_INVALID, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 4. LENGTH字段校验
    uint16_t lenid = 0;
    if(!Parse_LENGTH_Field(length_field, &lenid))
    {
        tx_len = Build_Response_Frame(tx_buf, Version, Adress, RTN_LCHKSUM_ERROR, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 5. 帧长度校验
    uint16_t expect_len = 13 + lenid + 4 + 1; // SOI+基础字段+INFO+CHKSUM+EOI
    if(rx_len != expect_len)
    {
        tx_len = Build_Response_Frame(tx_buf, Version, Adress, RTN_FORMAT_ERROR, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 6. CHKSUM整帧校验
    uint16_t chksum_rx = (Ascii_To_Hex(rx_buf[13+lenid]) << 12) | (Ascii_To_Hex(rx_buf[13+lenid+1]) << 8) | (Ascii_To_Hex(rx_buf[13+lenid+2]) << 4) | Ascii_To_Hex(rx_buf[13+lenid+3]);
    uint16_t chksum_calc = Calc_CHKSUM(&rx_buf[1], 12 + lenid);
    if(chksum_rx != chksum_calc)
    {
        tx_len = Build_Response_Frame(tx_buf, Version, Adress, RTN_CHKSUM_ERROR, NULL, 0);
        Ascii_Send_NByte(tx_buf, tx_len, UART1);
        return;
    }
    // 7. 解析INFO域（ASCII转HEX）
    uint8_t info_hex_buf[256] = {0};
    uint16_t info_hex_len = lenid / 2;
    for(uint16_t i = 0; i < info_hex_len; i++)
    {
        info_hex_buf[i] = (Ascii_To_Hex(rx_buf[13 + i*2]) << 4) | Ascii_To_Hex(rx_buf[13 + i*2 + 1]);
    }
    uint8_t cmd = info_hex_buf[0]; // INFO第一个字节为Command
    // 8. 命令分发处理
    switch(cid2)
    {
//        case CMD_GET_BATTERY_INFO:
//            tx_len = Cmd_Handle_Manufactory_Info(tx_buf);
//            break;
        case CMD_GET_ANALOG1_DATA:
            tx_len = Cmd_Handle_Analog1_Value(tx_buf);
            break;
        case CMD_GET_ANALOG2_DATA:
            tx_len = Cmd_Handle_Analog2_Value(tx_buf,cmd);
            break;
        case CMD_GET_ALARM_INFO:
            tx_len = Cmd_Handle_Alarm_Info(tx_buf);
            break;
        case CMD_GET_CHARGE_DIS_INFO:
            tx_len = Cmd_Handle_Charge_Dis_Info(tx_buf);
            break;
        default:
            tx_len = Build_Response_Frame(tx_buf, Version, Adress, RTN_CID2_INVALID, NULL, 0);
            break;
    }

    // 9. 发送响应帧
    if(tx_len > 0)
    {
        LED_L4851_ON();
        DMA_TX_data(tx_len-1,(uint32_t)tx_buf+(tx_len-1));
//        if(tx_len > 500)
//        {
//            Ascii_Send_NByte(tx_buf, tx_len, UART1);
//        }
//        else
//        {
//            DMA_TX_data(tx_len-1,(uint32_t)tx_buf+(tx_len-1));
//        }
    }
    // 清空接收缓冲区
    memset(uart_rx_buf, 0, MAX_FRAME_LEN);
    uart_rx_len = 0;
}

#endif

void Frame_Parse_Process(void)
{
    uint8_t *rx_buf = uart_rx_buf;
    uint16_t rx_len = uart_rx_len;
    uint16_t tx_len = 0U;
    uint16_t lenid = 0U;
    uint16_t expect_len;
    uint16_t chksum_rx;
    uint16_t chksum_calc;
    uint16_t length_field;
    uint16_t info_hex_len;
    uint8_t version;
    uint8_t address;
    uint8_t cid1;
    uint8_t cid2;
    uint8_t length_high;
    uint8_t length_low;
    uint8_t chksum_high;
    uint8_t chksum_low;
    uint8_t cmd = 0U;
    uint8_t info_hex_buf[256] = {0};
    uint16_t i;

    Ascii_Slave_PollTimeout();

    if(!frame_received_flag)
    {
        return;
    }

    frame_received_flag = 0U;

    if((rx_len < 18U) || (rx_buf[0] != SOI) || (rx_buf[rx_len - 1U] != EOI))
    {
        Ascii_Slave_ResetRx();
        return;
    }

    if(!Ascii_ParseHexByte(rx_buf[1], rx_buf[2], &version) ||
       !Ascii_ParseHexByte(rx_buf[3], rx_buf[4], &address) ||
       !Ascii_ParseHexByte(rx_buf[5], rx_buf[6], &cid1) ||
       !Ascii_ParseHexByte(rx_buf[7], rx_buf[8], &cid2) ||
       !Ascii_ParseHexByte(rx_buf[9], rx_buf[10], &length_high) ||
       !Ascii_ParseHexByte(rx_buf[11], rx_buf[12], &length_low))
    {
        Ascii_Slave_ResetRx();
        return;
    }

    Version = version;
    Adress = address;
    length_field = (uint16_t)(((uint16_t)length_high << 8) | length_low);

    if(Adress != SLAVE_ADDRESS)
    {
        Ascii_Slave_ResetRx();
        return;
    }

    if(cid1 != CID1_BAT_DATA)
    {
        tx_len = Build_Response_Frame(tx_buf, Version, Adress, RTN_CID2_INVALID, NULL, 0);
        goto send_and_reset;
    }

    if(!Parse_LENGTH_Field(length_field, &lenid) || ((lenid & 0x0001U) != 0U))
    {
        tx_len = Build_Response_Frame(tx_buf, Version, Adress, RTN_LCHKSUM_ERROR, NULL, 0);
        goto send_and_reset;
    }

    expect_len = (uint16_t)(13U + lenid + 4U + 1U);
    if(rx_len != expect_len)
    {
        tx_len = Build_Response_Frame(tx_buf, Version, Adress, RTN_FORMAT_ERROR, NULL, 0);
        goto send_and_reset;
    }

    if(!Ascii_ParseHexByte(rx_buf[13U + lenid], rx_buf[14U + lenid], &chksum_high) ||
       !Ascii_ParseHexByte(rx_buf[15U + lenid], rx_buf[16U + lenid], &chksum_low))
    {
        tx_len = Build_Response_Frame(tx_buf, Version, Adress, RTN_CHKSUM_ERROR, NULL, 0);
        goto send_and_reset;
    }

    chksum_rx = (uint16_t)(((uint16_t)chksum_high << 8) | chksum_low);
    chksum_calc = Calc_CHKSUM(&rx_buf[1], (uint16_t)(12U + lenid));
    if(chksum_rx != chksum_calc)
    {
        tx_len = Build_Response_Frame(tx_buf, Version, Adress, RTN_CHKSUM_ERROR, NULL, 0);
        goto send_and_reset;
    }

    info_hex_len = (uint16_t)(lenid / 2U);
    if(info_hex_len > sizeof(info_hex_buf))
    {
        tx_len = Build_Response_Frame(tx_buf, Version, Adress, RTN_FORMAT_ERROR, NULL, 0);
        goto send_and_reset;
    }

    for(i = 0U; i < info_hex_len; i++)
    {
        if(!Ascii_ParseHexByte(rx_buf[13U + i * 2U], rx_buf[14U + i * 2U], &info_hex_buf[i]))
        {
            tx_len = Build_Response_Frame(tx_buf, Version, Adress, RTN_FORMAT_ERROR, NULL, 0);
            goto send_and_reset;
        }
    }

    Ascii_Slave_RefreshBatteryData();

    if(info_hex_len > 0U)
    {
        cmd = info_hex_buf[0];
    }

    switch(cid2)
    {
//        case CMD_GET_BATTERY_INFO:
//            tx_len = Cmd_Handle_Manufactory_Info(tx_buf);
//            break;
        case CMD_GET_ANALOG1_DATA:
            tx_len = Cmd_Handle_Analog1_Value(tx_buf);
            break;
        case CMD_GET_ANALOG2_DATA:
            tx_len = Cmd_Handle_Analog2_Value(tx_buf, cmd);
            break;
        case CMD_GET_ALARM_INFO:
            tx_len = Cmd_Handle_Alarm_Info(tx_buf);
            break;
        case CMD_GET_CHARGE_DIS_INFO:
            tx_len = Cmd_Handle_Charge_Dis_Info(tx_buf);
            break;
        default:
            tx_len = Build_Response_Frame(tx_buf, Version, Adress, RTN_CID2_INVALID, NULL, 0);
            break;
    }

send_and_reset:
    if(tx_len > 0U)
    {
        Ascii_Send_NByte(tx_buf, tx_len, USART1);
    }

    Ascii_Slave_ResetRx();
}

////Ascii串口发送一个字节数据
//void Ascii_Send_Byte(uint8_t Modbus_byte,UART_TypeDef *UARTx)
//{  
//	LL_UART_TransmitData8(UARTx, Modbus_byte);   
//	while (!LL_UART_IsActiveFlag_TXCF(UARTx));
//	LL_UART_ClearFlag_TXCF(UARTx);                 
//}     


//Ascii串口发送N个字节数据
void Ascii_Send_NByte(uint8_t *buff,uint16_t len,USART_TypeDef *UARTx)
{    
	uint16_t t;
    TRANS_EN_485();
	//LL_mDelay(1); // 确保DE引脚稳定
	for(t = 0U; t < len; t++)
	{
		while((UARTx->ISR & USART_ISR_TXE) == 0U)
        {
        }
        UARTx->TDR = buff[t];
	}		
	//LL_mDelay(1); // 确保DE引脚稳定
    while((UARTx->ISR & USART_ISR_TC) == 0U)
    {
    }
    UARTx->ICR = USART_ICR_TCCF;
    RECV_EN_485();
}

#if 0
void DMA_TX_data(uint8_t lenth,uint32_t SrcEndAddress)
{  
	LL_UART_ClearFlag_TXCF(UART1); 
	LL_DMA_SetMode(LL_DMA_PRI_UART1_TX, LL_DMA_MODE_BASIC);//设置BASI模式
	LL_DMA_SetDataLength(LL_DMA_PRI_UART1_TX, lenth);//设置长度
    LL_DMA_SetSrcEndAddress(LL_DMA_PRI_UART1_TX,SrcEndAddress);//设置源地址结束地址

    LL_GPIO_SetOutputPin(RS485_1_Port, RS485_1_Pin);//切换RS485发送模式
    LL_mDelay(2);
    LL_DMA_EnableChannel(DMA1,LL_DMA_CHANNEL_UART1_TX);
    LL_UART_EnableDMAReq_TX(UART1);
}

#endif

void DMA_TX_data(uint8_t lenth,uint32_t SrcEndAddress)
{
    (void)lenth;
    (void)SrcEndAddress;
}

//void DMA_TX_data(uint8_t lenth,uint32_t SrcEndAddress)
//{  
//	LL_UART_ClearFlag_TXCF(UART1); 
//	LL_DMA_SetMode(LL_DMA_PRI_UART1_TX, LL_DMA_MODE_BASIC);//设置BASI模式
//	LL_DMA_SetDataLength(LL_DMA_PRI_UART1_TX, lenth);//设置长度
//    LL_DMA_SetSrcEndAddress(LL_DMA_PRI_UART1_TX,SrcEndAddress);//设置源地址结束地址

//    LL_GPIO_SetOutputPin(RS485_1_Port, RS485_1_Pin);//切换RS485发送模式
//    LL_mDelay(2);
//    LL_DMA_EnableChannel(DMA1,LL_DMA_CHANNEL_UART1_TX);
//    LL_UART_EnableDMAReq_TX(UART1);
//	
//	while(LL_DMA_IsActiveFlag_TC(DMA1,LL_DMA_CHANNEL_UART1_TX) == RESET);
//	LL_mDelay(2);
//	LL_DMA_DisableChannel(DMA1,LL_DMA_CHANNEL_UART1_TX);
//	LL_UART_DisableDMAReq_TX(UART1);
//    LL_GPIO_ResetOutputPin(RS485_1_Port, RS485_1_Pin);//切换RS485接收模式
//		  
//	LL_DMA_SetMode(LL_DMA_PRI_UART1_TX, LL_DMA_MODE_BASIC);//设置BASI模式
//	LL_DMA_ClearFlag_TC(DMA1,LL_DMA_CHANNEL_UART1_TX);
//}


