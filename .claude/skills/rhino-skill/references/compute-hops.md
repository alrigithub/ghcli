# Compute and Hops Reference

Rules for Rhino.Compute server and Hops (remote solving for Grasshopper).

---

## Setup

- Python package name: `compute_rhino3d` (underscore, not dash)
- Hops requires **two** installs: `pip install ghhops_server` + Hops component via GH Package Manager
- macOS does **not** support native Compute — use a remote Windows Compute server or a local Python `ghhops_server`

---

## Hops input/output components

| Role | Components |
|------|-----------|
| Inputs | Get Integer, Get Number, Get Point, Get String, etc. |
| Outputs | Context Bake, Context Print |

### Access control

- `AtMost = 1` → Item Access (one value per solve)
- `AtMost > 1` or unset → List Access (list of values)
- `Tree Access` checkbox → overrides AtMost, always Tree Access

### Non-standard types

Serialize to file and pass the file path as a string input instead.

---

## Data tree behavior

A Hops component receiving a data tree input launches **parallel computation per branch** by default. Each branch gets its own independent solve.

---

## Remote Compute authentication

Include `RhinoComputeKey` in HTTP request headers:

```python
headers = {'RhinoComputeKey': api_key}
```

---

## All rules

| Priority | ID | Rule |
|----------|-----|------|
| CRITICAL | compute_rhino3d_underscore_import | Package is `compute_rhino3d`, not `compute-rhino3d` |
| CRITICAL | ghhops_server_separate_install | Install both pip package and GH component |
| CRITICAL | hops_get_components_for_inputs | Use Get components for inputs, Context Bake/Print for outputs |
| CRITICAL | macos_no_local_compute | macOS: remote Windows Compute or local Python ghhops_server |
| CRITICAL | rhino_compute_key_header | Include `RhinoComputeKey` header for remote auth |
| HIGH | hops_at_most_controls_access | AtMost=1 is Item; AtMost>1 or unset is List |
| HIGH | hops_tree_access_supersedes_at_most | Tree Access overrides AtMost |
| HIGH | hops_file_path_for_non_standard_types | Serialize non-standard types to file, pass path |
| MEDIUM | hops_data_tree_parallel_per_branch | Tree input = parallel compute per branch |
