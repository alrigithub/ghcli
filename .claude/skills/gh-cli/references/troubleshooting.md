# Troubleshooting Reference

## Decision tree: what went wrong?

```
Problem?
├── CLI returns error
│   ├── Exit code 2: plugin_unavailable
│   │   ├── status also fails → Rhino/GH not open or plugin not loaded
│   │   └── status works but other commands fail → transient doc issue, retry once
│   ├── Exit code 3: validation error
│   │   └── Check: are you using an unimplemented op? (delete_node, upsert_native_node)
│   └── Exit code 1: runtime error
│       └── Read stderr for details
├── Node created but output is empty (0 items)
│   ├── Check debug.read → dbg output first (did the script run? what did it log?)
│   ├── Check debug.read → runtimeMessages (Python exceptions show here)
│   ├── Check node.read → port access and typeName (wrong access = wrong data shape)
│   └── If script logs correct data but output shows 0 → stale port typing (see below)
├── Node has runtime errors
│   ├── Read the actual error message from debug.read → runtimeMessages
│   ├── Common: missing import, wrong variable name, type mismatch
│   └── Fix the .py file, push upsert_python_node patch, solve again
└── Downstream node doesn't receive data
    ├── Check wiring: canvas.summary → edges (is the wire there?)
    ├── Check port names match exactly (case-sensitive)
    └── Check source node actually has data: debug.read → outputSummaries
```

---

## Common issues and fixes

### "No active Grasshopper document is available"

**Cause**: The GH plugin resolves the active document from `Instances.ActiveCanvas?.Document`, which can be null if Grasshopper's window is not focused.

**Fix**: Retry the command once. If it persists:
- Click the Grasshopper window to give it focus
- Make sure a .gh document is open (not just the Grasshopper splash)

### "Unknown operation: delete_node"

**Cause**: `delete_node` is documented in the contract but not yet implemented.

**Workaround**: You can't remove nodes. Overwrite a bad node by upserting with the same `node_id` and different content. If the node truly needs to be gone, the user must delete it manually in Grasshopper.

### Output shows 0 items but Python runs fine

**Diagnosis sequence**:
1. `debug.read --node-id <id>` — check `dbg` output. Does the script report it created geometry?
2. `node.read --node-id <id>` — check output port `typeName`, `access`, `branchCount`, `itemCount`
3. If the script logs data but the port shows 0 items:

**Cause**: Stale port typing. After schema changes, GH can keep the old runtime type on a port even though the name changed.

**Workaround**: Push an empty output schema, then push the desired schema:
```json
// Step 1: empty outputs
{ "op": "upsert_python_node", "args": { "node_id": "X", "outputs": [] } }
// Step 2: real outputs
{ "op": "upsert_python_node", "args": { "node_id": "X", "outputs": [{ "name": "geo", "typeName": "Brep", "access": "list" }] } }
```

### Wrong data shape in Python

**Symptoms**: Script gets a single item when it expected a list, or gets a flat list when it expected a tree.

**Cause**: The `access` field in the transaction doesn't match what the script needs.

**Fix**: Check the node's input definition. `"access": "item"` means the script runs once per item (GH iterates). `"access": "list"` means the script gets the whole list at once. Make sure the transaction matches the script's expectation.

### Wiring fails silently

**Symptoms**: `txn.apply` succeeds with `wireChanges: 0` even though you specified connections.

**Possible causes**:
- Port name doesn't match exactly (case-sensitive)
- Node ID doesn't exist (was it created in a previous transaction that failed?)
- Port is on the wrong side (you swapped source and target)

**Fix**: After wiring, run `canvas.summary --scope full` and check the `edges` array.

### Script works locally but fails in GH

**Cause**: GH's CPython 3 runtime has a specific import environment. Common mistakes:
- Using `rhinoscriptsyntax` (not available in GH Python components)
- Missing `#! python 3` directive at the top
- Assuming standard Python file I/O paths

**Fix**: Follow the rhino-skill patterns for GH Python component scripts (Context A).

---

## Diagnostic commands cheat sheet

| What you want to know | Command |
|----------------------|---------|
| Is the plugin alive? | `status` |
| What's on the canvas? | `canvas.summary --scope full` |
| Deep read of one node | `node.read --node-id <id>` |
| What did the node produce? | `debug.read --node-id <id>` |
| Force a re-solve | `solve.run` or `solve.run --node-id <id>` |

---

## Escalation guide

| Situation | Category | What to tell the user |
|-----------|----------|----------------------|
| Need to delete a node | Missing CLI feature | "delete_node isn't implemented yet — delete it manually in GH" |
| Need to place a native GH component | Missing CLI feature | "upsert_native_node isn't implemented — place it manually or wrap the logic in a Python node" |
| Need to search for GH components | Local catalog | "Search `resources/grasshopper-components/components.jsonl`; `catalog.search` is future CLI work" |
| Port typing is stuck | Known CLI sharp edge | "This is a known stale-port issue — try the empty-schema workaround" |
| Geometry question (booleans, NURBS, etc.) | Rhino knowledge | "This is a RhinoCommon question — check the rhino-skill" |
| Complex data tree logic | Rhino knowledge | "Data tree patterns are covered in the rhino-skill" |
