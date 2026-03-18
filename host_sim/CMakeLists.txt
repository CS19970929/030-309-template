cmake_minimum_required(VERSION 3.20)

project(host_sim_modbus C)

add_executable(modbus_parser_host_test
    tests/modbus_parser_host_test.c
    ../Code/Source/modbus_rtu_parser.c
)

target_include_directories(modbus_parser_host_test PRIVATE
    include
    ../Code/Source
)

if(MSVC)
    target_compile_options(modbus_parser_host_test PRIVATE /W4)
else()
    target_compile_options(modbus_parser_host_test PRIVATE -Wall -Wextra -Wpedantic)
endif()
