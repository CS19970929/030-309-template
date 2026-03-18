#!/usr/bin/env python3

import argparse
import socket
import subprocess
import sys
import telnetlib
import time
from pathlib import Path


TELNET_PROMPT = b"> "


def parse_args():
    parser = argparse.ArgumentParser(description="Use STM32CubeProgrammer CLI to flash a firmware image.")
    parser.add_argument("--programmer", required=True, help="STM32_Programmer_CLI.exe path")
    parser.add_argument("--artifact", required=True, help="Firmware image path")
    parser.add_argument("--address", default="0x08001C00", help="Flash start address")
    return parser.parse_args()


def is_port_open(host: str, port: int) -> bool:
    try:
        sock = socket.create_connection((host, port), timeout=1.0)
        sock.close()
        return True
    except OSError:
        return False


def shutdown_openocd_if_present() -> None:
    if not is_port_open("127.0.0.1", 4444):
        return
    print("detected active OpenOCD on 127.0.0.1:4444, sending shutdown")
    try:
        telnet = telnetlib.Telnet("127.0.0.1", 4444, timeout=3)
        telnet.read_until(TELNET_PROMPT, timeout=3)
        telnet.write(b"shutdown\n")
        time.sleep(0.5)
        telnet.close()
    except Exception as exc:
        print("warning: failed to shutdown existing OpenOCD: %s" % exc, file=sys.stderr)


def main():
    args = parse_args()
    artifact = Path(args.artifact).resolve()
    if not artifact.exists():
        print("artifact not found: %s" % artifact, file=sys.stderr)
        return 1

    shutdown_openocd_if_present()

    command = [
        str(Path(args.programmer).resolve()),
        "-c",
        "port=SWD mode=UR reset=HWrst",
        "-w",
        str(artifact),
        args.address,
        "-v",
        "-rst",
    ]
    print("flash command:")
    print(" ".join(command))
    return subprocess.call(command)


if __name__ == "__main__":
    raise SystemExit(main())
