# Rhino.Inside.Revit Guide

This reference covers writing GH Python scripts that run inside Autodesk Revit via Rhino.Inside.Revit.

## Table of Contents

1. [When to use this guide](#when-to-use-this-guide)
2. [Default imports](#default-imports)
3. [Context boundaries](#context-boundaries)
4. [Units](#units)
5. [Geometry conversion](#geometry-conversion)
6. [Revit write safety](#revit-write-safety)
7. [Native Revit vs Rhino bake](#native-revit-vs-rhino-bake)
8. [Family placement](#family-placement)
9. [Parameters and units](#parameters-and-units)
10. [Diagnostics](#diagnostics)
11. [Edge rules for RIR context](#edge-rules)

---

## When to use this guide

Use when a prompt involves:
- Rhino running inside Autodesk Revit
- Grasshopper definitions hosted by Rhino.Inside.Revit
- `Autodesk.Revit.DB` / `Autodesk.Revit.UI`
- Converting Rhino geometry into Revit elements
- Revit families, parameters, categories, or transactions

### Choose the host context first

- Result stays on the GH canvas → use Rhino/Grasshopper types, avoid Revit writes
- Result creates or modifies Revit elements → use Rhino.Inside.Revit context + `Autodesk.Revit.DB`
- Problem is load failure, missing tab, blocked add-ins → host diagnostics, not geometry scripting

---

## Default imports

```python
import clr
clr.AddReference("RhinoInside.Revit")
clr.AddReference("RevitAPI")
clr.AddReference("RevitAPIUI")

from RhinoInside.Revit import Revit
import RhinoInside.Revit.Convert.Geometry
clr.ImportExtensions(RhinoInside.Revit.Convert.Geometry)

from Autodesk.Revit import DB
from Autodesk.Revit import UI
```

Access active context:
- `Revit.ActiveUIApplication` — the active `UIApplication`
- `Revit.ActiveUIDocument` — the active `UIDocument`
- `Revit.ActiveDBDocument` — the active `DB.Document`

Do not use Rhino document state as source of truth for Revit writes.

---

## Context boundaries

- Rhino/GH geometry is for computation, preview, iteration, and DataTrees
- Revit DB objects are the only way to create, edit, or query native Revit elements
- **Convert once at the boundary** — do not bounce geometry back and forth

Typical flow:
1. Compute geometry in `Rhino.Geometry`
2. Validate while still in Rhino space
3. Convert to Revit types near the write boundary
4. Open a Revit transaction
5. Create or update Revit elements

---

## Units

Rhino and Revit do not share internal unit assumptions.

- Rhino model units depend on the active Rhino document
- Revit stores lengths as internal feet for `StorageType.Double` parameters
- Parameters need explicit conversion before `Set()` and after `AsDouble()`
- Conversion helpers handle geometry, but parameter values need unit awareness

```python
# Writing a parameter
feet = DB.UnitUtils.ConvertToInternalUnits(mm_value, DB.UnitTypeId.Millimeters)
param.Set(feet)

# Reading a parameter
mm_value = DB.UnitUtils.ConvertFromInternalUnits(param.AsDouble(), DB.UnitTypeId.Millimeters)
```

Never hardcode unit assumptions. Read the active document context and convert deliberately.

---

## Geometry conversion

RIR exposes extension methods and encoder/decoder helpers:

| Direction | Pattern |
|-----------|---------|
| Revit to Rhino | `revit_curve.ToCurve()`, `revit_point.ToPoint3d()`, `GeometryDecoder.*` |
| Rhino to Revit | `rhino_point.ToXYZ()`, `rhino_curve.ToCurve()`, `GeometryEncoder.*` |

Guidelines:
- Keep algorithmic work in Rhino types until the final bake step
- Validate planarity, closure, and `IsValid` before attempting Revit creation
- For family placement, convert points and directions — not entire Rhino scene objects
- For DirectShape or sketch-based creation, match the expected Revit type exactly
- Decoded Revit geometry (for preview) should stay separate from Revit write payloads

---

## Revit write safety

- Open a `DB.Transaction` for any model change
- Batch related writes into one transaction when possible
- Do not open a transaction for read-only queries
- Activate `FamilySymbol` objects before placement: `symbol.Activate(); doc.Regenerate()`
- Check `Parameter.IsReadOnly` and `Parameter.StorageType` before `Set()`

```python
t = DB.Transaction(doc, 'Create Elements')
t.Start()
# ... create/modify elements ...
t.Commit()
```

If a script both computes and writes, make the write boundary explicit in the output contract.

---

## Native Revit vs Rhino bake

**Prefer native Revit elements** when the user needs: schedules, tags, categories, families, documentation views, BIM parameters, or downstream coordination.

**Prefer Rhino-side preview** when the user needs: fast geometry iteration, exploratory form finding, temporary previews, non-BIM geometry that should not become Revit elements yet.

---

## Family placement

Before placing a family instance, confirm:
1. Exact family and type name
2. Host requirement (wall-hosted, face-hosted, non-hosted)
3. Level requirement
4. Insertion point
5. Orientation or facing vector
6. Instance parameters to set after creation

Use `FamilySymbol` (not `Family`) at placement time. Collect and match both family name and symbol name. For host-based families, use the overload that includes the host element.

```python
if not symbol.IsActive:
    symbol.Activate()
    doc.Regenerate()
instance = doc.Create.NewFamilyInstance(xyz, symbol, level, DB.Structure.StructuralType.NonStructural)
instance.LookupParameter('Mark').Set(mark_value)
```

Type parameters live on the `FamilySymbol`, not the instance.

---

## Parameters and units

### Reading parameters
```python
param = element.LookupParameter(name)
if param is None:
    # Check type parameters
    type_elem = doc.GetElement(element.GetTypeId())
    param = type_elem.LookupParameter(name)
```

### Writing parameters
1. Check `param is not None`
2. Check `not param.IsReadOnly`
3. Branch on `param.StorageType`:
   - `String` → `param.Set(text)`
   - `Integer` → `param.Set(int_val)`
   - `Double` → convert to internal units first, then `param.Set(feet_val)`
   - `ElementId` → `param.Set(DB.ElementId(id_value))`

---

## Diagnostics

Start with local machine truth:
- `sources/userpc_revit_addins/<year>/`
- `sources/userpc_revit_env/install_manifest.json`
- `sources/userpc_rir_xml_apis/<year>/`

### Common host failures

| Symptom | Likely cause |
|---------|-------------|
| Missing Rhino.Inside tab | `.addin` manifest path wrong or missing — inspect addin file |
| Missing GH add-ins | Revit blocks assemblies from network paths — check `loadFromRemoteSources` |
| openNURBS conflict | Another add-in won the load order — restart Revit, load RIR first |
| `-200` or long JSON errors | DLL conflicts with other add-ins — inspect loaded addin manifests |
| Init failure with pyRevit | pyRevit loader DLL incompatibility — verify pyRevit version |

---

## Edge rules

### Critical

- **rir_use_revit_static_context**: Obtain uiapp, uidoc, doc from `RhinoInside.Revit.Revit` — not Revit globals
- **revit_write_requires_transaction**: Wrap all creates/deletes/modifies in `DB.Transaction`
- **family_symbol_activate_then_regenerate**: Call `Activate()` + `doc.Regenerate()` before placement
- **parameter_exists_before_access**: Null-check `LookupParameter()` before calling `As*` or `Set`
- **parameter_read_only_check_before_set**: Check `IsReadOnly` before `Set()`
- **branch_on_parameter_storage_type**: Match `StorageType` to the correct `Set` payload
- **double_parameters_use_internal_units**: Convert to internal units before `Set()` on physical quantities
- **use_rir_geometry_conversion_helpers**: Use RIR conversion helpers, not manual coordinate copying
- **validate_geometry_before_revit_encode**: Check closure, planarity, `IsValid` before Revit encoding
- **revit_creation_needs_revit_db_types**: Pass exact Revit DB types (XYZ, Curve, CurveLoop), not Rhino objects
- **place_familysymbol_not_family**: Pass `FamilySymbol` to `NewFamilyInstance`, not `Family`
- **hosted_families_require_host_context**: Host-based families need the host element in the placement overload
- **activate_symbol_before_first_use**: `Activate()` before creating instances if symbol is unused
- **diagnose_missing_tab_from_addin_manifest**: Inspect `.addin` manifest before assuming broken install
- **opennurbs_conflict_load_order**: Restart Revit and load RIR first to avoid openNURBS conflicts

### High

- **rir_alias_revit_namespaces**: Alias as `DB` and `UI` for visual clarity
- **batch_revit_writes_in_single_transaction**: One transaction for many elements, not one per item
- **convert_at_boundary_once**: Keep geometry in Rhino types until the final write step
- **directshape_requires_valid_category_and_shape**: Valid category ID + converted geometry for DirectShape
- **collect_symbols_for_exact_type**: Match both family name and symbol name explicitly
- **level_aware_overload_for_level_based_families**: Use overload that includes target Level
- **double_parameter_reads_need_conversion**: Convert from internal units when exposing to GH/Rhino
- **elementid_parameters_need_elementid**: Set with `DB.ElementId`, not name or raw integer
- **instance_vs_type_parameter_lookup**: If param is missing on instance, check the type element
- **type_parameters_on_symbol_not_instance**: Write shared type params on `FamilySymbol`, not instance
- **error_200_or_json_is_plugin_conflict**: Inspect add-ins for DLL conflicts before deeper debugging
- **gh_addons_missing_check_rhino_history**: Inspect Rhino command history (F2) for blocked assemblies
- **network_loaded_gha_needs_loadfromremotesources**: Check `Revit.exe.config` for `loadFromRemoteSources`
- **older_pyrevit_can_conflict_with_rir**: Verify pyRevit version and loader DLL compatibility

### Medium

- **revit_read_only_no_transaction**: Don't open a transaction for read-only queries
- **set_instance_params_after_create**: Create instance first, then set parameters
- **decode_revit_geometry_for_preview_not_mutation**: Keep decoded geometry separate from write payloads

### Output contract for agent-generated RIR code

Before any code, explicitly state:
1. Target context: GH Python in Rhino.Inside.Revit
2. Required imports
3. Input and output types
4. Which values are Rhino types vs Revit DB types
5. Where unit conversion happens
6. Where geometry conversion happens
7. Whether a transaction is opened, and around which operations
