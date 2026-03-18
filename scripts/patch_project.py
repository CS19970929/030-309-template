import argparse
import json
from pathlib import Path


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="预留的模板补丁入口。")
    parser.add_argument("--project-root", required=True, help="项目根目录")
    parser.add_argument("--check-only", action="store_true", help="只输出计划，不执行修改")
    return parser


def main() -> int:
    args = build_parser().parse_args()
    project_root = normalize_path(args.project_root)

    plan = {
        "project_root": str(project_root),
        "check_only": args.check_only,
        "status": "stage-a-placeholder",
        "next_step": "在阶段 C 中补充占位符替换、工程改名、参数注入逻辑",
    }
    print(json.dumps(plan, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
