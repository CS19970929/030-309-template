#ifndef LED_SOC_H
#define LED_SOC_H


void SOC_LED_Init(float init_v);
void SOC_LED_Update(void);
void Key_Task(void);
void Board_PowerOn(void);
void Board_PowerOff(void);

#endif