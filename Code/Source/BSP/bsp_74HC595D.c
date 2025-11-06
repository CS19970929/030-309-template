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

/*======================== 共阴极段码表 ========================*/
#define SEG_0 0x3F
#define SEG_1 0x06
#define SEG_2 0x5B
#define SEG_3 0x4F
#define SEG_4 0x66
#define SEG_5 0x6D
#define SEG_6 0x7D
#define SEG_7 0x07
#define SEG_8 0x7F
#define SEG_9 0x6F
#define SEG_E 0x79
#define SEG_BLANK 0x00

/* 数字段码表：仅 10 B，驻 Flash */
static const uint8_t seg_digit[10] = {
    SEG_0, SEG_1, SEG_2, SEG_3, SEG_4,
    SEG_5, SEG_6, SEG_7, SEG_8, SEG_9};

/*======================== BCD 查表区 ========================*/
/* 0 – 100 → 101 B，每项高 4 位十位、低 4 位个位 */
static const uint8_t SOC_BCD[101] = {
    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
    0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19,
    0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29,
    0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
    0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
    0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
    0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
    0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79,
    0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
    0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99,
    0x00 // 100 特殊处理
};

/* 01 – 20 → 20 B，索引 0 → 01 */
static const uint8_t FLT_BCD[20] = {
    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
    0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20};

// 共阴数码管
const uint8_t SEG_CODE[16] = {
    0x3F, 0x06, 0x5B, 0x4F,
    0x66, 0x6D, 0x7D, 0x07,
    0x7F, 0x6F, 0x77, 0x7C,
    0x39, 0x5E, 0x79, 0x71};

uint16_t test_fault = 0;
uint8_t digits[3];

typedef struct
{
    DisplayMode_t mode;
    uint16_t soc;
    uint16_t fault;
    uint8_t current_digit; // 当前扫描位 (0~2)
    uint32_t toggle_timer; // 用于故障闪烁
    uint8_t toggle_state;  // 切换SOC/故障显示
} Display_t;

static struct
{
    DISP_Mode_t mode;
    uint8_t soc_seg[3];   // 缓存 SOC 段码
    uint8_t fault_seg[3]; // 缓存 E+故障段码
    uint16_t tick_ms;
    uint8_t toggle;
    uint8_t cur_digit;
} disp;

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

    MCUO_SEG_DIG1 = 0;
    MCUO_SEG_DIG2 = 0;
    MCUO_SEG_DIG3 = 0;

    disp.mode = DISP_MODE_SOC;
    disp.tick_ms = 0;
    disp.toggle = 0;
    disp.cur_digit = 0;
    disp.soc_seg[0] = SEG_0;
    disp.soc_seg[1] = SEG_BLANK;
    disp.soc_seg[2] = SEG_BLANK;
    disp.fault_seg[0] = SEG_1;
    disp.fault_seg[1] = SEG_0;
    disp.fault_seg[2] = SEG_E;
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
    // RCLK_LOW();
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

    RCLK_LOW();
    HC595_DELAY();
    RCLK_HIGH();
    HC595_DELAY();
    RCLK_LOW();

    SER_LOW();
    SRCLK_LOW();
    // HC595_DELAY();
    // HC595_DELAY();
}

