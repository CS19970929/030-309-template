import argparse
from pathlib import Path


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="生成调试或烧录操作计划。")
    parser.add_argument("mode", choices=["flash", "debug"], help="模式")
    parser.add_argument("--output", required=True, help="输出 Markdown 文件")
    return parser


def render_plan(mode: str) -> str:
    title = "烧录计划" if mode == "flash" else "调试采集计划"
    steps = [
        "确认 J-Link 或 OpenOCD 可执行文件已加入 PATH。",
        "确认目标板供电、SWD 连接、设备型号和接口速度。",
        "后续在阶段 B/C 中把这里替换为真实的无交互命令。",
    ]
    lines = [f"# {title}", "", f"- 模式：`{mode}`"]
    lines.extend(f"- {step}" for step in steps)
    return "\n".join(lines) + "\n"


def main() -> int:
    args = build_parser().parse_args()
    output_path = normalize_path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(render_plan(args.mode), encoding="utf-8")
    print(f"已生成计划文件：{output_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
