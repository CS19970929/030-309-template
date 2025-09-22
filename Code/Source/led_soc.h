#ifndef LED_SOC_H
#define LED_SOC_H

typedef enum _LEDBAR_COMMAND {
	LED_BAR_STARTUP = 0,
    LED_BAR_NORMAL,
    LED_BAR_CHG,
    LED_BAR_DSG,
	LED_BAR_FAULT,
}LEDBAR_COMMAND;

#define MCUI_SOC_KEY 		(PORT_IN_GPIOA->bit15)

#define MCUO_SOC_20 		(PORT_OUT_GPIOB->bit3)
#define MCUO_SOC_40 		(PORT_OUT_GPIOB->bit4)
#define MCUO_SOC_60 		(PORT_OUT_GPIOB->bit5)
#define MCUO_SOC_80 		(PORT_OUT_GPIOB->bit8)
#define MCUO_SOC_100 		(PORT_OUT_GPIOB->bit9)

void SOC_LED_Init(float init_v);
void SOC_LED_Update(void);
void Key_Task(void);
void Board_PowerOn(void);
void Board_PowerOff(void);

void APP_LedBar(void);

#endif