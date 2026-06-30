#include "main.h"

/*================= SOC 参数 =================*/
#define LED_COUNT 5
#define SAMPLE_MS 250
#define WINDOW_SIZE 5
#define DEBOUNCE_COUNT 3
#define PER_CELL_HYST 0.02f
#define PACK_CELLS 14

LEDBAR_COMMAND LedBar_Command = LED_BAR_STARTUP;

static const float cell_thresholds[LED_COUNT] = {3.30f, 3.55f, 3.70f, 3.85f, 4.00f};

/*================= 全局变量 =================*/
static float pack_thresholds[LED_COUNT];
static float pack_hysteresis;

static float volt_window[WINDOW_SIZE];
static int window_idx = 0, window_cnt = 0;

static int cur_level = 0;
static int cand_level = -1, cand_cnt = 0;

static uint8_t sys_on = 0;

/*================= LED 表描述 =================*/
typedef struct
{
    GPIO_TypeDef *port;
    uint16_t pin;
} LedPin_t;

static const LedPin_t leds[LED_COUNT] = {
    {GPIO_LED20, PIN_LED20},
    {GPIO_LED40, PIN_LED40},
    {GPIO_LED60, PIN_LED60},
    {GPIO_LED80, PIN_LED80},
    {GPIO_LED100, PIN_LED100}};

/*================= 工具函数 =================*/
static void LED_On(uint8_t idx) { GPIO_SetBits(leds[idx].port, leds[idx].pin); }
static void LED_Off(uint8_t idx) { GPIO_ResetBits(leds[idx].port, leds[idx].pin); }

void apply_led(int level)
{
    for (int i = 0; i < LED_COUNT; i++)
    {
        if (i < level)
            LED_On(i);
        else
            LED_Off(i);
    }
}

/*================= 滤波/等级 =================*/
static float add_sample(float v)
{
    volt_window[window_idx] = v;
    window_idx = (window_idx + 1) % WINDOW_SIZE;
    if (window_cnt < WINDOW_SIZE)
        window_cnt++;
    float s = 0;
    for (int i = 0; i < window_cnt; i++)
        s += volt_window[i];
    return s / (float)window_cnt;
}

static int raw_level(float avg)
{
    for (int lvl = LED_COUNT; lvl >= 1; lvl--)
    {
        if (avg >= pack_thresholds[lvl - 1])
            return lvl;
    }
    return 0;
}

/*================= 动画 =================*/
static void led_animation(uint8_t on)
{
    if (on)
    { // 开机 低->高
        for (int i = 0; i < LED_COUNT; i++)
        {
            LED_On(i);
            // for (volatile uint32_t d = 0; d < 300000; d++)
            //     ; // 简单延时约80m
            __delay_ms(150);
        }
    }
    else
    { // 关机 高->低
        for (int i = LED_COUNT - 1; i >= 0; i--)
        {
            LED_Off(i);
            // for (volatile uint32_t d = 0; d < 300000; d++)
            //     ;
            __delay_ms(300);
        }
    }
}

/*================= 对外接口 =================*/
void SOC_LED_Init(float init_v)
{
    for (int i = 0; i < LED_COUNT; i++)
        pack_thresholds[i] = cell_thresholds[i] * PACK_CELLS;
    pack_hysteresis = PER_CELL_HYST * PACK_CELLS;

    for (int i = 0; i < WINDOW_SIZE; i++)
        volt_window[i] = init_v;
    window_cnt = WINDOW_SIZE;
    window_idx = 0;
    cur_level = raw_level(init_v);
    cand_level = -1;
    cand_cnt = 0;
    apply_led(0);
}

void SOC_LED_Update(void)
{
    if (0 == g_st_SysTimeFlag.bits.b1Sys200msFlag3)
    {
        return;
    }

    // extern float Read_PackVoltage(void);
    // float v = Read_PackVoltage();
    sys_time.sample_voltage = (float)g_stCellInfoReport.u16VCellTotle / 100;
    float v = sys_time.sample_voltage;
    float avg = add_sample(v);
    int raw = raw_level(avg);

    if (raw != cur_level)
    {
        if (cand_level != raw)
        {
            cand_level = raw;
            cand_cnt = 1;
            return;
        }
        int ok = 0;
        if (raw > cur_level)
        {
            float need = pack_thresholds[raw - 1] + pack_hysteresis / 2;
            if (avg >= need)
                ok = 1;
        }
        else if (raw < cur_level)
        {
            if (raw == 0)
            {
                float need = pack_thresholds[0] - pack_hysteresis / 2;
                if (avg <= need)
                    ok = 1;
            }
            else
            {
                float need = pack_thresholds[raw] - pack_hysteresis / 2;
                if (avg <= need)
                    ok = 1;
            }
        }
        if (ok)
        {
            cand_cnt++;
            if (cand_cnt >= DEBOUNCE_COUNT)
            {
                cur_level = cand_level;
                cand_level = -1;
                cand_cnt = 0;
                if (sys_on)
                    apply_led(cur_level);
            }
        }
        else
            cand_cnt = 0;
    }
    else
    {
        cand_level = -1;
        cand_cnt = 0;
    }
}

extern bool key_func_enable;
void Board_PowerOn(void)
{
    sys_on = 1;
    led_animation(1);
    // __delay_ms(100);
    // apply_led(cur_level);
    // apply_led(0);
}

void Board_PowerOff(void)
{
    sys_on = 0;
    apply_led(5);
    __delay_ms(200);
    led_animation(0);
    apply_led(0);
}

