#include "Comm.h"

#include "ascii_slave.h"
#include "modbus_service.h"

CommPortContext g_comm_port1;
CommPortContext g_comm_port2;
static struct RS485MSG g_modbus_service_ctx;

static uint16_t Comm_RingNext(uint16_t index)
{
    return (uint16_t)((index + 1U) % COMM_RX_RING_SIZE);
}

static uint8_t Comm_RingIsEmpty(const CommPortContext *ctx)
{
    return (uint8_t)(ctx->ring_head == ctx->ring_tail);
}

static uint8_t Comm_RingPushByte(CommPortContext *ctx, uint8_t byte)
{
    uint16_t next_head;

    next_head = Comm_RingNext(ctx->ring_head);
    if (next_head == ctx->ring_tail)
    {
        return 0;
    }

    ctx->ring_buf[ctx->ring_head] = byte;
    ctx->ring_head = next_head;
    return 1;
}

static uint8_t Comm_RingPopByte(CommPortContext *ctx, uint8_t *byte)
{
    if (Comm_RingIsEmpty(ctx) != 0)
    {
        return 0;
    }

    *byte = ctx->ring_buf[ctx->ring_tail];
    ctx->ring_tail = Comm_RingNext(ctx->ring_tail);
    return 1;
}

static void Comm_PortDisableRx(CommPortContext *ctx)
{
    USART_ITConfig(ctx->instance, USART_IT_RXNE, DISABLE);
    ctx->instance->CR1 &= ~(1 << 2);
}

static void Comm_PortEnableRx(CommPortContext *ctx)
{
    ctx->instance->CR1 |= (1 << 2);
    USART_ITConfig(ctx->instance, USART_IT_RXNE, ENABLE);
}

static void Comm_PortResetRx(CommPortContext *ctx)
{
    DISABLE_INT();
    ctx->active_protocol = PROTO_NONE;
    ctx->frame_ready_flag = 0;
    ctx->rx_len = 0;
    ctx->rx_timeout_ms = 0;
    ctx->ring_head = 0;
    ctx->ring_tail = 0;
    ctx->rx_state.protocol = PROTO_NONE;
    ctx->rx_state.length = 0;
    ctx->rx_state.expected_length = 0;
    AsciiParser_Reset(&ctx->rx_state.parser.ascii);
    ModbusRtuParser_Reset(&ctx->rx_state.parser.modbus);
    ENABLE_INT();
}

static void Comm_PortInit(CommPortContext *ctx, USART_TypeDef *instance, uint8_t port_id)
{
    GPIO_InitTypeDef gpio_init;
    USART_InitTypeDef usart_init;
    NVIC_InitTypeDef nvic_init;

    memset(ctx, 0, sizeof(*ctx));
    ctx->instance = instance;
    ctx->port_id = port_id;
    Comm_PortResetRx(ctx);

    if (instance == USART1)
    {
        RCC_APB2PeriphClockCmd(RCC_APB2Periph_USART1, ENABLE);
        nvic_init.NVIC_IRQChannel = USART1_IRQn;
        GPIO_PinAFConfig(GPIOA, GPIO_PinSource9, GPIO_AF_1);
        GPIO_PinAFConfig(GPIOA, GPIO_PinSource10, GPIO_AF_1);
        gpio_init.GPIO_Pin = GPIO_Pin_9 | GPIO_Pin_10;
    }
    else
    {
        RCC_APB1PeriphClockCmd(RCC_APB1Periph_USART2, ENABLE);
        nvic_init.NVIC_IRQChannel = USART2_IRQn;
        GPIO_PinAFConfig(GPIOA, GPIO_PinSource2, GPIO_AF_1);
        GPIO_PinAFConfig(GPIOA, GPIO_PinSource3, GPIO_AF_1);
        gpio_init.GPIO_Pin = GPIO_Pin_2 | GPIO_Pin_3;
    }

    nvic_init.NVIC_IRQChannelPriority = 0;
    nvic_init.NVIC_IRQChannelCmd = ENABLE;
    NVIC_Init(&nvic_init);

    gpio_init.GPIO_Mode = GPIO_Mode_AF;
    gpio_init.GPIO_OType = GPIO_OType_PP;
    gpio_init.GPIO_PuPd = GPIO_PuPd_UP;
    gpio_init.GPIO_Speed = GPIO_Speed_2MHz;
    GPIO_Init(GPIOA, &gpio_init);

    usart_init.USART_BaudRate = 19200;
    usart_init.USART_WordLength = USART_WordLength_8b;
    usart_init.USART_StopBits = USART_StopBits_1;
    usart_init.USART_Parity = USART_Parity_No;
    usart_init.USART_HardwareFlowControl = USART_HardwareFlowControl_None;
    usart_init.USART_Mode = USART_Mode_Rx | USART_Mode_Tx;
    USART_Init(instance, &usart_init);

    instance->CR3 |= (1 << 0);
    instance->CR3 |= (1 << 11);
    USART_Cmd(instance, ENABLE);
    Comm_PortEnableRx(ctx);
}

