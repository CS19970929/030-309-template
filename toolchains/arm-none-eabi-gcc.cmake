set(CMAKE_SYSTEM_NAME Generic)
set(CMAKE_SYSTEM_PROCESSOR ARM)

set(TOOLCHAIN_PREFIX arm-none-eabi-)

find_program(ARM_NONE_EABI_GCC
    NAMES ${TOOLCHAIN_PREFIX}gcc ${TOOLCHAIN_PREFIX}gcc.exe
    HINTS
        "C:/Program Files (x86)/Arm GNU Toolchain arm-none-eabi/14.2 rel1/bin"
        "C:/Program Files/Arm GNU Toolchain arm-none-eabi/14.2 rel1/bin"
)

find_program(ARM_NONE_EABI_OBJCOPY
    NAMES ${TOOLCHAIN_PREFIX}objcopy ${TOOLCHAIN_PREFIX}objcopy.exe
    HINTS
        "C:/Program Files (x86)/Arm GNU Toolchain arm-none-eabi/14.2 rel1/bin"
        "C:/Program Files/Arm GNU Toolchain arm-none-eabi/14.2 rel1/bin"
)

find_program(ARM_NONE_EABI_SIZE
    NAMES ${TOOLCHAIN_PREFIX}size ${TOOLCHAIN_PREFIX}size.exe
    HINTS
        "C:/Program Files (x86)/Arm GNU Toolchain arm-none-eabi/14.2 rel1/bin"
        "C:/Program Files/Arm GNU Toolchain arm-none-eabi/14.2 rel1/bin"
)

if(NOT ARM_NONE_EABI_GCC)
    message(FATAL_ERROR "arm-none-eabi-gcc not found. Install the Arm GNU Toolchain or add it to PATH.")
endif()

set(CMAKE_C_COMPILER ${ARM_NONE_EABI_GCC})
set(CMAKE_ASM_COMPILER ${ARM_NONE_EABI_GCC})
set(CMAKE_OBJCOPY ${ARM_NONE_EABI_OBJCOPY} CACHE INTERNAL "")
set(CMAKE_SIZE ${ARM_NONE_EABI_SIZE} CACHE INTERNAL "")
set(CMAKE_EXECUTABLE_SUFFIX ".elf")
set(CMAKE_C_OUTPUT_EXTENSION ".o")
set(CMAKE_ASM_OUTPUT_EXTENSION ".o")
set(CMAKE_C_LINK_EXECUTABLE
    "<CMAKE_C_COMPILER> <FLAGS> <CMAKE_C_LINK_FLAGS> <LINK_FLAGS> <OBJECTS> -o <TARGET> <LINK_LIBRARIES>"
)

set(CMAKE_TRY_COMPILE_TARGET_TYPE STATIC_LIBRARY)

set(CMAKE_C_STANDARD 99)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS ON)
