import argparse
import json
from pathlib import Path


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="汇总 build/map/protection/soc/low-power 摘要，输出统一总览。")
    parser.add_argument("--build-summary", required=True, help="build-summary.json 路径")
    parser.add_argument("--map-summary", required=True, help="map-summary.json 路径")
    parser.add_argument("--protection-summary", required=True, help="protection-suite-summary.json 路径")
    parser.add_argument("--soc-summary", required=True, help="soc-suite-summary.json 路径")
    parser.add_argument("--low-power-summary", required=True, help="low-power-suite-summary.json 路径")
    parser.add_argument("--json", dest="json_output", required=True, help="总览 JSON 输出路径")
    parser.add_argument("--markdown", dest="markdown_output", required=True, help="总览 Markdown 输出路径")
    return parser


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def classify_host_suite(name: str, data: dict) -> tuple[str, list[str]]:
    totals = data.get("totals", {})
    notes: list[str] = []
    status = "ok"

    if name == "protection":
        if int(totals.get("fault_third_steps", 0)) == 0:
            status = "attention"
            notes.append("保护回归未覆盖三级故障，场景覆盖可能不足。")
    elif name == "soc":
        if int(totals.get("max_abs_error_pct_x10", 0)) >= 300:
            status = "attention"
            notes.append("SOC 最大绝对误差偏高，建议优先查看长跑或循环统计场景。")
        if int(totals.get("cycle_increment_steps", 0)) == 0:
            status = "attention"
            notes.append("SOC 循环统计未触发，寿命相关回归覆盖不足。")
    elif name == "low_power":
        if int(totals.get("to_sleep_steps", 0)) == 0:
            status = "attention"
            notes.append("低功耗回归未出现进入休眠行为。")
        if int(totals.get("deep_sleep_steps", 0)) == 0:
            notes.append("当前低功耗场景未进入深度休眠。")

    return status, notes


def merge_status(values: list[str]) -> str:
    if any(value == "failed" for value in values):
        return "failed"
    if any(value == "attention" for value in values):
        return "attention"
    return "ok"


