#ifndef COMM_H
#define COMM_H

#include "main.h"
#include "ascii_slave.h"
#include "modbus_rtu_parser.h"

typedef enum {
    PROTO_NONE = 0,
    PROTO_MODBUS_RTU,
    PROTO_ASCII
} ProtocolType;

typedef struct {
    ProtocolType protocol;
    uint8_t *payload;
    uint16_t payload_len;
} CommRequest;

typedef struct {
    uint8_t should_reply;
    uint8_t *payload;
    uint16_t payload_len;
} CommResponse;

typedef struct {
    USART_TypeDef *instance;
    uint8_t port_id;
    uint8_t frame_ready_flag;
    uint8_t tx_active;
    uint16_t error_count;
    uint16_t rx_len;
    uint16_t tx_len;
    uint16_t tx_pos;
    ProtocolType active_protocol;
    uint8_t rx_buf[MAX_FRAME_LEN];
    uint8_t tx_buf[MAX_FRAME_LEN];
    struct RS485MSG modbus_ctx;
    ModbusRtuParser modbus_parser;
    AsciiParser ascii_parser;
} CommPortContext;

extern CommPortContext g_comm_port1;
extern CommPortContext g_comm_port2;

void Comm_InitAll(void);
void Comm_PollAll(void);
void Comm_PortIrqHandler(CommPortContext *ctx);
void Comm_PortStartTx(CommPortContext *ctx, const uint8_t *data, uint16_t len);
void Comm_PortTxPump(CommPortContext *ctx);

#endif
