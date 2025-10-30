#include "stm32f10x.h"
#include <stdint.h>

/*======================== 74HC595 引脚定义 ========================*/
#define GPIO_595 GPIOA
#define SER_PIN GPIO_Pin_0   // DS
#define SRCLK_PIN GPIO_Pin_1 // SH_CP
#define RCLK_PIN GPIO_Pin_2  // ST_CP

#define GPIO_DIG GPIOB
#define DIG1_PIN GPIO_Pin_3 // 个
#define DIG2_PIN GPIO_Pin_4 // 十
#define DIG3_PIN GPIO_Pin_5 // 百 / E

#define SER_H() (GPIO_595->BSRR = SER_PIN)
#define SER_L() (GPIO_595->BRR = SER_PIN)
#define SRCLK_H() (GPIO_595->BSRR = SRCLK_PIN)
#define SRCLK_L() (GPIO_595->BRR = SRCLK_PIN)
#define RCLK_H() (GPIO_595->BSRR = RCLK_PIN)
#define RCLK_L() (GPIO_595->BRR = RCLK_PIN)

#define HC595_DELAY() \
    __NOP();          \
    __NOP();          \
    __NOP();          \
    __NOP()

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

/*======================== 显示状态结构 ========================*/
typedef enum
{
    DISP_MODE_SOC = 0,
    DISP_MODE_FAULT
} DISP_Mode_t;

static struct
{
    DISP_Mode_t mode;
    uint8_t soc_seg[3];   // 缓存 SOC 段码
    uint8_t fault_seg[3]; // 缓存 E+故障段码
    uint16_t tick_ms;
    uint8_t toggle;
    uint8_t cur_digit;
} disp;

/*======================== 基础发送函数 ========================*/
static void HC595_SendByte(uint8_t data)
{
    RCLK_L();
    for (uint8_t i = 0; i < 8; i++)
    {
        SRCLK_L();
        if (data & 0x01)
            SER_H();
        else
            SER_L();
        HC595_DELAY();
        SRCLK_H();
        HC595_DELAY();
        SRCLK_L();
        data >>= 1;
    }
    RCLK_H();
    HC595_DELAY();
    RCLK_L();
    SER_L();
}

/*======================== 初始化 ========================*/
void DISP_Init(void)
{
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOA | RCC_APB2Periph_GPIOB, ENABLE);
    GPIO_InitTypeDef gi = {0};
    gi.GPIO_Mode = GPIO_Mode_Out_PP;
    gi.GPIO_Speed = GPIO_Speed_2MHz; // 线长远→低速更稳
    gi.GPIO_Pin = SER_PIN | SRCLK_PIN | RCLK_PIN;
    GPIO_Init(GPIO_595, &gi);
    gi.GPIO_Pin = DIG1_PIN | DIG2_PIN | DIG3_PIN;
    GPIO_Init(GPIO_DIG, &gi);

    SER_L();
    SRCLK_L();
    RCLK_L();
    GPIO_ResetBits(GPIO_DIG, DIG1_PIN | DIG2_PIN | DIG3_PIN);

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

/*======================== 主循环更新接口（无除法） ========================*/
void DISP_UpdateSOC(uint16_t soc)
{
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

void DISP_UpdateFault(uint8_t code)
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

void DISP_SetMode(DISP_Mode_t m) { disp.mode = m; }

/*======================== 1 ms 扫描任务（ISR 调用） ========================*/
void DISP_Task_1ms(void)
{
    uint8_t seg;

    disp.tick_ms++;
    if (disp.mode == DISP_MODE_FAULT)
    {
        if (disp.tick_ms >= 1000)
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

    GPIO_ResetBits(GPIO_DIG, DIG1_PIN | DIG2_PIN | DIG3_PIN);

    if (disp.mode == DISP_MODE_FAULT && disp.toggle == 0)
        seg = disp.fault_seg[disp.cur_digit];
    else
        seg = disp.soc_seg[disp.cur_digit];

    HC595_SendByte(seg);

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
    disp.cur_digit = (disp.cur_digit + 1) % 3;
}

#include "stm32f10x.h"

// === 来自 display_595.c 的函数声明 ===
void DISP_Init(void);
void DISP_UpdateSOC(uint16_t soc);
void DISP_UpdateFault(uint8_t code);
void DISP_SetMode(int mode);
void DISP_Task_1ms(void);

// 模式定义（和 display_595.c 一致）
#define DISP_MODE_SOC 0
#define DISP_MODE_FAULT 1

// === 模拟应用状态 ===
static uint16_t soc_value = 0;
static uint8_t fault_code = 0;
static uint8_t has_fault = 0;

// === 初始化 ===
int main(void)
{
    SystemInit();
    DISP_Init();

    SysTick_Config(SystemCoreClock / 1000); // 1ms 中断刷新显示

    // 初始化显示
    DISP_UpdateSOC(soc_value);
    DISP_UpdateFault(1);
    DISP_SetMode(DISP_MODE_SOC);

    while (1)
    {
        // --------------- 示例：更新 SOC ----------------
        static uint16_t soc_timer = 0;
        soc_timer++;
        if (soc_timer >= 1000)
        { // 每秒更新一次 SOC 模拟变化
            soc_timer = 0;
            soc_value++;
            if (soc_value > 100)
                soc_value = 0;
            DISP_UpdateSOC(soc_value);
        }

        // --------------- 示例：控制报警 ----------------
        static uint32_t fault_timer = 0;
        fault_timer++;
        if (fault_timer >= 5000)
        { // 每5秒切换报警状态
            fault_timer = 0;
            has_fault = !has_fault;
            fault_code++;
            if (fault_code > 20)
                fault_code = 1;
            DISP_UpdateFault(fault_code);
        }

        // --------------- 选择显示模式 ----------------
        if (has_fault)
        {
            DISP_SetMode(DISP_MODE_FAULT); // 显示E+故障，与SOC交替1s闪烁
        }
        else
        {
            DISP_SetMode(DISP_MODE_SOC); // 只显示SOC
        }

        // （这里可以处理BMS逻辑、通讯、保护判断等）
    }
}

// === SysTick中断 ===
void SysTick_Handler(void)
{
    DISP_Task_1ms(); // 1ms刷新扫描
}
