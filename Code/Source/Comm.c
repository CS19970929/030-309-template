#include "Comm.h"

#include "ascii_slave.h"
#include "modbus_service.h"

CommPortContext g_comm_port1;
CommPortContext g_comm_port2;

static void Comm_PortResetRx(CommPortContext *ctx)
{
    ctx->active_protocol = PROTO_NONE;
    ctx->frame_ready_flag = 0;
    ctx->rx_len = 0;
    AsciiParser_Reset(&ctx->ascii_parser);
    ModbusRtuParser_Reset(&ctx->modbus_parser);
}

static void Comm_PortInit(CommPortContext *ctx, USART_TypeDef *instance, uint8_t port_id)
{
    GPIO_InitTypeDef gpio_init;
    USART_InitTypeDef usart_init;
    NVIC_InitTypeDef nvic_init;

    memset(ctx, 0, sizeof(*ctx));
    ctx->instance = instance;
    ctx->port_id = port_id;
    Modbus_ServiceInit(&ctx->modbus_ctx);
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
    USART_ITConfig(instance, USART_IT_RXNE, ENABLE);
}

static void Comm_PortCaptureFrame(CommPortContext *ctx, const uint8_t *frame, uint16_t frame_len)
{
    memcpy(ctx->rx_buf, frame, frame_len);
    ctx->rx_len = frame_len;
    ctx->frame_ready_flag = 1;
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
            AsciiParser_Reset(&ctx->ascii_parser);
        }
        else if ((byte == RS485_SLAVE_ADDR) || (byte == RS485_BROADCAST_ADDR))
        {
            ctx->active_protocol = PROTO_MODBUS_RTU;
            ModbusRtuParser_Reset(&ctx->modbus_parser);
        }
        else
        {
            return;
        }
    }

    if (ctx->active_protocol == PROTO_ASCII)
    {
        result = AsciiParser_ConsumeByte(&ctx->ascii_parser, byte);
        if (result == PROTO_PARSE_FRAME_READY)
        {
            Comm_PortCaptureFrame(ctx, ctx->ascii_parser.buffer, ctx->ascii_parser.length);
        }
        else if (result == PROTO_PARSE_FRAME_INVALID)
        {
            Comm_PortResetRx(ctx);
        }
    }
    else if (ctx->active_protocol == PROTO_MODBUS_RTU)
    {
        result = ModbusRtuParser_ConsumeByte(&ctx->modbus_parser, byte);
        if (result == PROTO_PARSE_FRAME_READY)
        {
            Comm_PortCaptureFrame(ctx, ctx->modbus_parser.buffer, ctx->modbus_parser.length);
        }
        else if (result == PROTO_PARSE_FRAME_INVALID)
        {
            Comm_PortResetRx(ctx);
        }
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
        tx_len = Ascii_HandleFrame(ctx->rx_buf, ctx->rx_len, ctx->tx_buf);
    }
    else if (ctx->active_protocol == PROTO_MODBUS_RTU)
    {
        tx_len = Modbus_ServiceHandleFrame(&ctx->modbus_ctx, ctx->rx_buf, ctx->rx_len, ctx->tx_buf);
    }

    ctx->frame_ready_flag = 0;
    ctx->rx_len = 0;
    ctx->active_protocol = PROTO_NONE;
    AsciiParser_Reset(&ctx->ascii_parser);
    ModbusRtuParser_Reset(&ctx->modbus_parser);

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
    Comm_PortDispatch(&g_comm_port1);
    Comm_PortTxPump(&g_comm_port1);
#endif
#ifdef _COMMOM_UPPER_SCI2
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

    if (ctx->tx_active != 0)
    {
        return;
    }

    Comm_PortFeedByte(ctx, rx_byte);
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
    ctx->tx_active = 1;
    TRANS_EN_485();
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
        RECV_EN_485();
        ctx->tx_active = 0;
        ctx->tx_len = 0;
        ctx->tx_pos = 0;
        if (u8FlashUpdateE2PROM != 0)
        {
            u8FlashUpdateE2PROM = 0;
            u8FlashUpdateFlag = 1;
        }
    }
}
