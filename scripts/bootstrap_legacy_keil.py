from __future__ import annotations

import argparse
import json
import re
import shutil
import textwrap
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path


HELPER_FILES = [
    "toolchains/arm-none-eabi-gcc.cmake",
    "scripts/check_toolchain.py",
    "scripts/analyze_map.py",
    "scripts/flash_stlink.py",
    "scripts/flash_stm32cube.py",
]


@dataclass
class LegacyProject:
    root: Path
    uvprojx: Path
    output_name: str
    device: str
    cpu: str
    defines: list[str]
    includes: list[str]
    sources: list[str]


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="为 Keil 老项目一键补齐 GCC/CMake/Task/VS Code 工具链骨架。"
    )
    parser.add_argument("--legacy-root", required=True, help="老项目根目录")
    parser.add_argument("--uvprojx", help="可选，显式指定 uvprojx 文件")
    parser.add_argument("--app-origin", default="0x08001C00", help="应用起始地址")
    parser.add_argument(
        "--reserved-flash-start",
        default="0x0800F000",
        help="保留区起始地址，用于推导应用可用 Flash 长度",
    )
    parser.add_argument("--stack-top", default="0x20001B80", help="应用栈顶地址")
    parser.add_argument("--heap-size", default="0x0", help="默认堆大小")
    parser.add_argument("--stack-size", default="0xC00", help="默认栈保留大小")
    parser.add_argument(
        "--report-json",
        default="artifacts/legacy-bootstrap-report.json",
        help="迁移报告 JSON 输出路径，相对 legacy-root",
    )
    parser.add_argument("--dry-run", action="store_true", help="只输出计划，不写入文件")
    parser.add_argument(
        "--force", action="store_true", help="允许覆盖已存在的自动化文件"
    )
    return parser


def split_macro_text(raw: str) -> list[str]:
    items = re.split(r"[,\s;]+", raw)
    return sorted({item.strip() for item in items if item.strip()})


def split_include_text(raw: str) -> list[str]:
    paths = []
    for item in raw.split(";"):
        item = item.strip()
        if item:
            paths.append(item.replace("\\", "/"))
    return sorted(dict.fromkeys(paths))


def normalize_source_path(raw: str) -> str:
    return raw.replace("\\", "/").lstrip("./")


def detect_cpu(device: str, cpu_text: str) -> str:
    upper = f"{device} {cpu_text}".upper()
    if "CORTEX-M0+" in upper:
        return "cortex-m0plus"
    if "CORTEX-M0" in upper or "STM32F0" in upper:
        return "cortex-m0"
    if "CORTEX-M3" in upper or "STM32F1" in upper:
        return "cortex-m3"
    if "CORTEX-M4" in upper or "STM32F3" in upper or "STM32F4" in upper:
        return "cortex-m4"
    return "cortex-m0"


def choose_uvprojx(project_root: Path, explicit: str | None) -> Path:
    if explicit:
        uvprojx = normalize_path(explicit)
        if not uvprojx.exists():
            raise SystemExit(f"uvprojx 不存在: {uvprojx}")
        return uvprojx

    matches = sorted(project_root.glob("*.uvprojx"))
    if not matches:
        raise SystemExit(f"在 {project_root} 下未找到 uvprojx")
    if len(matches) > 1:
        raise SystemExit(
            "找到多个 uvprojx，请显式传入 --uvprojx:\n"
            + "\n".join(str(item) for item in matches)
        )
    return matches[0]


def parse_uvprojx(project_root: Path, uvprojx: Path) -> LegacyProject:
    root = ET.parse(uvprojx).getroot()
    output_name = root.findtext(".//TargetCommonOption/OutputName") or uvprojx.stem
    device = root.findtext(".//TargetCommonOption/Device") or "STM32F030C8"
    cpu_text = root.findtext(".//TargetCommonOption/Cpu") or ""
    include_text = root.findtext(".//Cads/VariousControls/IncludePath") or ""
    define_text = root.findtext(".//Cads/VariousControls/Define") or ""

    sources: list[str] = []
    for file_node in root.findall(".//File"):
        rel = file_node.findtext("FilePath")
        if not rel:
            continue
        normalized = normalize_source_path(rel)
        if not normalized.lower().endswith((".c", ".s", ".S")):
            continue
        if normalized.lower().endswith("startup_stm32f0xx.s"):
            continue
        sources.append(normalized)

    sources = sorted(dict.fromkeys(sources))
    includes = split_include_text(include_text)
    defines = split_macro_text(define_text)

    for required in ("USE_STDPERIPH_DRIVER", "STM32F0XX"):
        if required not in defines and "STM32F0" in device.upper():
            defines.append(required)

    return LegacyProject(
        root=project_root,
        uvprojx=uvprojx,
        output_name=output_name,
        device=device,
        cpu=detect_cpu(device, cpu_text),
        defines=sorted(dict.fromkeys(defines)),
        includes=includes,
        sources=sources,
    )


