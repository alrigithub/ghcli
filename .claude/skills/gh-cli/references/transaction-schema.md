# Transaction Schema Reference

## Transaction envelope

```json
{
  "transactionId": "optional-string-id",
  "operations": [ ... ],
  "solveAfter": true
}
```

- `transactionId` is optional — useful for tracking
- `operations` is an ordered array of ops, executed sequentially
- `solveAfter` triggers a document solve after all ops complete

---

## upsert_python_node

Create or patch a CPython 3 node. If `node_id` already exists, the node is updated in place (wires preserved unless schema changes break them).

```json
{
  "op": "upsert_python_node",
  "args": {
    "node_id": "facade_panels",
    "nickname": "Facade Panels",
    "file_path": "C:/absolute/path/to/script.py",
    "runtime": "cpython3",
    "inputs": [
      { "name": "srfs", "typeName": "Brep", "access": "list" },
      { "name": "crvs", "typeName": "Curve", "access": "list" },
      { "name": "grid_w", "typeName": "float", "access": "item" },
      { "name": "offset", "typeName": "float", "access": "item" }
    ],
    "outputs": [
      { "name": "panels", "typeName": "Brep", "access": "tree" },
      { "name": "lvl_srfs", "typeName": "Brep", "access": "tree" },
      { "name": "dbg", "typeName": "string", "access": "item" }
    ],
    "position": { "x": 350.0, "y": 220.0 }
  }
}
```

### Field details

| Field | Required | Notes |
|-------|----------|-------|
| `node_id` | Yes | Stable ID. Use descriptive snake_case. |
| `nickname` | No | Display name on canvas. Defaults to node_id. |
| `file_path` | Yes | Absolute path to .py file. Forward slashes work on Windows. |
| `runtime` | Yes | Always `"cpython3"` for Rhino 8. |
| `inputs` | No | Array of input port definitions. Omit to keep existing schema on patch. |
| `outputs` | No | Array of output port definitions. Omit to keep existing. |
| `position` | No | `{x, y}` canvas position. Omit to keep existing on patch. |

### Port definition

| Field | Required | Values |
|-------|----------|--------|
| `name` | Yes | Python variable name for this port |
| `typeName` | No | `"Brep"`, `"Curve"`, `"Surface"`, `"Mesh"`, `"Point3d"`, `"Vector3d"`, `"Plane"`, `"float"`, `"int"`, `"string"`, `"bool"`, `"Generic Data"`, `"Colour"` |
| `access` | No | `"item"` (default), `"list"`, `"tree"` |
| `typeHint` | No | Rarely needed. Overrides auto type marshalling. |

### Common type mappings

| Geometry | typeName |
|----------|----------|
| Points | `"Point3d"` |
| Curves | `"Curve"` |
| Surfaces | `"Surface"` |
| Breps/polysurfaces | `"Brep"` |
| Meshes | `"Mesh"` |
| Planes/frames | `"Plane"` |
| Vectors | `"Vector3d"` |
| Numbers | `"float"` or `"int"` |
| Text | `"string"` |
| Anything | `"Generic Data"` |

---

## upsert_slider

```json
{
  "op": "upsert_slider",
  "args": {
    "node_id": "SLIDER_count",
    "nickname": "Tread Count",
    "min": 1,
    "max": 40,
    "value": 20,
    "position": { "x": 50.0, "y": 100.0 }
  }
}
```

- Output port name for wiring: `"0"` (sliders have one unnamed output)
- Values are numeric (int or float)

---

## upsert_toggle

```json
{
  "op": "upsert_toggle",
  "args": {
    "node_id": "TOGGLE_preview",
    "nickname": "Show Preview",
    "value": true,
    "position": { "x": 50.0, "y": 160.0 }
  }
}
```

- Output port name for wiring: `"0"`

---

## upsert_panel

```json
{
  "op": "upsert_panel",
  "args": {
    "node_id": "PANEL_debug",
    "nickname": "Debug Output",
    "position": { "x": 700.0, "y": 100.0 }
  }
}
```

