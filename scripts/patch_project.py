import argparse
import json
from pathlib import Path
from typing import Optional


TEXT_EXTENSIONS = {
    ".md",
    ".txt",
    ".json",
    ".toml",
    ".yml",
    ".yaml",
    ".cmake",
    ".c",
    ".h",
    ".s",
    ".S",
    ".ld",
}


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Replace placeholder tokens inside an existing project tree.")
    parser.add_argument("--project-root", required=True, help="Project root")
    parser.add_argument("--vars-file", required=False, help="JSON file that defines replacement pairs")
    parser.add_argument("--check-only", action="store_true", help="Preview only")
    return parser


def load_replacements(vars_file: Optional[Path]) -> dict:
    if vars_file is None:
        return {}
    return json.loads(vars_file.read_text(encoding="utf-8"))


def collect_text_files(project_root: Path) -> list:
    files = []
    for path in project_root.rglob("*"):
        if path.is_file() and path.suffix in TEXT_EXTENSIONS:
            files.append(path)
    return files


def replace_tokens(content: str, replacements: dict) -> str:
    result = content
    for old, new in replacements.items():
        result = result.replace(old, new)
    return result


def main() -> int:
    args = build_parser().parse_args()
    project_root = normalize_path(args.project_root)
    vars_file = normalize_path(args.vars_file) if args.vars_file else None
    replacements = load_replacements(vars_file)
    files = collect_text_files(project_root)

    changed = []
    for file_path in files:
        original = file_path.read_text(encoding="utf-8", errors="ignore")
        updated = replace_tokens(original, replacements)
        if updated != original:
            changed.append(str(file_path))
            if not args.check_only:
                file_path.write_text(updated, encoding="utf-8")

    report = {
        "project_root": str(project_root),
        "check_only": args.check_only,
        "replacement_count": len(replacements),
        "changed_files": changed,
    }
    print(json.dumps(report, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
