---
name: gh-cli
description: Use when an agent needs to push Python files and graph JSON to a live Grasshopper canvas through GhCLI/GhCLI. Trigger for Grasshopper automation, graph.apply, txn.apply, canvas.summary, debug.read, CPython 3 GH nodes, or "zap it to Grasshopper" workflows.
---

# GhCLI Fast Loop

This skill is intentionally short. Use the model's built-in coding and Grasshopper reasoning first; use this skill only to avoid known CLI and live-canvas edge cases. The root `CLAUDE.md` is the source of truth.

Core loop:

```text
status -> canvas.summary -> write .py + graph.json -> graph.apply -> patch if needed
```

Use the CLI as a machine channel:

- stdout is JSON only.
- non-zero exit means failure.
- exit code `2` means Rhino/Grasshopper/plugin unavailable.

Current commands:

- `status`
- `canvas.summary`
- `node.read`
- `txn.apply`
- `graph.apply`
- `solve.run`
- `debug.read`

Current transaction ops:

- `upsert_python_node`
- `upsert_slider`
- `upsert_toggle`
- `upsert_panel`
- `upsert_note`
- `set_wires`
- `move_node`
- `set_value`

Agent behavior:

- Do not read every reference before acting.
- Use references only after schema uncertainty or a live failure.
- Read the canvas before changing it.
- Write Python files to disk and reference absolute `file_path` values.
- Always use `"runtime": "cpython3"` for Python nodes.
- Batch graph creation and wiring in one `graph.apply`.
- Set `debugAfter` to the Python node ids you need verified in the response.
- Add a `dbg` string output to every Python node.
- Verify from returned `debugReads`; use `debug.read` or `node.read` for schema/type/access issues.
- Do not use planned commands just because docs mention them.
- For native GH components, search `resources/grasshopper-components/components.jsonl` with capped output. Do not load the whole catalog.

Read `references/transaction-schema.md` only when you need detailed payload fields. Read `references/troubleshooting.md` only after a live failure.
