#ifndef MODBUS_PROTO_H
#define MODBUS_PROTO_H

#include "stm32f0xx.h"

#define MODBUS_BROADCAST_ADDR       ((UINT8)0x00)
#define MODBUS_SLAVE_ADDR           ((UINT8)0x01)
#define MODBUS_MAX_ADU_SIZE         251
#define MODBUS_FC_READ_REGS         3U
#define MODBUS_FC_WRITE_REG         6U
#define MODBUS_FC_WRITE_REGS        16U

#define RS485_BROADCAST_ADDR        MODBUS_BROADCAST_ADDR
#define RS485_SLAVE_ADDR            MODBUS_SLAVE_ADDR
#define RS485_MAX_BUFFER_SIZE       MODBUS_MAX_ADU_SIZE

#endif
