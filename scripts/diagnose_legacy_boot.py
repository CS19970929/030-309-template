from __future__ import annotations

import argparse
import json
import re
import shutil
import subprocess
from pathlib import Path


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="静态诊断 Keil/GCC 固件在 bootloader + app 场景下的启动差异。"
    )
    parser.add_argument("--keil-bin", required=True, help="Keil 生成的 bin")
    parser.add_argument("--gcc-bin", required=True, help="GCC 生成的 bin")
    parser.add_argument("--gcc-elf", help="可选，GCC 生成的 elf，用于检查 PT_LOAD")
    parser.add_argument("--app-origin", default="0x08001C00", help="应用起始地址")
    parser.add_argument(
        "--reserved-flash-start",
        default="0x0800F000",
        help="保留区起始地址，用于判断镜像是否越界",
    )
    parser.add_argument(
        "--report-json",
        default="artifacts/legacy-boot-diagnosis.json",
        help="JSON 报告输出路径",
    )
    parser.add_argument(
        "--report-md",
        default="artifacts/legacy-boot-diagnosis.md",
        help="Markdown 报告输出路径",
    )
    return parser


def read_u32_le(blob: bytes, offset: int) -> int:
    return int.from_bytes(blob[offset : offset + 4], byteorder="little", signed=False)


def collect_vector_summary(blob: bytes, image_name: str) -> dict:
    return {
        "image": image_name,
        "msp": f"0x{read_u32_le(blob, 0):08X}",
        "reset_handler": f"0x{read_u32_le(blob, 4):08X}",
        "nmi_handler": f"0x{read_u32_le(blob, 8):08X}",
        "hardfault_handler": f"0x{read_u32_le(blob, 12):08X}",
        "size_bytes": len(blob),
    }


def analyze_image(path: Path, app_origin: int, reserved_flash_start: int) -> dict:
    blob = path.read_bytes()
    image_end = app_origin + len(blob)
    return {
        **collect_vector_summary(blob, path.name),
        "path": str(path),
        "flash_end": f"0x{image_end:08X}",
        "cross_reserved_flash": image_end > reserved_flash_start,
    }


def find_readelf() -> str | None:
    name = "arm-none-eabi-readelf"
    path = shutil.which(name)
    if path:
        return path
    candidates = [
        r"C:\Program Files (x86)\Arm GNU Toolchain arm-none-eabi\14.2 rel1\bin\arm-none-eabi-readelf.exe",
        r"C:\Program Files\Arm GNU Toolchain arm-none-eabi\14.2 rel1\bin\arm-none-eabi-readelf.exe",
    ]
    for item in candidates:
        if Path(item).exists():
            return item
    return None


def parse_program_headers(elf_path: Path) -> list[dict]:
    readelf = find_readelf()
    if not readelf:
        return []
    output = subprocess.check_output(
        [readelf, "-l", str(elf_path)],
        text=True,
        encoding="utf-8",
        errors="ignore",
    )
    loads: list[dict] = []
    for line in output.splitlines():
        line = line.strip()
        if not line.startswith("LOAD"):
            continue
        parts = re.split(r"\s+", line)
        if len(parts) < 7:
            continue
        loads.append(
            {
                "offset": parts[1],
                "virt_addr": parts[2],
                "phys_addr": parts[3],
                "file_size": parts[4],
                "mem_size": parts[5],
                "flags": " ".join(parts[6:]),
            }
        )
    return loads


def render_markdown(report: dict) -> str:
    keil = report["keil"]
    gcc = report["gcc"]
    lines = [
        "# 启动差异诊断",
        "",
        "## 镜像头部对比",
        "",
        f"- Keil MSP: `{keil['msp']}`",
        f"- GCC MSP: `{gcc['msp']}`",
        f"- Keil Reset_Handler: `{keil['reset_handler']}`",
        f"- GCC Reset_Handler: `{gcc['reset_handler']}`",
        f"- Keil HardFault_Handler: `{keil['hardfault_handler']}`",
        f"- GCC HardFault_Handler: `{gcc['hardfault_handler']}`",
        "",
        "## 尺寸与保留区",
        "",
        f"- Keil size: `{keil['size_bytes']}` bytes",
        f"- GCC size: `{gcc['size_bytes']}` bytes",
        f"- Keil flash_end: `{keil['flash_end']}`",
        f"- GCC flash_end: `{gcc['flash_end']}`",
        f"- Keil 是否踩保留区: `{keil['cross_reserved_flash']}`",
        f"- GCC 是否踩保留区: `{gcc['cross_reserved_flash']}`",
        "",
    ]
    if report["gcc_loads"]:
        lines.extend(
            [
                "## GCC ELF 段",
                "",
            ]
        )
        for load in report["gcc_loads"]:
            lines.append(
                f"- `{load['virt_addr']}` <- offset `{load['offset']}`, size `{load['file_size']}`, flags `{load['flags']}`"
            )
        lines.append("")
    lines.extend(
        [
            "## 建议检查项",
            "",
            "- 确认 GCC 首向量栈顶与 bootloader 允许的 SRAM 区间一致。",
            "- 确认 GCC Reset_Handler 地址带 Thumb 位，并且在应用区内部。",
            "- 确认 GCC bin 不踩 `reserved_flash_start` 之后的参数区。",
            "- 确认 GCC ELF 的首个 `PT_LOAD` 从应用起始地址开始，而不是更早地址。",
            "- 若项目带 bootloader 且无 `VTOR`，确认应用在第一次中断前已经完成 SRAM 向量表 remap。",
            "",
        ]
    )
    return "\n".join(lines) + "\n"


def main() -> int:
    args = build_parser().parse_args()
    app_origin = int(args.app_origin, 16)
    reserved_flash_start = int(args.reserved_flash_start, 16)

    keil_bin = normalize_path(args.keil_bin)
    gcc_bin = normalize_path(args.gcc_bin)
    gcc_elf = normalize_path(args.gcc_elf) if args.gcc_elf else None
    report_json = normalize_path(args.report_json)
    report_md = normalize_path(args.report_md)

    report = {
        "app_origin": args.app_origin,
        "reserved_flash_start": args.reserved_flash_start,
        "keil": analyze_image(keil_bin, app_origin, reserved_flash_start),
        "gcc": analyze_image(gcc_bin, app_origin, reserved_flash_start),
        "gcc_loads": parse_program_headers(gcc_elf) if gcc_elf else [],
    }

    report_json.parent.mkdir(parents=True, exist_ok=True)
    report_md.parent.mkdir(parents=True, exist_ok=True)
    report_json.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    report_md.write_text(render_markdown(report), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
