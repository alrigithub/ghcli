---
description: Peek inside temp/ for existing scripts or graph payloads worth reusing before writing new ones
argument-hint: "[topic | script name]"
---

# Use Temp

Before writing a new Python node or graph payload, check `temp/` for prior scratch files that already do something similar.

Filter:

```text
$ARGUMENTS
```

Behavior:

1. List `temp/` and skim filenames for anything matching the request (or all of it if no filter).
2. If a relevant `.py` or `graph_*.json` exists, read it and reuse or fork it instead of starting from scratch.
3. If nothing fits, just write a new file in `temp/` per the temp-files rule.

Keep it quick — this is a hint, not a workflow.
