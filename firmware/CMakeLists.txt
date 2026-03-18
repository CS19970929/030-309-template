cmake_minimum_required(VERSION 3.25)

project(stm32f030_app C ASM)

set(PROJECT_ROOT "${CMAKE_CURRENT_LIST_DIR}/..")
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

set(FIRMWARE_SOURCES
    "${PROJECT_ROOT}/Code/Drivers/stm32f0xx_it.c"
    "${PROJECT_ROOT}/Code/Drivers/system_stm32f0xx.c"
    "${PROJECT_ROOT}/Code/Source/ADC.c"
    "${PROJECT_ROOT}/Code/Source/Cell_balance.c"
    "${PROJECT_ROOT}/Code/Source/ChargerLoadFunc.c"
    "${PROJECT_ROOT}/Code/Source/Comm.c"
    "${PROJECT_ROOT}/Code/Source/DataDeal.c"
    "${PROJECT_ROOT}/Code/Source/EEPROM.c"
    "${PROJECT_ROOT}/Code/Source/Fault.c"
    "${PROJECT_ROOT}/Code/Source/Flash.c"
    "${PROJECT_ROOT}/Code/Source/Heat_Cool.c"
    "${PROJECT_ROOT}/Code/Source/I2C_AFE1.c"
    "${PROJECT_ROOT}/Code/Source/IODrivers.c"
    "${PROJECT_ROOT}/Code/Source/IO_Control.c"
    "${PROJECT_ROOT}/Code/Source/LogRecord.c"
    "${PROJECT_ROOT}/Code/Source/main.c"
    "${PROJECT_ROOT}/Code/Source/ProductionID.c"
    "${PROJECT_ROOT}/Code/Source/PubFunc.c"
    "${PROJECT_ROOT}/Code/Source/RTC.c"
    "${PROJECT_ROOT}/Code/Source/Sci_Upper.c"
    "${PROJECT_ROOT}/Code/Source/SH367309_DataDeal.c"
    "${PROJECT_ROOT}/Code/Source/SH367309_Func.c"
    "${PROJECT_ROOT}/Code/Source/ShortFunc.c"
    "${PROJECT_ROOT}/Code/Source/SleepDeal.c"
    "${PROJECT_ROOT}/Code/Source/SOC.c"
    "${PROJECT_ROOT}/Code/Source/SocEnhance.c"
    "${PROJECT_ROOT}/Code/Source/System_Init.c"
    "${PROJECT_ROOT}/Code/Source/System_Monitor.c"
    "${PROJECT_ROOT}/Code/Source/ascii_slave.c"
    "${PROJECT_ROOT}/Code/Source/bms_comm_data_adapter.c"
    "${PROJECT_ROOT}/Code/Source/modbus_rtu_parser.c"
    "${PROJECT_ROOT}/Code/Source/modbus_service.c"
    "${PROJECT_ROOT}/Code/Source/BSP/bsp.c"
    "${PROJECT_ROOT}/Code/Source/BSP/bsp_timer.c"
    "${PROJECT_ROOT}/Code/Source/BSP/Time_Triggered.c"
    "${PROJECT_ROOT}/Code/Source/BSP/src/bsp_key.c"
    "${PROJECT_ROOT}/Code/Source/NewFunc/LedBar.c"
    "${PROJECT_ROOT}/Code/Source/conf/conf.c"
    "${CMAKE_CURRENT_LIST_DIR}/startup/startup_stm32f0xx_gcc.S"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_adc.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_can.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_cec.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_comp.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_crc.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_crs.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_dac.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_dbgmcu.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_dma.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_exti.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_flash.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_gpio.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_i2c.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_iwdg.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_misc.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_pwr.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_rcc.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_rtc.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_spi.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_syscfg.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_tim.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_usart.c"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/src/stm32f0xx_wwdg.c"
)

add_executable(${PROJECT_NAME}.elf ${FIRMWARE_SOURCES})

target_include_directories(${PROJECT_NAME}.elf PRIVATE
    "${PROJECT_ROOT}/Code/Drivers"
    "${PROJECT_ROOT}/Code/STM32F0xx_StdPeriph_Driver/inc"
    "${PROJECT_ROOT}/Code/Source"
    "${PROJECT_ROOT}/Code/Source/BSP"
    "${PROJECT_ROOT}/Code/Source/BSP/inc"
    "${PROJECT_ROOT}/Code/Source/NewFunc"
    "${PROJECT_ROOT}/Code/Source/conf"
)

target_compile_definitions(${PROJECT_NAME}.elf PRIVATE
    USE_STDPERIPH_DRIVER
    STM32F0XX
)

target_compile_options(${PROJECT_NAME}.elf PRIVATE
    -mcpu=cortex-m0
    -mthumb
    -ffunction-sections
    -fdata-sections
    -fno-common
    -fmessage-length=0
    -std=gnu99
    $<$<COMPILE_LANGUAGE:C>:-Wall>
)

target_link_options(${PROJECT_NAME}.elf PRIVATE
    -T${CMAKE_CURRENT_LIST_DIR}/linker/stm32f030c8_app.ld
    -mcpu=cortex-m0
    -mthumb
    -Wl,--gc-sections
    -Wl,-Map=${CMAKE_BINARY_DIR}/stm32f030_app.map
    --specs=nano.specs
    --specs=nosys.specs
)

set_target_properties(${PROJECT_NAME}.elf PROPERTIES
    OUTPUT_NAME "CommomSH367309_16series_030C8T6_C"
)

find_program(CMAKE_OBJCOPY arm-none-eabi-objcopy)
find_program(CMAKE_SIZE arm-none-eabi-size)

if(CMAKE_OBJCOPY)
    add_custom_command(TARGET ${PROJECT_NAME}.elf POST_BUILD
        COMMAND ${CMAKE_OBJCOPY} -O binary
            $<TARGET_FILE:${PROJECT_NAME}.elf>
            ${CMAKE_BINARY_DIR}/CommomSH367309_16series_030C8T6_C.bin
        COMMAND ${CMAKE_OBJCOPY} -O ihex
            $<TARGET_FILE:${PROJECT_NAME}.elf>
            ${CMAKE_BINARY_DIR}/CommomSH367309_16series_030C8T6_C.hex
        VERBATIM
    )
endif()

if(CMAKE_SIZE)
    add_custom_command(TARGET ${PROJECT_NAME}.elf POST_BUILD
        COMMAND ${CMAKE_SIZE} $<TARGET_FILE:${PROJECT_NAME}.elf>
        VERBATIM
    )
endif()
