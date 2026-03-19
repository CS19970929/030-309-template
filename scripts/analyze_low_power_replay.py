import argparse
import json
from pathlib import Path


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="汇总低功耗回放 JSONL，输出可供 Codex 读取的摘要。")
    parser.add_argument("--input", dest="inputs", action="append", required=True, help="JSONL 输入文件，可重复传入")
    parser.add_argument("--json", dest="json_output", required=True, help="JSON 摘要输出路径")
    parser.add_argument("--markdown", dest="markdown_output", help="Markdown 摘要输出路径")
    return parser


def summarize_one(input_path: Path) -> dict:
    steps = 0
    to_sleep_steps = 0
    wake_steps = 0
    deep_sleep_steps = 0
    normal_l2_steps = 0
    normal_l3_steps = 0
    first_to_sleep_step = None
    first_wake_step = None
    first_deep_sleep_step = None

    for line in input_path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        item = json.loads(line)
        steps += 1
        flags = int(item.get("power_state_flags", 0))
        mode = int(item.get("power_mode", 0))

        if flags & (1 << 2):
            to_sleep_steps += 1
            if first_to_sleep_step is None:
                first_to_sleep_step = int(item.get("step", steps))
        if flags & (1 << 3):
            wake_steps += 1
            if first_wake_step is None:
                first_wake_step = int(item.get("step", steps))
        if flags & (1 << 4):
            deep_sleep_steps += 1
            if first_deep_sleep_step is None:
                first_deep_sleep_step = int(item.get("step", steps))
        if mode == 1:
            normal_l2_steps += 1
        elif mode == 2:
            normal_l3_steps += 1

    return {
        "input_file": str(input_path),
        "steps": steps,
        "to_sleep_steps": to_sleep_steps,
        "wake_steps": wake_steps,
        "deep_sleep_steps": deep_sleep_steps,
        "normal_l2_steps": normal_l2_steps,
        "normal_l3_steps": normal_l3_steps,
        "first_to_sleep_step": first_to_sleep_step,
        "first_wake_step": first_wake_step,
        "first_deep_sleep_step": first_deep_sleep_step,
        "status": "ok" if steps > 0 else "empty",
    }


def render_markdown(report: dict) -> str:
    lines = ["# 低功耗回放摘要", ""]
    lines.append(f"- 总场景数：{len(report['scenarios'])}")
    lines.append(f"- 总步数：{report['totals']['steps']}")
    lines.append(f"- 进入休眠总步数：{report['totals']['to_sleep_steps']}")
    lines.append(f"- 唤醒事件总步数：{report['totals']['wake_steps']}")
    lines.append(f"- 深度休眠总步数：{report['totals']['deep_sleep_steps']}")
    lines.append("")
    lines.append("## 分场景")
    lines.append("")
    for item in report["scenarios"]:
        lines.append(f"### {Path(item['input_file']).stem}")
        lines.append(f"- 步数：{item['steps']}")
        lines.append(f"- `to_sleep_steps`：{item['to_sleep_steps']}")
        lines.append(f"- `wake_steps`：{item['wake_steps']}")
        lines.append(f"- `deep_sleep_steps`：{item['deep_sleep_steps']}")
        lines.append(f"- `normal_l2_steps`：{item['normal_l2_steps']}")
        lines.append(f"- `normal_l3_steps`：{item['normal_l3_steps']}")
        lines.append(f"- 首次进入休眠步：{item['first_to_sleep_step']}")
        lines.append(f"- 首次唤醒步：{item['first_wake_step']}")
        lines.append(f"- 首次深度休眠步：{item['first_deep_sleep_step']}")
        lines.append("")
    return "\n".join(lines) + "\n"


def main() -> int:
    args = build_parser().parse_args()
    inputs = [normalize_path(raw_path) for raw_path in args.inputs]
    for input_path in inputs:
        if not input_path.exists():
            raise SystemExit(f"低功耗回放文件不存在：{input_path}")

    scenarios = [summarize_one(input_path) for input_path in inputs]
    report = {
        "scenario_count": len(scenarios),
        "scenarios": scenarios,
        "totals": {
            "steps": sum(item["steps"] for item in scenarios),
            "to_sleep_steps": sum(item["to_sleep_steps"] for item in scenarios),
            "wake_steps": sum(item["wake_steps"] for item in scenarios),
            "deep_sleep_steps": sum(item["deep_sleep_steps"] for item in scenarios),
            "normal_l2_steps": sum(item["normal_l2_steps"] for item in scenarios),
            "normal_l3_steps": sum(item["normal_l3_steps"] for item in scenarios),
        },
    }

    json_path = normalize_path(args.json_output)
    json_path.parent.mkdir(parents=True, exist_ok=True)
    json_path.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    if args.markdown_output:
        markdown_path = normalize_path(args.markdown_output)
        markdown_path.parent.mkdir(parents=True, exist_ok=True)
        markdown_path.write_text(render_markdown(report), encoding="utf-8")

    print(json.dumps(report, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