- Input port name for wiring: `"0"` (panels have one unnamed input)
- Panels display text/data from connected outputs

---

## upsert_note

```json
{
  "op": "upsert_note",
  "args": {
    "node_id": "NOTE_description",
    "text": "Spiral stair generator - v1",
    "position": { "x": 50.0, "y": 50.0 }
  }
}
```

---

## set_wires

Wire nodes together. Each connection specifies source (output) and target (input).

```json
{
  "op": "set_wires",
  "args": {
    "connect": [
      {
        "source_node_id": "SLIDER_count",
        "source_port": "0",
        "target_node_id": "stair_treads",
        "target_port": "count"
      },
      {
        "source_node_id": "stair_treads",
        "source_port": "treads",
        "target_node_id": "PANEL_debug",
        "target_port": "0"
      }
    ]
  }
}
```

### Port name rules

- **Python nodes**: use the `name` from the input/output definition
- **Sliders/toggles**: output port is `"0"`
- **Panels**: input port is `"0"`
- **Existing canvas nodes**: use the port name from `canvas.summary` (e.g., `"Srf"`, `"Crv"`)

### Wiring to existing geometry params

When wiring from geometry containers already on the canvas, use their full node ID from `canvas.summary`:

```json
{
  "source_node_id": "NODE_673D98F2297A450689B2705FEE06E9C4",
  "source_port": "Srf",
  "target_node_id": "my_python_node",
  "target_port": "surfaces"
}
```

---

## set_value

Change a slider or toggle value without rebuilding the node.

```json
{
  "op": "set_value",
  "args": {
    "node_id": "SLIDER_count",
    "value": 30
  }
}
```

---

## move_node

Reposition a node on canvas.

```json
{
  "op": "move_node",
  "args": {
    "node_id": "stair_treads",
    "position": { "x": 400.0, "y": 300.0 }
  }
}
```

---

## Full example: multi-node definition

This creates a complete 3-node stair definition in one transaction:

```json
{
  "operations": [
    {
      "op": "upsert_slider",
      "args": {
        "node_id": "SLIDER_tread_count",
        "nickname": "Tread Count",
        "min": 4, "max": 40, "value": 20,
        "position": { "x": 50, "y": 100 }
      }
    },
    {
      "op": "upsert_slider",
      "args": {
        "node_id": "SLIDER_radius",
        "nickname": "Radius",
        "min": 500, "max": 5000, "value": 1500,
        "position": { "x": 50, "y": 140 }
      }
    },
    {
      "op": "upsert_python_node",
      "args": {
        "node_id": "stair_treads",
        "nickname": "Stair Treads",
        "file_path": "C:/path/to/stair_treads.py",
        "runtime": "cpython3",
        "inputs": [
          { "name": "count", "typeName": "int", "access": "item" },
          { "name": "radius", "typeName": "float", "access": "item" }
        ],
        "outputs": [
          { "name": "treads", "typeName": "Brep", "access": "list" },
          { "name": "edges", "typeName": "Curve", "access": "list" },
          { "name": "dbg", "typeName": "string", "access": "item" }
        ],
        "position": { "x": 350, "y": 100 }
      }
    },
    {
      "op": "upsert_panel",
      "args": {
        "node_id": "PANEL_debug_treads",
        "nickname": "Debug",
        "position": { "x": 650, "y": 180 }
      }
    },
    {
      "op": "set_wires",
      "args": {
        "connect": [
          { "source_node_id": "SLIDER_tread_count", "source_port": "0", "target_node_id": "stair_treads", "target_port": "count" },
          { "source_node_id": "SLIDER_radius", "source_port": "0", "target_node_id": "stair_treads", "target_port": "radius" },
          { "source_node_id": "stair_treads", "source_port": "dbg", "target_node_id": "PANEL_debug_treads", "target_port": "0" }
        ]
      }
    }
  ],
  "solveAfter": true
}
```
