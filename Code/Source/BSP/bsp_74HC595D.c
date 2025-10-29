#include "main.h"

/*
     共阴数码管编码表：
 0x3f   0x06   0x5b   0x4f  0x66  0x6d
   0      1     2      3     4     5
 0x7d  0x07   0x7f    0x6f  0x77  0x7c
   6    7      8       9     A     B
 0x39  0x5e   0x79    0x71
   C    D       E      F


   共阳数码管编码表：
 0xc0  0xf9   0xa4    0xb0  0x99   0x92
  0      1     2       3     4     5
 0x82  0xf8   0x80    0x90  0x88   0x83
   6    7      8       9     A     B
 0xc6  0xa1   0x86    0x8e
   C    D       E      F
*/

// 共阴数码管
const uint8_t SEG_CODE[16] = {
    0x3F, 0x06, 0x5B, 0x4F,
    0x66, 0x6D, 0x7D, 0x07,
    0x7F, 0x6F, 0x77, 0x7C,
    0x39, 0x5E, 0x79, 0x71};

typedef enum
{
    DISPLAY_SOC,
    DISPLAY_FAULT
} DisplayMode_t;

typedef struct
{
    DisplayMode_t mode;
    uint16_t soc;
    uint16_t fault;
    uint8_t current_digit; // 当前扫描位 (0~2)
    uint32_t toggle_timer; // 用于故障闪烁
    uint8_t toggle_state;  // 切换SOC/故障显示
} Display_t;

Display_t gDisplay;

// 假设使用 PB0-2 控制 74HC595，PB3-5 控制位选
#define SER_HIGH() GPIO_SetBits(GPIO_MOSI, PIN_MOSI)
#define SER_LOW() GPIO_ResetBits(GPIO_MOSI, PIN_MOSI)
#define SRCLK_HIGH() GPIO_SetBits(GPIO_SCK, PIN_SCK)
#define SRCLK_LOW() GPIO_ResetBits(GPIO_SCK, PIN_SCK)
#define RCLK_HIGH() GPIO_SetBits(GPIO_NSS, PIN_NSS)
#define RCLK_LOW() GPIO_ResetBits(GPIO_NSS, PIN_NSS)

#define MCUO_SEG_DIG1 (PORT_OUT_GPIOB->bit9) // AFE_SHIP
#define MCUO_SEG_DIG2 (PORT_OUT_GPIOF->bit6) // AFE_SHIP
#define MCUO_SEG_DIG3 (PORT_OUT_GPIOF->bit7) // AFE_SHIP

// #define DIGIT1_PIN GPIO_Pin_3
// #define DIGIT2_PIN GPIO_Pin_4
// #define DIGIT3_PIN GPIO_Pin_5
void bsp_74HC595D_init(void)
{
    GPIO_InitTypeDef GPIO_InitStructure;

    GPIO_InitStructure.GPIO_Pin = PIN_NSS;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
    GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
    GPIO_Init(GPIO_NSS, &GPIO_InitStructure);

    GPIO_InitStructure.GPIO_Pin = PIN_MOSI;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
    GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
    GPIO_Init(GPIO_MOSI, &GPIO_InitStructure);

    GPIO_InitStructure.GPIO_Pin = PIN_SCK;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
    GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
    GPIO_Init(GPIO_SCK, &GPIO_InitStructure);

    GPIO_InitStructure.GPIO_Pin = PIN_MCU_DIG1;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
    GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
    GPIO_Init(GPIO_MCU_DIG1, &GPIO_InitStructure);
    GPIO_InitStructure.GPIO_Pin = PIN_MCU_DIG2;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
    GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
    GPIO_Init(GPIO_MCU_DIG2, &GPIO_InitStructure);
    GPIO_InitStructure.GPIO_Pin = PIN_MCU_DIG3;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
    GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
    GPIO_Init(GPIO_MCU_DIG3, &GPIO_InitStructure);

    SER_LOW();
    SRCLK_LOW();
    RCLK_LOW();
}

// #define HC595_DELAY()  __NOP();__NOP();__NOP();__NOP();__NOP();__NOP();__NOP();__NOP();
#define HC595_DELAY() \
    __NOP();          \
    __NOP();          \
    __NOP();          \
    __NOP();
// #define HC595_DELAY()  ;
//--------------------------------------
// 发送一字节给74HC595
//--------------------------------------
static void HC595_SendByte(uint8_t data)
{
    RCLK_LOW();
    for (int i = 0; i < 8; i++)
    {
        SRCLK_LOW();
        if (data & 0x80)
            SER_HIGH();
        else
            SER_LOW();
        SRCLK_HIGH();
        HC595_DELAY();
        data <<= 1;
    }
    RCLK_HIGH();
    HC595_DELAY();
    RCLK_LOW();

    SER_LOW();
    SRCLK_LOW();
}

//--------------------------------------
// 更新显示数据（SOC / 故障）
//--------------------------------------
void Display_UpdateData(DisplayMode_t mode, uint16_t soc, uint16_t fault)
{
    gDisplay.mode = mode;
    gDisplay.soc = soc;
    gDisplay.fault = fault;
}

