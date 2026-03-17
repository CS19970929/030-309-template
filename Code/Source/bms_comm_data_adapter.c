#include "bms_comm_data_adapter.h"

typedef struct
{
    uint16_t avg_temp_k_x10;
    uint16_t max_temp_k_x10;
    uint16_t min_temp_k_x10;
    uint16_t max_location;
    uint16_t min_location;
} BmsComm_TemperatureStats;

static uint16_t BmsComm_EncodePylonLocation(uint16_t point_index)
{
    uint16_t point;

    point = point_index;
    if (point == 0U)
    {
        point = 1U;
    }
    if (point > 0x00FFU)
    {
        point = 0x00FFU;
    }

    return (uint16_t)(0x0100U | point);
}

static uint16_t BmsComm_EncodePylonTemperature(int16_t temp_c_x10)
{
    int32_t temp_k_x10;

    temp_k_x10 = (int32_t)temp_c_x10 + 2730;
    if (temp_k_x10 < 0)
    {
        temp_k_x10 = 0;
    }
    if (temp_k_x10 > 0xFFFF)
    {
        temp_k_x10 = 0xFFFF;
    }

    return (uint16_t)temp_k_x10;
}

static int16_t BmsComm_SaturateS16(int32_t value)
{
    if (value < -32768L)
    {
        return (int16_t)-32768;
    }
    if (value > 32767L)
    {
        return (int16_t)32767;
    }

    return (int16_t)value;
}

static uint16_t BmsComm_SaturateU16(uint32_t value)
{
    if (value > 0xFFFFUL)
    {
        return 0xFFFFU;
    }

    return (uint16_t)value;
}

static uint16_t BmsComm_GetPackVoltageMv(void)
{
    return BmsComm_SaturateU16((uint32_t)g_stCellInfoReport.u16VCellTotle * 10UL);
}

static uint8_t BmsComm_GetChargeDischargeStatus(void)
{
    uint8_t status;

    status = 0;
    if (SystemStatus.bits.b1Status_MOS_CHG != 0U)
    {
        status |= 0x80U;
    }
    if (SystemStatus.bits.b1Status_MOS_DSG != 0U)
    {
        status |= 0x40U;
    }

    return status;
}

static uint8_t BmsComm_IsBmsCommFaultActive(void)
{
    if (SystemStatus.bits.b1StartUpBMS == 0U)
    {
        return 0U;
    }

    return (uint8_t)(SystemStatus.bits.b1Status_AFE1 == 0U);
}

static void BmsComm_SetUnsupportedTemperatureStats(BmsComm_TemperatureStats *stats)
{
    stats->avg_temp_k_x10 = 0xFFFFU;
    stats->max_temp_k_x10 = 0xFFFFU;
    stats->min_temp_k_x10 = 0xFFFFU;
    stats->max_location = 0xFFFFU;
    stats->min_location = 0xFFFFU;
}

static void BmsComm_GetCellTemperatureStats(BmsComm_TemperatureStats *stats)
{
    uint8_t i;
    uint8_t valid_count;
    uint32_t temp_sum;
    uint16_t temp_raw;
    uint16_t temp_max;
    uint16_t temp_min;
    uint8_t temp_max_index;
    uint8_t temp_min_index;

    valid_count = 0U;
    temp_sum = 0U;
    temp_max = 0U;
    temp_min = 0xFFFFU;
    temp_max_index = 0U;
    temp_min_index = 0U;

    for (i = AFE1_TEMP1; i <= AFE2_TEMP3; ++i)
    {
        temp_raw = g_stCellInfoReport.u16Temperature[i];
        if (temp_raw == 0U)
        {
            continue;
        }

        temp_sum += temp_raw;
        if (temp_raw > temp_max)
        {
            temp_max = temp_raw;
            temp_max_index = i;
        }
        if (temp_raw < temp_min)
        {
            temp_min = temp_raw;
            temp_min_index = i;
        }
        valid_count++;
    }

    if (valid_count == 0U)
    {
        BmsComm_SetUnsupportedTemperatureStats(stats);
        return;
    }

    stats->avg_temp_k_x10 = BmsComm_EncodePylonTemperature((int16_t)(temp_sum / valid_count) - 400);
    stats->max_temp_k_x10 = BmsComm_EncodePylonTemperature((int16_t)temp_max - 400);
    stats->min_temp_k_x10 = BmsComm_EncodePylonTemperature((int16_t)temp_min - 400);
    stats->max_location = BmsComm_EncodePylonLocation((uint16_t)(temp_max_index + 1U));
    stats->min_location = BmsComm_EncodePylonLocation((uint16_t)(temp_min_index + 1U));
}