static const uint8_t *Comm_PortGetFrameBuffer(const CommPortContext *ctx)
{
    if (ctx->active_protocol == PROTO_ASCII)
    {
        return ctx->rx_state.parser.ascii.buffer;
    }

    return ctx->rx_state.parser.modbus.buffer;
}

static void Comm_PortFeedByte(CommPortContext *ctx, uint8_t byte)
{
    ProtocolParseResult result;

    if (ctx->frame_ready_flag != 0)
    {
        return;
    }

    if (ctx->active_protocol == PROTO_NONE)
    {
        if (byte == SOI)
        {
            ctx->active_protocol = PROTO_ASCII;
            ctx->rx_state.protocol = PROTO_ASCII;
            ctx->rx_timeout_ms = COMM_ASCII_RX_TIMEOUT_MS;
            AsciiParser_Reset(&ctx->rx_state.parser.ascii);
        }
        else if ((byte == RS485_SLAVE_ADDR) || (byte == RS485_BROADCAST_ADDR))
        {
            ctx->active_protocol = PROTO_MODBUS_RTU;
            ctx->rx_state.protocol = PROTO_MODBUS_RTU;
            ctx->rx_timeout_ms = COMM_RTU_RX_TIMEOUT_MS;
            ModbusRtuParser_Reset(&ctx->rx_state.parser.modbus);
        }
        else
        {
            return;
        }
    }

    if (ctx->active_protocol == PROTO_ASCII)
    {
        result = AsciiParser_ConsumeByte(&ctx->rx_state.parser.ascii, byte);
        if (result == PROTO_PARSE_FRAME_READY)
        {
            ctx->rx_len = ctx->rx_state.parser.ascii.length;
            ctx->frame_ready_flag = 1;
        }
        else if (result == PROTO_PARSE_FRAME_INVALID)
        {
            Comm_PortResetRx(ctx);
        }
    }
    else if (ctx->active_protocol == PROTO_MODBUS_RTU)
    {
        result = ModbusRtuParser_ConsumeByte(&ctx->rx_state.parser.modbus, byte);
        if (result == PROTO_PARSE_FRAME_READY)
        {
            ctx->rx_len = ctx->rx_state.parser.modbus.length;
            ctx->frame_ready_flag = 1;
        }
        else if (result == PROTO_PARSE_FRAME_INVALID)
        {
            Comm_PortResetRx(ctx);
        }
    }
}

static void Comm_PortDrainRxRing(CommPortContext *ctx)
{
    uint8_t rx_byte;

    if (ctx->tx_active != 0)
    {
        return;
    }

    while ((ctx->frame_ready_flag == 0) && (Comm_RingPopByte(ctx, &rx_byte) != 0))
    {
        Comm_PortFeedByte(ctx, rx_byte);
    }
}

static void Comm_PortCheckTimeout(CommPortContext *ctx)
{
    int32_t elapsed_ms;

    if (ctx->active_protocol == PROTO_NONE)
    {
        return;
    }

    if (ctx->frame_ready_flag != 0)
    {
        return;
    }

    elapsed_ms = bsp_CheckRunTime(ctx->last_rx_tick);
    if ((elapsed_ms >= 0) && ((uint16_t)elapsed_ms >= ctx->rx_timeout_ms))
    {
        Comm_PortResetRx(ctx);
    }
}

static void Comm_PortHandleErrors(CommPortContext *ctx)
{
    uint8_t fault_count = 0;

    if (ctx->instance->ISR & 0x08)
    {
        ctx->instance->ICR |= 1 << 3;
        fault_count++;
    }
    if (ctx->instance->ISR & 0x04)
    {
        ctx->instance->ICR |= 1 << 2;
        fault_count++;
    }
    if (ctx->instance->ISR & 0x02)
    {
        ctx->instance->ICR |= 1 << 1;
        fault_count++;
    }
    if (ctx->instance->ISR & 0x01)
    {
        ctx->instance->ICR |= 1 << 0;
        fault_count++;
    }

    if (fault_count != 0)
    {
        ctx->error_count++;
        Comm_PortResetRx(ctx);
    }
}