def ensure_parent(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)


def write_text(path: Path, content: str, dry_run: bool, force: bool) -> None:
    ensure_parent(path)
    if path.exists() and not force:
        if path.read_text(encoding="utf-8", errors="ignore") == content:
            return
        raise SystemExit(f"目标文件已存在且内容不同，请使用 --force: {path}")
    if not dry_run:
        path.write_text(content, encoding="utf-8")


def copy_helper_file(
    source_root: Path, legacy_root: Path, relative: str, dry_run: bool, force: bool
) -> None:
    source = source_root / relative
    destination = legacy_root / relative
    ensure_parent(destination)
    if destination.exists() and not force:
        if source.read_bytes() == destination.read_bytes():
            return
        raise SystemExit(f"目标文件已存在且内容不同，请使用 --force: {destination}")
    if not dry_run:
        shutil.copy2(source, destination)


def quote_cmake_string(value: str) -> str:
    return value.replace("\\", "/")


def render_manifest(project: LegacyProject) -> str:
    source_lines = "\n".join(
        f'    "${{PROJECT_ROOT}}/{quote_cmake_string(item)}"' for item in project.sources
    )
    include_lines = "\n".join(
        f'    "${{PROJECT_ROOT}}/{quote_cmake_string(item)}"' for item in project.includes
    )
    define_lines = "\n".join(f"    {item}" for item in project.defines)

    return textwrap.dedent(
        f"""\
        set(LEGACY_OUTPUT_NAME "{project.output_name}")
        set(LEGACY_DEVICE "{project.device}")
        set(LEGACY_CPU "{project.cpu}")

        set(LEGACY_SOURCES
        {source_lines}
        )

        set(LEGACY_INCLUDE_DIRS
        {include_lines}
        )

        set(LEGACY_DEFINES
        {define_lines}
        )
        """
    )


def render_root_cmakelists() -> str:
    return textwrap.dedent(
        """\
        cmake_minimum_required(VERSION 3.25)

        option(BUILD_FIRMWARE "Build legacy firmware target" ON)

        if(BUILD_FIRMWARE)
            add_subdirectory(firmware)
        endif()
        """
    )


