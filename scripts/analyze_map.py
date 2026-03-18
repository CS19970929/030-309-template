import argparse
import json
from pathlib import Path


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="分析 map 文件并输出基础摘要。")
    parser.add_argument("--map", required=True, help="map 文件路径")
    parser.add_argument("--top", type=int, default=20, help="预留的热点数量参数")
    parser.add_argument("--json", dest="json_output", help="输出 JSON 摘要文件")
    return parser


def main() -> int:
    args = build_parser().parse_args()
    map_path = normalize_path(args.map)
    if not map_path.exists():
        raise SystemExit(f"map 文件不存在：{map_path}")

    lines = map_path.read_text(encoding="utf-8", errors="ignore").splitlines()
    summary = {
        "map_file": str(map_path),
        "line_count": len(lines),
        "top": args.top,
        "status": "stage-a-placeholder",
    }

    if args.json_output:
        output_path = normalize_path(args.json_output)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(
            json.dumps(summary, indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8",
        )

    print(json.dumps(summary, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
