#include "display_595.h"
#include <stdint.h>

/*================= 用户配置区 =================*/
// 74HC595 接口
// #define GPIO_595 GPIOA
// #define SER_PIN GPIO_Pin_0
// #define SRCLK_PIN GPIO_Pin_1
// #define RCLK_PIN GPIO_Pin_2

// // 数码管位选
// #define GPIO_DIG GPIOB
// #define DIG1_PIN GPIO_Pin_3
// #define DIG2_PIN GPIO_Pin_4
// #define DIG3_PIN GPIO_Pin_5

// // 推荐 GPIO 模式：2MHz 推挽输出（远距更稳）
// #define SER_H() (GPIO_595->BSRR = SER_PIN)
// #define SER_L() (GPIO_595->BRR = SER_PIN)
// #define SRCLK_H() (GPIO_595->BSRR = SRCLK_PIN)
// #define SRCLK_L() (GPIO_595->BRR = SRCLK_PIN)
// #define RCLK_H() (GPIO_595->BSRR = RCLK_PIN)
// #define RCLK_L() (GPIO_595->BRR = RCLK_PIN)

#define SER_HIGH() GPIO_SetBits(GPIO_MOSI, PIN_MOSI)
#define SER_LOW() GPIO_ResetBits(GPIO_MOSI, PIN_MOSI)
#define SRCLK_HIGH() GPIO_SetBits(GPIO_SCK, PIN_SCK)
#define SRCLK_LOW() GPIO_ResetBits(GPIO_SCK, PIN_SCK)
#define RCLK_HIGH() GPIO_SetBits(GPIO_NSS, PIN_NSS)
#define RCLK_LOW() GPIO_ResetBits(GPIO_NSS, PIN_NSS)

#define MCUO_SEG_DIG1 (PORT_OUT_GPIOB->bit9) // AFE_SHIP
#define MCUO_SEG_DIG2 (PORT_OUT_GPIOF->bit6) // AFE_SHIP
#define MCUO_SEG_DIG3 (PORT_OUT_GPIOF->bit7) // AFE_SHIP

// 线长 15cm -> 加几个 NOP 做建立时间
#define HC595_DELAY() \
    __NOP();          \
    __NOP();          \
    __NOP();          \
    __NOP();

/*================= 段码表 =================*/
// 共阴极，高电平点亮
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

static const uint8_t seg_table[10] = {
    SEG_0, SEG_1, SEG_2, SEG_3, SEG_4,
    SEG_5, SEG_6, SEG_7, SEG_8, SEG_9};

/*================= 显示结构体 =================*/
static struct
{
    DISP_Mode_t mode;
    uint8_t digits[3]; // 预分解的段数据（非数值）
    uint16_t soc_cache;
    uint8_t fault_cache;
    uint16_t t_ms;
    uint8_t toggle;
    uint8_t cur_digit;
} disp;

/*================= 底层函数 =================*/
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

/*================= 初始化 =================*/
void DISP_Init(void)
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

    disp.mode = DISP_MODE_SOC;
    disp.soc_cache = 0;
    disp.fault_cache = 0;
    disp.t_ms = 0;
    disp.toggle = 0;
    disp.cur_digit = 0;
    disp.digits[0] = disp.digits[1] = disp.digits[2] = SEG_BLANK;
}

/*================= 外部接口 =================*/
void DISP_UpdateSOC(uint16_t soc)
{
    if (soc > 999)
        soc = 999;
    disp.soc_cache = soc;
    disp.digits[0] = seg_table[soc % 10];
    disp.digits[1] = (soc < 10) ? SEG_BLANK : seg_table[(soc / 10) % 10];
    disp.digits[2] = (soc < 100) ? SEG_BLANK : seg_table[(soc / 100) % 10];
}

void DISP_UpdateFault(uint8_t fault)
{
    disp.fault_cache = fault;
}

void DISP_SetMode(DISP_Mode_t mode)
{
    disp.mode = mode;
}

/*================= 每1ms扫描 =================*/
void DISP_Task_1ms(void)
{
    uint8_t out_seg[3] = {SEG_BLANK, SEG_BLANK, SEG_BLANK};

    // 每1s切换一次SOC / 故障显示
    disp.t_ms++;
    if (disp.mode == DISP_MODE_FAULT && disp.t_ms >= 1000)
    {
        disp.t_ms = 0;
        disp.toggle ^= 1;
    }
    else if (disp.mode == DISP_MODE_SOC)
    {
        disp.toggle = 0;
        disp.t_ms = 0;
    }

    // 根据模式准备要显示的三位
    if (disp.mode == DISP_MODE_FAULT)
    {
        if (disp.toggle == 0)
        {
            // 显示 E + 故障码
            out_seg[2] = SEG_E;
            out_seg[1] = (disp.fault_cache >= 10) ? seg_table[(disp.fault_cache / 10) % 10] : SEG_BLANK;
            out_seg[0] = seg_table[disp.fault_cache % 10];
        }
        else
        {
            // 显示 SOC 数字
            for (int i = 0; i < 3; i++)
                out_seg[i] = disp.digits[i];
        }
    }
    else
    {
        // 正常显示SOC
        for (int i = 0; i < 3; i++)
            out_seg[i] = disp.digits[i];
    }

    // --- 扫描 ---
    GPIO_ResetBits(GPIO_DIG, DIG1_PIN | DIG2_PIN | DIG3_PIN);
    HC595_SendByte(out_seg[disp.cur_digit]);

    switch (disp.cur_digit)
    {
    case 0:
        GPIO_SetBits(GPIO_DIG, DIG1_PIN);
        break;
    case 1:
        GPIO_SetBits(GPIO_DIG, DIG2_PIN);
        break;
    case 2:
        GPIO_SetBits(GPIO_DIG, DIG3_PIN);
        break;
    }

    disp.cur_digit++;
    if (disp.cur_digit >= 3)
        disp.cur_digit = 0;
}
