#ifndef ADAPTER_4G_H
#define ADAPTER_4G_H


#define WAKEUP_4G_MODULE()         GPIO_WriteBit(GPIO_MCU_DTR, PIN_MCU_DTR, 0)
#define SLEEP_4G_MODULE()         GPIO_WriteBit(GPIO_MCU_DTR, PIN_MCU_DTR, 1)

void POWER_ON_4G_AND_INIT(void);
void POWER_OFF_4G_AND_INIT(void);
void RESET_4G_AND_INIT(void);

void SendByte_4G(unsigned char data);
void RecvByte_4G(void);



#endif
