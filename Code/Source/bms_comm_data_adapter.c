#include "bms_comm_data_adapter.h"

static uint16_t BmsComm_EncodePylonLocation(uint16_t point_index)
{
    if (point_index == 0U)
    {
        point_index = 1U;
    }
    if (point_index > 0x00FFU)
    {
        point_index = 0x00FFU;
    }

    return (uint16_t)(0x0100U | point_index);
}

static uint16_t BmsComm_EncodePylonTemperature(int16_t temp_c_x10)
{
    temp_c_x10 = (int16_t)(temp_c_x10 + 2730);
    if (temp_c_x10 < 0)
    {
        return 0U;
    }
    return (uint16_t)temp_c_x10;
}

static uint16_t BmsComm_SaturateU16(uint32_t value)
{
    if (value > 0xFFFFUL)
    {
        return 0xFFFFU;
    }

    return (uint16_t)value;
}

static uint16_t BmsComm_AppendAsciiField(uint8_t *dst, uint16_t idx, uint16_t capacity, const uint8_t *src, uint16_t src_len, uint16_t dst_len)
{
    uint16_t i;

    if ((uint16_t)(idx + dst_len) > capacity)
    {
        return 0U;
    }

    for (i = 0; i < dst_len; ++i)
    {
        dst[idx + i] = 0U;
    }

    for (i = 0; (i < dst_len) && (i < src_len); ++i)
    {
        if (src[i] == 0U)
        {
            break;
        }
        dst[idx + i] = src[i];
    }

    return (uint16_t)(idx + dst_len);
}

