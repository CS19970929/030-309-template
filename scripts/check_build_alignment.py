from __future__ import annotations

import argparse
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="检查 Keil 与 GCC/CMake 构建输入是否对齐。"
    )
    parser.add_argument(
        "--uvprojx",
        default="CommomSH367309_16series_030C8T6_C.uvprojx",
        help="Keil uvprojx 路径",
    )
    parser.add_argument(
        "--cmake",
        default="firmware/CMakeLists.txt",
        help="firmware/CMakeLists.txt 路径",
    )
    parser.add_argument(
        "--mode",
        choices=("board-safe", "optimized"),
        default="board-safe",
        help="按哪种 GCC 构建模式进行比较",
    )
    parser.add_argument(
        "--json",
        dest="json_path",
        default="artifacts/build-alignment.json",
        help="输出 JSON 报告路径",
    )
    return parser.parse_args()


def parse_uvprojx(path: Path) -> tuple[set[str], set[str]]:
    root = ET.parse(path).getroot()
    files: set[str] = set()
    defines: set[str] = set()

    for file_node in root.findall(".//File"):
        rel = file_node.findtext("FilePath")
        if rel and rel.lower().endswith((".c", ".s", ".S")):
            files.add(rel.replace("\\", "/").lstrip("./"))

    define_text = root.findtext(".//Cads/VariousControls/Define") or ""
    for item in define_text.split():
        item = item.strip()
        if item:
            defines.add(item)

    return files, defines


def parse_cmake(path: Path, mode: str) -> tuple[set[str], set[str]]:
    text = path.read_text(encoding="utf-8")
    files: set[str] = set()
    defines = {"USE_STDPERIPH_DRIVER", "STM32F0XX"}

    project_root_sources = re.findall(
        r'"\$\{PROJECT_ROOT\}/([^"]+\.(?:c|S))"', text
    )
    for item in project_root_sources:
        files.add(item)

    monitor_sources = {
        "Code/Source/BSP/app_state_snapshot.c",
        "Code/Source/BSP/app_runtime_monitor.c",
    }

    if mode == "optimized":
        files.update(monitor_sources)
        defines.add("APP_RUNTIME_MONITOR_ENABLE")
    else:
        files.difference_update(monitor_sources)

    return files, defines


def main() -> int:
    args = parse_args()
    uvprojx = Path(args.uvprojx)
    cmake = Path(args.cmake)

    uv_files, uv_defines = parse_uvprojx(uvprojx)
    cmake_files, cmake_defines = parse_cmake(cmake, args.mode)

    ignored_uv_only = {"Code/Drivers/startup_stm32f0xx.s"}
    ignored_cmake_only = set()

    uv_only = sorted((uv_files - cmake_files) - ignored_uv_only)
    cmake_only = sorted((cmake_files - uv_files) - ignored_cmake_only)
    uv_define_only = sorted(uv_defines - cmake_defines)
    cmake_define_only = sorted(cmake_defines - uv_defines)

    report = {
        "mode": args.mode,
        "uvprojx": str(uvprojx),
        "cmake": str(cmake),
        "uv_only_sources": uv_only,
        "cmake_only_sources": cmake_only,
        "uv_only_defines": uv_define_only,
        "cmake_only_defines": cmake_define_only,
        "aligned": not any(
            [uv_only, cmake_only, uv_define_only, cmake_define_only]
        ),
    }

    out = Path(args.json_path)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    print(f"对齐模式: {args.mode}")
    print(f"Keil 独有源文件: {len(uv_only)}")
    for item in uv_only:
        print(f"  UV only: {item}")
    print(f"GCC 独有源文件: {len(cmake_only)}")
    for item in cmake_only:
        print(f"  CMake only: {item}")
    print(f"Keil 独有宏: {len(uv_define_only)}")
    for item in uv_define_only:
        print(f"  UV define only: {item}")
    print(f"GCC 独有宏: {len(cmake_define_only)}")
    for item in cmake_define_only:
        print(f"  CMake define only: {item}")
    print(f"JSON 报告: {out}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
