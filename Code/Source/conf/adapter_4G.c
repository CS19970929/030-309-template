#include "main.h"
#include "adapter_4G.h"


/*
PIN_PWR_4G(硬件开、关机键)    拉低超过1s释放，模块开机；拉低超过3s释放，关机

PIN_RST_4G（硬件复位键）      拉低100ms，复位，模块重启

//led1指示4G网络状态
//led2指示模块开、关机状态
网络状态指示引脚的工作状态
熄灭                    Power off
64ms 亮/800ms 熄灭      Shut down network
64ms 亮/3000ms 熄灭     Registered network

L511-Y6 系列模块提供了与应用处理器通信的直接交互信号。
•MAIN_DTR：模块进入睡眠后，主机可以通过置低该信号唤醒模块，主机置高电平后，模
块允许进入睡眠。MAIN_DTR 可以直接连接 MCU 的 1.8V～3.3V 的 GPIO 口。
•MAIN_RI：模块有事件需要与应用处理器通信时，模块可通过该管脚输出低电平（低电平
会持续 120ms）来唤醒应用处理器。
•STATUS：模块状态查询，低电平表示为关机状态或开机初始化状态，高电平表示为开机状
态。



*/

void POWER_ON_4G_AND_INIT(void)
{
    GPIO_InitTypeDef GPIO_InitStructure;

    GPIO_WriteBit(GPIO_PWR_4G, PIN_PWR_4G, 0);
    GPIO_InitStructure.GPIO_Pin = PIN_PWR_4G;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
    GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
    GPIO_Init(GPIO_PWR_4G, &GPIO_InitStructure);
    __delay_ms(1100);
    GPIO_WriteBit(GPIO_PWR_4G, PIN_PWR_4G, 1);

    WAKEUP_4G_MODULE();
    GPIO_InitStructure.GPIO_Pin = PIN_MCU_DTR;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
    GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
    GPIO_Init(GPIO_MCU_DTR, &GPIO_InitStructure);

    bsp_74HC595D_init();
}
void POWER_OFF_4G_AND_INIT(void)
{
    GPIO_InitTypeDef GPIO_InitStructure;

    GPIO_WriteBit(GPIO_PWR_4G, PIN_PWR_4G, 0);
    GPIO_InitStructure.GPIO_Pin = PIN_PWR_4G;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
    GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
    GPIO_Init(GPIO_PWR_4G, &GPIO_InitStructure);
    __delay_ms(3100);
    GPIO_WriteBit(GPIO_PWR_4G, PIN_PWR_4G, 1);
}

void RESET_4G_AND_INIT(void)
{
    GPIO_InitTypeDef GPIO_InitStructure;

    GPIO_WriteBit(GPIO_RST_4G, PIN_RST_4G, 0);
    GPIO_InitStructure.GPIO_Pin = PIN_PWR_4G;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
    GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
    GPIO_Init(GPIO_PWR_4G, &GPIO_InitStructure);
    __delay_ms(110);
    GPIO_WriteBit(GPIO_PWR_4G, PIN_PWR_4G, 1);
}

void SendByte_4G(unsigned char data)
{
    // while ((USART3->SR & USART_FLAG_TXE) != USART_FLAG_TXE)
    //     ;
    // USART3->DR = data;

    while (!((USART1->ISR) & (1 << 7)))
        ;               // 1<<6 也可以
    USART1->TDR = data; // load data
}

void RecvByte_4G(void)
{
    unsigned char Res = 0;

    if ((USART1->ISR & USART_IT_RXNE) != 0)
    {
        Res = USART1->RDR;
        uart_receive_input(Res);
    }
}
