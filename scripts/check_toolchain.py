import json
import platform
import shutil
import sys


TOOLS = [
    "python",
    "uv",
    "cmake",
    "ninja",
    "arm-none-eabi-gcc",
]


def main() -> int:
    report = {
        "platform": platform.platform(),
        "python": sys.version.split()[0],
        "tools": {},
    }
    missing = []

    for tool in TOOLS:
        path = shutil.which(tool)
        report["tools"][tool] = path
        if path is None:
            missing.append(tool)

    print(json.dumps(report, indent=2, ensure_ascii=False))

    if missing:
        print("\n缺少工具：" + ", ".join(missing))
        return 1

    print("\n工具链检查通过。")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
