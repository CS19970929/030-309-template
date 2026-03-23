cmake_minimum_required(VERSION 3.25)

set(CMAKE_TRY_COMPILE_TARGET_TYPE STATIC_LIBRARY)

project(legacy_keil_app C ASM)

set(PROJECT_ROOT "${CMAKE_CURRENT_LIST_DIR}/..")
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

option(APP_ENABLE_LTO "Enable link time optimization for firmware build" ON)
set(APP_OPT_LEVEL "s" CACHE STRING "Firmware optimization level for GCC builds (0,1,2,3,s,g)")
set(APP_HEAP_SIZE "0x0" CACHE STRING "Reserved heap bytes in linker script")
set(APP_STACK_SIZE "0x0" CACHE STRING "Reserved stack bytes in linker script")

include("${CMAKE_CURRENT_LIST_DIR}/generated/legacy_keil_manifest.cmake")

set(APP_LINKER_TEMPLATE "${CMAKE_CURRENT_LIST_DIR}/linker/legacy_stm32_app.ld")
set(APP_LINKER_SCRIPT "${CMAKE_BINARY_DIR}/generated/legacy_stm32_app.ld")
file(MAKE_DIRECTORY "${CMAKE_BINARY_DIR}/generated")
configure_file("${APP_LINKER_TEMPLATE}" "${APP_LINKER_SCRIPT}" @ONLY)

set(FIRMWARE_SOURCES
    "${CMAKE_CURRENT_LIST_DIR}/startup/startup_stm32f0xx_gcc.S"
    ${LEGACY_SOURCES}
)

add_executable(${PROJECT_NAME}.elf ${FIRMWARE_SOURCES})

target_include_directories(${PROJECT_NAME}.elf PRIVATE
    ${LEGACY_INCLUDE_DIRS}
)

target_compile_definitions(${PROJECT_NAME}.elf PRIVATE
    ${LEGACY_DEFINES}
)

set(APP_GCC_OPT_FLAG "-Os")
if(APP_OPT_LEVEL STREQUAL "0")
    set(APP_GCC_OPT_FLAG "-O0")
elseif(APP_OPT_LEVEL STREQUAL "1")
    set(APP_GCC_OPT_FLAG "-O1")
elseif(APP_OPT_LEVEL STREQUAL "2")
    set(APP_GCC_OPT_FLAG "-O2")
elseif(APP_OPT_LEVEL STREQUAL "3")
    set(APP_GCC_OPT_FLAG "-O3")
elseif(APP_OPT_LEVEL STREQUAL "s")
    set(APP_GCC_OPT_FLAG "-Os")
elseif(APP_OPT_LEVEL STREQUAL "g")
    set(APP_GCC_OPT_FLAG "-Og")
endif()

target_compile_options(${PROJECT_NAME}.elf PRIVATE
    -mcpu=cortex-m0
    -mthumb
    -g3
    -ffunction-sections
    -fdata-sections
    -fno-common
    -fno-strict-aliasing
    -fmessage-length=0
    -fno-unwind-tables
    -fno-asynchronous-unwind-tables
    -std=gnu99
    $<$<OR:$<CONFIG:Release>,$<CONFIG:MinSizeRel>>:${APP_GCC_OPT_FLAG}>
    $<$<COMPILE_LANGUAGE:C>:-Wno-implicit-function-declaration>
    $<$<COMPILE_LANGUAGE:C>:-Wall>
)

target_link_options(${PROJECT_NAME}.elf PRIVATE
    -nostartfiles
    -T${APP_LINKER_SCRIPT}
    -mcpu=cortex-m0
    -mthumb
    -Wl,--gc-sections
    -Wl,--nmagic
    -Wl,-Map=${CMAKE_BINARY_DIR}/${LEGACY_OUTPUT_NAME}.map
    --specs=nano.specs
    --specs=nosys.specs
)

if(APP_ENABLE_LTO)
    target_compile_options(${PROJECT_NAME}.elf PRIVATE
        $<$<OR:$<CONFIG:Release>,$<CONFIG:MinSizeRel>>:-flto>
    )
    target_link_options(${PROJECT_NAME}.elf PRIVATE
        $<$<OR:$<CONFIG:Release>,$<CONFIG:MinSizeRel>>:-flto>
    )
endif()

set_target_properties(${PROJECT_NAME}.elf PROPERTIES
    OUTPUT_NAME "${LEGACY_OUTPUT_NAME}"
    SUFFIX ".elf"
)

if(CMAKE_OBJCOPY)
    add_custom_command(TARGET ${PROJECT_NAME}.elf POST_BUILD
        COMMAND ${CMAKE_OBJCOPY} -O binary
            $<TARGET_FILE:${PROJECT_NAME}.elf>
            ${CMAKE_BINARY_DIR}/${LEGACY_OUTPUT_NAME}.bin
        COMMAND ${CMAKE_OBJCOPY} -O ihex
            $<TARGET_FILE:${PROJECT_NAME}.elf>
            ${CMAKE_BINARY_DIR}/${LEGACY_OUTPUT_NAME}.hex
        VERBATIM
    )
endif()

if(CMAKE_SIZE)
    add_custom_command(TARGET ${PROJECT_NAME}.elf POST_BUILD
        COMMAND ${CMAKE_SIZE} $<TARGET_FILE:${PROJECT_NAME}.elf>
        VERBATIM
    )
endif()