/*================= 按键任务 =================*/
void Key_Task(void)
{
    static uint8_t prev = 0;
    uint8_t now = GPIO_ReadInputDataBit(GPIO_LED_KEY, PIN_LED_KEY);
    if (now && !prev)
    {
        if (sys_on)
        {
            apply_led(cur_level);
            for (volatile uint32_t d = 0; d < 1500000; d++)
                ; // ~1.5s
            apply_led(0);
        }
        else
        {
            Board_PowerOn();
        }
    }
    prev = now;
}

void LedBar_StartUp(void)
{
    static UINT16 su16_ShowDelay_Tcnt = 0;
    static uint8_t led_on_index = 0;

    static uint8_t state = 0;
    static uint16_t led_animation_cnt = 0;
    switch (state)
    {
    case 0:
        ++led_animation_cnt;
        if (led_animation_cnt % 3 == 0)
        {
            if (led_on_index < 5)
                LED_On(led_on_index);

            led_on_index++;
            if (led_on_index == 8)
            {
                apply_led(0);
                state = 1;
            }
        }
        break;
    case 1:
        if (++su16_ShowDelay_Tcnt <= 10 * 3)
        {
            if (cur_level == 0)
                MCUO_SOC_20 = !MCUO_SOC_20;
            else
                apply_led(cur_level);
        }
        else
        {
            // apply_led(0);
            su16_ShowDelay_Tcnt = 0;

            LedBar_Command = LED_BAR_NORMAL;
        }

    default:
        break;
    }
}

void LedBar_Show_Normal(void)
{
    static UINT8 su8_ShowStatus = 0; // 开机亮5s
    static UINT16 su16_ShowDelay_Tcnt = 0;

    switch (su8_ShowStatus)
    {
    case 0:

        if (MCUI_SOC_KEY == 0)
        {
            su8_ShowStatus = 1;
        }
        // if (g_stCellInfoReport.u16IDischg && bms_status == S_DSG)
        // if (bms_status == S_DSG)
        // {
        //     LedBar_Command = LED_BAR_DSG;
        // }
        // else
        if (bms_status == S_CHG)
        {
            LedBar_Command = LED_BAR_CHG;
        }
        else
        {
            apply_led(0);
        }
        break;
    case 1:
        if (++su16_ShowDelay_Tcnt <= 10 * 5)
        {
            if (cur_level == 0)
            {
                MCUO_SOC_20 = !MCUO_SOC_20;
            }
            else
                apply_led(cur_level);
        }
        else
        {
            apply_led(0);

            su16_ShowDelay_Tcnt = 0;
            su8_ShowStatus = 0;
        }

        // 一直按着
        if (!MCUI_SOC_KEY)
            su16_ShowDelay_Tcnt = 0;
        break;

    default:
        break;
    }
}

void LedBar_Show_CHG(void)
{
    static UINT16 su16_ShowDelay = 0;

    if (bms_status == S_CHG)
    {
        if (cur_level == 5)
        {
            apply_led(5);
            return;
        }

        if (++su16_ShowDelay <= 5)
        {
            apply_led(cur_level);
        }
        else if (++su16_ShowDelay <= 10)
        {
            apply_led(cur_level + 1);
        }
        else
        {
            su16_ShowDelay = 0;
        }
    }
    else
    {
        apply_led(0);

        LedBar_Command = LED_BAR_NORMAL;
    }
}

void LedBar_Show_DSG(void)
{
    if (bms_status == S_DSG)
    {
        // if (cur_level == 0 || cur_level == 1)
        if (cur_level == 0)
        {
            MCUO_SOC_20 = !MCUO_SOC_20;
        }
        else
            apply_led(cur_level);
    }
    else
    {
        apply_led(0);

        LedBar_Command = LED_BAR_NORMAL;
    }
}

static void led_soc_update(void)
{
    if (g_stCellInfoReport.u16IDischg >= 20)
    {
        if (g_stCellInfoReport.u16VCellTotle >= 4900)
            cur_level = 5;
        else if (g_stCellInfoReport.u16VCellTotle >= 4750)
        {
            cur_level = 4;
        }
        else if (g_stCellInfoReport.u16VCellTotle >= 4600)
        {
            cur_level = 3;
        }
        else if (g_stCellInfoReport.u16VCellTotle >= 4450)
        {
            cur_level = 2;
        }
        else if (g_stCellInfoReport.u16VCellTotle >= 4100)
        {
            cur_level = 1;
        }
        else
        {
            cur_level = 0;
        }
    }
    else
    {
        if (g_stCellInfoReport.u16VCellTotle >= 5100)
            cur_level = 5;
        else if (g_stCellInfoReport.u16VCellTotle >= 4850)
        {
            cur_level = 4;
        }
        else if (g_stCellInfoReport.u16VCellTotle >= 4700)
        {
            cur_level = 3;
        }
        else if (g_stCellInfoReport.u16VCellTotle >= 4600)
        {
            cur_level = 2;
        }
        else if (g_stCellInfoReport.u16VCellTotle >= 4350)
        {
            cur_level = 1;
        }
        else
        {
            cur_level = 0;
        }
    }
}

void APP_LedBar(void)
{
    if (0 == g_st_SysTimeFlag.bits.b1Sys100msFlag)
    {
        return;
    }

    // if (SystemStatus.bits.b1StartUpBMS)
    // {
    //     return;
    // }
    led_soc_update();

    switch (LedBar_Command)
    {
    case LED_BAR_STARTUP:
        LedBar_StartUp();
        break;
    case LED_BAR_NORMAL:
        LedBar_Show_Normal();
        break;
    case LED_BAR_CHG:
        LedBar_Show_CHG();
        break;
    case LED_BAR_DSG:
        LedBar_Show_DSG();
        break;
    case LED_BAR_FAULT:
        // 下面长期监控
        break;

    default:
        break;
    }

    // LedBar_Show_Sleep();
}
