"""Build Grasshopper component JSONL files from legacy ghpythonlib text dumps."""
from __future__ import annotations

import json
import re
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
LEGACY_DIR = ROOT / "legacy-text"
COMPONENTS_PATH = ROOT / "components.jsonl"
MANIFEST_PATH = ROOT / "manifest.json"

HEADER_RE = re.compile(r"^---\s*(?P<name>.+?)\s*---\s*$", re.MULTILINE)
PORT_RE = re.compile(
    r"^\s*(?P<name>[^\[\]-]+?)"
    r"(?:\s*\((?P<meta>[^)]*)\))?"
    r"\s*\[(?P<type>[^\]]*)\]"
    r"(?:\s*-\s*(?P<description>.*))?\s*$"
)
INLINE_RETURNS_RE = re.compile(r"^Returns:\s*\[(?P<type>[^\]]*)\](?:\s*-\s*(?P<description>.*))?\s*$")


def slug(value: str) -> str:
    text = re.sub(r"[^a-zA-Z0-9]+", "_", value.strip()).strip("_").lower()
    return text or "unknown"


def parse_port(line: str, fallback_name: str = "result") -> dict[str, object] | None:
    match = PORT_RE.match(line)
    if not match:
        return None

    raw_name = match.group("name").strip()
    meta = (match.group("meta") or "").lower()
    return {
        "name": raw_name or fallback_name,
        "type": (match.group("type") or "").strip(),
        "description": (match.group("description") or "").strip(),
        "optional": "optional" in meta,
        "access": None,
    }


def parse_block(category: str, name: str, body: str, source_file: str) -> dict[str, object]:
    lines = [line.rstrip() for line in body.strip().splitlines()]
    description_lines: list[str] = []
    inputs: list[dict[str, object]] = []
    outputs: list[dict[str, object]] = []
    mode = "description"

    for line in lines:
        stripped = line.strip()
        if not stripped:
            continue

        inline_returns = INLINE_RETURNS_RE.match(stripped)
        if inline_returns:
            outputs.append(
                {
                    "name": "result",
                    "type": inline_returns.group("type").strip(),
                    "description": (inline_returns.group("description") or "").strip(),
                    "optional": False,
                    "access": None,
                }
            )
            mode = "outputs"
            continue

        if stripped == "Input:":
            mode = "inputs"
            continue
        if stripped == "Returns:":
            mode = "outputs"
            continue

        if mode == "description":
            description_lines.append(stripped)
        elif mode == "inputs":
            port = parse_port(line)
            if port:
                inputs.append(port)
        elif mode == "outputs":
            port = parse_port(line, fallback_name="result")
            if port:
                outputs.append(port)

    description = " ".join(description_lines).strip()
    keywords = sorted({slug(part) for part in re.split(r"\s+", f"{category} {name} {description}") if part})

    return {
        "id": f"{slug(category)}.{slug(name)}",
        "plugin": category,
        "category": category,
        "subcategory": None,
        "name": name,
        "display_name": name,
        "description": description,
        "inputs": inputs,
        "outputs": outputs,
        "ghpythonlib_name": name,
        "assembly": None,
        "component_guid": None,
        "keywords": keywords,
        "source": f"legacy-text/{source_file}",
    }


def parse_legacy_file(path: Path) -> list[dict[str, object]]:
    category = path.stem
    text = path.read_text(encoding="utf-8")
    matches = list(HEADER_RE.finditer(text))
    records: list[dict[str, object]] = []

    for index, match in enumerate(matches):
        start = match.end()
        end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        name = match.group("name").strip()
        body = text[start:end]
        records.append(parse_block(category, name, body, path.name))

    return records


def write_jsonl(path: Path, records: list[dict[str, object]]) -> None:
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        for record in records:
            handle.write(json.dumps(record, ensure_ascii=False, sort_keys=True))
            handle.write("\n")


def main() -> int:
    if not LEGACY_DIR.exists() or not any(LEGACY_DIR.glob("*.txt")):
        print(f"No legacy source text found at {LEGACY_DIR}. Refusing to overwrite catalog.")
        return 1

    records: list[dict[str, object]] = []
    legacy_files = sorted(LEGACY_DIR.glob("*.txt"))
    for path in legacy_files:
        records.extend(parse_legacy_file(path))

    records.sort(key=lambda item: (str(item["plugin"]).lower(), str(item["name"]).lower()))
    write_jsonl(COMPONENTS_PATH, records)

    grouped: dict[str, list[dict[str, object]]] = {}
    for record in records:
        grouped.setdefault(str(record["plugin"]), []).append(record)

    manifest = {
        "catalogVersion": 1,
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "source": "legacy-text",
        "componentCount": len(records),
        "pluginCount": len(grouped),
        "plugins": [
            {"name": plugin, "componentCount": len(plugin_records)}
            for plugin, plugin_records in sorted(grouped.items(), key=lambda item: item[0].lower())
        ],
    }
    MANIFEST_PATH.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    print(f"Wrote {len(records)} components across {len(grouped)} plugin/category files.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
