import json
import platform
import shutil
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
    path = shutil.which(tool)
    if path:
        return path
    for candidate in FALLBACKS.get(tool, []):
        if shutil.os.path.exists(candidate):
            return candidate
    return None


def main() -> int:
    report = {
        "platform": platform.platform(),
        "python": sys.version.split()[0],
        "python_ok": sys.version_info >= (3, 8),
        "tools": {},
    }
    missing = []

    for tool in TOOLS:
        path = resolve_tool(tool)
        report["tools"][tool] = path
        if path is None:
            missing.append(tool)

    print(json.dumps(report, indent=2, ensure_ascii=False))

    if not report["python_ok"]:
        print("\n默认 python 版本过低，建议在 Windows 上使用: py -3.12")
        return 1

    if missing:
        print("\n缺少工具：" + ", ".join(missing))
        return 1

    print("\n工具链检查通过。")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