def render_firmware_cmakelists(cpu: str) -> str:
    return textwrap.dedent(
        f"""\
        cmake_minimum_required(VERSION 3.25)

        set(CMAKE_TRY_COMPILE_TARGET_TYPE STATIC_LIBRARY)

        project(legacy_keil_app C ASM)

        set(PROJECT_ROOT "${{CMAKE_CURRENT_LIST_DIR}}/..")
        set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

        option(APP_ENABLE_LTO "Enable link time optimization for firmware build" ON)
        set(APP_OPT_LEVEL "s" CACHE STRING "Firmware optimization level for GCC builds (0,1,2,3,s,g)")
        set(APP_HEAP_SIZE "0x0" CACHE STRING "Reserved heap bytes in linker script")
        set(APP_STACK_SIZE "0xC00" CACHE STRING "Reserved stack bytes in linker script")

        include("${{CMAKE_CURRENT_LIST_DIR}}/generated/legacy_keil_manifest.cmake")

        set(APP_LINKER_TEMPLATE "${{CMAKE_CURRENT_LIST_DIR}}/linker/legacy_stm32_app.ld")
        set(APP_LINKER_SCRIPT "${{CMAKE_BINARY_DIR}}/generated/legacy_stm32_app.ld")
        file(MAKE_DIRECTORY "${{CMAKE_BINARY_DIR}}/generated")
        configure_file("${{APP_LINKER_TEMPLATE}}" "${{APP_LINKER_SCRIPT}}" @ONLY)

        set(FIRMWARE_SOURCES
            "${{CMAKE_CURRENT_LIST_DIR}}/startup/startup_stm32f0xx_gcc.S"
            ${{LEGACY_SOURCES}}
        )

        add_executable(${{PROJECT_NAME}}.elf ${{FIRMWARE_SOURCES}})

        target_include_directories(${{PROJECT_NAME}}.elf PRIVATE
            ${{LEGACY_INCLUDE_DIRS}}
        )

        target_compile_definitions(${{PROJECT_NAME}}.elf PRIVATE
            ${{LEGACY_DEFINES}}
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

        target_compile_options(${{PROJECT_NAME}}.elf PRIVATE
            -mcpu={cpu}
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
            $<$<OR:$<CONFIG:Release>,$<CONFIG:MinSizeRel>>:${{APP_GCC_OPT_FLAG}}>
            $<$<COMPILE_LANGUAGE:C>:-Wno-implicit-function-declaration>
            $<$<COMPILE_LANGUAGE:C>:-Wall>
        )

        target_link_options(${{PROJECT_NAME}}.elf PRIVATE
            -nostartfiles
            -T${{APP_LINKER_SCRIPT}}
            -mcpu={cpu}
            -mthumb
            -Wl,--gc-sections
            -Wl,--nmagic
            -Wl,-Map=${{CMAKE_BINARY_DIR}}/${{LEGACY_OUTPUT_NAME}}.map
            --specs=nano.specs
            --specs=nosys.specs
        )

        if(APP_ENABLE_LTO)
            target_compile_options(${{PROJECT_NAME}}.elf PRIVATE
                $<$<OR:$<CONFIG:Release>,$<CONFIG:MinSizeRel>>:-flto>
            )
            target_link_options(${{PROJECT_NAME}}.elf PRIVATE
                $<$<OR:$<CONFIG:Release>,$<CONFIG:MinSizeRel>>:-flto>
            )
        endif()

        set_target_properties(${{PROJECT_NAME}}.elf PROPERTIES
            OUTPUT_NAME "${{LEGACY_OUTPUT_NAME}}"
            SUFFIX ".elf"
        )

        if(CMAKE_OBJCOPY)
            add_custom_command(TARGET ${{PROJECT_NAME}}.elf POST_BUILD
                COMMAND ${{CMAKE_OBJCOPY}} -O binary
                    $<TARGET_FILE:${{PROJECT_NAME}}.elf>
                    ${{CMAKE_BINARY_DIR}}/${{LEGACY_OUTPUT_NAME}}.bin
                COMMAND ${{CMAKE_OBJCOPY}} -O ihex
                    $<TARGET_FILE:${{PROJECT_NAME}}.elf>
                    ${{CMAKE_BINARY_DIR}}/${{LEGACY_OUTPUT_NAME}}.hex
                VERBATIM
            )
        endif()

        if(CMAKE_SIZE)
            add_custom_command(TARGET ${{PROJECT_NAME}}.elf POST_BUILD
                COMMAND ${{CMAKE_SIZE}} $<TARGET_FILE:${{PROJECT_NAME}}.elf>
                VERBATIM
            )
        endif()
        """
    )


