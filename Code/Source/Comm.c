#include "Comm.h"

#include "ascii_slave.h"
#include "SleepDeal.h"
#include "System_Init.h"
#include "BSP\\bsp.h"
#include "BSP\\bsp_timer.h"
#include "conf_gpio.h"
#include "modbus_proto.h"
#include "modbus_service.h"
#include "main.h"

CommPortContext g_comm_port1;
CommPortContext g_comm_port2;
static struct RS485MSG g_modbus_service_ctx;
static void Comm_EnterCritical(void);
static void Comm_ExitCritical(void);
static int32_t Comm_GetRuntimeMs(void);
static int32_t Comm_CheckElapsedMs(int32_t last_tick);
static void Comm_SetRs485TxMode(CommPortContext *ctx);
static void Comm_SetRs485RxMode(CommPortContext *ctx);
static void Comm_DelayUs(uint32_t us);
static void Comm_NotifyRxActivity(uint8_t port_id);
static void Comm_NotifyTxComplete(void);
static void Comm_PortResetParser(CommPortContext *ctx);
static void Comm_PortFlushRxRing(CommPortContext *ctx);
static void Comm_PortProcessTxSwitchback(CommPortContext *ctx);

static uint16_t Comm_RingNext(uint16_t index)
{
    index++;
    if (index >= COMM_RX_RING_SIZE)
    {
        index = 0;
    }
    return index;
}

static uint8_t Comm_RingPushByte(CommPortContext *ctx, uint8_t byte)
{
    uint16_t next_head;

    next_head = Comm_RingNext(ctx->ring_head);
    if (next_head == ctx->ring_tail)
    {
        return 0;
    }

    ctx->io_buf.ring_buf[ctx->ring_head] = byte;
    ctx->ring_head = next_head;
    return 1;
}

