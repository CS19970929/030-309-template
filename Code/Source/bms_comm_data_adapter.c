#include "bms_comm_data_adapter.h"

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
    int16_t pack_current;
    int16_t avg_temp;

    memset(data, 0, sizeof(*data));

    pack_current = (g_stCellInfoReport.u16Ichg > 0) ? (int16_t)g_stCellInfoReport.u16Ichg : (int16_t)(-((int16_t)g_stCellInfoReport.u16IDischg));
    avg_temp = (int16_t)((((int32_t)g_stCellInfoReport.u16TempMax + (int32_t)g_stCellInfoReport.u16TempMin) / 2) - 400);

    data->pack_total_avg_voltage = g_stCellInfoReport.u16VCellTotle;
    data->pack_total_current = pack_current;
    data->pack_soc = (uint8_t)g_stCellInfoReport.SocElement.u16Soc;
    data->pack_avg_cycle_count = g_stCellInfoReport.SocElement.u16Cycle_times;
    data->pack_max_cycle_count = g_stCellInfoReport.SocElement.u16Cycle_times;
    data->pack_avg_soh = (uint8_t)g_stCellInfoReport.SocElement.u16Soh;
    data->pack_min_soh = (uint8_t)g_stCellInfoReport.SocElement.u16Soh;

    data->cell_max_voltage = g_stCellInfoReport.u16VCellMax;
    data->cell_max_voltage_module = g_stCellInfoReport.u16VCellMaxPosition;
    data->cell_min_voltage = g_stCellInfoReport.u16VCellMin;
    data->cell_min_voltage_module = g_stCellInfoReport.u16VCellMinPosition;

    data->cell_avg_temp = avg_temp;
    data->cell_max_temp = (int16_t)g_stCellInfoReport.u16TempMax - 400;
    data->cell_max_temp_module = 1;
    data->cell_min_temp = (int16_t)g_stCellInfoReport.u16TempMin - 400;
    data->cell_min_temp_module = 1;

    data->mosfet_avg_temp = avg_temp;
    data->mosfet_max_temp = data->cell_max_temp;
    data->mosfet_max_temp_module = 1;
    data->mosfet_min_temp = data->cell_min_temp;
    data->mosfet_min_temp_module = 1;

    data->bms_avg_temp = avg_temp;
    data->bms_max_temp = data->cell_max_temp;
    data->bms_max_temp_module = 1;
    data->bms_min_temp = data->cell_min_temp;
    data->bms_min_temp_module = 1;
}

void BmsComm_GetAlarmData(Battery_Alarm_T *data)
{
    memset(data, 0, sizeof(*data));

    data->system_alarm1 = (uint8_t)(g_stCellInfoReport.unMdlFault_Second.all & 0x00FF);
    data->system_alarm2 = (uint8_t)((g_stCellInfoReport.unMdlFault_Second.all >> 8) & 0x00FF);
    data->system_protect1 = (uint8_t)(g_stCellInfoReport.unMdlFault_Third.all & 0x00FF);
    data->system_protect2 = (uint8_t)((g_stCellInfoReport.unMdlFault_Third.all >> 8) & 0x00FF);
}

void BmsComm_GetChargeDischargeInfo(Battery_Charge_Dis_Info_T *data)
{
    memset(data, 0, sizeof(*data));

    data->charge_volt_limit = OtherElement.u16Soc_V_100;
    data->discharge_volt_limit = OtherElement.u16Soc_V_0;
    data->max_charge_current = (int16_t)(OtherElement.u16CS_Cur_CHGmax * 10);
    data->max_discharge_current = (int16_t)(OtherElement.u16CS_Cur_DSGmax * 10);

    if (g_stCellInfoReport.u16Ichg > 0)
    {
        data->charge_dis_status = 0x80;
    }
    else if (g_stCellInfoReport.u16IDischg > 0)
    {
        data->charge_dis_status = 0x40;
    }
    else
    {
        data->charge_dis_status = 0x00;
    }
}
