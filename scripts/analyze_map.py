import argparse
import json
import re
from pathlib import Path


MEMORY_LINE_RE = re.compile(
    r"^(?P<name>\S+)\s+(?P<origin>0x[0-9a-fA-F]+)\s+(?P<length>0x[0-9a-fA-F]+)\s+(?P<attrs>\S+)$"
)
SECTION_LINE_RE = re.compile(
    r"^\.(?P<name>[A-Za-z0-9_.$]+)\s+"
    r"(?P<address>0x[0-9a-fA-F]+)\s+"
    r"(?P<size>0x[0-9a-fA-F]+)"
    r"(?:\s+load address\s+(?P<load_address>0x[0-9a-fA-F]+))?$"
)
SYMBOL_LINE_RE = re.compile(
    r"^\s*(?P<address>0x[0-9a-fA-F]+)\s+(?P<symbol>_[A-Za-z0-9_]+)\s*=\s*(?P<expr>.+)$"
)
HEAP_STACK_ADD_RE = re.compile(
    r"^\s*(?P<address>0x[0-9a-fA-F]+)\s+\.\s*=\s*\(\.\s*\+\s*(?P<size>0x[0-9a-fA-F]+)\)$"
)
INPUT_SECTION_RE = re.compile(
    r"^\s+\.(?P<section>[A-Za-z0-9_.$]+)\S*\s+"
    r"(?P<address>0x[0-9a-fA-F]+)\s+"
    r"(?P<size>0x[0-9a-fA-F]+)\s+"
    r"(?P<path>\S+\.(?:o|obj))$"
)


def normalize_path(raw_path: str) -> Path:
    path = Path(raw_path)
    if path.is_absolute():
        return path
    return Path.cwd() / path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="分析 GNU ld map 文件并输出内存摘要。")
    parser.add_argument("--map", required=True, help="map 文件路径")
    parser.add_argument("--top", type=int, default=20, help="热点对象保留数量")
    parser.add_argument("--json", dest="json_output", help="输出 JSON 摘要文件")
    parser.add_argument("--md", dest="markdown_output", help="输出 Markdown 摘要文件")
    return parser


def parse_hex(raw_value: str) -> int:
    return int(raw_value, 16)


def find_memory_table(lines: list[str]) -> dict[str, dict[str, object]]:
    memory: dict[str, dict[str, object]] = {}
    in_table = False
    for line in lines:
        if line.strip() == "Memory Configuration":
            in_table = True
            continue
        if not in_table:
            continue
        stripped = line.strip()
        if not stripped:
            if memory:
                break
            continue
        if stripped.startswith("Name "):
            continue
        match = MEMORY_LINE_RE.match(stripped)
        if match:
            memory[match.group("name")] = {
                "origin": parse_hex(match.group("origin")),
                "length": parse_hex(match.group("length")),
                "attrs": match.group("attrs"),
            }
    return memory


def find_sections(lines: list[str]) -> dict[str, dict[str, int]]:
    sections: dict[str, dict[str, int]] = {}
    for line in lines:
        match = SECTION_LINE_RE.match(line)
        if not match:
            continue
        entry = {
            "address": parse_hex(match.group("address")),
            "size": parse_hex(match.group("size")),
        }
        if match.group("load_address"):
            entry["load_address"] = parse_hex(match.group("load_address"))
        sections[match.group("name")] = entry
    return sections


def find_symbols(lines: list[str]) -> dict[str, int]:
    symbols: dict[str, int] = {}
    for line in lines:
        match = SYMBOL_LINE_RE.match(line)
        if not match:
            continue
        symbols[match.group("symbol")] = parse_hex(match.group("address"))
    return symbols


def find_heap_and_stack(lines: list[str]) -> tuple[int, int]:
    in_heap_stack = False
    values: list[int] = []
    for line in lines:
        if line.startswith("._user_heap_stack"):
            in_heap_stack = True
            continue
        if not in_heap_stack:
            continue
        if line and not line.startswith(" "):
            break
        match = HEAP_STACK_ADD_RE.match(line)
        if match:
            values.append(parse_hex(match.group("size")))
        if len(values) >= 2:
            break
    if len(values) == 0:
        return 0, 0
    if len(values) == 1:
        return values[0], 0
    return values[0], values[1]