def render_linker(app_origin: int, reserved_flash_start: int, stack_top: int) -> str:
    flash_length = reserved_flash_start - app_origin
    ram_vector_base = 0x20000000
    ram_vector_length = 0xC0
    ram_origin = 0x200000C0
    ram_total_end = 0x20002000
    ram_length = stack_top - ram_origin
    ram_top_rsvd = ram_total_end - stack_top
    if flash_length <= 0 or ram_length <= 0 or ram_top_rsvd < 0:
        raise SystemExit("地址参数非法，无法生成链接脚本")

    return textwrap.dedent(
        f"""\
        ENTRY(Reset_Handler)

        _estack = 0x{stack_top:08X};

        MEMORY
        {{
          RAM_VECTOR (xrw) : ORIGIN = 0x{ram_vector_base:08X}, LENGTH = 0x{ram_vector_length:08X}
          RAM        (xrw) : ORIGIN = 0x{ram_origin:08X}, LENGTH = 0x{ram_length:08X}
          RAM_TOP_RSVD (xrw) : ORIGIN = 0x{stack_top:08X}, LENGTH = 0x{ram_top_rsvd:08X}
          FLASH      (rx)  : ORIGIN = 0x{app_origin:08X}, LENGTH = 0x{flash_length:08X}
        }}

        SECTIONS
        {{
          .isr_vector :
          {{
            . = ALIGN(4);
            KEEP(*(.isr_vector))
            . = ALIGN(4);
          }} > FLASH

          .text :
          {{
            . = ALIGN(4);
            KEEP(*(.text.Reset_Handler))
            KEEP(*(.text.Default_Handler))
            *(.text*)
            *(.rodata*)
            . = ALIGN(4);
            _etext = .;
          }} > FLASH

          _sidata = _etext;

          .data : AT(_sidata)
          {{
            . = ALIGN(4);
            _sdata = .;
            *(.data*)
            . = ALIGN(4);
            _edata = .;
          }} > RAM

          .bss :
          {{
            . = ALIGN(4);
            _sbss = .;
            *(.bss*)
            *(COMMON)
            . = ALIGN(4);
            _ebss = .;
          }} > RAM

          .RAMVectorTable (NOLOAD) :
          {{
            . = ALIGN(4);
            KEEP(*(.RAMVectorTable))
            . = ALIGN(4);
          }} > RAM_VECTOR

          ._user_heap_stack (NOLOAD):
          {{
            . = ALIGN(8);
            . = . + @APP_HEAP_SIZE@;
            . = . + @APP_STACK_SIZE@;
            . = ALIGN(8);
          }} > RAM

          /DISCARD/ :
          {{
            *(.note*)
            *(.comment*)
            *(.eh_frame*)
            *(.ARM.extab*)
            *(.ARM.exidx*)
            *(.preinit_array*)
            *(.init_array*)
            *(.fini_array*)
          }}
        }}
        """
    )


