import argparse
import json
from pathlib import Path


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="汇总 SOC 回放 JSONL，输出可供 Codex 读取的摘要。")
    parser.add_argument("--input", dest="inputs", action="append", required=True, help="JSONL 输入文件，可重复传入")
    parser.add_argument("--json", dest="json_output", required=True, help="JSON 摘要输出路径")
    parser.add_argument("--markdown", dest="markdown_output", help="Markdown 摘要输出路径")
    return parser


def summarize_one(input_path: Path) -> dict:
    steps = 0
    min_soc_est = None
    max_soc_est = None
    final_soc_est = None
    max_abs_error = 0
    ocv_corrected_steps = 0
    clamp_empty_steps = 0
    clamp_full_steps = 0
    first_ocv_corrected_step = None

    for line in input_path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        item = json.loads(line)
        steps += 1
        soc_est = int(item.get("soc_est_pct_x10", 0))
        abs_error = abs(int(item.get("soc_error_pct_x10", 0)))
        flags = int(item.get("soc_state_flags", 0))

        min_soc_est = soc_est if min_soc_est is None else min(min_soc_est, soc_est)
        max_soc_est = soc_est if max_soc_est is None else max(max_soc_est, soc_est)
        final_soc_est = soc_est
        max_abs_error = max(max_abs_error, abs_error)

        if flags & (1 << 2):
            ocv_corrected_steps += 1
            if first_ocv_corrected_step is None:
                first_ocv_corrected_step = int(item.get("cycle", steps))
        if flags & (1 << 3):
            clamp_empty_steps += 1
        if flags & (1 << 4):
            clamp_full_steps += 1

    return {
        "input_file": str(input_path),
        "steps": steps,
        "min_soc_est_pct_x10": min_soc_est,
        "max_soc_est_pct_x10": max_soc_est,
        "final_soc_est_pct_x10": final_soc_est,
        "max_abs_error_pct_x10": max_abs_error,
        "ocv_corrected_steps": ocv_corrected_steps,
        "clamp_empty_steps": clamp_empty_steps,
        "clamp_full_steps": clamp_full_steps,
        "first_ocv_corrected_step": first_ocv_corrected_step,
        "status": "ok" if steps > 0 else "empty",
    }


def render_markdown(report: dict) -> str:
    lines = ["# SOC 回放摘要", ""]
    lines.append(f"- 总场景数：{len(report['scenarios'])}")
    lines.append(f"- 总步数：{report['totals']['steps']}")
    lines.append(f"- OCV 修正总步数：{report['totals']['ocv_corrected_steps']}")
    lines.append(f"- 最大绝对误差：{report['totals']['max_abs_error_pct_x10']}")
    lines.append("")
    lines.append("## 分场景")
    lines.append("")
    for item in report["scenarios"]:
        lines.append(f"### {Path(item['input_file']).stem}")
        lines.append(f"- 步数：{item['steps']}")
        lines.append(f"- `min_soc_est_pct_x10`：{item['min_soc_est_pct_x10']}")
        lines.append(f"- `max_soc_est_pct_x10`：{item['max_soc_est_pct_x10']}")
        lines.append(f"- `final_soc_est_pct_x10`：{item['final_soc_est_pct_x10']}")
        lines.append(f"- `max_abs_error_pct_x10`：{item['max_abs_error_pct_x10']}")
        lines.append(f"- `ocv_corrected_steps`：{item['ocv_corrected_steps']}")
        lines.append(f"- `clamp_empty_steps`：{item['clamp_empty_steps']}")
        lines.append(f"- `clamp_full_steps`：{item['clamp_full_steps']}")
        lines.append(f"- 首次 OCV 修正步：{item['first_ocv_corrected_step']}")
        lines.append("")
    return "\n".join(lines) + "\n"


def main() -> int:
    args = build_parser().parse_args()
    inputs = [normalize_path(raw_path) for raw_path in args.inputs]
    for input_path in inputs:
        if not input_path.exists():
            raise SystemExit(f"SOC 回放文件不存在：{input_path}")

    scenarios = [summarize_one(input_path) for input_path in inputs]
    report = {
        "scenario_count": len(scenarios),
        "scenarios": scenarios,
        "totals": {
            "steps": sum(item["steps"] for item in scenarios),
            "ocv_corrected_steps": sum(item["ocv_corrected_steps"] for item in scenarios),
            "clamp_empty_steps": sum(item["clamp_empty_steps"] for item in scenarios),
            "clamp_full_steps": sum(item["clamp_full_steps"] for item in scenarios),
            "max_abs_error_pct_x10": max((item["max_abs_error_pct_x10"] for item in scenarios), default=0),
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
