#include "main.h"

enum HEAT_COOL_CTRL_STATUS HeatCtrl_Command = ST_HEAT_DET_SELF;
enum HEAT_COOL_CTRL_STATUS CoolCtrl_Command = ST_COOL_DET_SELF;
union HEAT_COOL_FAULT_FLAG Heat_Cool_FaultFlag;

struct HEAT_COOL_ELEMENT Heat_Cool_Element;

void Heat_Control(void)
{
	if (!System_OnOFF_Func.bits.b1OnOFF_Heat)
	{ // 没有这个功能，不进来
		return;
	}



	MCUO_RELAY_HEAT = SystemStatus.bits.b1Status_Heat;
}

void InitHeat_Cool(void)
{
	GPIO_InitTypeDef GPIO_InitStructure;

	GPIO_InitStructure.GPIO_Pin = PIN_HT_EN;
	GPIO_InitStructure.GPIO_Mode = GPIO_Mode_OUT;
	GPIO_InitStructure.GPIO_Speed = GPIO_Speed_Level_1;
	GPIO_InitStructure.GPIO_OType = GPIO_OType_PP;
	GPIO_Init(PORT_HT_EN, &GPIO_InitStructure);
}

// 加热冷凝电流均无法检测
void App_Heat_Cool_Ctrl(void)
{
	Heat_Control(); // 关闭和电流不挂钩(证明开始使用了)，打开必须挂钩，不然就长期在那里耗，把电池耗到低压保护。
}
