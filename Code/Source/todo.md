
梳理eeprom、flash、backup，读、写协议、校验回读
iap、app地址划分，跳转逻辑


1、eeprom
- PRT_E2ROMParas                    (addr = 0, len = 65, end addr = )
- g_u16CalibCoefK,      (addr = 154, len = 47 )
- g_i16CalibCoefB,      (addr = 258, len = 47 )
- OtherElement          (addr = 676, len = 32)
- Heat_Cool_Element     (addr = 740 , len = 24)
- AFE_Parameters_RS485_Struction  (addr = 950, len = 24 )
- BMS_LOG_POINT         (addr = 1200, len = 1)
- BMS_LOG_RECORD[EVENT_RECORD_LENGTH][2]     (addr = 1000, len = 100)

2、flash
#define FLASH_ADDR_IAP_START 			0x08000000		//IAP=7K，这个地方出了一次问题，修改后大于6K，侧面反映编译出来的Zi-data也是flash的东西
#define FLASH_ADDR_APP_START 			0x08001C00		//APP=64-7-1-1=55K
#define FLASH_ADDR_SH367309_VALUE 		0x0800F000
#define FLASH_ADDR_UPDATE_FLAG 			0x0800F800		//升级标志位，1K
#define FLASH_ADDR_SLEEP_FLAG           0x0800FC00

3、eeprom参数 上位机读写逻辑
