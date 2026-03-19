import argparse
import json
import subprocess
import sys
import time
from pathlib import Path

from analyze_map import build_summary as build_map_summary


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="执行固件构建并生成结构化摘要。")
    parser.add_argument("--cmake", default="cmake", help="cmake 可执行文件")
    parser.add_argument("--configure-preset", required=True, help="configure preset 名称")
    parser.add_argument("--build-preset", required=True, help="build preset 名称")
    parser.add_argument("--arm-gnu-toolchain-root", help="ARM GNU Toolchain 根目录")
    parser.add_argument("--app-heap-size", required=True, help="heap 预留值")
    parser.add_argument("--app-stack-size", required=True, help="stack 预留值")
    parser.add_argument("--log", required=True, help="完整构建日志输出路径")
    parser.add_argument("--json", dest="json_output", required=True, help="JSON 摘要路径")
    parser.add_argument("--md", dest="markdown_output", required=True, help="Markdown 摘要路径")
    parser.add_argument("--map", dest="map_file", help="可选 map 文件路径，用于附加内存摘要")
    return parser


def run_command(command: list[str], cwd: Path) -> dict[str, object]:
    started = time.time()
    completed = subprocess.run(
        command,
        cwd=cwd,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )
    duration = round(time.time() - started, 3)
    return {
        "command": command,
        "returncode": completed.returncode,
        "duration_seconds": duration,
        "output": completed.stdout,
    }


def classify_line(line: str) -> tuple[str, str] | None:
    raw = line.strip()
    lowered = raw.lower()
    if "warning:" in lowered:
        if "is not implemented and will always fail" in lowered or "nosys" in lowered:
            return ("warning", "newlib_nosys_stub")
        if "load segment with rwx permissions" in lowered:
            return ("warning", "linker_rwx_segment")
        if "unused variable" in lowered:
            return ("warning", "unused_variable")
        if "implicit declaration" in lowered:
            return ("warning", "implicit_declaration")
        return ("warning", "generic_warning")
    if " error:" in lowered or lowered.startswith("error:") or "cmake error" in lowered:
        return ("error", "compiler_or_cmake_error")
    if lowered.startswith("failed:") or " ninja: build stopped:" in lowered:
        return ("error", "build_failed")
    return None


def summarize_issues(lines: list[str]) -> tuple[list[dict[str, object]], int, int]:
    grouped: dict[tuple[str, str], dict[str, object]] = {}
    warning_count = 0
    error_count = 0
    for line in lines:
        hit = classify_line(line)
        if not hit:
            continue
        level, category = hit
        if level == "warning":
            warning_count += 1
        else:
            error_count += 1
        key = (level, category)
        if key not in grouped:
            grouped[key] = {
                "level": level,
                "category": category,
                "count": 0,
                "samples": [],
            }
        grouped[key]["count"] += 1
        if len(grouped[key]["samples"]) < 3 and line.strip() not in grouped[key]["samples"]:
            grouped[key]["samples"].append(line.strip())
    issues = sorted(grouped.values(), key=lambda item: (item["level"] != "error", -item["count"], item["category"]))
    return issues, warning_count, error_count


def load_map_summary(map_file: str | None) -> dict[str, object] | None:
    if not map_file:
        return None
    map_path = normalize_path(map_file)
    if not map_path.exists():
        return {
            "status": "missing",
            "map_file": str(map_path),
            "warnings": ["指定的 map 文件不存在，未生成 map 摘要。"],
        }
    lines = map_path.read_text(encoding="utf-8", errors="ignore").splitlines()
    return build_map_summary(map_path, lines, top=10)


def render_markdown(summary: dict[str, object]) -> str:
    issues = summary["issues"]
    lines = [
        "# Build 摘要",
        "",
        f"- 状态：`{summary['status']}`",
        f"- configure 返回码：`{summary['configure']['returncode']}`",
        f"- build 返回码：`{summary['build']['returncode']}`",
        f"- warning 数：`{summary['warning_count']}`",
        f"- error 数：`{summary['error_count']}`",
        f"- heap：`{summary['parameters']['app_heap_size']}`",
        f"- stack：`{summary['parameters']['app_stack_size']}`",
        "",
        "## 命令",
        "",
        f"- configure：`{' '.join(summary['configure']['command'])}`",
        f"- build：`{' '.join(summary['build']['command'])}`",
        "",
        "## 问题分类",
        "",
    ]

    if issues:
        for issue in issues:
            lines.append(f"- `{issue['level']}` / `{issue['category']}` / `{issue['count']}` 次")
            for sample in issue["samples"]:
                lines.append(f"  - `{sample}`")
    else:
        lines.append("- 当前未识别到 warning/error")

    if summary.get("map_summary"):
        map_summary = summary["map_summary"]
        lines.extend(
            [
                "",
                "## Map 关联摘要",
                "",
                f"- map 状态：`{map_summary.get('status', 'unknown')}`",
            ]
        )
        if "ram" in map_summary:
            lines.append(f"- RAM 占用：`{map_summary['ram']['used_percent']}`%")
        if "flash" in map_summary:
            lines.append(f"- FLASH 占用：`{map_summary['flash']['used_percent']}`%")
        if "heap_stack" in map_summary:
            lines.append(f"- stack 预留：`{map_summary['heap_stack']['stack_bytes']}` B")
        for warning in map_summary.get("warnings", []):
            lines.append(f"- map 警告：{warning}")

    lines.extend(["", "## 建议", ""])
    for recommendation in summary["recommendations"]:
        lines.append(f"- {recommendation}")
    lines.append("")
    return "\n".join(lines)


