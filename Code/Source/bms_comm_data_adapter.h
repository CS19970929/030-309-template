#ifndef BMS_COMM_DATA_ADAPTER_H
#define BMS_COMM_DATA_ADAPTER_H

#include "main.h"
#include "ascii_slave.h"

uint16_t BmsComm_BuildBaseInfoPayload(uint8_t *info_buf, uint16_t capacity);
uint16_t BmsComm_BuildAnalogPayload(uint8_t *info_buf, uint16_t capacity);
uint16_t BmsComm_BuildAlarmPayload(uint8_t *info_buf, uint16_t capacity);
uint16_t BmsComm_BuildChargeDischargePayload(uint8_t *info_buf, uint16_t capacity);

#endif