def render_startup() -> str:
    return textwrap.dedent(
        """\
        .syntax unified
        .cpu cortex-m0
        .thumb

        .global g_pfnVectors
        .global Default_Handler
        .global Reset_Handler

        .equ APP_VECTOR_WORDS, 48
        .equ RAM_VECTOR_BASE, 0x20000000
        .equ RCC_APB2ENR, 0x40021018
        .equ RCC_APB2ENR_SYSCFGEN, 0x00000001
        .equ SYSCFG_CFGR1, 0x40010000
        .equ SYSCFG_MEM_MODE_SRAM, 0x00000003

        .word _sidata
        .word _sdata
        .word _edata
        .word _sbss
        .word _ebss

        .section .text.Reset_Handler
        .weak Reset_Handler
        .type Reset_Handler, %function
        Reset_Handler:
            ldr r0, =g_pfnVectors
            ldr r1, =RAM_VECTOR_BASE
            movs r2, #APP_VECTOR_WORDS
        0:
            ldr r3, [r0]
            str r3, [r1]
            adds r0, r0, #4
            adds r1, r1, #4
            subs r2, r2, #1
            bne 0b

            ldr r0, =RCC_APB2ENR
            ldr r1, [r0]
            ldr r2, =RCC_APB2ENR_SYSCFGEN
            orrs r1, r2
            str r1, [r0]

            ldr r0, =SYSCFG_CFGR1
            ldr r1, [r0]
            movs r2, #3
            bics r1, r2
            movs r2, #SYSCFG_MEM_MODE_SRAM
            orrs r1, r2
            str r1, [r0]

            ldr r0, =_sidata
            ldr r1, =_sdata
            ldr r2, =_edata
        1:
            cmp r1, r2
            bcc 2f
            b 3f
        2:
            ldr r3, [r0]
            str r3, [r1]
            adds r0, r0, #4
            adds r1, r1, #4
            b 1b
        3:
            ldr r0, =_sbss
            ldr r1, =_ebss
            movs r2, #0
        4:
            cmp r0, r1
            bcc 5f
            b 6f
        5:
            str r2, [r0]
            adds r0, r0, #4
            b 4b
        6:
            bl SystemInit
            bl main
        7:
            b 7b

        .section .text.Default_Handler,"ax",%progbits
        .type Default_Handler, %function
        Default_Handler:
            b .

        .macro def_irq_handler handler
            .weak \\handler
            .thumb_set \\handler, Default_Handler
        .endm

        def_irq_handler NMI_Handler
        def_irq_handler HardFault_Handler
        def_irq_handler SVC_Handler
        def_irq_handler PendSV_Handler
        def_irq_handler SysTick_Handler
        def_irq_handler WWDG_IRQHandler
        def_irq_handler PVD_IRQHandler
        def_irq_handler RTC_IRQHandler
        def_irq_handler FLASH_IRQHandler
        def_irq_handler RCC_IRQHandler
        def_irq_handler EXTI0_1_IRQHandler
        def_irq_handler EXTI2_3_IRQHandler
        def_irq_handler EXTI4_15_IRQHandler
        def_irq_handler TS_IRQHandler
        def_irq_handler DMA1_Channel1_IRQHandler
        def_irq_handler DMA1_Channel2_3_IRQHandler
        def_irq_handler DMA1_Channel4_5_IRQHandler
        def_irq_handler ADC1_COMP_IRQHandler
        def_irq_handler TIM1_BRK_UP_TRG_COM_IRQHandler
        def_irq_handler TIM1_CC_IRQHandler
        def_irq_handler TIM2_IRQHandler
        def_irq_handler TIM3_IRQHandler
        def_irq_handler TIM6_DAC_IRQHandler
        def_irq_handler TIM14_IRQHandler
        def_irq_handler TIM15_IRQHandler
        def_irq_handler TIM16_IRQHandler
        def_irq_handler TIM17_IRQHandler
        def_irq_handler I2C1_IRQHandler
        def_irq_handler I2C2_IRQHandler
        def_irq_handler SPI1_IRQHandler
        def_irq_handler SPI2_IRQHandler
        def_irq_handler USART1_IRQHandler
        def_irq_handler USART2_IRQHandler
        def_irq_handler CEC_IRQHandler

        .section .isr_vector,"a",%progbits
        .type g_pfnVectors, %object
        g_pfnVectors:
            .word _estack
            .word Reset_Handler
            .word NMI_Handler
            .word HardFault_Handler
            .word 0
            .word 0
            .word 0
            .word 0
            .word 0
            .word 0
            .word 0
            .word SVC_Handler
            .word 0
            .word 0
            .word PendSV_Handler
            .word SysTick_Handler
            .word WWDG_IRQHandler
            .word PVD_IRQHandler
            .word RTC_IRQHandler
            .word FLASH_IRQHandler
            .word RCC_IRQHandler
            .word EXTI0_1_IRQHandler
            .word EXTI2_3_IRQHandler
            .word EXTI4_15_IRQHandler
            .word TS_IRQHandler
            .word DMA1_Channel1_IRQHandler
            .word DMA1_Channel2_3_IRQHandler
            .word DMA1_Channel4_5_IRQHandler
            .word ADC1_COMP_IRQHandler
            .word TIM1_BRK_UP_TRG_COM_IRQHandler
            .word TIM1_CC_IRQHandler
            .word TIM2_IRQHandler
            .word TIM3_IRQHandler
            .word TIM6_DAC_IRQHandler
            .word 0
            .word TIM14_IRQHandler
            .word TIM15_IRQHandler
            .word TIM16_IRQHandler
            .word TIM17_IRQHandler
            .word I2C1_IRQHandler
            .word I2C2_IRQHandler
            .word SPI1_IRQHandler
            .word SPI2_IRQHandler
            .word USART1_IRQHandler
            .word USART2_IRQHandler
            .word 0
            .word CEC_IRQHandler
            .word 0
        .size g_pfnVectors, .-g_pfnVectors
        """
    )


