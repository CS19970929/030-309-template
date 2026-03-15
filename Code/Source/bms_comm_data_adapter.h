#ifndef BMS_COMM_DATA_ADAPTER_H
#define BMS_COMM_DATA_ADAPTER_H

#include "main.h"
#include "ascii_slave.h"

void BmsComm_GetBaseInfo(Battery_Base_Info_T *info);
void BmsComm_GetAnalogData(Battery_Analog_T *data);
void BmsComm_GetAlarmData(Battery_Alarm_T *data);
void BmsComm_GetChargeDischargeInfo(Battery_Charge_Dis_Info_T *data);

#endif
