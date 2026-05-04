---
name: rhino-skill
description: Use when writing or debugging Rhino.Geometry code for a Grasshopper CPython 3 script component. For this repo, prefer the Grasshopper component context and avoid direct Rhino or Revit workflows unless explicitly requested.
---

# Grasshopper CPython 3 Geometry Scripts

In this repo, Python usually runs inside a Grasshopper Python 3 component created by `upsert_python_node`.

Use the model's baked-in RhinoCommon and computational-geometry ability first. This skill exists to amplify edge cases: GH input marshaling, port output shape, stale schema, tuple returns, and common RhinoCommon gotchas.

Use this context by default:

```python
#! python 3
import Rhino
import Rhino.Geometry as rg
import System
```

Do not use `rhinoscriptsyntax` inside GH Python nodes. Use `Rhino.Geometry` objects in memory and assign them to output variables.

## Standard Node Shape

Every script should have:

- guarded inputs
- typed RhinoCommon geometry operations
- one `dbg` string output
- explicit empty outputs on failure

```python
#! python 3
import Rhino
import Rhino.Geometry as rg
import System

debug_lines = []

def log(message):
    debug_lines.append(str(message))

try:
    if count is None or count < 1:
        count = 1

    result = []
    log("count: {}".format(count))
except Exception as exc:
    result = []
    log("ERROR: {}".format(exc))

dbg = "\n".join(debug_lines)
```

## Geometry Input Extraction

Inputs can arrive as raw RhinoCommon geometry, GH goo, or Rhino object GUIDs. For geometry inputs, use live component volatile data:

```python
def extract_geo(param_index):
    comp = ghenv.Component
    param = comp.Params.Input[param_index]
    results = []
    for pi in range(param.VolatileData.PathCount):
        branch = param.VolatileData.get_Branch(pi)
        for goo in branch:
            if goo is None:
                continue
            geo = getattr(goo, "Value", None)
            if geo is not None and isinstance(geo, rg.GeometryBase):
                results.append(geo)
                continue
            if isinstance(geo, System.Guid):
                rhobj = Rhino.RhinoDoc.ActiveDoc.Objects.FindId(geo)
                if rhobj:
                    results.append(rhobj.Geometry)
                    continue
            if hasattr(goo, "ScriptVariable"):
                sv = goo.ScriptVariable()
                if isinstance(sv, rg.GeometryBase):
                    results.append(sv)
    return results
```

## Output Rules

- Lists: assign Python lists to outputs declared with `"access": "list"`.
- Trees: build a `Grasshopper.DataTree` only when output access is `"tree"`.
- Breps: convert `Surface` and `Extrusion` with `.ToBrep()`.
- Curves and points: output RhinoCommon `rg.Curve`, `rg.Point3d`, etc.

If Python logs say data exists but GH shows zero output, inspect the live port with `node.read`. The issue may be stale port type/access, not Python logic.

## Geometry Safety

- Check `None` after booleans, splits, intersections, lofts, sweeps, and offsets.
- Use document tolerance when available: `Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance`.
- Remember many RhinoCommon methods with C# `out` params return tuples in Python, for example `ok, t = curve.ClosestPoint(point)`.
- Keep scripts focused. Split the graph into a few chunky nodes when intermediate geometry matters.

Read references only for specialized work:

- `references/edge-rules-complete.md` for deeper RhinoCommon gotchas
- `resources/rhinocommon-samples/rhinocommon-samples-llms.txt` for online RhinoCommon sample links
- `references/eto-ui.md` for custom UI
- `references/compute-hops.md` for Hops/Compute
- `references/rhino-inside-revit.md` only when the user explicitly asks for Revit