def render_taskfile(output_name: str, device: str) -> str:
    return textwrap.dedent(
        f"""\
        version: "3"

        vars:
          PYTHON: '{{{{default (ternary "py -3.12" "python3" (eq OS "windows")) .PYTHON}}}}'
          CMAKE: '{{{{default (ternary "C:/Program Files/CMake/bin/cmake.exe" "cmake" (eq OS "windows")) .CMAKE}}}}'
          OPENOCD: '{{{{default (ternary "C:/Users/Administrator/AppData/Local/Microsoft/WinGet/Packages/xpack-dev-tools.openocd-xpack_Microsoft.Winget.Source_8wekyb3d8bbwe/xpack-openocd-0.12.0-7/bin/openocd.exe" "openocd" (eq OS "windows")) .OPENOCD}}}}'
          OPENOCD_SCRIPTS: '{{{{default (ternary "C:/Users/Administrator/AppData/Local/Microsoft/WinGet/Packages/xpack-dev-tools.openocd-xpack_Microsoft.Winget.Source_8wekyb3d8bbwe/xpack-openocd-0.12.0-7/openocd/scripts" "" (eq OS "windows")) .OPENOCD_SCRIPTS}}}}'
          STM32_PROGRAMMER: '{{{{default "C:/Program Files/STMicroelectronics/STM32Cube/STM32CubeProgrammer/bin/STM32_Programmer_CLI.exe" .STM32_PROGRAMMER}}}}'
          MAP_FILE: '{{{{default "artifacts/cmake/firmware-release/{output_name}.map" .MAP_FILE}}}}'
          FIRMWARE_BIN: '{{{{default "artifacts/cmake/firmware-release/{output_name}.bin" .FIRMWARE_BIN}}}}'
          DEVICE: '{{{{default "{device}" .DEVICE}}}}'

        tasks:
          default:
            cmds:
              - task --list

          doctor:
            cmds:
              - "{{{{.PYTHON}}}} scripts/check_toolchain.py"

          build:
            cmds:
              - '"{{{{.CMAKE}}}}" --fresh --preset firmware-release'
              - '"{{{{.CMAKE}}}}" --build --preset build-firmware-release'

          build-debug:
            cmds:
              - '"{{{{.CMAKE}}}}" --fresh --preset firmware-debug'
              - '"{{{{.CMAKE}}}}" --build --preset build-firmware-debug'

          map:
            cmds:
              - '{{{{.PYTHON}}}} scripts/analyze_map.py --map "{{{{.MAP_FILE}}}}" --top 20 --json artifacts/map-summary.json'

          flash-stlink:
            cmds:
              - '{{{{.PYTHON}}}} scripts/flash_stlink.py --openocd "{{{{.OPENOCD}}}}" --scripts-dir "{{{{.OPENOCD_SCRIPTS}}}}" --artifact "{{{{.FIRMWARE_BIN}}}}" --address 0x08001C00'

          flash-stm32cube:
            cmds:
              - '{{{{.PYTHON}}}} scripts/flash_stm32cube.py --programmer "{{{{.STM32_PROGRAMMER}}}}" --artifact "{{{{.FIRMWARE_BIN}}}}" --address 0x08001C00'

          test:
            cmds:
              - "{{{{.PYTHON}}}} scripts/check_toolchain.py"
              - "{{{{.PYTHON}}}} scripts/analyze_map.py --help"
              - "{{{{.PYTHON}}}} scripts/flash_stlink.py --help"
              - "{{{{.PYTHON}}}} scripts/flash_stm32cube.py --help"
        """
    )


def render_presets() -> str:
    return textwrap.dedent(
        """\
        {
          "version": 6,
          "cmakeMinimumRequired": {
            "major": 3,
            "minor": 25,
            "patch": 0
          },
          "configurePresets": [
            {
              "name": "firmware-release",
              "generator": "Ninja",
              "binaryDir": "${sourceDir}/artifacts/cmake/firmware-release",
              "cacheVariables": {
                "BUILD_FIRMWARE": "ON",
                "CMAKE_BUILD_TYPE": "Release",
                "CMAKE_TOOLCHAIN_FILE": "${sourceDir}/toolchains/arm-none-eabi-gcc.cmake",
                "APP_ENABLE_LTO": "ON",
                "APP_OPT_LEVEL": "s"
              }
            },
            {
              "name": "firmware-debug",
              "generator": "Ninja",
              "binaryDir": "${sourceDir}/artifacts/cmake/firmware-debug",
              "cacheVariables": {
                "BUILD_FIRMWARE": "ON",
                "CMAKE_BUILD_TYPE": "Debug",
                "CMAKE_TOOLCHAIN_FILE": "${sourceDir}/toolchains/arm-none-eabi-gcc.cmake",
                "APP_ENABLE_LTO": "ON",
                "APP_OPT_LEVEL": "s"
              }
            }
          ],
          "buildPresets": [
            {
              "name": "build-firmware-release",
              "configurePreset": "firmware-release"
            },
            {
              "name": "build-firmware-debug",
              "configurePreset": "firmware-debug"
            }
          ]
        }
        """
    )


