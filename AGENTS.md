# AGENTS.md

Use `CLAUDE.md` for the full instructions. This repo is an agent-to-Grasshopper fast loop:

```text
status -> canvas.summary -> write CPython 3 .py + graph.json -> graph.apply -> patch if needed
```

Key rules:

- Use built-in model intelligence first; skills are for sharp edges and failures.
- Python nodes are Rhino 8 CPython 3: `"runtime": "cpython3"`.
- Python source lives in files, never embedded in transaction JSON.
- Batch graph edits in one `graph.apply`.
- Use `debugAfter` to get solved output summaries in the apply response.
- Every node has a `dbg` output.
- Verify with `debug.read` / `node.read`, not assumptions.
- For native GH components, search `resources/grasshopper-components/components.jsonl` narrowly; do not load the whole catalog.
