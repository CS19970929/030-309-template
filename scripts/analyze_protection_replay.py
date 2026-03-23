import argparse
import json
from collections import Counter
from pathlib import Path


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="汇总保护策略回放 JSONL，输出可供 Codex 读取的摘要。")
    parser.add_argument("--input", dest="inputs", action="append", required=True, help="JSONL 输入文件，可重复传入")
    parser.add_argument("--json", dest="json_output", required=True, help="JSON 摘要输出路径")
    parser.add_argument("--markdown", dest="markdown_output", help="Markdown 摘要输出路径")
    return parser


def summarize_one(input_path: Path) -> dict:
    steps = 0
    first_steps = 0
    second_steps = 0
    third_steps = 0
    charge_mos_off_steps = 0
    discharge_mos_off_steps = 0
    fault_first_counter: Counter[str] = Counter()
    fault_second_counter: Counter[str] = Counter()
    fault_third_counter: Counter[str] = Counter()
    first_trigger_step = None
    first_third_step = None
    max_fault_mask = 0

    for line in input_path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        item = json.loads(line)
        steps += 1
        fault_first = int(item.get("fault_first", 0))
        fault_second = int(item.get("fault_second", 0))
        fault_third = int(item.get("fault_third", 0))

        if fault_first != 0:
            first_steps += 1
            fault_first_counter[f"0x{fault_first:08X}"] += 1
            if first_trigger_step is None:
                first_trigger_step = int(item.get("cycle", steps))
        if fault_second != 0:
            second_steps += 1
            fault_second_counter[f"0x{fault_second:08X}"] += 1
        if fault_third != 0:
            third_steps += 1
            fault_third_counter[f"0x{fault_third:08X}"] += 1
            if first_third_step is None:
                first_third_step = int(item.get("cycle", steps))
        if int(item.get("charge_mos_off", 0)) != 0:
            charge_mos_off_steps += 1
        if int(item.get("discharge_mos_off", 0)) != 0:
            discharge_mos_off_steps += 1

        max_fault_mask = max(max_fault_mask, fault_first, fault_second, fault_third)

    return {
        "input_file": str(input_path),
        "steps": steps,
        "fault_first_steps": first_steps,
        "fault_second_steps": second_steps,
        "fault_third_steps": third_steps,
        "charge_mos_off_steps": charge_mos_off_steps,
        "discharge_mos_off_steps": discharge_mos_off_steps,
        "first_trigger_step": first_trigger_step,
        "first_third_step": first_third_step,
        "max_fault_mask": f"0x{max_fault_mask:08X}",
        "fault_first_masks": dict(fault_first_counter),
        "fault_second_masks": dict(fault_second_counter),
        "fault_third_masks": dict(fault_third_counter),
        "status": "ok" if steps > 0 else "empty",
    }


def render_markdown(report: dict) -> str:
    lines = ["# 保护回放摘要", ""]
    lines.append(f"- 总场景数：{len(report['scenarios'])}")
    lines.append(f"- 总步数：{report['totals']['steps']}")
    lines.append(f"- `fault_third` 总步数：{report['totals']['fault_third_steps']}")
    lines.append(f"- 充电 MOS 关闭总步数：{report['totals']['charge_mos_off_steps']}")
    lines.append(f"- 放电 MOS 关闭总步数：{report['totals']['discharge_mos_off_steps']}")
    lines.append("")
    lines.append("## 分场景")
    lines.append("")
    for item in report["scenarios"]:
        lines.append(f"### {Path(item['input_file']).stem}")
        lines.append(f"- 步数：{item['steps']}")
        lines.append(f"- `fault_first_steps`：{item['fault_first_steps']}")
        lines.append(f"- `fault_second_steps`：{item['fault_second_steps']}")
        lines.append(f"- `fault_third_steps`：{item['fault_third_steps']}")
        lines.append(f"- `charge_mos_off_steps`：{item['charge_mos_off_steps']}")
        lines.append(f"- `discharge_mos_off_steps`：{item['discharge_mos_off_steps']}")
        lines.append(f"- 首次触发步：{item['first_trigger_step']}")
        lines.append(f"- 首次三级故障步：{item['first_third_step']}")
        lines.append(f"- 最大故障掩码：{item['max_fault_mask']}")
        lines.append("")
    return "\n".join(lines) + "\n"


def main() -> int:
    args = build_parser().parse_args()
    inputs = [normalize_path(raw_path) for raw_path in args.inputs]
    for input_path in inputs:
        if not input_path.exists():
            raise SystemExit(f"保护回放文件不存在：{input_path}")

    scenarios = [summarize_one(input_path) for input_path in inputs]
    report = {
        "scenario_count": len(scenarios),
        "scenarios": scenarios,
        "totals": {
            "steps": sum(item["steps"] for item in scenarios),
            "fault_first_steps": sum(item["fault_first_steps"] for item in scenarios),
            "fault_second_steps": sum(item["fault_second_steps"] for item in scenarios),
            "fault_third_steps": sum(item["fault_third_steps"] for item in scenarios),
            "charge_mos_off_steps": sum(item["charge_mos_off_steps"] for item in scenarios),
            "discharge_mos_off_steps": sum(item["discharge_mos_off_steps"] for item in scenarios),
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
