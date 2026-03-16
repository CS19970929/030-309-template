#ifndef COMM_H
#define COMM_H

#include "stm32f0xx.h"
#include "Sci_Upper.h"
#include "ascii_slave.h"

/* Keep this header self-contained. Do not include main.h here. */
typedef enum {
    PROTO_NONE = 0,
    PROTO_MODBUS_RTU,
    PROTO_ASCII
} ProtocolType;

typedef struct {
    void (*enter_critical)(void);
    void (*exit_critical)(void);
    int32_t (*get_runtime_ms)(void);
    int32_t (*check_elapsed_ms)(int32_t last_tick);
    void (*set_rs485_tx_mode)(uint8_t port_id);
    void (*set_rs485_rx_mode)(uint8_t port_id);
    void (*delay_us)(uint32_t us);
    void (*notify_rx_activity)(uint8_t port_id);
    void (*notify_tx_complete)(uint8_t port_id);
} CommPlatformOps;

typedef struct {
    USART_TypeDef *instance;
    uint8_t port_id;
    IRQn_Type irq_channel;
    FunctionalState clock_cmd;
    uint32_t peripheral_clock;
    GPIO_TypeDef *gpio_port;
    uint16_t gpio_pins;
    uint8_t tx_pin_source;
    uint8_t rx_pin_source;
    uint8_t gpio_af;
    uint32_t baud_rate;
} CommPortConfig;

#define COMM_RX_RING_SIZE           56
#define COMM_RTU_RX_TIMEOUT_MS      20
#define COMM_ASCII_RX_TIMEOUT_MS    100
#define COMM_RS485_TURNAROUND_US    100

typedef struct {
    uint16_t length;
    uint16_t expected_length;
    ProtocolType protocol;
    union {
        ModbusRtuParser modbus;
        AsciiParser ascii;
    } parser;
} CommRxState;

typedef struct {
    const CommPortConfig *config;
    const CommPlatformOps *ops;
    USART_TypeDef *instance;
    uint8_t port_id;
    volatile uint8_t frame_ready_flag;
    volatile uint8_t tx_active;
    volatile uint16_t error_count;
    volatile uint16_t rx_len;
    volatile uint16_t ring_head;
    volatile uint16_t ring_tail;
    volatile uint16_t tx_len;
    volatile uint16_t tx_pos;
    volatile int32_t last_rx_tick;
    volatile uint16_t rx_timeout_ms;
    volatile ProtocolType active_protocol;
    uint8_t ring_buf[COMM_RX_RING_SIZE];
    uint8_t tx_buf[MAX_FRAME_LEN];
    CommRxState rx_state;
} CommPortContext;

extern CommPortContext g_comm_port1;
extern CommPortContext g_comm_port2;

void Comm_InitAll(void);
void Comm_PollAll(void);
void Comm_PortIrqHandler(CommPortContext *ctx);
void Comm_PortStartTx(CommPortContext *ctx, const uint8_t *data, uint16_t len);
void Comm_PortTxPump(CommPortContext *ctx);

#endif
