#!/usr/bin/env python3

import argparse
import ctypes
import json
import os
import re
import socket
import subprocess
import sys
import telnetlib
import time
from pathlib import Path
from typing import Dict


TELNET_PROMPT = b"> "


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
    parser = argparse.ArgumentParser(description="通过 ST-Link/OpenOCD 轮询板上运行时状态快照。")
    parser.add_argument("--openocd", required=True, help="OpenOCD 可执行文件")
    parser.add_argument("--scripts-dir", required=True, help="OpenOCD scripts 目录")
    parser.add_argument("--elf", required=True, help="带运行时监控的 ELF 文件")
    parser.add_argument("--interface", default="interface/stlink.cfg", help="OpenOCD interface 配置")
    parser.add_argument("--target", default="target/stm32f0x.cfg", help="OpenOCD target 配置")
    parser.add_argument("--nm", default="arm-none-eabi-nm", help="nm 工具路径")
    parser.add_argument("--poll-ms", type=int, default=500, help="轮询周期，毫秒")
    parser.add_argument("--duration-s", type=int, default=0, help="持续时长，0 表示持续运行直到 Ctrl+C")
    parser.add_argument("--output", default="artifacts/board-status-watch.jsonl", help="输出 JSONL 文件")
    parser.add_argument("--halt-sample", action="store_true", help="采样时先 halt 再 resume，侵入性更强但更稳定")
    return parser.parse_args()


def resolve_symbols(nm_tool: str, elf_path: Path) -> Dict[str, int]:
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
        raise RuntimeError(f"ELF 缺少运行时监控符号: {', '.join(sorted(missing))}")
    return symbols


class OpenOcdTelnet:
    def __init__(self, host: str, port: int):
        self.telnet = telnetlib.Telnet(host, port, timeout=5)
        self._read_until_prompt()

    def _read_until_prompt(self) -> str:
        data = self.telnet.read_until(TELNET_PROMPT, timeout=5)
        return data.decode(errors="ignore")

    def command(self, text: str) -> str:
        self.telnet.write(text.encode() + b"\n")
        return self._read_until_prompt()

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
    raise TimeoutError(f"等待 OpenOCD telnet 端口 {port} 超时")


def parse_mdw_word(output: str) -> int:
    match = re.search(r":\s*([0-9A-Fa-f]{1,8})", output)
    if not match:
        raise RuntimeError(f"无法解析 mdw 输出: {output!r}")
    return int(match.group(1), 16)


def read_mdw_with_retry(telnet: OpenOcdTelnet, address: int, attempts: int = 3) -> int:
    last_output = ""
    for _ in range(attempts):
        output = telnet.command("mdw 0x%08X 1" % address)
        last_output = output
        try:
            return parse_mdw_word(output)
        except RuntimeError:
            time.sleep(0.1)
    raise RuntimeError("无法解析 mdw 输出: %r" % last_output)


def parse_mdb_bytes(output: str) -> bytes:
    values: list[int] = []
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


def main() -> int:
    args = parse_args()
    elf_path = Path(args.elf).resolve()
    if not elf_path.exists():
        print(f"ELF 不存在: {elf_path}", file=sys.stderr)
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
            wait_for_port(4444)
            telnet = OpenOcdTelnet("127.0.0.1", 4444)
            started_local_openocd = True
        telnet.command("init")
        telnet.command("reset run")
        time.sleep(0.3)
        telnet.command("")

        print("board watch start")
        print(f"json_addr=0x{symbols['g_app_runtime_monitor_json']:08X}")
        print(f"len_addr=0x{symbols['g_app_runtime_monitor_json_length']:08X}")
        print(f"cnt_addr=0x{symbols['g_app_runtime_monitor_update_count']:08X}")

        with output_path.open("w", encoding="utf-8") as output_file:
            while True:
                if args.duration_s > 0 and (time.time() - start_time) >= args.duration_s:
                    break

                if args.halt_sample:
                    telnet.command("halt")

                update_count = read_mdw_with_retry(telnet, symbols['g_app_runtime_monitor_update_count'])

                if last_update is None:
                    last_update = update_count - 1 if update_count > 0 else 0xFFFFFFFF

                if update_count != last_update:
                    json_length = read_mdw_with_retry(telnet, symbols['g_app_runtime_monitor_json_length'])
                    if 0 < json_length < 256:
                        json_text = telnet.command(
                            f"mdb 0x{symbols['g_app_runtime_monitor_json']:08X} {json_length}"
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
                            print(f"[raw] {payload!r}")
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
