

1、长按电量显示板5s，电池开机或关机，开机时从灯L1到灯L5跑马，关机时从灯L5到L1跑马。 2、闭合放电开关，电池可放电，充电口不带电，放电状态下电量显示板根据当前电压长亮灯，电压小于43.4V时，灯L1闪烁。断开放电开关，电池无输出，电量显示板长灭，单击电量显示板按键可查看当前电量，5s后灯自动熄灭。 3、插入充电器，电池充电，放电口不带电，充电状态下闪跑马灯, 电压大于等于57.4时满电，L1 ~L5常亮。

    void
    Display_ScanTask(uint32_t now_ms)
{
    uint8_t digits[3];
    uint8_t seg_data;
    uint16_t value;

    // 获取显示值
    if (gDisplay.mode == DISPLAY_FAULT && gDisplay.toggle_state == 0)
    {
        uint8_t fault = gDisplay.fault;
        digits[0] = fault % 10;
        digits[1] = (fault / 10) % 10;
        digits[2] = 0xEE; // E
    }
    else
    {
        value = gDisplay.soc;
        digits[0] = value % 10;
        digits[1] = (value / 10) % 10;
        digits[2] = (value / 100) % 10;
    }

    // -------- 安全时序开始 --------
    // 1. 关闭所有位选
    GPIO_ResetBits(GPIOB, DIGIT1_PIN | DIGIT2_PIN | DIGIT3_PIN);

    // 2. 计算段码
    if (digits[gDisplay.current_digit] == 0xEE)
        seg_data = SEG_E;
    else
        seg_data = SEG_CODE[digits[gDisplay.current_digit]];

    // 如果是共阳数码管，加上取反
    // seg_data = ~seg_data;

    // 3. 发送数据并锁存
    HC595_SendByte(seg_data);

    // 4. 打开对应位选
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

    // 5. 下一位
    gDisplay.current_digit++;
    if (gDisplay.current_digit >= 3)
        gDisplay.current_digit = 0;
}

#include "bms.h"
#include "display.h"
#include "tuya_mcu_api.h"
#include "button.h"

#define PRESS_3S_MS 3000
#define PRESS_10S_MS 10000
#define SCAN_INTERVAL 100 // 按键扫描周期(ms)

typedef enum
{
    KEY_IDLE = 0,
    KEY_PRESSED,
} key_state_t;

static key_state_t key_state = KEY_IDLE;
static uint32_t press_time_ms = 0;
static bool reset_triggered = false;

void key_task(void)
{
    static uint32_t last_tick = 0;
    uint32_t now = millis(); // 或 SysTick 计时
    if (now - last_tick < SCAN_INTERVAL)
        return;
    last_tick = now;

    bool pressed = button_is_pressed();

    switch (key_state)
    {
    case KEY_IDLE:
        if (pressed)
        {
            key_state = KEY_PRESSED;
            press_time_ms = 0;
            reset_triggered = false;
        }
        break;

    case KEY_PRESSED:
        if (pressed)
        {
            press_time_ms += SCAN_INTERVAL;

            // 达到10秒立即触发模组复位
            if (press_time_ms >= PRESS_10S_MS && !reset_triggered)
            {
                reset_triggered = true;
                display_show_text("CLr");
                display_blink(5, 200);
                delay_ms(1000);
                mcu_reset_cellular();
            }
        }
        else
        { // 松开
            if (!reset_triggered)
            {
                if (press_time_ms >= PRESS_3S_MS && press_time_ms < PRESS_10S_MS)
                {
                    display_show_text("OFF");
                    display_blink(3, 300);
                    bms_enter_sleep();
                }
            }
            key_state = KEY_IDLE;
        }
        break;
    }
}

if (pressed)
{
    press_time_ms += SCAN_INTERVAL;

    if (press_time_ms == PRESS_3S_MS)
    {
        display_show_text("OFF");
        display_blink(1, 300); // 提示可松手休眠
    }

    if (press_time_ms >= PRESS_10S_MS && !reset_triggered)
    {
        reset_triggered = true;
        display_show_text("CLr");
        display_blink(5, 200);
        delay_ms(1000);
        mcu_reset_cellular();
    }
}
else
{
    if (!reset_triggered && press_time_ms >= PRESS_3S_MS && press_time_ms < PRESS_10S_MS)
    {
        display_show_text("OFF");
        display_blink(3, 300);
        bms_enter_sleep();
    }
    key_state = KEY_IDLE;
}

#include "bms.h"
#include "display.h"
#include "tuya_mcu_api.h"
#include "button.h"

#define PRESS_3S_MS 3000
#define PRESS_10S_MS 10000
#define CONFIRM_TIMEOUT_MS 10000
#define SCAN_INTERVAL 100

typedef enum
{
    KEY_IDLE = 0,
    KEY_PRESSED,
    WAIT_CONFIRM_RESET,
} key_state_t;

static key_state_t key_state = KEY_IDLE;
static uint32_t press_time_ms = 0;
static uint32_t confirm_timer = 0;
static bool reset_triggered = false;

void key_task(void)
{
    static uint32_t last_tick = 0;
    uint32_t now = millis(); // 系统时间，ms
    if (now - last_tick < SCAN_INTERVAL)
        return;
    last_tick = now;

    bool pressed = button_is_pressed();

    switch (key_state)
    {
    case KEY_IDLE:
        if (pressed)
        {
            key_state = KEY_PRESSED;
            press_time_ms = 0;
            reset_triggered = false;
        }
        break;

    case KEY_PRESSED:
        if (pressed)
        {
            press_time_ms += SCAN_INTERVAL;

            if (press_time_ms == PRESS_3S_MS)
            {
                display_show_text("OFF");
                display_blink(1, 300);
            }

            if (press_time_ms >= PRESS_10S_MS)
            {
                key_state = WAIT_CONFIRM_RESET;
                confirm_timer = 0;
                display_show_text("CLr");
                display_blink(5, 200); // 显示“等待确认”
            }
        }
        else
        { // 松开
            if (!reset_triggered &&
                press_time_ms >= PRESS_3S_MS &&
                press_time_ms < PRESS_10S_MS)
            {
                display_show_text("OFF");
                display_blink(3, 300);
                bms_enter_sleep();
            }
            key_state = KEY_IDLE;
        }
        break;

    case WAIT_CONFIRM_RESET:
        confirm_timer += SCAN_INTERVAL;

        if (pressed)
        { // 用户确认单击
            display_show_text("YES");
            display_blink(3, 200);
            delay_ms(1000);
            mcu_reset_cellular();
            reset_triggered = true;
            key_state = KEY_IDLE;
        }
        else if (confirm_timer >= CONFIRM_TIMEOUT_MS)
        {
            display_show_text("---"); // 恢复正常显示
            key_state = KEY_IDLE;
        }
        break;
    }
}