//--------------------------------------
// 更新显示数据（SOC / 故障）
//--------------------------------------
void Display_UpdateData(DISP_Mode_t mode, uint16_t soc, uint16_t code)
{
    static uint8_t ge, shi, bai = 0;
    uint16_t value;

    // gDisplay.mode = mode;
    // gDisplay.soc = soc;
    // gDisplay.fault = fault;
    disp.mode = mode;

    if (mode == DISPLAY_FAULT)
    {
        {
            if (code < 1)
                code = 1;
            if (code > 20)
                code = 20;
            uint8_t b = FLT_BCD[code - 1];
            uint8_t ones = b & 0x0F;
            uint8_t tens = (b >> 4) & 0x0F;
            disp.fault_seg[2] = SEG_E;
            disp.fault_seg[1] = (tens == 0) ? SEG_BLANK : seg_digit[tens];
            disp.fault_seg[0] = seg_digit[ones];
        }
    }

    // {
    //     disp.soc_seg[0] = seg_digit[ge];
    //     disp.soc_seg[1] = seg_digit[shi];
    //     disp.soc_seg[2] = seg_digit[bai];
    //     return;
    // }

    {
        // if(soc > 100)
        // {
        //      disp.soc_seg[0] = seg_digit[0];
        //     disp.soc_seg[1] = seg_digit[0];
        //     disp.soc_seg[2] = seg_digit[1];
        //     return;
        // }
        if (soc > 100)
            soc = 100;
        if (soc == 100)
        { // 100 特殊显示
            disp.soc_seg[0] = seg_digit[0];
            disp.soc_seg[1] = seg_digit[0];
            disp.soc_seg[2] = seg_digit[1];
            return;
        }
        uint8_t b = SOC_BCD[soc];
        uint8_t ones = b & 0x0F;
        uint8_t tens = (b >> 4) & 0x0F;
        disp.soc_seg[0] = seg_digit[ones];
        disp.soc_seg[1] = (tens == 0) ? SEG_BLANK : seg_digit[tens];
        disp.soc_seg[2] = SEG_BLANK;
    }

    // else if (mode == DISPLAY_SOC)
    // {
    //     value = gDisplay.soc;
    //     digits[0] = value % 10;
    //     digits[1] = (value / 10) % 10;
    //     digits[2] = (value / 100) % 10;

    //     {
    //         if (soc > 100)
    //             soc = 100;
    //         if (soc == 100)
    //         { // 100 特殊显示
    //             disp.soc_seg[0] = seg_digit[0];
    //             disp.soc_seg[1] = seg_digit[0];
    //             disp.soc_seg[2] = seg_digit[1];
    //             return;
    //         }
    //         uint8_t b = SOC_BCD[soc];
    //         uint8_t ones = b & 0x0F;
    //         uint8_t tens = (b >> 4) & 0x0F;
    //         disp.soc_seg[0] = seg_digit[ones];
    //         disp.soc_seg[1] = (tens == 0) ? SEG_BLANK : seg_digit[tens];
    //         disp.soc_seg[2] = SEG_BLANK;
    //     }
    // }
}

uint8_t display_fault(void)
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
    // if (g_stCellInfoReport.SocElement.u16Soc == 100)
    //     fault_code = 12;
    if (System_ERROR_UserCallback(ERROR_STATUS_AFE1))
        fault_code = 13;

    return fault_code;
}

void Display_ScanTask(void)
{
    uint8_t seg;
    static bool enable = true;
    static uint16_t delay = 0;

    if (++delay >= (200 * 10))
    {
        delay = 0;
        enable = false;
    }
    if (0 == MCUI_ENI_DI1)
    {
        delay = 0;
        enable = true;
    }

    if (!enable)
    {
        MCUO_SEG_DIG1 = 0;
        MCUO_SEG_DIG2 = 0;
        MCUO_SEG_DIG3 = 0;
        disp.cur_digit = 0;
        return;
    }

    disp.tick_ms++;
    if (disp.mode == DISP_MODE_FAULT)
    {
        if (disp.tick_ms >= 100)
        {
            disp.tick_ms = 0;
            disp.toggle ^= 1;
        }
    }
    else
    {
        disp.tick_ms = 0;
        disp.toggle = 0;
    }
    // 关闭所有位选
    MCUO_SEG_DIG1 = 0;
    MCUO_SEG_DIG2 = 0;
    MCUO_SEG_DIG3 = 0;

    // HC595_DELAY();
    // HC595_DELAY();
    // HC595_DELAY();
    // HC595_DELAY();
    // HC595_DELAY();
    // HC595_DELAY();
    // HC595_DELAY();
    // HC595_DELAY();

#if 1
    if (disp.mode == DISP_MODE_FAULT && disp.toggle == 0)
        seg = disp.fault_seg[disp.cur_digit];
    else
        seg = disp.soc_seg[disp.cur_digit];
#else
    seg = disp.fault_seg[disp.cur_digit];
#endif

    HC595_SendByte(seg);

    // HC595_DELAY();
    // HC595_DELAY();

    if (seg != SEG_BLANK)
    {
#if 1
        switch (disp.cur_digit)
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
#else
        switch (disp.cur_digit)
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
#endif
    }
    // disp.cur_digit = (disp.cur_digit + 1) % 3;
    disp.cur_digit++;
    if (disp.cur_digit >= 3)
        disp.cur_digit = 0;

    // gDisplay.current_digit++;
    // if (gDisplay.current_digit >= 3)
    //     gDisplay.current_digit = 0;
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