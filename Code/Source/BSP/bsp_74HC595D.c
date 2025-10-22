#include "main.h"

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
}

#include "stm32f10x.h"

// 假设使用 PB0-2 控制 74HC595，PB3-5 控制位选
#define SER_HIGH() GPIO_SetBits(GPIOB, GPIO_Pin_0)
#define SER_LOW() GPIO_ResetBits(GPIOB, GPIO_Pin_0)
#define SRCLK_HIGH() GPIO_SetBits(GPIOB, GPIO_Pin_1)
#define SRCLK_LOW() GPIO_ResetBits(GPIOB, GPIO_Pin_1)
#define RCLK_HIGH() GPIO_SetBits(GPIOB, GPIO_Pin_2)
#define RCLK_LOW() GPIO_ResetBits(GPIOB, GPIO_Pin_2)

#define DIGIT1_PIN GPIO_Pin_3
#define DIGIT2_PIN GPIO_Pin_4
#define DIGIT3_PIN GPIO_Pin_5

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

//--------------------------------------
// 发送一字节给74HC595
//--------------------------------------
static void HC595_SendByte(uint8_t data)
{
    for (int i = 0; i < 8; i++)
    {
        if (data & 0x80)
            SER_HIGH();
        else
            SER_LOW();
        SRCLK_HIGH();
        SRCLK_LOW();
        data <<= 1;
    }
    RCLK_HIGH();
    RCLK_LOW();
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
    GPIO_ResetBits(GPIOB, DIGIT1_PIN | DIGIT2_PIN | DIGIT3_PIN);

    // 输出段码
    HC595_SendByte(SEG_CODE[digits[gDisplay.current_digit]]);

    // 打开当前位选
    switch (gDisplay.current_digit)
    {
    case 0:
        GPIO_SetBits(GPIOB, DIGIT1_PIN);
        break;
    case 1:
        GPIO_SetBits(GPIOB, DIGIT2_PIN);
        break;
    case 2:
        GPIO_SetBits(GPIOB, DIGIT3_PIN);
        break;
    }

    // 下次扫描切换下一位
    gDisplay.current_digit++;
    if (gDisplay.current_digit >= 3)
        gDisplay.current_digit = 0;
}

void SysTick_Handler(void)
{
    static uint32_t tick = 0;
    tick++;
    Display_ScanTask(tick);
}

void test_main(void)
{
    Display_UpdateData(DISPLAY_SOC, 78, 5);
}