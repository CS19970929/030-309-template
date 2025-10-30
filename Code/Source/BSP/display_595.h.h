#ifndef __DISPLAY_595_H
#define __DISPLAY_595_H

// #include "stm32f10x.h"

typedef enum {
    DISP_MODE_SOC = 0,
    DISP_MODE_FAULT
} DISP_Mode_t;

void DISP_Init(void);
void DISP_UpdateSOC(uint16_t soc);
void DISP_UpdateFault(uint8_t fault);
void DISP_SetMode(DISP_Mode_t mode);
void DISP_Task_1ms(void);

#endif