static uint8_t BmsComm_GetChargeDischargeStatus(void)
{
    uint8_t status;

    status = 0U;
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

uint16_t BmsComm_BuildBaseInfoPayload(uint8_t *info_buf, uint16_t capacity)
{
    uint16_t idx;
    uint16_t i;
    uint16_t battery_count;

    idx = 0U;
    idx = BmsComm_AppendAsciiField(info_buf, idx, capacity,
                                   ProductionInfor.BMS_HardWareVersion,
                                   ProductionInfor.BMS_HardWareVersionLength,
                                   10U);
    if (idx == 0U)
    {
        return 0U;
    }

    idx = BmsComm_AppendAsciiField(info_buf, idx, capacity, (const uint8_t *)"BMS", 3U, 20U);
    if (idx == 0U)
    {
        return 0U;
    }

    if ((uint16_t)(idx + 2U) > capacity)
    {
        return 0U;
    }
    info_buf[idx++] = (ProductionInfor.BMS_SoftWareVersionLength > 0U) ? ProductionInfor.BMS_SoftWareVersion[0] : '0';
    info_buf[idx++] = (ProductionInfor.BMS_SoftWareVersionLength > 1U) ? ProductionInfor.BMS_SoftWareVersion[1] : '0';

    battery_count = SeriesNum;
    if (battery_count > CELL_MAX_NUM)
    {
        battery_count = CELL_MAX_NUM;
    }
    if (capacity < (uint16_t)(33U + (battery_count * 16U)))
    {
        return 0U;
    }

    info_buf[idx++] = (uint8_t)battery_count;

    for (i = 0U; i < battery_count; ++i)
    {
        idx = BmsComm_AppendAsciiField(info_buf, idx, capacity,
                                       ProductionInfor.BMS_SerialNumber,
                                       ProductionInfor.BMS_SerialNumberLength,
                                       16U);
        if (idx == 0U)
        {
            return 0U;
        }
    }

    return idx;
}

uint16_t BmsComm_BuildAnalogPayload(uint8_t *info_buf, uint16_t capacity)
{
    uint16_t cell_avg_temp;
    uint16_t cell_max_temp;
    uint16_t cell_min_temp;
    uint16_t cell_max_location;
    uint16_t cell_min_location;
    uint16_t mos_temp;
    uint16_t temp_raw;
    uint16_t idx;
    uint8_t valid_count;
    uint8_t i;
    uint8_t temp_max_index;
    uint8_t temp_min_index;
    uint32_t temp_sum;
    int16_t pack_current;

    if (capacity < 51U)
    {
        return 0U;
    }

#define APPEND_U16(v)                  \
    do                                 \
    {                                  \
        info_buf[idx++] = (uint8_t)((v) >> 8); \
        info_buf[idx++] = (uint8_t)(v);        \
    } while (0)

#define APPEND_U8(v)   \
    do                 \
    {                  \
        info_buf[idx++] = (uint8_t)(v); \
    } while (0)

    pack_current = (g_stCellInfoReport.u16Ichg > 0U) ? (int16_t)g_stCellInfoReport.u16Ichg : (int16_t)(-((int16_t)g_stCellInfoReport.u16IDischg));
    temp_sum = 0UL;
    valid_count = 0U;
    temp_max_index = 0U;
    temp_min_index = 0U;
    cell_max_temp = 0U;
    cell_min_temp = 0xFFFFU;

    for (i = AFE1_TEMP1; i <= AFE2_TEMP3; ++i)
    {
        temp_raw = g_stCellInfoReport.u16Temperature[i];
        if (temp_raw == 0U)
        {
            continue;
        }
        temp_sum += temp_raw;
        if (temp_raw > cell_max_temp)
        {
            cell_max_temp = temp_raw;
            temp_max_index = i;
        }
        if (temp_raw < cell_min_temp)
        {
            cell_min_temp = temp_raw;
            temp_min_index = i;
        }
        ++valid_count;
    }

    if (valid_count == 0U)
    {
        cell_avg_temp = 0xFFFFU;
        cell_max_temp = 0xFFFFU;
        cell_min_temp = 0xFFFFU;
        cell_max_location = 0xFFFFU;
        cell_min_location = 0xFFFFU;
    }
    else
    {
        cell_avg_temp = BmsComm_EncodePylonTemperature((int16_t)(temp_sum / valid_count) - 400);
        cell_max_temp = BmsComm_EncodePylonTemperature((int16_t)cell_max_temp - 400);
        cell_min_temp = BmsComm_EncodePylonTemperature((int16_t)cell_min_temp - 400);
        cell_max_location = BmsComm_EncodePylonLocation((uint16_t)(temp_max_index + 1U));
        cell_min_location = BmsComm_EncodePylonLocation((uint16_t)(temp_min_index + 1U));
    }

    temp_raw = g_stCellInfoReport.u16Temperature[MOS_TEMP1];
    if (temp_raw == 0U)
    {
        mos_temp = 0xFFFFU;
    }
    else
    {
        mos_temp = BmsComm_EncodePylonTemperature((int16_t)temp_raw - 400);
    }

    idx = 0U;
    APPEND_U16(BmsComm_SaturateU16((uint32_t)g_stCellInfoReport.u16VCellTotle * 10UL));
    APPEND_U16((uint16_t)(pack_current * 10));
    APPEND_U8(g_stCellInfoReport.SocElement.u16Soc);
    APPEND_U16(g_stCellInfoReport.SocElement.u16Cycle_times);
    APPEND_U16(g_stCellInfoReport.SocElement.u16Cycle_times);
    APPEND_U8(g_stCellInfoReport.SocElement.u16Soh);
    APPEND_U8(g_stCellInfoReport.SocElement.u16Soh);
    APPEND_U16(g_stCellInfoReport.u16VCellMax);
    APPEND_U16(BmsComm_EncodePylonLocation(g_stCellInfoReport.u16VCellMaxPosition));
    APPEND_U16(g_stCellInfoReport.u16VCellMin);
    APPEND_U16(BmsComm_EncodePylonLocation(g_stCellInfoReport.u16VCellMinPosition));
    APPEND_U16(cell_avg_temp);
    APPEND_U16(cell_max_temp);
    APPEND_U16(cell_max_location);
    APPEND_U16(cell_min_temp);
    APPEND_U16(cell_min_location);
    APPEND_U16(mos_temp);
    APPEND_U16(mos_temp);
    APPEND_U16((mos_temp == 0xFFFFU) ? 0xFFFFU : 0x0101U);
    APPEND_U16(mos_temp);
    APPEND_U16((mos_temp == 0xFFFFU) ? 0xFFFFU : 0x0101U);
    APPEND_U16(0xFFFFU);
    APPEND_U16(0xFFFFU);
    APPEND_U16(0xFFFFU);
    APPEND_U16(0xFFFFU);
    APPEND_U16(0xFFFFU);

#undef APPEND_U8
#undef APPEND_U16

    return idx;
}

uint16_t BmsComm_BuildAlarmPayload(uint8_t *info_buf, uint16_t capacity)
{
    uint8_t bms_error;
    union MDLCHGFAULT_REG alarm_fault;
    union MDLCHGFAULT_REG protect_fault;
    uint16_t idx;

    if (capacity < 4U)
    {
        return 0U;
    }

    alarm_fault = g_stCellInfoReport.unMdlFault_Second;
    protect_fault = g_stCellInfoReport.unMdlFault_Third;
    bms_error = (uint8_t)((SystemStatus.bits.b1StartUpBMS != 0U) && (SystemStatus.bits.b1Status_AFE1 == 0U));

    idx = 0U;
    info_buf[idx++] = (uint8_t)((alarm_fault.bits.b1BatOvp << 7) |
                                (alarm_fault.bits.b1BatUvp << 6) |
                                (alarm_fault.bits.b1CellOvp << 5) |
                                (alarm_fault.bits.b1CellUvp << 4) |
                                (((alarm_fault.bits.b1CellChgOtp != 0U) || (alarm_fault.bits.b1CellDischgOtp != 0U)) << 3) |
                                (((alarm_fault.bits.b1CellChgUtp != 0U) || (alarm_fault.bits.b1CellDischgUtp != 0U)) << 2) |
                                (alarm_fault.bits.b1TmosOtp << 1) |
                                alarm_fault.bits.b1VcellDeltaBig);
    info_buf[idx++] = (uint8_t)((alarm_fault.bits.b1TempDeltaBig << 7) |
                                (alarm_fault.bits.b1IchgOcp << 6) |
                                (alarm_fault.bits.b1IdischgOcp << 5) |
                                (bms_error << 4));
    info_buf[idx++] = (uint8_t)((protect_fault.bits.b1BatOvp << 7) |
                                (protect_fault.bits.b1BatUvp << 6) |
                                (protect_fault.bits.b1CellOvp << 5) |
                                (protect_fault.bits.b1CellUvp << 4) |
                                (((protect_fault.bits.b1CellChgOtp != 0U) || (protect_fault.bits.b1CellDischgOtp != 0U)) << 3) |
                                (((protect_fault.bits.b1CellChgUtp != 0U) || (protect_fault.bits.b1CellDischgUtp != 0U)) << 2) |
                                (protect_fault.bits.b1TmosOtp << 1));
    info_buf[idx++] = (uint8_t)((protect_fault.bits.b1IchgOcp << 7) |
                                (protect_fault.bits.b1IdischgOcp << 6) |
                                (bms_error << 4));

    return idx;
}

uint16_t BmsComm_BuildChargeDischargePayload(uint8_t *info_buf, uint16_t capacity)
{
    uint16_t charge_volt_limit;
    uint16_t discharge_volt_limit;
    uint32_t series_count;
    uint16_t idx;

    series_count = (SeriesNum > 0U) ? SeriesNum : 1U;
    if (capacity < 9U)
    {
        return 0U;
    }

    charge_volt_limit = BmsComm_SaturateU16((uint32_t)OtherElement.u16Soc_V_100 * series_count);
    discharge_volt_limit = BmsComm_SaturateU16((uint32_t)OtherElement.u16Soc_V_0 * series_count);

    idx = 0U;
    info_buf[idx++] = (uint8_t)(charge_volt_limit >> 8);
    info_buf[idx++] = (uint8_t)charge_volt_limit;
    info_buf[idx++] = (uint8_t)(discharge_volt_limit >> 8);
    info_buf[idx++] = (uint8_t)discharge_volt_limit;
    info_buf[idx++] = (uint8_t)(OtherElement.u16CS_Cur_CHGmax >> 8);
    info_buf[idx++] = (uint8_t)OtherElement.u16CS_Cur_CHGmax;
    info_buf[idx++] = (uint8_t)(OtherElement.u16CS_Cur_DSGmax >> 8);
    info_buf[idx++] = (uint8_t)OtherElement.u16CS_Cur_DSGmax;
    info_buf[idx++] = BmsComm_GetChargeDischargeStatus();

    return idx;
}