def render_vscode_tasks() -> str:
    return textwrap.dedent(
        """\
        {
          "version": "2.0.0",
          "tasks": [
            {
              "label": "mcu: doctor",
              "type": "shell",
              "command": "task",
              "args": ["doctor"],
              "options": { "cwd": "${workspaceFolder}" },
              "windows": {
                "command": "C:\\Users\\Administrator\\AppData\\Local\\Microsoft\\WinGet\\Links\\task.exe"
              },
              "problemMatcher": []
            },
            {
              "label": "mcu: build gcc release",
              "type": "shell",
              "command": "task",
              "args": ["build"],
              "options": { "cwd": "${workspaceFolder}" },
              "windows": {
                "command": "C:\\Users\\Administrator\\AppData\\Local\\Microsoft\\WinGet\\Links\\task.exe"
              },
              "group": { "kind": "build", "isDefault": true },
              "problemMatcher": []
            },
            {
              "label": "mcu: build gcc debug",
              "type": "shell",
              "command": "task",
              "args": ["build-debug"],
              "options": { "cwd": "${workspaceFolder}" },
              "windows": {
                "command": "C:\\Users\\Administrator\\AppData\\Local\\Microsoft\\WinGet\\Links\\task.exe"
              },
              "problemMatcher": []
            },
            {
              "label": "mcu: flash stlink",
              "type": "shell",
              "command": "task",
              "args": ["flash-stlink"],
              "options": { "cwd": "${workspaceFolder}" },
              "windows": {
                "command": "C:\\Users\\Administrator\\AppData\\Local\\Microsoft\\WinGet\\Links\\task.exe"
              },
              "problemMatcher": []
            }
          ]
        }
        """
    )


def render_vscode_launch(output_name: str, device: str) -> str:
    return textwrap.dedent(
        f"""\
        {{
          "version": "0.2.0",
          "configurations": [
            {{
              "name": "STM32 Debug (ST-Link/OpenOCD)",
              "type": "cortex-debug",
              "request": "launch",
              "cwd": "${{workspaceFolder}}",
              "servertype": "openocd",
              "device": "{device}",
              "interface": "swd",
              "executable": "${{workspaceFolder}}/artifacts/cmake/firmware-debug/{output_name}.elf",
              "configFiles": [
                "interface/stlink.cfg",
                "target/stm32f0x.cfg"
              ],
              "preLaunchTask": "mcu: build gcc debug"
            }}
          ]
        }}
        """
    )


def render_extensions() -> str:
    return textwrap.dedent(
        """\
        {
          "recommendations": [
            "marus25.cortex-debug",
            "ms-vscode.cpptools",
            "ms-python.python"
          ]
        }
        """
    )


