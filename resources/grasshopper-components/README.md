# Grasshopper Components

Local cache of installed Grasshopper components for agent lookup.

This is not a skill. It is inventory data. Agents should query or search it when they need native Grasshopper components, especially after installing plugins such as Ladybug, BEAM, Kangaroo, or other Rhino/GH extensions.

## Files

- `components.jsonl`: flat machine-searchable component index.
- `manifest.json`: extraction metadata.
- `schema.json`: record shape.
- `tools/build_catalog.py`: builds JSONL from extracted source text when available.
- `tools/extract_catalog.py`: future live Grasshopper extraction script.

## Agent Use

Prefer narrow lookup:

1. Search `components.jsonl` for the terms you need.
2. Return only a few matching records.
3. Do not print or load the whole catalog.

Do not load the whole catalog into context.

## Refresh

`components.jsonl` is the tracked cache. `tools/build_catalog.py` only rebuilds it when extracted source text exists locally; it refuses to overwrite the cache with an empty result.

```powershell
python resources\grasshopper-components\tools\build_catalog.py
```

Future target:

```powershell
GhCLI.exe catalog.refresh
GhCLI.exe catalog.search --query "sun path"
GhCLI.exe catalog.read --id ladybug.sunpath
```
