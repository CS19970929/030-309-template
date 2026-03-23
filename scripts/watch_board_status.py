#!/usr/bin/env python3

import argparse
import json
import re
import socket
import subprocess
import sys
import telnetlib
import time
from pathlib import Path


TELNET_PROMPT = b"> "


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Poll runtime monitor state from the board through ST-Link/OpenOCD."
    )
    parser.add_argument("--openocd", required=True, help="OpenOCD executable path")
    parser.add_argument("--scripts-dir", required=True, help="OpenOCD scripts directory")
    parser.add_argument("--elf", required=True, help="Firmware ELF with runtime monitor symbols")
    parser.add_argument("--interface", default="interface/stlink.cfg", help="OpenOCD interface config")
    parser.add_argument("--target", default="target/stm32f0x.cfg", help="OpenOCD target config")
    parser.add_argument("--nm", default="arm-none-eabi-nm", help="nm tool path")
    parser.add_argument("--poll-ms", type=int, default=500, help="Polling interval in milliseconds")
    parser.add_argument("--duration-s", type=int, default=0, help="Duration in seconds, 0 means until Ctrl+C")
    parser.add_argument("--output", default="artifacts/board-status-watch.jsonl", help="Output JSONL path")
    parser.add_argument("--halt-sample", action="store_true", help="Halt before sampling and resume afterwards")
    return parser.parse_args()


def resolve_symbols(nm_tool: str, elf_path: Path) -> dict:
    result = subprocess.run(
        [nm_tool, str(elf_path)],
        check=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        universal_newlines=True,
    )
    symbols = {}
    for line in result.stdout.splitlines():
        parts = line.split()
        if len(parts) >= 3:
            address_text, _sym_type, name = parts[0], parts[1], parts[2]
            if name in {
                "g_app_runtime_monitor_json",
                "g_app_runtime_monitor_json_length",
                "g_app_runtime_monitor_update_count",
            }:
                symbols[name] = int(address_text, 16)

    required = {
        "g_app_runtime_monitor_json",
        "g_app_runtime_monitor_json_length",
        "g_app_runtime_monitor_update_count",
    }
    missing = required - symbols.keys()
    if missing:
        raise RuntimeError("ELF missing runtime monitor symbols: %s" % ", ".join(sorted(missing)))
    return symbols


class OpenOcdTelnet:
    def __init__(self, host: str, port: int):
        self.telnet = telnetlib.Telnet(host, port, timeout=5)
        self._read_until_prompt()

    def _read_until_prompt(self, timeout: float = 5.0) -> str:
        data = self.telnet.read_until(TELNET_PROMPT, timeout=timeout)
        return data.decode(errors="ignore")

    def command(self, text: str) -> str:
        self.telnet.write(text.encode() + b"\n")
        output = self._read_until_prompt()
        if output:
            return output
        time.sleep(0.1)
        return self._read_until_prompt(timeout=8.0)

    def close(self) -> None:
        try:
            self.telnet.close()
        except Exception:
            pass


def wait_for_port(port: int, timeout_s: float = 10.0) -> bool:
    deadline = time.time() + timeout_s
    while time.time() < deadline:
        try:
            sock = socket.create_connection(("127.0.0.1", port), timeout=1.0)
            sock.close()
            return True
        except OSError:
            time.sleep(0.2)
    raise TimeoutError("Timed out waiting for OpenOCD telnet port %d" % port)


def parse_mdw_word(output: str) -> int:
    match = re.search(r":\s*([0-9A-Fa-f]{1,8})", output)
    if not match:
        raise RuntimeError("Unable to parse mdw output: %r" % output)
    return int(match.group(1), 16)


def read_mdw_with_retry(telnet: OpenOcdTelnet, address: int, attempts: int = 3) -> int:
    last_output = ""
    for _ in range(attempts):
        output = telnet.command("mdw 0x%08X 1" % address)
        last_output = output
        try:
            return parse_mdw_word(output)
        except RuntimeError:
            try:
                telnet.command("")
            except Exception:
                pass
            time.sleep(0.1)
    raise RuntimeError("Unable to parse mdw output: %r" % last_output)