static void BmsComm_GetMosTemperatureStats(BmsComm_TemperatureStats *stats)
{
    uint16_t temp_raw;

    temp_raw = g_stCellInfoReport.u16Temperature[MOS_TEMP1];
    if (temp_raw == 0U)
    {
        BmsComm_SetUnsupportedTemperatureStats(stats);
        return;
    }

    stats->avg_temp_k_x10 = BmsComm_EncodePylonTemperature((int16_t)temp_raw - 400);
    stats->max_temp_k_x10 = stats->avg_temp_k_x10;
    stats->min_temp_k_x10 = stats->avg_temp_k_x10;
    stats->max_location = BmsComm_EncodePylonLocation(1U);
    stats->min_location = BmsComm_EncodePylonLocation(1U);
}

static uint8_t BmsComm_BuildAlarm1(union MDLCHGFAULT_REG fault)
{
    uint8_t value;

    value = 0U;
    value |= (uint8_t)(fault.bits.b1BatOvp << 7);
    value |= (uint8_t)(fault.bits.b1BatUvp << 6);
    value |= (uint8_t)(fault.bits.b1CellOvp << 5);
    value |= (uint8_t)(fault.bits.b1CellUvp << 4);
    value |= (uint8_t)(((fault.bits.b1CellChgOtp != 0U) || (fault.bits.b1CellDischgOtp != 0U)) << 3);
    value |= (uint8_t)(((fault.bits.b1CellChgUtp != 0U) || (fault.bits.b1CellDischgUtp != 0U)) << 2);
    value |= (uint8_t)(fault.bits.b1TmosOtp << 1);
    value |= (uint8_t)(fault.bits.b1VcellDeltaBig);
    return value;
}

static uint8_t BmsComm_BuildAlarm2(union MDLCHGFAULT_REG fault)
{
    uint8_t value;

    value = 0U;
    value |= (uint8_t)(fault.bits.b1TempDeltaBig << 7);
    value |= (uint8_t)(fault.bits.b1IchgOcp << 6);
    value |= (uint8_t)(fault.bits.b1IdischgOcp << 5);
    value |= (uint8_t)(BmsComm_IsBmsCommFaultActive() << 4);
    return value;
}

static uint8_t BmsComm_BuildProtect1(union MDLCHGFAULT_REG fault)
{
    uint8_t value;

    value = 0U;
    value |= (uint8_t)(fault.bits.b1BatOvp << 7);
    value |= (uint8_t)(fault.bits.b1BatUvp << 6);
    value |= (uint8_t)(fault.bits.b1CellOvp << 5);
    value |= (uint8_t)(fault.bits.b1CellUvp << 4);
    value |= (uint8_t)(((fault.bits.b1CellChgOtp != 0U) || (fault.bits.b1CellDischgOtp != 0U)) << 3);
    value |= (uint8_t)(((fault.bits.b1CellChgUtp != 0U) || (fault.bits.b1CellDischgUtp != 0U)) << 2);
    value |= (uint8_t)(fault.bits.b1TmosOtp << 1);
    return value;
}

static uint8_t BmsComm_BuildProtect2(union MDLCHGFAULT_REG fault)
{
    uint8_t value;

    value = 0U;
    value |= (uint8_t)(fault.bits.b1IchgOcp << 7);
    value |= (uint8_t)(fault.bits.b1IdischgOcp << 6);
    value |= (uint8_t)(BmsComm_IsBmsCommFaultActive() << 4);
    return value;
}

static void BmsComm_CopyAsciiField(uint8_t *dst, uint16_t dst_len, const uint8_t *src, uint16_t src_len)
{
    uint16_t i;

    for (i = 0; i < dst_len; ++i)
    {
        dst[i] = 0;
    }

    for (i = 0; (i < dst_len) && (i < src_len); ++i)
    {
        if (src[i] == 0)
        {
            break;
        }
        dst[i] = src[i];
    }
}

void BmsComm_GetBaseInfo(Battery_Base_Info_T *info)
{
    uint16_t i;

    memset(info, 0, sizeof(*info));

    BmsComm_CopyAsciiField(info->device_name, sizeof(info->device_name),
                           ProductionInfor.BMS_HardWareVersion, ProductionInfor.BMS_HardWareVersionLength);
    BmsComm_CopyAsciiField(info->manufactory_name, sizeof(info->manufactory_name),
                           (const uint8_t *)"BMS", 3);

    info->software_ver[0] = (ProductionInfor.BMS_SoftWareVersionLength > 0) ? ProductionInfor.BMS_SoftWareVersion[0] : '0';
    info->software_ver[1] = (ProductionInfor.BMS_SoftWareVersionLength > 1) ? ProductionInfor.BMS_SoftWareVersion[1] : '0';
    info->battery_num = (uint8_t)((SeriesNum > CELL_MAX_NUM) ? CELL_MAX_NUM : SeriesNum);

    for (i = 0; i < info->battery_num; ++i)
    {
        BmsComm_CopyAsciiField(info->battery_barcode[i], sizeof(info->battery_barcode[i]),
                               ProductionInfor.BMS_SerialNumber, ProductionInfor.BMS_SerialNumberLength);
    }
}