def render_markdown(report: dict) -> str:
    lines = [
        "# Codex 接管总览",
        "",
        f"- 总状态：`{report['status']}`",
        f"- 构建状态：`{report['build']['status']}`",
        f"- Map 状态：`{report['map']['status']}`",
        f"- 保护回归状态：`{report['protection']['status']}`",
        f"- SOC 回归状态：`{report['soc']['status']}`",
        f"- 低功耗回归状态：`{report['low_power']['status']}`",
        "",
        "## 关键指标",
        "",
        f"- build warning 数：`{report['build']['warning_count']}`",
        f"- build error 数：`{report['build']['error_count']}`",
        f"- RAM 占用：`{report['map']['ram_used_percent']}`%",
        f"- FLASH 占用：`{report['map']['flash_used_percent']}`%",
        f"- 保护三级故障步数：`{report['protection']['fault_third_steps']}`",
        f"- SOC 最大绝对误差：`{report['soc']['max_abs_error_pct_x10']}`",
        f"- SOC 循环计数步数：`{report['soc']['cycle_increment_steps']}`",
        f"- 低功耗进入休眠步数：`{report['low_power']['to_sleep_steps']}`",
        f"- 低功耗深度休眠步数：`{report['low_power']['deep_sleep_steps']}`",
        "",
        "## 建议动作",
        "",
    ]

    for item in report["recommendations"]:
        lines.append(f"- {item}")

    lines.extend(
        [
            "",
            "## 产物入口",
            "",
            f"- [build-summary.json]({report['artifacts']['build_summary']})",
            f"- [map-summary.json]({report['artifacts']['map_summary']})",
            f"- [protection-suite-summary.json]({report['artifacts']['protection_summary']})",
            f"- [soc-suite-summary.json]({report['artifacts']['soc_summary']})",
            f"- [low-power-suite-summary.json]({report['artifacts']['low_power_summary']})",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> int:
    args = build_parser().parse_args()

    build_path = normalize_path(args.build_summary)
    map_path = normalize_path(args.map_summary)
    protection_path = normalize_path(args.protection_summary)
    soc_path = normalize_path(args.soc_summary)
    low_power_path = normalize_path(args.low_power_summary)

    for path in [build_path, map_path, protection_path, soc_path, low_power_path]:
        if not path.exists():
            raise SystemExit(f"摘要文件不存在：{path}")

    build_data = load_json(build_path)
    map_data = load_json(map_path)
    protection_data = load_json(protection_path)
    soc_data = load_json(soc_path)
    low_power_data = load_json(low_power_path)

    build_status = build_data.get("status", "unknown")
    map_status = map_data.get("status", "unknown")
    protection_status, protection_notes = classify_host_suite("protection", protection_data)
    soc_status, soc_notes = classify_host_suite("soc", soc_data)
    low_power_status, low_power_notes = classify_host_suite("low_power", low_power_data)

    recommendations: list[str] = []
    recommendations.extend(build_data.get("recommendations", []))
    recommendations.extend(map_data.get("warnings", []))
    recommendations.extend(protection_notes)
    recommendations.extend(soc_notes)
    recommendations.extend(low_power_notes)

    unique_recommendations: list[str] = []
    for item in recommendations:
        if item and item not in unique_recommendations:
            unique_recommendations.append(item)

    report = {
        "status": merge_status([build_status, map_status, protection_status, soc_status, low_power_status]),
        "build": {
            "status": build_status,
            "warning_count": int(build_data.get("warning_count", 0)),
            "error_count": int(build_data.get("error_count", 0)),
        },
        "map": {
            "status": map_status,
            "ram_used_percent": map_data.get("ram", {}).get("used_percent", 0),
            "flash_used_percent": map_data.get("flash", {}).get("used_percent", 0),
            "ram_risk_level": map_data.get("ram", {}).get("risk", {}).get("level", "unknown"),
            "flash_risk_level": map_data.get("flash", {}).get("risk", {}).get("level", "unknown"),
        },
        "protection": {
            "status": protection_status,
            "scenario_count": int(protection_data.get("scenario_count", 0)),
            "fault_third_steps": int(protection_data.get("totals", {}).get("fault_third_steps", 0)),
            "chg_mos_off_steps": int(protection_data.get("totals", {}).get("chg_mos_off_steps", 0)),
            "dsg_mos_off_steps": int(protection_data.get("totals", {}).get("dsg_mos_off_steps", 0)),
        },
        "soc": {
            "status": soc_status,
            "scenario_count": int(soc_data.get("scenario_count", 0)),
            "max_abs_error_pct_x10": int(soc_data.get("totals", {}).get("max_abs_error_pct_x10", 0)),
            "cycle_increment_steps": int(soc_data.get("totals", {}).get("cycle_increment_steps", 0)),
            "restore_steps": int(soc_data.get("totals", {}).get("restore_steps", 0)),
        },
        "low_power": {
            "status": low_power_status,
            "scenario_count": int(low_power_data.get("scenario_count", 0)),
            "to_sleep_steps": int(low_power_data.get("totals", {}).get("to_sleep_steps", 0)),
            "wake_steps": int(low_power_data.get("totals", {}).get("wake_steps", 0)),
            "deep_sleep_steps": int(low_power_data.get("totals", {}).get("deep_sleep_steps", 0)),
        },
        "recommendations": unique_recommendations,
        "artifacts": {
            "build_summary": str(build_path),
            "map_summary": str(map_path),
            "protection_summary": str(protection_path),
            "soc_summary": str(soc_path),
            "low_power_summary": str(low_power_path),
        },
    }

    json_path = normalize_path(args.json_output)
    json_path.parent.mkdir(parents=True, exist_ok=True)
    json_path.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    markdown_path = normalize_path(args.markdown_output)
    markdown_path.parent.mkdir(parents=True, exist_ok=True)
    markdown_path.write_text(render_markdown(report) + "\n", encoding="utf-8")

    print(json.dumps(report, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
