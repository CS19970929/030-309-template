cmake_minimum_required(VERSION 3.20)

project(host_sim_modbus C)

set(HOST_SIM_COMMON_SOURCES
    ../Code/Source/modbus_rtu_parser.c
    support/app_log_runtime.c
)

set(HOST_SIM_COMMON_INCLUDES
    include
    ../Code/Source
    ../Code/Source/BSP
)

add_executable(modbus_parser_host_test
    tests/modbus_parser_host_test.c
    ${HOST_SIM_COMMON_SOURCES}
)

add_executable(modbus_parser_host_replay
    tools/modbus_parser_host_replay.c
    ${HOST_SIM_COMMON_SOURCES}
)

foreach(target modbus_parser_host_test modbus_parser_host_replay)
    target_include_directories(${target} PRIVATE ${HOST_SIM_COMMON_INCLUDES})
    target_compile_definitions(${target} PRIVATE BMS_HOST_SIM APP_LOG_RUNTIME APP_LOG_LEVEL=3)
    if(MSVC)
        target_compile_options(${target} PRIVATE /W4)
    else()
        target_compile_options(${target} PRIVATE -Wall -Wextra -Wpedantic)
    endif()
endforeach()