def collect_top_objects(lines: list[str], top: int) -> dict[str, list[dict[str, object]]]:
    flash_by_object: dict[str, int] = {}
    ram_by_object: dict[str, int] = {}
    flash_sections = ("text", "rodata", "init_array", "fini_array", "ARM.extab", "ARM.exidx")
    ram_sections = ("data", "bss")

    for line in lines:
        match = INPUT_SECTION_RE.match(line)
        if not match:
            continue
        section = match.group("section")
        size = parse_hex(match.group("size"))
        path = match.group("path")
        if size == 0:
            continue
        if any(section.startswith(prefix) for prefix in flash_sections):
            flash_by_object[path] = flash_by_object.get(path, 0) + size
        if any(section.startswith(prefix) for prefix in ram_sections):
            ram_by_object[path] = ram_by_object.get(path, 0) + size

    def rank(data: dict[str, int]) -> list[dict[str, object]]:
        ranked = sorted(data.items(), key=lambda item: item[1], reverse=True)[:top]
        return [{"object": path, "bytes": size} for path, size in ranked]

    return {
        "flash": rank(flash_by_object),
        "ram": rank(ram_by_object),
    }


def ratio(value: int, total: int) -> float:
    if total <= 0:
        return 0.0
    return round(value / total, 4)


def percent(value: int, total: int) -> float:
    if total <= 0:
        return 0.0
    return round((value / total) * 100, 2)


def classify_usage(used: int, total: int) -> dict[str, object]:
    used_percent = percent(used, total)
    if used > total:
        level = "critical"
        message = "已超过区域上限"
    elif used_percent >= 90:
        level = "critical"
        message = "剩余空间很小，后续改动极易触发越界"
    elif used_percent >= 75:
        level = "warning"
        message = "占用已偏高，建议关注增长趋势"
    else:
        level = "ok"
        message = "当前空间仍有余量"
    return {
        "level": level,
        "used_percent": used_percent,
        "free_bytes": max(total - used, 0),
        "message": message,
    }


def build_summary(map_path: Path, lines: list[str], top: int) -> dict[str, object]:
    memory = find_memory_table(lines)
    sections = find_sections(lines)
    symbols = find_symbols(lines)
    heap_size, stack_size = find_heap_and_stack(lines)
    top_objects = collect_top_objects(lines, top)

    ram_vector_total = int(memory.get("RAM_VECTOR", {}).get("length", 0))
    ram_total = int(memory.get("RAM", {}).get("length", 0))
    flash_total = int(memory.get("FLASH", {}).get("length", 0))
    flash_origin = int(memory.get("FLASH", {}).get("origin", 0))

    data_size = max(symbols.get("_edata", 0) - symbols.get("_sdata", 0), 0)
    bss_size = max(symbols.get("_ebss", 0) - symbols.get("_sbss", 0), 0)
    vector_size = int(sections.get("RAMVectorTable", {}).get("size", ram_vector_total))

    heap_stack_reserved = heap_size + stack_size
    ram_used_main = data_size + bss_size + heap_stack_reserved
    ram_used_total = ram_used_main + vector_size

    sidata = symbols.get("_sidata", 0)
    flash_used = 0
    if sidata and flash_origin:
        flash_used = max((sidata + data_size) - flash_origin, 0)

    flash_sections = {
        "isr_vector": int(sections.get("isr_vector", {}).get("size", 0)),
        "text": int(sections.get("text", {}).get("size", 0)),
        "arm_extab": int(sections.get("ARM.extab", {}).get("size", 0)),
        "arm_exidx": int(sections.get("ARM.exidx", {}).get("size", 0)),
        "preinit_array": int(sections.get("preinit_array", {}).get("size", 0)),
        "init_array": int(sections.get("init_array", {}).get("size", 0)),
        "fini_array": int(sections.get("fini_array", {}).get("size", 0)),
        "data_load_image": data_size,
    }

    ram_sections = {
        "ram_vector_table": vector_size,
        "data": data_size,
        "bss": bss_size,
        "heap": heap_size,
        "stack": stack_size,
    }

    warnings: list[str] = []
    ram_risk = classify_usage(ram_used_total, ram_total + ram_vector_total)
    flash_risk = classify_usage(flash_used, flash_total)

    if heap_stack_reserved == 0:
        warnings.append("未从 map 中识别到 heap/stack 预留，建议检查 linker script。")
    if stack_size == 0:
        warnings.append("stack 预留未识别到，Codex 无法评估栈安全余量。")
    if heap_size == 0:
        warnings.append("heap 预留未识别到，Codex 无法评估堆安全余量。")
    if ram_risk["level"] != "ok":
        warnings.append(f"RAM 风险：{ram_risk['message']}。")
    if flash_risk["level"] != "ok":
        warnings.append(f"FLASH 风险：{flash_risk['message']}。")

    summary = {
        "map_file": str(map_path),
        "line_count": len(lines),
        "top": top,
        "memory_regions": {
            name: {
                "origin": entry["origin"],
                "origin_hex": hex(entry["origin"]),
                "length": entry["length"],
                "length_hex": hex(entry["length"]),
                "attrs": entry["attrs"],
            }
            for name, entry in memory.items()
        },
        "ram": {
            "total_bytes": ram_total + ram_vector_total,
            "used_bytes": ram_used_total,
            "used_ratio": ratio(ram_used_total, ram_total + ram_vector_total),
            "used_percent": percent(ram_used_total, ram_total + ram_vector_total),
            "main_region_total_bytes": ram_total,
            "main_region_used_bytes": ram_used_main,
            "sections": ram_sections,
            "risk": ram_risk,
        },
        "flash": {
            "total_bytes": flash_total,
            "used_bytes": flash_used,
            "used_ratio": ratio(flash_used, flash_total),
            "used_percent": percent(flash_used, flash_total),
            "sections": flash_sections,
            "risk": flash_risk,
        },
        "symbols": {
            name: {
                "value": value,
                "value_hex": hex(value),
            }
            for name, value in symbols.items()
            if name in {"_sidata", "_sdata", "_edata", "_sbss", "_ebss"}
        },
        "heap_stack": {
            "heap_bytes": heap_size,
            "stack_bytes": stack_size,
            "reserved_bytes": heap_stack_reserved,
            "reserved_percent_of_total_ram": percent(heap_stack_reserved, ram_total + ram_vector_total),
        },
        "top_objects": top_objects,
        "warnings": warnings,
        "status": "ok" if not warnings else "attention",
    }
    return summary


