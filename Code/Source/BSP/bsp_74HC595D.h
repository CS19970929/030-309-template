#ifndef bsp_74HC595D_H
#define bsp_74HC595D_H

typedef enum
{
    DISPLAY_SOC,
    DISPLAY_FAULT
} DisplayMode_t;

void bsp_74HC595D_init(void);
void test_main(void);

#endif
