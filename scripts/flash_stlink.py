#!/usr/bin/env python3

import argparse
import ctypes
import os
import subprocess
import sys
from pathlib import Path


def to_windows_short_path(path: Path) -> str:
    path_str = str(path)
    if os.name != "nt":
        return path_str.replace("\\", "/")

    kernel32 = ctypes.windll.kernel32
    buffer_size = 4096
    buffer = ctypes.create_unicode_buffer(buffer_size)
    result = kernel32.GetShortPathNameW(path_str, buffer, buffer_size)
    if result == 0 or result > buffer_size:
        return path_str.replace("\\", "/")
    return buffer.value.replace("\\", "/")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Use ST-Link + OpenOCD to flash a firmware image.")
    parser.add_argument("--openocd", required=True, help="OpenOCD executable path")
    parser.add_argument("--scripts-dir", required=True, help="OpenOCD scripts directory")
    parser.add_argument("--artifact", required=True, help="Firmware binary path")
    parser.add_argument("--address", default="0x08001C00", help="Flash start address for the binary image")
    parser.add_argument("--interface", default="interface/stlink.cfg", help="OpenOCD interface config")
    parser.add_argument("--target", default="target/stm32f0x.cfg", help="OpenOCD target config")
    return parser.parse_args()


def main() -> int:
    args = parse_args()

    artifact = Path(args.artifact).resolve()
    if not artifact.exists():
        print(f"artifact not found: {artifact}", file=sys.stderr)
        return 1

    openocd = Path(args.openocd).resolve()
    scripts_dir = Path(args.scripts_dir).resolve()
    artifact_for_openocd = to_windows_short_path(artifact)

    command = [
        str(openocd),
        "-s",
        str(scripts_dir),
        "-f",
        args.interface,
        "-f",
        args.target,
        "-c",
        f"program {artifact_for_openocd} {args.address} verify reset exit",
    ]

    print("flash command:")
    print(" ".join(command))
    completed = subprocess.run(command, check=False)
    return completed.returncode


if __name__ == "__main__":
    raise SystemExit(main())