def parse_mdb_bytes(output: str) -> bytes:
    values = []
    for line in output.splitlines():
        if ":" not in line:
            continue
        _, rhs = line.split(":", 1)
        for item in rhs.strip().split():
            if re.fullmatch(r"[0-9A-Fa-f]{2}", item):
                values.append(int(item, 16))
    return bytes(values)


def ensure_parent_dir(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)


def read_process_output(proc: subprocess.Popen) -> str:
    if proc.stdout is None:
        return ""
    try:
        return proc.stdout.read()
    except Exception:
        return ""


def main() -> int:
    args = parse_args()
    elf_path = Path(args.elf).resolve()
    if not elf_path.exists():
        print("ELF not found: %s" % elf_path, file=sys.stderr)
        return 1

    symbols = resolve_symbols(args.nm, elf_path)
    output_path = Path(args.output).resolve()
    ensure_parent_dir(output_path)

    openocd_cmd = [
        str(Path(args.openocd).resolve()),
        "-s",
        str(Path(args.scripts_dir).resolve()),
        "-f",
        args.interface,
        "-f",
        args.target,
        "-c",
        "gdb_port disabled",
        "-c",
        "tcl_port disabled",
    ]

    proc = None
    started_local_openocd = False
    telnet = None
    start_time = time.time()
    last_update = None

    try:
        try:
            telnet = OpenOcdTelnet("127.0.0.1", 4444)
        except Exception:
            proc = subprocess.Popen(
                openocd_cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                universal_newlines=True,
            )
            try:
                wait_for_port(4444, timeout_s=20.0)
            except Exception:
                output = read_process_output(proc)
                raise RuntimeError("OpenOCD did not expose telnet 4444 in time.\n%s" % output)
            if proc.poll() is not None:
                output = read_process_output(proc)
                raise RuntimeError("OpenOCD exited before telnet was ready.\n%s" % output)
            telnet = OpenOcdTelnet("127.0.0.1", 4444)
            started_local_openocd = True

        telnet.command("init")
        telnet.command("reset run")
        time.sleep(0.3)
        telnet.command("")

        print("board watch start")
        print("json_addr=0x%08X" % symbols["g_app_runtime_monitor_json"])
        print("len_addr=0x%08X" % symbols["g_app_runtime_monitor_json_length"])
        print("cnt_addr=0x%08X" % symbols["g_app_runtime_monitor_update_count"])

        with output_path.open("w", encoding="utf-8") as output_file:
            while True:
                if args.duration_s > 0 and (time.time() - start_time) >= args.duration_s:
                    break

                if args.halt_sample:
                    telnet.command("halt")

                update_count = read_mdw_with_retry(telnet, symbols["g_app_runtime_monitor_update_count"])

                if last_update is None:
                    last_update = update_count - 1 if update_count > 0 else 0xFFFFFFFF

                if update_count != last_update:
                    json_length = read_mdw_with_retry(telnet, symbols["g_app_runtime_monitor_json_length"])
                    if 0 < json_length < 256:
                        json_text = telnet.command(
                            "mdb 0x%08X %d" % (symbols["g_app_runtime_monitor_json"], json_length)
                        )
                        payload = parse_mdb_bytes(json_text)[:json_length]
                        try:
                            line = payload.decode("utf-8", errors="ignore").rstrip("\x00\r\n")
                            parsed = json.loads(line)
                            formatted = json.dumps(parsed, ensure_ascii=False)
                            print(formatted)
                            output_file.write(formatted + "\n")
                            output_file.flush()
                        except json.JSONDecodeError:
                            print("[raw] %r" % payload)
                    last_update = update_count

                if args.halt_sample:
                    telnet.command("resume")

                time.sleep(args.poll_ms / 1000.0)
    except KeyboardInterrupt:
        print("board watch stopped by user")
    finally:
        if telnet is not None:
            try:
                if started_local_openocd:
                    telnet.command("shutdown")
            except Exception:
                pass
            telnet.close()
        if proc is not None and proc.poll() is None:
            proc.terminate()
            try:
                proc.wait(timeout=5)
            except subprocess.TimeoutExpired:
                proc.kill()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