#if 0
//--------------------------------------
// 定时任务：1ms 调用一次
//--------------------------------------
void Display_ScanTask(uint32_t now_ms)
{
    uint16_t value;
    if (gDisplay.mode == DISPLAY_FAULT)
    {
        // 每 1000ms 在 SOC 和 故障码之间切换
        if (now_ms - gDisplay.toggle_timer >= 1000)
        {
            gDisplay.toggle_timer = now_ms;
            gDisplay.toggle_state ^= 1;
        }
        value = gDisplay.toggle_state ? gDisplay.soc : gDisplay.fault;
    }
    else
    {
        value = gDisplay.soc;
    }

    uint8_t digits[3];
    digits[0] = value % 10;
    digits[1] = (value / 10) % 10;
    digits[2] = (value / 100) % 10;

    // 关闭所有位选
    // GPIO_ResetBits(GPIOB, DIGIT1_PIN | DIGIT2_PIN | DIGIT3_PIN);
    MCUO_SEG_DIG1 = 0;
    MCUO_SEG_DIG2 = 0;
    MCUO_SEG_DIG3 = 0;

    // 输出段码
    HC595_SendByte(SEG_CODE[digits[gDisplay.current_digit]]);

    // 打开当前位选
    switch (gDisplay.current_digit)
    {
    case 0:
        GPIO_SetBits(GPIO_MCU_DIG1, PIN_MCU_DIG1);
        break;
    case 1:
        GPIO_SetBits(GPIO_MCU_DIG2, PIN_MCU_DIG2);
        break;
    case 2:
        GPIO_SetBits(GPIO_MCU_DIG3, PIN_MCU_DIG3);
        break;
    }

    // 下次扫描切换下一位
    gDisplay.current_digit++;
    if (gDisplay.current_digit >= 3)
        gDisplay.current_digit = 0;
}
#endif

void display_fault(void)
{
    uint16_t fault_code = 0;

    if (g_stCellInfoReport.unMdlFault_Third.bits.b1CellOvp)
        fault_code = 1;
    if (g_stCellInfoReport.unMdlFault_Third.bits.b1CellUvp)
        fault_code = 2;
    if (g_stCellInfoReport.unMdlFault_Third.bits.b1TmosOtp)
        fault_code = 3;
    // if(g_stCellInfoReport.unMdlFault_Third.bits.b1CellUvp)
    //     fault_code = 1 << 2;
    if (g_stCellInfoReport.unMdlFault_Third.bits.b1CellChgOtp)
        fault_code = 5;
    if (g_stCellInfoReport.unMdlFault_Third.bits.b1CellChgUtp)
        fault_code = 6;
    if (g_stCellInfoReport.unMdlFault_Third.bits.b1CellDischgOtp)
        fault_code = 7;
    if (g_stCellInfoReport.unMdlFault_Third.bits.b1CellDischgUtp)
        fault_code = 8;
    if (g_stCellInfoReport.unMdlFault_Third.bits.b1IchgOcp)
        fault_code = 9;
    if (g_stCellInfoReport.unMdlFault_Third.bits.b1IdischgOcp)
        fault_code = 10;
    if (System_ERROR_UserCallback(ERROR_STATUS_CBC_DSG))
        fault_code = 11;
    if (g_stCellInfoReport.SocElement.u16Soc == 100)
        fault_code = 12;
    if (System_ERROR_UserCallback(ERROR_AFE1))
        fault_code = 13;

    Display_UpdateData(DISPLAY_FAULT, g_stCellInfoReport.SocElement.u16Soc, fault_code);
}

