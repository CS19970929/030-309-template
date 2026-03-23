import json
import os
import platform
import shutil
import subprocess
import sys


FALLBACKS = {
    "uv": [
        r"C:\Users\Administrator\AppData\Local\Microsoft\WinGet\Links\uv.exe",
    ],
    "cmake": [
        r"C:\Program Files\CMake\bin\cmake.exe",
    ],
    "ninja": [
        r"C:\Users\Administrator\AppData\Local\Microsoft\WinGet\Links\ninja.exe",
    ],
    "arm-none-eabi-gcc": [
        str(
            (
                shutil.os.path.expanduser(
                    "~/.local/toolchains/arm-gnu-toolchain-15.2.rel1-darwin-arm64-arm-none-eabi/bin/arm-none-eabi-gcc"
                )
            )
        ),
        r"C:\Program Files (x86)\Arm GNU Toolchain arm-none-eabi\14.2 rel1\bin\arm-none-eabi-gcc.exe",
    ],
    "JLinkExe": [
        r"C:\Program Files\SEGGER\JLink_V818\JLink.exe",
        r"C:\Program Files\SEGGER\JLink_V796h\JLink.exe",
    ],
    "openocd": [
        r"C:\Users\Administrator\AppData\Local\Microsoft\WinGet\Packages\xpack-dev-tools.openocd-xpack_Microsoft.Winget.Source_8wekyb3d8bbwe\xpack-openocd-0.12.0-7\bin\openocd.exe",
    ],
}


TOOLS = [
    "python",
    "uv",
    "cmake",
    "ninja",
    "arm-none-eabi-gcc",
    "JLinkExe",
    "openocd",
]


def resolve_tool(tool: str):
    for candidate in FALLBACKS.get(tool, []):
        if os.path.exists(candidate):
            return candidate

    candidates = [tool]
    if tool == "python" and os.name != "nt":
        candidates.insert(0, "python3")

    for candidate in candidates:
        path = shutil.which(candidate)
        if path:
            return path
    return None


def check_arm_gcc_headers(gcc_path: str):
    command = [gcc_path, "-xc", "-E", "-"]
    source = b"#include <stdint.h>\n"
    completed = subprocess.run(
        command,
        input=source,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.PIPE,
        check=False,
    )
    stderr = completed.stderr.decode("utf-8", errors="replace").strip()
    if completed.returncode == 0:
        return True, ""
    if stderr:
        return False, stderr.splitlines()[-1]
    return False, "arm-none-eabi-gcc 无法预处理 <stdint.h>"


def main() -> int:
    report = {
        "platform": platform.platform(),
        "python": sys.version.split()[0],
        "python_ok": sys.version_info >= (3, 8),
        "tools": {},
        "checks": {},
    }
    missing = []

    for tool in TOOLS:
        path = resolve_tool(tool)
        report["tools"][tool] = path
        if path is None and tool not in {"JLinkExe", "openocd"}:
            missing.append(tool)

    arm_gcc = report["tools"].get("arm-none-eabi-gcc")
    if arm_gcc:
        headers_ok, detail = check_arm_gcc_headers(arm_gcc)
        report["checks"]["arm-none-eabi-gcc-headers"] = {
            "ok": headers_ok,
            "detail": detail,
        }
        if not headers_ok:
            missing.append("arm-none-eabi-gcc headers")

    if not report["tools"].get("JLinkExe") and not report["tools"].get("openocd"):
        missing.append("JLinkExe/openocd")

    print(json.dumps(report, indent=2, ensure_ascii=False))

    if not report["python_ok"]:
        print("\n默认 python 版本过低，建议在 Windows 上使用: py -3.12")
        return 1

    if missing:
        print("\n缺少工具：" + ", ".join(missing))
        if "arm-none-eabi-gcc headers" in missing:
            print("提示：当前 arm-none-eabi-gcc 可执行文件存在，但标准头文件不可用。")
            print("macOS 上请优先安装完整 Arm GNU Toolchain，而不是 only-compiler 形态。")
        return 1

    print("\n工具链检查通过。")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
