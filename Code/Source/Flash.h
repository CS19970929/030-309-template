#ifndef FLASH_H
#define FLASH_H

//ϵͳ�洢����������ST����������ڲ�Ԥ����һ��BootLoader��Ҳ����ISP��������һ��ROM
//û��ʲô�е�������Ʒ��ֻ����C8��R8����Ϊ64K��	 ÿҳ1KB��ϵͳ�洢��3KB��
#define FLASH_ADDR_IAP_START 			0x08000000		//IAP=7K������ط�����һ�����⣬�޸ĺ����6K�����淴ӳ���������Zi-dataҲ��flash�Ķ���
#define FLASH_ADDR_APP_START 			0x08001C00		//APP=64-7-1-1=55K

#define FLASH_ADDR_PARAM_VALUE 		0x0800EC00		//���ߴ���ؼ�����(����ƫ��ֵ)��1K
#define FLASH_ADDR_TEST_BMS_PARAM       0x0800F000
#define FLASH_ADDR_LOG_FLASH_START       0x0800F400

#define FLASH_ADDR_UPDATE_FLAG 			0x0800F800		//������־λ��1K
#define FLASH_ADDR_SLEEP_FLAG           0x0800FC00		//���߹ؼ�ָ�1K


#define FLASH_309_RTC_RTC_VALUE			((UINT16)0x1222)	//RTC���ߣ�RTC���ѣ�ֻ��RTC���ߣ����п���RTC���ѡ�
#define FLASH_309_RTC_NORMAL_VALUE		((UINT16)0x2333)	//RTC���ߣ������ʽ����(ͨѶ����ť�ȵ�)
#define FLASH_309_NORMAL_NORMAL_VALUE	((UINT16)0xFFFF)	//��ͨ���ߣ������ʽ����(ͨѶ����ť�ȵ�)


#define FLASH_TO_IAP_VALUE				((UINT16)0x00AB)
#define FLASH_TO_APP_VALUE				((UINT16)0xFFFF)

#define FLASH_NORMAL_SLEEP_VALUE    	((UINT16)0x1234)
#define FLASH_DEEP_SLEEP_VALUE    		((UINT16)0x1235)
#define FLASH_HICCUP_SLEEP_VALUE    	((UINT16)0x1236)
#define FLASH_SLEEP_RESET_VALUE    		((UINT16)0xFFFF)


#define MCU_RESET()	NVIC_SystemReset()


FLASH_Status FlashWriteOneWord(uint32_t Address, uint32_t Data);
UINT32 FlashReadOneWord(UINT32 faddr);
uint32_t FlashReadOneWord(UINT32 faddr);

FLASH_Status FlashWriteOneHalfWord(uint32_t StartAddr,uint16_t Buffer);
UINT16 FlashReadOneHalfWord(UINT32 faddr);
void App_FlashUpdateDet(void);
void Init_IAPAPP(void);

#endif	/* FLASH_H */

