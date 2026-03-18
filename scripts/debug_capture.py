import argparse
import shutil
import subprocess
from pathlib import Path


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Generate or run J-Link/OpenOCD flash and debug commands.")
    parser.add_argument("mode", choices=["flash", "debug"], help="Action mode")
    parser.add_argument("--output", required=True, help="Output markdown file")
    parser.add_argument("--probe", choices=["auto", "jlink", "openocd"], default="auto", help="Probe type")
    parser.add_argument("--artifact", default="artifacts/cmake/firmware-debug/CommomSH367309_16series_030C8T6_C.hex", help="Firmware image path")
    parser.add_argument("--chip", default="STM32F030C8", help="Target MCU")
    parser.add_argument("--interface", default="SWD", help="Debug interface")
    parser.add_argument("--speed", default="4000", help="Interface speed in kHz")
    parser.add_argument("--run", action="store_true", help="Execute the command if the tool exists")
    return parser


def detect_probe(preferred: str) -> str:
    if preferred != "auto":
        return preferred
    if shutil.which("JLinkExe"):
        return "jlink"
    if shutil.which("openocd"):
        return "openocd"
    return "jlink"


def build_jlink_command(args, command_file: Path) -> list:
    image_path = normalize_path(args.artifact)
    commands = [
        f"device {args.chip}",
        f"si {args.interface}",
        f"speed {args.speed}",
        "r",
    ]
    if args.mode == "flash":
        commands.extend(
            [
                f"loadfile {image_path}",
                "r",
                "g",
                "qc",
            ]
        )
    else:
        commands.extend(["halt", "g", "qc"])

    command_file.write_text("\n".join(commands) + "\n", encoding="utf-8")
    return ["JLinkExe", "-CommandFile", str(command_file)]


def build_openocd_command(args) -> list:
    image_path = normalize_path(args.artifact)
    script = (
        "interface jlink; "
        "transport select swd; "
        "source [find target/stm32f0x.cfg]; "
        f"adapter speed {args.speed}; "
        "init; reset init; "
    )
    if args.mode == "flash":
        script += f"program {image_path} verify reset exit"
    else:
        script += "halt; reset halt; exit"
    return ["openocd", "-c", script]


def render_plan(mode: str, probe: str, command: list, artifact: Path, run: bool) -> str:
    title = "Flash Plan" if mode == "flash" else "Debug Plan"
    lines = [f"# {title}", "", f"- Mode: `{mode}`", f"- Tool: `{probe}`", f"- Firmware: `{artifact}`"]
    lines.append(f"- Run directly: `{'yes' if run else 'no'}`")
    lines.append(f"- Command: `{' '.join(command)}`")
    lines.append("")
    lines.append("## Checklist")
    lines.append("- Confirm the board is powered.")
    lines.append("- Confirm the SWD wiring is correct.")
    lines.append("- Confirm MCU, speed and firmware path match the target.")
    return "\n".join(lines) + "\n"


def main() -> int:
    args = build_parser().parse_args()
    output_path = normalize_path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    probe = detect_probe(args.probe)
    command_file = output_path.with_suffix(".jlink")

    if probe == "openocd":
        command = build_openocd_command(args)
        executable = "openocd"
    else:
        command = build_jlink_command(args, command_file)
        executable = "JLinkExe"

    artifact_path = normalize_path(args.artifact)
    output_path.write_text(render_plan(args.mode, probe, command, artifact_path, args.run), encoding="utf-8")
    print(f"Plan file generated: {output_path}")

    if args.run:
        if shutil.which(executable) is None:
            print(f"Executable not found: {executable}")
            return 1
        if args.mode == "flash" and not artifact_path.exists():
            print(f"Firmware image not found: {artifact_path}")
            return 1
        return subprocess.call(command)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
