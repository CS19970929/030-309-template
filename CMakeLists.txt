cmake_minimum_required(VERSION 3.25)

project(toolchain_workspace NONE)

option(BUILD_FIRMWARE "Build embedded firmware target" OFF)
option(BUILD_HOST_SIM "Build host-side simulation target" OFF)

if(BUILD_FIRMWARE)
    add_subdirectory(firmware)
endif()

if(BUILD_HOST_SIM)
    add_subdirectory(host_sim)
endif()
