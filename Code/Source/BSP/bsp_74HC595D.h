#ifndef bsp_74HC595D_H
#define bsp_74HC595D_H

typedef enum
{
    DISPLAY_SOC,
    DISPLAY_FAULT
} DisplayMode_t;


typedef enum
{
    DISP_MODE_SOC = 0,
    DISP_MODE_FAULT
} DISP_Mode_t;

void bsp_74HC595D_init(void);
void test_main(void);
void Display_UpdateData(DISP_Mode_t mode, uint16_t soc, uint16_t code);
void Display_ScanTask(void);

#endif