void BmsComm_GetAnalogData(Battery_Analog_T *data)
{
    BmsComm_TemperatureStats cell_temp;
    BmsComm_TemperatureStats mos_temp;
    BmsComm_TemperatureStats bms_temp;
    int16_t pack_current;

    memset(data, 0, sizeof(*data));

    pack_current = (g_stCellInfoReport.u16Ichg > 0) ? (int16_t)g_stCellInfoReport.u16Ichg : (int16_t)(-((int16_t)g_stCellInfoReport.u16IDischg));
    BmsComm_GetCellTemperatureStats(&cell_temp);
    BmsComm_GetMosTemperatureStats(&mos_temp);
    BmsComm_SetUnsupportedTemperatureStats(&bms_temp);

    data->pack_total_avg_voltage = BmsComm_GetPackVoltageMv();
    data->pack_total_current = BmsComm_SaturateS16((int32_t)pack_current * 10L);
    data->pack_soc = (uint8_t)g_stCellInfoReport.SocElement.u16Soc;
    data->pack_avg_cycle_count = g_stCellInfoReport.SocElement.u16Cycle_times;
    data->pack_max_cycle_count = g_stCellInfoReport.SocElement.u16Cycle_times;
    data->pack_avg_soh = (uint8_t)g_stCellInfoReport.SocElement.u16Soh;
    data->pack_min_soh = (uint8_t)g_stCellInfoReport.SocElement.u16Soh;

    data->cell_max_voltage = g_stCellInfoReport.u16VCellMax;
    data->cell_max_voltage_module = BmsComm_EncodePylonLocation(g_stCellInfoReport.u16VCellMaxPosition);
    data->cell_min_voltage = g_stCellInfoReport.u16VCellMin;
    data->cell_min_voltage_module = BmsComm_EncodePylonLocation(g_stCellInfoReport.u16VCellMinPosition);

    data->cell_avg_temp = (int16_t)cell_temp.avg_temp_k_x10;
    data->cell_max_temp = (int16_t)cell_temp.max_temp_k_x10;
    data->cell_max_temp_module = cell_temp.max_location;
    data->cell_min_temp = (int16_t)cell_temp.min_temp_k_x10;
    data->cell_min_temp_module = cell_temp.min_location;

    data->mosfet_avg_temp = (int16_t)mos_temp.avg_temp_k_x10;
    data->mosfet_max_temp = (int16_t)mos_temp.max_temp_k_x10;
    data->mosfet_max_temp_module = mos_temp.max_location;
    data->mosfet_min_temp = (int16_t)mos_temp.min_temp_k_x10;
    data->mosfet_min_temp_module = mos_temp.min_location;

    data->bms_avg_temp = (int16_t)bms_temp.avg_temp_k_x10;
    data->bms_max_temp = (int16_t)bms_temp.max_temp_k_x10;
    data->bms_max_temp_module = bms_temp.max_location;
    data->bms_min_temp = (int16_t)bms_temp.min_temp_k_x10;
    data->bms_min_temp_module = bms_temp.min_location;
}

void BmsComm_GetAlarmData(Battery_Alarm_T *data)
{
    memset(data, 0, sizeof(*data));

    data->system_alarm1 = BmsComm_BuildAlarm1(g_stCellInfoReport.unMdlFault_Second);
    data->system_alarm2 = BmsComm_BuildAlarm2(g_stCellInfoReport.unMdlFault_Second);
    data->system_protect1 = BmsComm_BuildProtect1(g_stCellInfoReport.unMdlFault_Third);
    data->system_protect2 = BmsComm_BuildProtect2(g_stCellInfoReport.unMdlFault_Third);
}

void BmsComm_GetChargeDischargeInfo(Battery_Charge_Dis_Info_T *data)
{
    uint32_t series_count;

    memset(data, 0, sizeof(*data));

    series_count = (SeriesNum > 0U) ? SeriesNum : 1U;
    data->charge_volt_limit = BmsComm_SaturateU16((uint32_t)OtherElement.u16Soc_V_100 * series_count);
    data->discharge_volt_limit = BmsComm_SaturateU16((uint32_t)OtherElement.u16Soc_V_0 * series_count);
    data->max_charge_current = BmsComm_SaturateS16((int32_t)OtherElement.u16CS_Cur_CHGmax);
    data->max_discharge_current = BmsComm_SaturateS16((int32_t)OtherElement.u16CS_Cur_DSGmax);
    data->charge_dis_status = BmsComm_GetChargeDischargeStatus();
}