static uint8_t Comm_RingPopByte(CommPortContext *ctx, uint8_t *byte)
{
    if (ctx->ring_head == ctx->ring_tail)
    {
        return 0;
    }

    *byte = ctx->io_buf.ring_buf[ctx->ring_tail];
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

static void Comm_PortDisableTxInterrupts(CommPortContext *ctx)
{
    USART_ITConfig(ctx->instance, USART_IT_TXE, DISABLE);
    USART_ITConfig(ctx->instance, USART_IT_TC, DISABLE);
}

static void Comm_PortFinishTx(CommPortContext *ctx)
{
    ctx->tx_switchback_pending = 1;
    Comm_PortDisableTxInterrupts(ctx);
}

static void Comm_PortResetParser(CommPortContext *ctx)
{
    Comm_EnterCritical();
    ctx->active_protocol = PROTO_NONE;
    ctx->frame_ready_flag = 0;
    ctx->rx_len = 0;
    ctx->rx_timeout_ms = 0;
    ctx->ring_head = 0;
    ctx->ring_tail = 0;
    AsciiParser_Reset(&ctx->parser.ascii);
    ModbusRtuParser_Reset(&ctx->parser.modbus);
    Comm_ExitCritical();
}

static void Comm_PortFlushRxRing(CommPortContext *ctx)
{
    Comm_EnterCritical();
    ctx->ring_head = 0;
    ctx->ring_tail = 0;
    Comm_ExitCritical();
}

static void Comm_PortResetRx(CommPortContext *ctx)
{
    Comm_PortResetParser(ctx);
    Comm_PortDisableTxInterrupts(ctx);
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
        ctx->is_rs485 = 1U;
        GPIO_PinAFConfig(GPIOA, GPIO_PinSource9, GPIO_AF_1);
        GPIO_PinAFConfig(GPIOA, GPIO_PinSource10, GPIO_AF_1);
        gpio_init.GPIO_Pin = GPIO_Pin_9 | GPIO_Pin_10;
        usart_init.USART_BaudRate = 115200;
    }
    else
    {
        RCC_APB1PeriphClockCmd(RCC_APB1Periph_USART2, ENABLE);
        nvic_init.NVIC_IRQChannel = USART2_IRQn;
        ctx->is_rs485 = 0U;
        GPIO_PinAFConfig(GPIOA, GPIO_PinSource2, GPIO_AF_1);
        GPIO_PinAFConfig(GPIOA, GPIO_PinSource3, GPIO_AF_1);
        gpio_init.GPIO_Pin = GPIO_Pin_2 | GPIO_Pin_3;
        usart_init.USART_BaudRate = 19200;
    }

    nvic_init.NVIC_IRQChannelPriority = 0;
    nvic_init.NVIC_IRQChannelCmd = ENABLE;
    NVIC_Init(&nvic_init);

    gpio_init.GPIO_Mode = GPIO_Mode_AF;
    gpio_init.GPIO_OType = GPIO_OType_PP;
    gpio_init.GPIO_PuPd = GPIO_PuPd_UP;
    gpio_init.GPIO_Speed = GPIO_Speed_50MHz;
    GPIO_Init(GPIOA, &gpio_init);

    usart_init.USART_WordLength = USART_WordLength_8b;
    usart_init.USART_StopBits = USART_StopBits_1;
    usart_init.USART_Parity = USART_Parity_No;
    usart_init.USART_HardwareFlowControl = USART_HardwareFlowControl_None;
    usart_init.USART_Mode = USART_Mode_Rx | USART_Mode_Tx;
    USART_Init(instance, &usart_init);

    instance->CR3 |= (1 << 0);
    instance->CR3 &= ~(1 << 11);
    USART_Cmd(instance, ENABLE);
    Comm_PortEnableRx(ctx);
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
            ctx->rx_timeout_ms = COMM_ASCII_RX_TIMEOUT_MS;
            AsciiParser_Reset(&ctx->parser.ascii);
        }
        else if ((byte == MODBUS_SLAVE_ADDR) || (byte == MODBUS_BROADCAST_ADDR))
        {
            ctx->active_protocol = PROTO_MODBUS_RTU;
            ctx->rx_timeout_ms = COMM_RTU_RX_TIMEOUT_MS;
            ModbusRtuParser_Reset(&ctx->parser.modbus);
        }
        else
        {
            return;
        }
    }

    if (ctx->active_protocol == PROTO_ASCII)
    {
        result = AsciiParser_ConsumeByte(&ctx->parser.ascii, byte);
        if (result == PROTO_PARSE_FRAME_READY)
        {
            ctx->rx_len = ctx->parser.ascii.length;
            ctx->frame_ready_flag = 1;
        }
        else if (result == PROTO_PARSE_FRAME_INVALID)
        {
            Comm_PortResetRx(ctx);
        }
    }
    else if (ctx->active_protocol == PROTO_MODBUS_RTU)
    {
        result = ModbusRtuParser_ConsumeByte(&ctx->parser.modbus, byte);
        if (result == PROTO_PARSE_FRAME_READY)
        {
            ctx->rx_len = ctx->parser.modbus.length;
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

static void Comm_PortProcessTxSwitchback(CommPortContext *ctx)
{
    if (ctx->tx_switchback_pending == 0)
    {
        return;
    }

    if (ctx->is_rs485 != 0U)
    {
        Comm_DelayUs(COMM_RS485_TURNAROUND_US);
        Comm_SetRs485RxMode(ctx);
    }
    ctx->tx_active = 0;
    ctx->tx_switchback_pending = 0;
    ctx->tx_len = 0;
    ctx->tx_pos = 0;
    Comm_PortEnableRx(ctx);
    Comm_NotifyTxComplete();
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

    elapsed_ms = Comm_CheckElapsedMs(ctx->last_rx_tick);
    if ((elapsed_ms >= 0) && ((uint16_t)elapsed_ms >= ctx->rx_timeout_ms))
    {
        Comm_PortResetParser(ctx);
    }
}

static uint8_t Comm_PortHandleErrors(CommPortContext *ctx)
{
    uint8_t fault_count = 0;
    uint32_t isr;

    isr = ctx->instance->ISR;

    if ((isr & USART_ISR_ORE) != 0U)
    {
        ctx->instance->ICR = USART_ICR_ORECF;
        ctx->overrun_count++;
        fault_count++;
    }
    if ((isr & USART_ISR_NE) != 0U)
    {
        ctx->instance->ICR = USART_ICR_NCF;
        ctx->noise_error_count++;
        fault_count++;
    }
    if ((isr & USART_ISR_FE) != 0U)
    {
        ctx->instance->ICR = USART_ICR_FECF;
        ctx->frame_error_count++;
        fault_count++;
    }
    if ((isr & USART_ISR_PE) != 0U)
    {
        ctx->instance->ICR = USART_ICR_PECF;
        ctx->parity_error_count++;
        fault_count++;
    }

    if (fault_count != 0)
    {
        ctx->error_count++;
        Comm_PortResetParser(ctx);
        return 1;
    }

    return 0;
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
        tx_len = Ascii_HandleFrame(ctx->parser.ascii.buffer, ctx->rx_len, ctx->io_buf.tx_buf, MAX_FRAME_LEN);
    }
    else if (ctx->active_protocol == PROTO_MODBUS_RTU)
    {
        tx_len = Modbus_ServiceHandleFrame(&g_modbus_service_ctx, ctx->parser.modbus.buffer, ctx->rx_len, ctx->io_buf.tx_buf, MAX_FRAME_LEN);
    }

    ctx->frame_ready_flag = 0;
    ctx->rx_len = 0;
    ctx->active_protocol = PROTO_NONE;
    AsciiParser_Reset(&ctx->parser.ascii);
    ModbusRtuParser_Reset(&ctx->parser.modbus);

    if (tx_len > 0)
    {
        Comm_PortStartTx(ctx, ctx->io_buf.tx_buf, tx_len);
    }
}

void Comm_InitAll(void)
{
    NVIC_SetPriority(SysTick_IRQn, 3);
#ifdef _COMMOM_UPPER_SCI1
    Comm_PortInit(&g_comm_port1, USART1, 1U);
#endif
#ifdef _COMMOM_UPPER_SCI2
    Comm_PortInit(&g_comm_port2, USART2, 2U);
#endif
}

void Comm_PollAll(void)
{
#ifdef _COMMOM_UPPER_SCI1
    Comm_PortProcessTxSwitchback(&g_comm_port1);
    Comm_PortDrainRxRing(&g_comm_port1);
    Comm_PortCheckTimeout(&g_comm_port1);
    Comm_PortDispatch(&g_comm_port1);
#endif
#ifdef _COMMOM_UPPER_SCI2
    Comm_PortProcessTxSwitchback(&g_comm_port2);
    Comm_PortDrainRxRing(&g_comm_port2);
    Comm_PortCheckTimeout(&g_comm_port2);
    Comm_PortDispatch(&g_comm_port2);
#endif
}

void Comm_PortIrqHandler(CommPortContext *ctx)
{
    uint8_t rx_byte;
    uint32_t isr;

    if ((ctx->tx_active != 0) && (USART_GetITStatus(ctx->instance, USART_IT_TXE) != RESET))
    {
        if (ctx->tx_pos < ctx->tx_len)
        {
            ctx->instance->TDR = ctx->io_buf.tx_buf[ctx->tx_pos++];
        }
        else
        {
            USART_ITConfig(ctx->instance, USART_IT_TXE, DISABLE);
            USART_ITConfig(ctx->instance, USART_IT_TC, ENABLE);
        }
    }

    if ((ctx->tx_active != 0) && (USART_GetITStatus(ctx->instance, USART_IT_TC) != RESET))
    {
        USART_ClearFlag(ctx->instance, USART_FLAG_TC);
        Comm_PortFinishTx(ctx);
        return;
    }

    isr = ctx->instance->ISR;
    while ((isr & USART_ISR_RXNE) != 0U)
    {
        rx_byte = (uint8_t)ctx->instance->RDR;
        Comm_NotifyRxActivity(ctx->port_id);
        ctx->last_rx_tick = Comm_GetRuntimeMs();

        if (ctx->tx_active == 0)
        {
            if (Comm_RingPushByte(ctx, rx_byte) == 0)
            {
                ctx->error_count++;
                ctx->ring_overflow_count++;
                Comm_PortResetParser(ctx);
                Comm_PortFlushRxRing(ctx);
                break;
            }
        }

        isr = ctx->instance->ISR;
    }

    Comm_PortHandleErrors(ctx);
}

void Comm_PortStartTx(CommPortContext *ctx, const uint8_t *data, uint16_t len)
{
    if ((len == 0) || (len > MAX_FRAME_LEN))
    {
        return;
    }

    if (data != ctx->io_buf.tx_buf)
    {
        memcpy(ctx->io_buf.tx_buf, data, len);
    }
    ctx->tx_len = len;
    ctx->tx_pos = 0;
    Comm_PortResetRx(ctx);
    Comm_PortDisableRx(ctx);
    Comm_PortDisableTxInterrupts(ctx);
    USART_ClearFlag(ctx->instance, USART_FLAG_TC);
    ctx->tx_switchback_pending = 0;
    ctx->tx_active = 1;
    Comm_SetRs485TxMode(ctx);
    if (ctx->is_rs485 != 0U)
    {
        Comm_DelayUs(COMM_RS485_TURNAROUND_US);
    }
    USART_ITConfig(ctx->instance, USART_IT_TXE, ENABLE);
}

static void Comm_EnterCritical(void)
{
    DISABLE_INT();
}

static void Comm_ExitCritical(void)
{
    ENABLE_INT();
}

static int32_t Comm_GetRuntimeMs(void)
{
    return bsp_GetRunTime();
}

static int32_t Comm_CheckElapsedMs(int32_t last_tick)
{
    return bsp_CheckRunTime(last_tick);
}

static void Comm_SetRs485TxMode(CommPortContext *ctx)
{
    if (ctx->is_rs485 != 0U)
    {
        TRANS_EN_485();
    }
}

static void Comm_SetRs485RxMode(CommPortContext *ctx)
{
    if (ctx->is_rs485 != 0U)
    {
        RECV_EN_485();
    }
}

static void Comm_DelayUs(uint32_t us)
{
    __delay_us(us);
}

static void Comm_NotifyRxActivity(uint8_t port_id)
{
    RTC_ExtComCnt++;
    if (port_id == 1U)
    {
        RTC_ExtComCnt1++;
    }
}

static void Comm_NotifyTxComplete(void)
{
    if (u8FlashUpdateE2PROM != 0)
    {
        u8FlashUpdateE2PROM = 0;
        u8FlashUpdateFlag = 1;
    }
}
