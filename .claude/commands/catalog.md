---
description: Search or refresh the local Grasshopper component catalog for native GH/plugin component lookup
argument-hint: "<search terms | refresh>"
---

# Grasshopper Component Catalog

Use this command when the user asks what native Grasshopper components are available, asks whether a plugin component exists, installs a new Rhino/Grasshopper plugin, or wants the local component catalog refreshed.

Request:

```text
$ARGUMENTS
```

## Catalog Location

The local component catalog lives at:

```text
resources\grasshopper-components\
```

It contains:

- `components.jsonl`: flat searchable component index.
- `manifest.json`: current cache summary.
- `schema.json`: record shape.
- `tools/build_catalog.py`: rebuilds the tracked cache from extracted source text when available.
- `tools/extract_catalog.py`: future/live extraction helper.

## Search Behavior

For lookup requests:

1. Search `resources\grasshopper-components\components.jsonl` narrowly using the user's terms.
2. Return only a few relevant records.
3. Prefer exact component names, plugin names, categories, and keywords.
4. Do not load or print the whole catalog.
5. If no match is found, say that the tracked cache does not include it yet.

Useful PowerShell pattern:

```powershell
Select-String -Path resources\grasshopper-components\components.jsonl -Pattern "<term>" | Select-Object -First 20
```

## Refresh Behavior

The tracked cache is `components.jsonl`.

The build tool only refreshes it when extracted source text exists locally:

```powershell
python resources\grasshopper-components\tools\build_catalog.py
```

If refresh fails because no extracted source text exists, keep the tracked cache and explain that live extraction is not implemented as a CLI command yet.

Future target commands:

```powershell
GhCLI.exe catalog.refresh
GhCLI.exe catalog.search --query "sun path"
GhCLI.exe catalog.read --id ladybug.sunpath
```

Do not use those future commands until `GhCLI.exe status` reports them in capabilities.

## Graph Apply Note

Use catalog results only when native GH/plugin components are actually useful. For agent-authored geometry, prefer file-based CPython 3 nodes and `graph.apply`.
