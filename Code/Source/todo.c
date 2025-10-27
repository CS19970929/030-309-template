

1、长按电量显示板5s，电池开机或关机，开机时从灯L1到灯L5跑马，关机时从灯L5到L1跑马。
2、闭合放电开关，电池可放电，充电口不带电，放电状态下电量显示板根据当前电压长亮灯，电压小于43.4V时，灯L1闪烁。断开放电开关，电池无输出，电量显示板长灭，单击电量显示板按键可查看当前电量，5s后灯自动熄灭。
3、插入充电器，电池充电，放电口不带电，充电状态下闪跑马灯,电压大于等于57.4时满电，L1~L5常亮。

void Display_ScanTask(uint32_t now_ms)
{
    uint8_t digits[3];
    uint8_t seg_data;
    uint16_t value;

    // 获取显示值
    if (gDisplay.mode == DISPLAY_FAULT && gDisplay.toggle_state == 0) {
        uint8_t fault = gDisplay.fault;
        digits[0] = fault % 10;
        digits[1] = (fault / 10) % 10;
        digits[2] = 0xEE; // E
    } else {
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
    switch (gDisplay.current_digit) {
        case 0: GPIO_SetBits(GPIOB, DIGIT1_PIN); break;
        case 1: GPIO_SetBits(GPIOB, DIGIT2_PIN); break;
        case 2: GPIO_SetBits(GPIOB, DIGIT3_PIN); break;
    }

    // 5. 下一位
    gDisplay.current_digit++;
    if (gDisplay.current_digit >= 3) gDisplay.current_digit = 0;
}
