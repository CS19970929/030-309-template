import argparse
import json
from pathlib import Path


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="基于模板生成新项目骨架。")
    parser.add_argument("--template", required=True, help="模板名称，例如 firmware-stm32f0")
    parser.add_argument("--project-name", required=True, help="新项目名称")
    parser.add_argument("--output", required=True, help="输出目录")
    return parser


def main() -> int:
    args = build_parser().parse_args()
    output_dir = normalize_path(args.output)
    output_dir.mkdir(parents=True, exist_ok=True)

    metadata = {
        "template": args.template,
        "project_name": args.project_name,
        "generated_by": "scripts/new_project.py",
    }

    (output_dir / "project.json").write_text(
        json.dumps(metadata, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    (output_dir / "README.md").write_text(
        f"# {args.project_name}\n\n"
        f"- 模板：`{args.template}`\n"
        "- 说明：这是阶段 A 生成的新项目占位骨架。\n",
        encoding="utf-8",
    )
    print(f"已生成项目骨架：{output_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
