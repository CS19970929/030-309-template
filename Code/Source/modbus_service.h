#ifndef MODBUS_SERVICE_H
#define MODBUS_SERVICE_H

#include "main.h"

void Modbus_ServiceInit(struct RS485MSG *msg);
uint16_t Modbus_ServiceHandleFrame(struct RS485MSG *msg, const uint8_t *frame, uint16_t frame_len, uint8_t *tx_buf);

#endif