def main() -> int:
    args = build_parser().parse_args()
    cwd = Path.cwd()

    configure_command = [
        args.cmake,
        "--fresh",
        "--preset",
        args.configure_preset,
        f"-DAPP_HEAP_SIZE={args.app_heap_size}",
        f"-DAPP_STACK_SIZE={args.app_stack_size}",
    ]
    if args.arm_gnu_toolchain_root:
        configure_command.append(f"-DARM_GNU_TOOLCHAIN_ROOT={args.arm_gnu_toolchain_root}")

    build_command = [args.cmake, "--build", "--preset", args.build_preset]

    configure_result = run_command(configure_command, cwd)
    build_result = {
        "command": build_command,
        "returncode": -1,
        "duration_seconds": 0.0,
        "output": "",
    }
    if configure_result["returncode"] == 0:
        build_result = run_command(build_command, cwd)

    combined_output = []
    combined_output.append("$ " + " ".join(configure_command))
    combined_output.append(configure_result["output"])
    if configure_result["returncode"] == 0:
        combined_output.append("$ " + " ".join(build_command))
        combined_output.append(build_result["output"])
    full_log = "\n".join(part.rstrip("\n") for part in combined_output if part is not None) + "\n"

    all_lines = full_log.splitlines()
    issues, warning_count, error_count = summarize_issues(all_lines)
    map_summary = load_map_summary(args.map_file) if build_result["returncode"] == 0 else None

    recommendations: list[str] = []
    if any(issue["category"] == "newlib_nosys_stub" for issue in issues):
        recommendations.append("`nosys.specs` 警告当前属于预期现象，若后续引入文件/终端 IO，再决定是否补系统调用封装。")
    if any(issue["category"] == "linker_rwx_segment" for issue in issues):
        recommendations.append("当前存在 `RWX LOAD segment` 警告，后续应结合 linker script 检查段权限是否需要进一步收紧。")
    if map_summary and "ram" in map_summary and map_summary["ram"]["risk"]["level"] != "ok":
        recommendations.append("RAM 已进入风险区，后续改动前优先审查 `.bss`、`heap/stack` 和热点对象。")
    if map_summary and "flash" in map_summary and map_summary["flash"]["risk"]["level"] != "ok":
        recommendations.append("FLASH 占用已偏高，新增功能前优先检查热点对象与可裁剪模块。")
    if build_result["returncode"] != 0 or configure_result["returncode"] != 0:
        recommendations.append("构建失败时先看 `issues` 分类，再回到完整日志定位首个错误。")
    if not recommendations:
        recommendations.append("当前构建链路未识别到显著 warning/error，可继续进入回归与板级验证。")

    status = "ok"
    if configure_result["returncode"] != 0 or build_result["returncode"] != 0 or error_count > 0:
        status = "failed"
    elif warning_count > 0 or (map_summary and map_summary.get("status") == "attention"):
        status = "attention"

    summary = {
        "status": status,
        "parameters": {
            "app_heap_size": args.app_heap_size,
            "app_stack_size": args.app_stack_size,
            "configure_preset": args.configure_preset,
            "build_preset": args.build_preset,
        },
        "configure": {
            "command": configure_result["command"],
            "returncode": configure_result["returncode"],
            "duration_seconds": configure_result["duration_seconds"],
        },
        "build": {
            "command": build_result["command"],
            "returncode": build_result["returncode"],
            "duration_seconds": build_result["duration_seconds"],
        },
        "warning_count": warning_count,
        "error_count": error_count,
        "issues": issues,
        "recommendations": recommendations,
        "log_file": str(normalize_path(args.log)),
        "map_summary": map_summary,
    }

    log_path = normalize_path(args.log)
    log_path.parent.mkdir(parents=True, exist_ok=True)
    log_path.write_text(full_log, encoding="utf-8")

    json_path = normalize_path(args.json_output)
    json_path.parent.mkdir(parents=True, exist_ok=True)
    json_path.write_text(json.dumps(summary, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    md_path = normalize_path(args.markdown_output)
    md_path.parent.mkdir(parents=True, exist_ok=True)
    md_path.write_text(render_markdown(summary), encoding="utf-8")

    print(json.dumps(summary, indent=2, ensure_ascii=False))
    return 0 if status != "failed" else 1


if __name__ == "__main__":
    raise SystemExit(main())