static void Comm_PortDispatch(CommPortContext *ctx)
{
    uint16_t tx_len = 0;

    if (ctx->frame_ready_flag == 0)
    {
        return;
    }

    if (ctx->tx_active != 0)
    {
        return;
    }

    if (ctx->active_protocol == PROTO_ASCII)
    {
        tx_len = Ascii_HandleFrame(Comm_PortGetFrameBuffer(ctx), ctx->rx_len, ctx->tx_buf);
    }
    else if (ctx->active_protocol == PROTO_MODBUS_RTU)
    {
        tx_len = Modbus_ServiceHandleFrame(&g_modbus_service_ctx, Comm_PortGetFrameBuffer(ctx), ctx->rx_len, ctx->tx_buf);
    }

    ctx->frame_ready_flag = 0;
    ctx->rx_len = 0;
    ctx->active_protocol = PROTO_NONE;
    ctx->rx_state.protocol = PROTO_NONE;
    AsciiParser_Reset(&ctx->rx_state.parser.ascii);
    ModbusRtuParser_Reset(&ctx->rx_state.parser.modbus);

    if (tx_len > 0)
    {
        Comm_PortStartTx(ctx, ctx->tx_buf, tx_len);
    }
}

void Comm_InitAll(void)
{
#ifdef _COMMOM_UPPER_SCI1
    Comm_PortInit(&g_comm_port1, USART1, 1);
#endif
#ifdef _COMMOM_UPPER_SCI2
    Comm_PortInit(&g_comm_port2, USART2, 2);
#endif
}

void Comm_PollAll(void)
{
#ifdef _COMMOM_UPPER_SCI1
    Comm_PortDrainRxRing(&g_comm_port1);
    Comm_PortCheckTimeout(&g_comm_port1);
    Comm_PortDispatch(&g_comm_port1);
    Comm_PortTxPump(&g_comm_port1);
#endif
#ifdef _COMMOM_UPPER_SCI2
    Comm_PortDrainRxRing(&g_comm_port2);
    Comm_PortCheckTimeout(&g_comm_port2);
    Comm_PortDispatch(&g_comm_port2);
    Comm_PortTxPump(&g_comm_port2);
#endif
}

void Comm_PortIrqHandler(CommPortContext *ctx)
{
    uint8_t rx_byte;

    Comm_PortHandleErrors(ctx);

    if (USART_GetITStatus(ctx->instance, USART_IT_RXNE) == RESET)
    {
        return;
    }

    rx_byte = (uint8_t)ctx->instance->RDR;

    if (ctx->instance == USART1)
    {
        RTC_ExtComCnt++;
        RTC_ExtComCnt1++;
    }
    else
    {
        RTC_ExtComCnt++;
    }

    ctx->last_rx_tick = bsp_GetRunTime();

    if (ctx->tx_active != 0)
    {
        return;
    }

    if (Comm_RingPushByte(ctx, rx_byte) == 0)
    {
        ctx->error_count++;
        Comm_PortResetRx(ctx);
        ctx->ring_tail = ctx->ring_head;
    }
}

void Comm_PortStartTx(CommPortContext *ctx, const uint8_t *data, uint16_t len)
{
    if ((len == 0) || (len > MAX_FRAME_LEN))
    {
        return;
    }

    memcpy(ctx->tx_buf, data, len);
    ctx->tx_len = len;
    ctx->tx_pos = 0;
    Comm_PortResetRx(ctx);
    Comm_PortDisableRx(ctx);
    USART_ClearFlag(ctx->instance, USART_FLAG_TC);
    ctx->tx_active = 1;
    TRANS_EN_485();
    __delay_us(COMM_RS485_TURNAROUND_US);
}

void Comm_PortTxPump(CommPortContext *ctx)
{
    if (ctx->tx_active == 0)
    {
        return;
    }

    if (ctx->tx_pos < ctx->tx_len)
    {
        if (USART_GetFlagStatus(ctx->instance, USART_FLAG_TXE) != RESET)
        {
            ctx->instance->TDR = ctx->tx_buf[ctx->tx_pos++];
        }
        return;
    }

    if (USART_GetFlagStatus(ctx->instance, USART_FLAG_TC) != RESET)
    {
        __delay_us(COMM_RS485_TURNAROUND_US);
        RECV_EN_485();
        ctx->tx_active = 0;
        ctx->tx_len = 0;
        ctx->tx_pos = 0;
        Comm_PortEnableRx(ctx);
        if (u8FlashUpdateE2PROM != 0)
        {
            u8FlashUpdateE2PROM = 0;
            u8FlashUpdateFlag = 1;
        }
    }
}