def render_markdown(summary: dict[str, object]) -> str:
    ram = summary["ram"]
    flash = summary["flash"]
    heap_stack = summary["heap_stack"]
    top_objects = summary["top_objects"]
    warnings = summary["warnings"]

    lines = [
        "# Map 摘要",
        "",
        f"- map 文件：`{summary['map_file']}`",
        f"- 行数：`{summary['line_count']}`",
        f"- 状态：`{summary['status']}`",
        "",
        "## RAM 概览",
        "",
        f"- 总空间：`{ram['total_bytes']}` B",
        f"- 已使用：`{ram['used_bytes']}` B",
        f"- 占用率：`{ram['used_percent']}`%",
        f"- 风险：`{ram['risk']['level']}` {ram['risk']['message']}",
        "",
        "### RAM 分项",
        "",
        f"- 向量表：`{ram['sections']['ram_vector_table']}` B",
        f"- .data：`{ram['sections']['data']}` B",
        f"- .bss：`{ram['sections']['bss']}` B",
        f"- heap：`{ram['sections']['heap']}` B",
        f"- stack：`{ram['sections']['stack']}` B",
        "",
        "## FLASH 概览",
        "",
        f"- 总空间：`{flash['total_bytes']}` B",
        f"- 已使用：`{flash['used_bytes']}` B",
        f"- 占用率：`{flash['used_percent']}`%",
        f"- 风险：`{flash['risk']['level']}` {flash['risk']['message']}",
        "",
        "### FLASH 分项",
        "",
        f"- .isr_vector：`{flash['sections']['isr_vector']}` B",
        f"- .text：`{flash['sections']['text']}` B",
        f"- .data load image：`{flash['sections']['data_load_image']}` B",
        "",
        "## Heap / Stack",
        "",
        f"- heap 预留：`{heap_stack['heap_bytes']}` B",
        f"- stack 预留：`{heap_stack['stack_bytes']}` B",
        f"- 合计：`{heap_stack['reserved_bytes']}` B",
        "",
        "## 热点对象",
        "",
        "### FLASH Top",
        "",
    ]

    if top_objects["flash"]:
        for entry in top_objects["flash"]:
            lines.append(f"- `{entry['object']}`: `{entry['bytes']}` B")
    else:
        lines.append("- 未识别到 FLASH 热点对象")

    lines.extend(["", "### RAM Top", ""])
    if top_objects["ram"]:
        for entry in top_objects["ram"]:
            lines.append(f"- `{entry['object']}`: `{entry['bytes']}` B")
    else:
        lines.append("- 未识别到 RAM 热点对象")

    lines.extend(["", "## 警告", ""])
    if warnings:
        for warning in warnings:
            lines.append(f"- {warning}")
    else:
        lines.append("- 当前未发现显著内存风险")

    lines.append("")
    return "\n".join(lines)


def write_output(path_str: str | None, content: str) -> None:
    if not path_str:
        return
    output_path = normalize_path(path_str)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(content, encoding="utf-8")


def main() -> int:
    args = build_parser().parse_args()
    map_path = normalize_path(args.map)
    if not map_path.exists():
        raise SystemExit(f"map 文件不存在：{map_path}")

    lines = map_path.read_text(encoding="utf-8", errors="ignore").splitlines()
    summary = build_summary(map_path, lines, args.top)
    summary_json = json.dumps(summary, indent=2, ensure_ascii=False) + "\n"

    write_output(args.json_output, summary_json)
    write_output(args.markdown_output, render_markdown(summary))

    print(summary_json, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
