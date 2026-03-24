#ifndef ASCII_SLAVE_H
#define ASCII_SLAVE_H

#include "stm32f0xx.h"
#include <stdint.h>

#define ASCII_SLAVE_SOF             ((uint8_t)0x7E)
#define ASCII_SLAVE_EOF             ((uint8_t)0x0D)
#define ASCII_SLAVE_PROTOCOL_VER    ((uint8_t)0x20)
#define ASCII_SLAVE_ADDR            ((uint8_t)0x02)
#define ASCII_SLAVE_CID1_BATTERY    ((uint8_t)0x46)

#define ASCII_SLAVE_CMD_BATTERY_INFO      ((uint8_t)0x60)
#define ASCII_SLAVE_CMD_ANALOG1_DATA      ((uint8_t)0x61)
#define ASCII_SLAVE_CMD_ANALOG2_DATA      ((uint8_t)0x42)
#define ASCII_SLAVE_CMD_ALARM_INFO        ((uint8_t)0x62)
#define ASCII_SLAVE_CMD_CHARGE_DIS_INFO   ((uint8_t)0x63)

#define ASCII_SLAVE_RTN_OK           ((uint8_t)0x00)
#define ASCII_SLAVE_RTN_VER_ERROR    ((uint8_t)0x01)
#define ASCII_SLAVE_RTN_CHKSUM_ERROR ((uint8_t)0x02)
#define ASCII_SLAVE_RTN_LCHKSUM_ERR  ((uint8_t)0x03)
#define ASCII_SLAVE_RTN_CID2_INVALID ((uint8_t)0x04)
#define ASCII_SLAVE_RTN_FORMAT_ERROR ((uint8_t)0x05)
#define ASCII_SLAVE_RTN_DATA_INVALID ((uint8_t)0x06)
#define ASCII_SLAVE_RTN_ADDR_ERROR   ((uint8_t)0x90)

#define ASCII_SLAVE_MAX_FRAME_LEN    600u

typedef struct
{
    uint8_t rx_buf[ASCII_SLAVE_MAX_FRAME_LEN];
    uint8_t tx_buf[ASCII_SLAVE_MAX_FRAME_LEN];
    uint16_t rx_len;
    uint16_t tx_len;
    uint8_t frame_ready;
    uint8_t receiving;
    int32_t last_rx_tick;
} ASCII_SLAVE_CTX;

void AsciiSlave_Init(ASCII_SLAVE_CTX *ctx);
void AsciiSlave_Reset(ASCII_SLAVE_CTX *ctx);
uint8_t AsciiSlave_InputByte(ASCII_SLAVE_CTX *ctx, uint8_t data, int32_t now_ms);
void AsciiSlave_CheckTimeout(ASCII_SLAVE_CTX *ctx, int32_t now_ms, uint16_t timeout_ms);
uint16_t AsciiSlave_ProcessFrame(ASCII_SLAVE_CTX *ctx);
void AsciiSlave_SendBuffer(USART_TypeDef *uart, uint8_t *buf, uint16_t len, uint8_t use_rs485_dir);

#endif
