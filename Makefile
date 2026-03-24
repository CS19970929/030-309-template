PROJECT := CommomSH367309_16series_030C8T6_C_gcc
BUILD_DIR := build_gcc
LINKER_SCRIPT := gcc_stm32f030c8_app.ld

TOOLCHAIN_ROOT ?= /tmp/arm-gcc-xpack/xpack-arm-none-eabi-gcc-15.2.1-1.1
TOOLCHAIN_BIN := $(TOOLCHAIN_ROOT)/bin

CC := $(TOOLCHAIN_BIN)/arm-none-eabi-gcc
AS := $(CC)
OBJCOPY := $(TOOLCHAIN_BIN)/arm-none-eabi-objcopy
SIZE := $(TOOLCHAIN_BIN)/arm-none-eabi-size

CPUFLAGS := -mcpu=cortex-m0 -mthumb
DEFS := -DUSE_STDPERIPH_DRIVER -DSTM32F0XX
INCLUDES := \
	-ICode/Drivers \
	-ICode/STM32F0xx_StdPeriph_Driver/inc \
	-ICode/Source \
	-ICode/Source/conf \
	-ICode/Source/BSP \
	-ICode/Source/BSP/inc \
	-ICode/Source/NewFunc

CFLAGS := $(CPUFLAGS) $(DEFS) $(INCLUDES) -std=gnu99 -ffunction-sections -fdata-sections -Wall -Wextra -Wno-unused-parameter -Wno-sign-compare -g3 -Os
LDFLAGS := $(CPUFLAGS) -T$(LINKER_SCRIPT) -Wl,--gc-sections -Wl,-Map,$(BUILD_DIR)/$(PROJECT).map --specs=nano.specs --specs=nosys.specs

SRCS := \
	Code/Drivers/system_stm32f0xx.c \
	Code/Drivers/stm32f0xx_it.c \
	Code/Drivers/startup_gcc.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_adc.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_can.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_cec.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_comp.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_crc.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_crs.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_dac.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_dbgmcu.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_dma.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_exti.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_flash.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_gpio.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_i2c.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_iwdg.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_misc.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_pwr.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_rcc.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_rtc.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_spi.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_syscfg.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_tim.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_usart.c \
	Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_wwdg.c \
	Code/Source/ADC.c \
	Code/Source/ascii_slave.c \
	Code/Source/Cell_balance.c \
	Code/Source/ChargerLoadFunc.c \
	Code/Source/DataDeal.c \
	Code/Source/EEPROM.c \
	Code/Source/Fault.c \
	Code/Source/Flash.c \
	Code/Source/Heat_Cool.c \
	Code/Source/I2C_AFE1.c \
	Code/Source/IODrivers.c \
	Code/Source/IO_Control.c \
	Code/Source/LogRecord.c \
	Code/Source/main.c \
	Code/Source/NewFunc/LedBar.c \
	Code/Source/ProductionID.c \
	Code/Source/PubFunc.c \
	Code/Source/RTC.c \
	Code/Source/Sci_Upper.c \
	Code/Source/SH367309_DataDeal.c \
	Code/Source/SH367309_Func.c \
	Code/Source/ShortFunc.c \
	Code/Source/SleepDeal.c \
	Code/Source/SOC.c \
	Code/Source/SocEnhance.c \
	Code/Source/System_Init.c \
	Code/Source/System_Monitor.c \
	Code/Source/conf/conf.c \
	Code/Source/BSP/bsp.c \
	Code/Source/BSP/bsp_timer.c \
	Code/Source/BSP/Time_Triggered.c

OBJS := $(patsubst %.c,$(BUILD_DIR)/%.o,$(SRCS))

.PHONY: all clean size

all: $(BUILD_DIR)/$(PROJECT).elf $(BUILD_DIR)/$(PROJECT).bin

$(BUILD_DIR)/$(PROJECT).elf: $(OBJS) $(LINKER_SCRIPT)
	@mkdir -p $(dir $@)
	$(CC) $(OBJS) $(LDFLAGS) -o $@
	$(SIZE) $@

$(BUILD_DIR)/$(PROJECT).bin: $(BUILD_DIR)/$(PROJECT).elf
	$(OBJCOPY) -O binary $< $@

$(BUILD_DIR)/%.o: %.c
	@mkdir -p $(dir $@)
	$(CC) $(CFLAGS) -c $< -o $@

clean:
	rm -rf $(BUILD_DIR)

size: $(BUILD_DIR)/$(PROJECT).elf
	$(SIZE) $<
