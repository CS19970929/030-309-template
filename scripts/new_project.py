import argparse
import json
import shutil
from pathlib import Path


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
    parser = argparse.ArgumentParser(description="Generate a new project from a local template directory.")
    parser.add_argument("--template", required=True, help="Template name under templates/")
    parser.add_argument("--project-name", required=True, help="New project name")
    parser.add_argument("--output", required=True, help="Output directory")
    parser.add_argument("--product-family", default="bms-family", help="Product family identifier")
    parser.add_argument("--board-code", default="board-default", help="Board code")
    parser.add_argument("--afe-code", default="afe-default", help="AFE code")
    parser.add_argument("--device-code", default="STM32F030C8", help="Target device code")
    parser.add_argument("--protocol-variant", default="default", help="Protocol variant")
    parser.add_argument("--customer-code", default="common", help="Customer code")
    return parser


def load_template_manifest(template_dir: Path) -> dict:
    manifest_path = template_dir / "template.json"
    if not manifest_path.exists():
        raise SystemExit(f"Template manifest not found: {manifest_path}")
    return json.loads(manifest_path.read_text(encoding="utf-8"))


def build_replacements(args, manifest: dict) -> dict:
    replacements = {
        "__PROJECT_SLUG__": args.project_name,
        "__PRODUCT_FAMILY__": args.product_family,
        "__BOARD_CODE__": args.board_code,
        "__AFE_CODE__": args.afe_code,
        "__DEVICE_CODE__": args.device_code,
        "__PROTOCOL_VARIANT__": args.protocol_variant,
        "__CUSTOMER_CODE__": args.customer_code,
    }
    replacements.update(manifest.get("replacements", {}))
    return replacements


def replace_tokens(content: str, replacements: dict) -> str:
    result = content
    for old, new in replacements.items():
        result = result.replace(old, new)
    return result


def render_relative_path(path: Path, replacements: dict) -> Path:
    rendered = replace_tokens(path.as_posix(), replacements)
    return Path(rendered)


def should_treat_as_text(path: Path) -> bool:
    return path.suffix in TEXT_EXTENSIONS or path.name in {"README", "LICENSE"}


def copy_template_tree(template_dir: Path, output_dir: Path, replacements: dict) -> list:
    created_files = []
    for source in template_dir.rglob("*"):
        if source.name == "template.json":
            continue
        relative = source.relative_to(template_dir)
        rendered_relative = render_relative_path(relative, replacements)
        destination = output_dir / rendered_relative

        if source.is_dir():
            destination.mkdir(parents=True, exist_ok=True)
            continue

        destination.parent.mkdir(parents=True, exist_ok=True)
        if should_treat_as_text(source):
            content = source.read_text(encoding="utf-8")
            destination.write_text(replace_tokens(content, replacements), encoding="utf-8")
        else:
            shutil.copy2(str(source), str(destination))
        created_files.append(str(destination))
    return created_files


def main() -> int:
    args = build_parser().parse_args()
    template_dir = normalize_path(f"templates/{args.template}")
    output_dir = normalize_path(args.output)

    if output_dir.exists() and any(output_dir.iterdir()):
        raise SystemExit(f"Output directory is not empty: {output_dir}")

    manifest = load_template_manifest(template_dir)
    replacements = build_replacements(args, manifest)
    output_dir.mkdir(parents=True, exist_ok=True)
    created_files = copy_template_tree(template_dir, output_dir, replacements)

    metadata = {
        "template": args.template,
        "project_name": args.project_name,
        "product_family": args.product_family,
        "board_code": args.board_code,
        "afe_code": args.afe_code,
        "device_code": args.device_code,
        "protocol_variant": args.protocol_variant,
        "customer_code": args.customer_code,
        "generated_by": "scripts/new_project.py",
        "created_files": created_files,
    }
    (output_dir / "project.json").write_text(
        json.dumps(metadata, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    print(f"Project generated: {output_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