def render_bootstrap_doc(
    project: LegacyProject, app_origin: str, reserved_flash_start: str, stack_top: str
) -> str:
    return textwrap.dedent(
        f"""\
        # 旧项目工具链迁移说明

        本目录已经由 `scripts/bootstrap_legacy_keil.py` 自动补齐 GCC/CMake/Task/VS Code 工具链骨架。

        ## 当前识别结果

        - `uvprojx`: `{project.uvprojx.name}`
        - `OutputName`: `{project.output_name}`
        - `Device`: `{project.device}`
        - `CPU`: `{project.cpu}`
        - `Source 数量`: `{len(project.sources)}`
        - `Include 目录数量`: `{len(project.includes)}`
        - `宏定义数量`: `{len(project.defines)}`

        ## 当前地址参数

        - `APP_ORIGIN`: `{app_origin}`
        - `RESERVED_FLASH_START`: `{reserved_flash_start}`
        - `STACK_TOP`: `{stack_top}`

        ## 统一命令

        - `task doctor`
        - `task build`
        - `task build-debug`
        - `task map`
        - `task flash-stlink`
        - `task flash-stm32cube`

        ## 首次迁移后必须人工确认的项

        - `firmware/linker/legacy_stm32_app.ld` 中的 Flash 起始地址、保留区起始地址、栈顶地址
        - `firmware/generated/legacy_keil_manifest.cmake` 中的源文件、宏定义、头文件目录
        - `Code/Drivers/system_stm32f0xx.c` 的时钟配置是否与硬件一致
        - `main.c / Flash.c` 中是否存在 IAP、向量表 remap、BootFlag 等启动约束

        ## VS Code 调试

        - 安装推荐扩展后，使用 `STM32 Debug (ST-Link/OpenOCD)`
        - 默认会先执行 `mcu: build gcc debug`
        - 可执行文件路径：`artifacts/cmake/firmware-debug/{project.output_name}.elf`

        ## 建议排障顺序

        1. 先执行 `task doctor`
        2. 再执行 `task build`
        3. 若固件可编译但不能运行，优先比较 `Keil bin` 与 `GCC bin` 的首向量、入口地址、Flash 尺寸和 ELF 段布局
        4. 若项目带 bootloader，重点检查应用向量表 remap 是否早于第一次中断触发
        """
    )


def build_report(project: LegacyProject, legacy_root: Path) -> dict:
    return {
        "legacy_root": str(legacy_root),
        "uvprojx": str(project.uvprojx),
        "output_name": project.output_name,
        "device": project.device,
        "cpu": project.cpu,
        "source_count": len(project.sources),
        "include_count": len(project.includes),
        "define_count": len(project.defines),
        "generated_files": [
            "CMakeLists.txt",
            "CMakePresets.json",
            "Taskfile.yml",
            "firmware/CMakeLists.txt",
            "firmware/generated/legacy_keil_manifest.cmake",
            "firmware/linker/legacy_stm32_app.ld",
            "firmware/startup/startup_stm32f0xx_gcc.S",
            ".vscode/tasks.json",
            ".vscode/launch.json",
            ".vscode/extensions.json",
            "docs/LEGACY_TOOLCHAIN_BOOTSTRAP.md",
        ]
        + HELPER_FILES,
    }


def main() -> int:
    args = build_parser().parse_args()
    source_root = Path(__file__).resolve().parent.parent
    legacy_root = normalize_path(args.legacy_root)
    uvprojx = choose_uvprojx(legacy_root, args.uvprojx)
    project = parse_uvprojx(legacy_root, uvprojx)

    app_origin = int(args.app_origin, 16)
    reserved_flash_start = int(args.reserved_flash_start, 16)
    stack_top = int(args.stack_top, 16)

    writes = {
        legacy_root / "CMakeLists.txt": render_root_cmakelists(),
        legacy_root / "CMakePresets.json": render_presets(),
        legacy_root / "Taskfile.yml": render_taskfile(project.output_name, project.device),
        legacy_root / "firmware/CMakeLists.txt": render_firmware_cmakelists(project.cpu),
        legacy_root / "firmware/generated/legacy_keil_manifest.cmake": render_manifest(project),
        legacy_root / "firmware/linker/legacy_stm32_app.ld": render_linker(
            app_origin, reserved_flash_start, stack_top
        ),
        legacy_root / "firmware/startup/startup_stm32f0xx_gcc.S": render_startup(),
        legacy_root / ".vscode/tasks.json": render_vscode_tasks(),
        legacy_root / ".vscode/launch.json": render_vscode_launch(project.output_name, project.device),
        legacy_root / ".vscode/extensions.json": render_extensions(),
        legacy_root / "docs/LEGACY_TOOLCHAIN_BOOTSTRAP.md": render_bootstrap_doc(
            project,
            args.app_origin,
            args.reserved_flash_start,
            args.stack_top,
        ),
    }

    for path, content in writes.items():
        write_text(path, content, args.dry_run, args.force)

    for helper in HELPER_FILES:
        copy_helper_file(source_root, legacy_root, helper, args.dry_run, args.force)

    report = build_report(project, legacy_root)
    report_path = legacy_root / args.report_json
    if not args.dry_run:
        ensure_parent(report_path)
        report_path.write_text(
            json.dumps(report, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )

    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