uint16_t test_soc = 0;
uint16_t test_fault = 0;
void Display_ScanTask(uint32_t now_ms)
{
    uint16_t value;
    uint8_t digits[3];
    uint8_t seg_data;
    static uint16_t delay_toggle = 0;

    static uint16_t delay = 0;
    static uint16_t delay_fault = 0;
    if (test_soc < 100)
    {
        if (++delay >= 1000)
        {
            delay = 0;
            test_soc++;
        }
    }
    else
    {
        test_soc = 0;
    }
    if (test_fault < 20)
    {
        if (++delay_fault >= 1000)
        {
            delay_fault = 0;
            test_fault++;
        }
    }
    else
    {
        test_fault = 0;
    }
    // Display_UpdateData(DISPLAY_SOC, test_soc, 1);
    Display_UpdateData(DISPLAY_FAULT, test_soc, test_fault);

    // if (g_stCellInfoReport.unMdlFault_Third.all)
    // {
    //     display_fault();
    // }
    // else
    // {
    //     Display_UpdateData(DISPLAY_SOC, g_stCellInfoReport.SocElement.u16Soc, 1);
    //     // Display_UpdateData(DISPLAY_SOC, test_soc, 1);
    // }

    if (gDisplay.mode == DISPLAY_FAULT)
    {
        // 每 1000ms 切换显示 SOC / 故障码
        // if (now_ms - gDisplay.toggle_timer >= 1000)
        if (++delay_toggle >= 1000)
        {
            delay_toggle = 0;
            // gDisplay.toggle_timer = now_ms;
            gDisplay.toggle_state ^= 1;
        }

        // if (gDisplay.toggle_state == 0)
        { // 显示故障
            uint8_t fault = gDisplay.fault;
            // digits[0] = 0xEE; // E 的标志（我们用特殊码表示）
            // digits[0] = 0x79; // E 的标志（我们用特殊码表示）
            digits[0] = fault % 10;
            digits[1] = (fault / 10) % 10;
            digits[2] = 14; // E 的标志（我们用特殊码表示）
        }
        // else
        // { // 显示SOC
        //     value = gDisplay.soc;
        //     digits[0] = value % 10;
        //     digits[1] = (value / 10) % 10;
        //     digits[2] = (value / 100) % 10;
        // }
    }
    else
    {
        value = gDisplay.soc;
        digits[0] = value % 10;
        digits[1] = (value / 10) % 10;
        digits[2] = (value / 100) % 10;
    }

    // 关闭所有位选
    // GPIO_ResetBits(GPIOB, DIGIT1_PIN | DIGIT2_PIN | DIGIT3_PIN);
    MCUO_SEG_DIG1 = 0;
    MCUO_SEG_DIG2 = 0;
    MCUO_SEG_DIG3 = 0;

    // MCUO_SEG_DIG1 = 1;
    // MCUO_SEG_DIG2 = 1;
    // MCUO_SEG_DIG3 = 1;

    // uint8_t fault = gDisplay.fault;
    // digits[0] = 0xEE; // E 的标志（我们用特殊码表示）
    // digits[1] = fault % 10;
    // digits[2] = (fault / 10) % 10;

    // // 确定当前位段码
    // if (digits[gDisplay.current_digit] == 0xEE)
    //     seg_data = SEG_E;
    // else
    //     seg_data = SEG_CODE[digits[gDisplay.current_digit]];

    // ????输出段码
    // HC595_SendByte(seg_data);
    HC595_SendByte(SEG_CODE[digits[gDisplay.current_digit]]);

    // 打开对应位
    switch (gDisplay.current_digit)
    {
    case 0:
        GPIO_SetBits(GPIO_MCU_DIG3, PIN_MCU_DIG3);
        break;
    case 1:
        GPIO_SetBits(GPIO_MCU_DIG2, PIN_MCU_DIG2);
        break;
    case 2:
        GPIO_SetBits(GPIO_MCU_DIG1, PIN_MCU_DIG1);
        break;
    }

    gDisplay.current_digit++;
    if (gDisplay.current_digit >= 3)
        gDisplay.current_digit = 0;
}

uint16_t test_segcode = 0;
void test_main(void)
{
    static uint8_t soc = 0;
    static uint8_t fault = 0;
    // if (0 == g_st_SysTimeFlag.bits.b1Sys1000msFlag3)
    if (0 == g_st_SysTimeFlag.bits.b1Sys200msFlag1)
    {
        return;
    }

    // Display_UpdateData(DISPLAY_SOC, soc, fault);
    // Display_UpdateData(DISPLAY_SOC, g_stCellInfoReport.SocElement.u16Soc, fault);

    MCUO_SEG_DIG1 = 1;
    MCUO_SEG_DIG2 = 1;
    MCUO_SEG_DIG3 = 1;

    // HC595_SendByte(0x79);
    // HC595_SendByte(sys_time.occ1_cnt);
    // HC595_SendByte(soc);
    // HC595_SendByte(SEG_CODE[soc]);
    HC595_SendByte(0x79);
    if (soc < 9)
    {
        soc++;
    }
    else
    {
        soc = 0;
    }

    // MCUO_SEG_DIG3 = 0;
    // MCUO_SEG_DIG2 = 0;
    // MCUO_SEG_DIG1 = 0;
    // HC595_SendByte(SEG_CODE[0]);
    // HC595_SendByte(SEG_CODE[1]);
    // HC595_SendByte(SEG_CODE[2]);
    // HC595_SendByte(SEG_CODE[3]);
    // HC595_SendByte(SEG_CODE[4]);
    // HC595_SendByte(0x3f);
    // HC595_SendByte(0x06);

    // // 确定当前位段码
    // if (digits[gDisplay.current_digit] == 0xEE)
    //     seg_data = SEG_E;
    // else
    //     seg_data = SEG_CODE[digits[gDisplay.current_digit]];

    // ????输出段码
    // HC595_SendByte(seg_data);
    // HC595_SendByte(SEG_CODE[test_segcode]);
    // for (size_t i = 0; i < 10; i++)
    // {
    //     HC595_SendByte(SEG_CODE[i]);
    //     __delay_ms(1000);
    // }

    // 1、test1
#if 0
    if (soc < 100)
    {
        soc++;
        Display_UpdateData(DISPLAY_SOC, soc, fault);
    }
    else
    {
        soc = 0;
    }
#endif

    // 2、 test2
#if 0
    if (fault < 99)
    {
        fault++;
        Display_UpdateData(DISPLAY_FAULT, soc, fault);
    }
    else
    {
        fault = 0;
    }
#endif
}