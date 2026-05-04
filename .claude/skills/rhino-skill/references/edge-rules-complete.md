# Complete Edge Rules Reference

121 edge-case rules for RhinoCommon, Grasshopper, Rhino.Inside.Revit, Revit host diagnostics, Eto, and Compute scripting.

## Table of Contents

1. [Document Operations (rhino_script)](#document-operations)
2. [C# to Python Tuple Unpacking (any)](#c-to-python-tuple-unpacking)
3. [GH Component Patterns (gh_component)](#gh-component-patterns)
4. [Eto UI (eto)](#eto-ui)
5. [Compute and Hops (compute)](#compute-and-hops)
6. [Geometry Safety (any)](#geometry-safety)
7. [RIR Context and Transactions (gh_component_rir, revit_api)](#rir-context-and-transactions)
8. [Revit Parameters and Units (gh_component_rir, revit_api)](#revit-parameters-and-units)
9. [Revit Families and Instances (gh_component_rir, revit_api)](#revit-families-and-instances)
10. [Revit Geometry Conversion (gh_component_rir)](#revit-geometry-conversion)
11. [Revit Host Troubleshooting (revit_host)](#revit-host-troubleshooting)

Priority key: **CRITICAL** = will crash or corrupt, **HIGH** = silent wrong result, **MEDIUM** = best practice

---

## Document Operations

Context: `rhino_script`

| Priority | ID | Rule | Example |
|----------|-----|------|---------|
| CRITICAL | redraw_after_add | After `Objects.Add*()`, call `Views.Redraw()` | `sc.doc.Objects.AddSphere(sphere); sc.doc.Views.Redraw()` |
| CRITICAL | guid_empty_check_on_add | Check Add* return against `System.Guid.Empty` (not None) | `if sc.doc.Objects.AddBrep(brep) != System.Guid.Empty:` |
| CRITICAL | commit_changes_after_attribute_modify | After modifying attributes, call `obj.CommitChanges()` | `obj.Attributes.ObjectColor = color; obj.CommitChanges()` |
| CRITICAL | scriptcontext_doc_access | Use `scriptcontext.doc`, not `RhinoDoc.ActiveDoc` | `import scriptcontext; scriptcontext.doc.Objects.AddSphere(sphere)` |
| HIGH | color_source_before_object_color | Set `ColorSource = ObjectColorSource.ColorFromObject` before color | `attributes.ColorSource = ObjectColorSource.ColorFromObject` |
| HIGH | layer_find_index_check | `doc.Layers.Find(name, True)` returns int; negative = not found | `layer_index = sc.doc.Layers.Find(name, True); if layer_index >= 0:` |
| HIGH | layers_add_failure_check | Check `Layers.Add()` return < 0 for failure (not None) | `if sc.doc.Layers.Add(name, Color.Black) < 0: return` |
| HIGH | layer_commit_changes_after_rename | Call `layer.CommitChanges()` after rename/modify | `layer.Name = new_name; layer.CommitChanges()` |
| HIGH | replace_then_redraw | After `Objects.Replace()`, call `Redraw()` | `sc.doc.Objects.Replace(objref, geom); sc.doc.Views.Redraw()` |
| HIGH | render_materials_add_before_assign | `doc.RenderMaterials.Add(mat)` first, then assign, then `CommitChanges()` | `doc.RenderMaterials.Add(mat); obj.RenderMaterial = mat` |
| MEDIUM | enable_redraw_batch_pattern | `rs.EnableRedraw(False)` before batch, `True` after | `rs.EnableRedraw(False); [loop]; rs.EnableRedraw(True)` |

---

## C# to Python Tuple Unpacking

Context: `any`

| Priority | ID | Rule | Example |
|----------|-----|------|---------|
| CRITICAL | get_one_object_tuple_unpack | `GetOneObject()` → `(rc, objref)` | `rc, objref = RhinoGet.GetOneObject(msg, True, ObjectType.Brep)` |
| CRITICAL | get_multiple_objects_tuple_unpack | `GetMultipleObjects()` → `(rc, objrefs)` | `rc, objrefs = RhinoGet.GetMultipleObjects(msg, False, filter)` |
| CRITICAL | get_point_tuple_unpack | `GetPoint()` → `(rc, point)` | `rc, base_point = RhinoGet.GetPoint(msg, False)` |
| CRITICAL | get_string_tuple_unpack | `GetString()` → `(rc, string_val)` | `rc, name = RhinoGet.GetString(msg, True, name)` |
| CRITICAL | get_number_tuple_unpack | `GetNumber()` → `(rc, number)` | `rc, radius = RhinoGet.GetNumber(msg, False, radius)` |
| CRITICAL | get_integer_tuple_unpack | `GetInteger()` → `(rc, integer_val)` | `rc, count = RhinoGet.GetInteger(msg, False, count)` |
| CRITICAL | get_angle_tuple_unpack | `GetAngle()` → `(rc, angle_radians)` | `rc, angle = RhinoGet.GetAngle(msg, base_pt, first_pt, 0)` |
| CRITICAL | surface_closest_point_triple_unpack | `Surface.ClosestPoint()` → `(bool, u, v)` | `b, u, v = surface.ClosestPoint(point)` |
| CRITICAL | curve_closest_point_pair_unpack | `Curve.ClosestPoint()` → `(bool, t)` | `b, cp = crv.ClosestPoint(p)` |
| CRITICAL | divide_by_count_returns_t_values | `DivideByCount/Length` returns t-values, not points | `pts = [curve.PointAt(t) for t in curve.DivideByCount(n, True)]` |
| CRITICAL | get_object_indexed_method_call | `go.Object(0)` is a method call, NOT `go.Object[0]` | `objref = go.Object(0)` |
| HIGH | surface_frame_at_pair_unpack | `FrameAt()` → `(bool, plane)` | `b, plane = surface.FrameAt(u, v)` |
| HIGH | unroller_perform_unroll_quad_unpack | `PerformUnroll()` → `(breps, curves, points, dots)` | `breps, curves, points, dots = unroll.PerformUnroll()` |
| HIGH | show_color_dialog_pair_unpack | `ShowColorDialog()` → `(bool, color)` | `b, color = ShowColorDialog(color)` |
| HIGH | overloads_bracket_syntax | Use `.Overloads[]` for disambiguation | `method.Overloads[type1, type2](arg1, arg2)` |
| HIGH | list_t_bracket_syntax | `List[T]` with bracket syntax for generics | `from System.Collections.Generic import List; List[Curve]([c])` |
| HIGH | system_drawing_color_import | Must explicitly import `System.Drawing.Color` | `from System.Drawing import Color` |
| HIGH | add_option_current_value | Read option results from `option.CurrentValue` | `intOption = OptionInteger(1,1,99); val = intOption.CurrentValue` |

---

## GH Component Patterns

Context: `gh_component`

| Priority | ID | Rule | Example |
|----------|-----|------|---------|
| CRITICAL | no_rhinoscriptsyntax_in_gh | Don't pass RhinoCommon objects to `rs` functions (they need GUIDs) | `crv.PointAtStart  # not rs.CurveStartPoint(crv)` |
| CRITICAL | scriptcontext_doc_switch_for_baking | Switch to `RhinoDoc.ActiveDoc` before baking, back to `ghdoc` after | `sc.doc = Rhino.RhinoDoc.ActiveDoc; rs.AddPoint(pt); sc.doc = ghdoc` |
| CRITICAL | treehelpers_list_to_tree | Use `treehelpers.list_to_tree(nested)` for DataTree output | `import ghpythonlib.treehelpers as th; a = th.list_to_tree(nested)` |
| CRITICAL | type_hints_on_inputs | Set Type Hint on geometry inputs (Point3d, Curve, Brep) | Right-click input -> Type Hint -> Point3d |
| CRITICAL | python3_directive | First line must be `#! python 3` (or `#! python 2`) | `#! python 3` |
| CRITICAL | no_async_in_gh | `# async: true` not supported in GH components | Do NOT use `# async: true` |
| CRITICAL | system_drawing_not_eto_for_preview | Preview overrides use `System.Drawing`, NOT `Eto.Drawing` | `import System.Drawing; color = System.Drawing.Color.Red` |
| HIGH | ghdoc_object_type_hint | ghdoc Object Type Hint marshals geometry to GUID for rs functions | Set Type Hint to 'ghdoc Object' in component UI |
| HIGH | param_access_item_list_tree | List Access for whole lists; Tree Access for DataTree input | Right-click input param -> List Access or Tree Access |
| HIGH | ghpythonlib_argument_order | Match arg order to component's parameter order | `ghpythonlib.components.Move(geometry, vector)` |
| HIGH | ghpythonlib_named_tuple | Multi-output components return named tuples | `result = ghcomp.BrepClosestPoint(brep, pt); cp = result.Point` |
| HIGH | sdk_mode_runscript_return | SDK-Mode RunScript must use return statement | `def RunScript(self, x): return x * 2` |
| HIGH | clippingbox_override | Override `ClippingBox` when implementing preview methods | `def get_ClippingBox(self): return BoundingBox(...)` |
| HIGH | data_tree_matching | Longest List reuses last element; single item repeats in all branches | 3 points + 1 vector -> vector reused for all 3 |
| MEDIUM | none_input_without_hint | Unconnected input with no Type Hint arrives as `None` | `if x is None: x = default_value` |
| MEDIUM | requirements_directive | Use `# requirements: numpy` for PyPI packages | `# requirements: numpy` |
| MEDIUM | guid_auto_marshalling | rs GUID outputs auto-convert to geometry (disable via context menu) | Right-click -> Avoid Marshalling Output Guids |
| MEDIUM | sticky_variables | Use `scriptcontext.sticky` dict for persistent data | `sc.sticky['counter'] = sc.sticky.get('counter', 0) + 1` |

---

## Eto UI

Context: `eto`

| Priority | ID | Rule | Example |
|----------|-----|------|---------|
| CRITICAL | eto_super_init_first | Call `super().__init__()` first in Eto dialog `__init__` | `class MyDialog(forms.Dialog): def __init__(self): super().__init__()` |
| CRITICAL | eto_three_required_imports | Import all three: `Rhino.UI`, `Eto.Drawing as drawing`, `Eto.Forms as forms` | `import Rhino.UI; import Eto.Drawing as drawing; import Eto.Forms as forms` |
| CRITICAL | eto_showmodal_parent_mac_safe | Use `MainWindowForDocument(sc.doc)` as parent (not `MainWindow`) | `dialog.ShowModal(Rhino.UI.RhinoEtoApp.MainWindowForDocument(sc.doc))` |
| CRITICAL | eto_default_abort_buttons | Assign `DefaultButton` and `AbortButton` for Enter/Escape | `self.DefaultButton = ok_btn; self.AbortButton = cancel_btn` |
| CRITICAL | eto_gridview_observable_collection | Use `ObservableCollection[object]()` for GridView DataStore | `grid.DataStore = ObservableCollection[object](items)` |
| CRITICAL | eto_no_viewmodel_binding_python | No ViewModel/Binding in Python — use manual property updates | `label.Text = new_value` |
| HIGH | eto_show_form_static | Show non-modal forms via `EtoExtensions.Show(form, sc.doc)` | `Rhino.UI.EtoExtensions.Show(form, sc.doc)` |
| HIGH | eto_showsemimodal_static | `EtoExtensions.ShowSemiModal(dialog, sc.doc, parent)` — static call | `Rhino.UI.EtoExtensions.ShowSemiModal(dialog, sc.doc, parent)` |
| HIGH | eto_padding_spacing_wrap | Wrap Padding in `drawing.Padding(n)`, Spacing in `drawing.Size(w, h)` | `layout.Padding = drawing.Padding(10); table.Spacing = drawing.Size(5,5)` |
| HIGH | eto_stacklayoutitem_wrap | Wrap controls in `forms.StackLayoutItem()` for StackLayout | `stack.Items.Add(forms.StackLayoutItem(button))` |
| HIGH | eto_tablerow_tablecell | Build TableLayout with `TableRow()` + `TableCell()` | `forms.TableRow(forms.TableCell(label), forms.TableCell(textbox))` |
| HIGH | eto_dynamiclayout_bracketing | `BeginVertical()/BeginHorizontal()` before, `End*()` after | `layout.BeginVertical(); layout.AddRow(label, tb); layout.EndVertical()` |
| HIGH | eto_customcell_configure_not_create | Use `ConfigureCell` for per-row data, not `CreateCell` | `def ConfigureCell(self, args): args.Cell.FindChild...` |
| HIGH | eto_treegridview_datastore_after_columns | Set DataStore after all columns and items are configured | `tree.Columns.Add(col1); tree.DataStore = collection` |
| HIGH | eto_override_events_call_base | Call `base.<EventName>(e)` in overrides or subscribers stop working | `def OnClick(self, e): base.OnClick(e)` |
| MEDIUM | eto_extensions_static_helpers | `UseRhinoStyle`, `PushPickButton` are static — not instance methods | `Rhino.UI.EtoExtensions.UseRhinoStyle(form)` |
| MEDIUM | eto_keydown_handled | Set `e.Handled = True` in KeyDown to get KeyUp | `def on_key_down(sender, e): e.Handled = True` |
| MEDIUM | eto_drawable_invalidate | Call `drawable.Invalidate(True)` after state change to repaint | `self.state = new; self.Invalidate(True)` |

---

## Compute and Hops

Context: `compute`

| Priority | ID | Rule | Example |
|----------|-----|------|---------|
| CRITICAL | compute_rhino3d_underscore_import | Package name is `compute_rhino3d` (underscore, not dash) | `import compute_rhino3d` |
| CRITICAL | ghhops_server_separate_install | Install both `ghhops_server` (pip) and Hops GH component | `pip install ghhops_server` |
| CRITICAL | hops_get_components_for_inputs | Use Get components for inputs, Context Bake/Print for outputs | `GetNumber -> [logic] -> ContextBake` |
| CRITICAL | macos_no_local_compute | macOS: use remote Windows Compute or local Python ghhops_server | `ghhops_server.HopsFlask.run()` |
| CRITICAL | rhino_compute_key_header | Include `RhinoComputeKey` in HTTP request headers | `headers = {'RhinoComputeKey': api_key}` |
| HIGH | hops_at_most_controls_access | AtMost=1 is Item Access; AtMost>1 or unset is List Access | `GetNumber.AtMost = 1` |
| HIGH | hops_tree_access_supersedes_at_most | Tree Access checkbox overrides AtMost value | `GetNumber.TreeAccess = True` |
| HIGH | hops_file_path_for_non_standard_types | Serialize non-standard types to file, pass path as string | `GetString -> file_path -> deserialize` |
| MEDIUM | hops_data_tree_parallel_per_branch | Data tree input launches parallel compute per branch | `tree{0} -> branch0; tree{1} -> branch1` |

---

## Geometry Safety

Context: `any` / `rhino_script`

| Priority | ID | Rule | Example |
|----------|-----|------|---------|
| CRITICAL | null_check_getobject_geometry | Null-check `.Curve()` / `.Brep()` from GetObject | `crv = objref.Curve(); if crv is None: return` |
| CRITICAL | check_getobject_getpoint_result | Check CommandResult or None before proceeding | `if go.CommandResult() != Result.Success: return` |
| CRITICAL | boolean_op_tolerance_and_none | Pass `doc.ModelAbsoluteTolerance`; check for None/empty | `breps = Brep.CreateBooleanDifference(a, b, tol); if not breps: return` |
| CRITICAL | nurbs_surface_order_not_degree | `NurbsSurface.Create()` takes order (degree+1), not degree | `NurbsSurface.Create(3, False, deg+1, deg+1, u_count, v_count)` |
| HIGH | primitive_to_brep_conversion | `Cylinder.ToBrep(True, True)`, `Torus.ToRevSurface()` | `brep = cylinder.ToBrep(True, True)` |
| HIGH | mesh_from_brep_append | `Mesh.CreateFromBrep()` returns array — Append into one mesh | `[combined.Append(m) for m in meshes]` |
| HIGH | mesh_compute_normals_compact | Call `ComputeNormals()` + `Compact()` on scratch meshes | `mesh.Normals.ComputeNormals(); mesh.Compact()` |
| HIGH | controlpoint_wrapping | Wrap Point3d in `ControlPoint()` for `SetControlPoint` | `srf.Points.SetControlPoint(i, j, ControlPoint(pt))` |
| HIGH | brepface_orientation_reversed | Check `OrientationIsReversed`, call `.Reverse()` if True | `if face.OrientationIsReversed: n.Reverse()` |
| HIGH | rs_coerce_curve_brep | Use `rs.coercecurve()` / `rs.coercebrep()` to bridge GUID↔geometry | `crv = rs.coercecurve(curve_id)` |
| HIGH | sweep_one_rail_tolerances | Set tolerances before `PerformSweep()` | `sweep.SweepTolerance = doc.ModelAbsoluteTolerance` |
| HIGH | escape_test_in_loops | `scriptcontext.escape_test(False)` in long loops | `for i in range(100000): sc.escape_test(False)` |
| MEDIUM | vector_direction_point_order | `p2 - p1` gives vector FROM p1 TO p2 | `direction = Point3d(5,0,0) - Point3d(0,0,0)` |
| MEDIUM | loft_point3d_unset | Pass `Point3d.Unset` for loft start/end when unneeded | `Brep.CreateFromLoft(curves, Point3d.Unset, Point3d.Unset, ...)` |
| MEDIUM | unselect_all_between_selections | `doc.Objects.UnselectAll()` between two selection prompts | `sc.doc.Objects.UnselectAll()` |

---

## RIR Context and Transactions

Context: `gh_component_rir`, `revit_api`

| Priority | ID | Rule | Example |
|----------|-----|------|---------|
| CRITICAL | rir_use_revit_static_context | Get context from `Revit.ActiveUIApplication` etc. | `doc = Revit.ActiveDBDocument` |
| CRITICAL | revit_write_requires_transaction | Wrap creates/deletes/modifies in `DB.Transaction` | `t = DB.Transaction(doc, 'Op'); t.Start(); t.Commit()` |
| CRITICAL | family_symbol_activate_then_regenerate | `Activate()` + `doc.Regenerate()` before placement | `if not symbol.IsActive: symbol.Activate(); doc.Regenerate()` |
| HIGH | batch_revit_writes_in_single_transaction | One transaction for batch operations | `t.Start(); [place_many]; t.Commit()` |
| MEDIUM | rir_alias_revit_namespaces | Alias as `DB` and `UI` for clarity | `from Autodesk.Revit import DB, UI` |
| MEDIUM | revit_read_only_no_transaction | Don't open transactions for read-only queries | `walls = DB.FilteredElementCollector(doc).OfClass(DB.Wall)` |

---

## Revit Parameters and Units

Context: `gh_component_rir`, `revit_api`

| Priority | ID | Rule | Example |
|----------|-----|------|---------|
| CRITICAL | parameter_exists_before_access | Null-check `LookupParameter()` before use | `param = element.LookupParameter(name); if param is None: return` |
| CRITICAL | parameter_read_only_check_before_set | Check `IsReadOnly` before `Set()` | `if param and not param.IsReadOnly: param.Set(value)` |
| CRITICAL | branch_on_parameter_storage_type | Match StorageType to correct Set payload | `if param.StorageType == DB.StorageType.String: param.Set(text)` |
| CRITICAL | double_parameters_use_internal_units | Convert to internal units before `Set()` | `feet = DB.UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters)` |
| HIGH | elementid_parameters_need_elementid | Use `DB.ElementId`, not name or raw integer | `param.Set(DB.ElementId(type_id.IntegerValue))` |
| HIGH | double_parameter_reads_need_conversion | Convert from internal units when reading | `mm = DB.UnitUtils.ConvertFromInternalUnits(param.AsDouble(), ...)` |
| HIGH | instance_vs_type_parameter_lookup | If missing on instance, check type element | `type_elem = doc.GetElement(element.GetTypeId())` |

---

## Revit Families and Instances

Context: `gh_component_rir`, `revit_api`

| Priority | ID | Rule | Example |
|----------|-----|------|---------|
| CRITICAL | place_familysymbol_not_family | Pass `FamilySymbol`, not `Family`, to `NewFamilyInstance` | `doc.Create.NewFamilyInstance(xyz, symbol, level, StructuralType.NonStructural)` |
| CRITICAL | hosted_families_require_host_context | Host-based families need host element in overload | `doc.Create.NewFamilyInstance(xyz, symbol, host, level, ...)` |
| CRITICAL | activate_symbol_before_first_use | `Activate()` before creating instances | `if not symbol.IsActive: symbol.Activate(); doc.Regenerate()` |
| HIGH | collect_symbols_for_exact_type | Match both family name and symbol name | `DB.FilteredElementCollector(doc).OfClass(DB.FamilySymbol)` |
| HIGH | level_aware_overload_for_level_based_families | Include target Level in overload | `NewFamilyInstance(xyz, symbol, level, StructuralType.NonStructural)` |
| HIGH | type_parameters_on_symbol_not_instance | Write shared type params on `FamilySymbol` | `symbol.LookupParameter('Width').Set(width)` |
| MEDIUM | set_instance_params_after_create | Create instance first, then set params | `inst = doc.Create.NewFamilyInstance(...); inst.LookupParameter('Mark').Set(v)` |

---

## Revit Geometry Conversion

Context: `gh_component_rir`

| Priority | ID | Rule | Example |
|----------|-----|------|---------|
| CRITICAL | use_rir_geometry_conversion_helpers | Use RIR helpers, not manual coord copying | `xyz = point3d.ToXYZ(); curve = revit_curve.ToCurve()` |
| CRITICAL | validate_geometry_before_revit_encode | Check closure, planarity, IsValid first | `if not curve.IsClosed or not curve.IsValid: return` |
| CRITICAL | revit_creation_needs_revit_db_types | Pass exact Revit DB types, not Rhino objects | `curve_loop = DB.CurveLoop(); curve_loop.Append(rh_crv.ToCurve())` |
| HIGH | convert_at_boundary_once | Keep Rhino types until final write step | `db_curves = [c.ToCurve() for c in rh_curves]` |
| HIGH | directshape_requires_valid_category_and_shape | Valid category ID + converted shape | `DB.DirectShape.CreateElement(doc, DB.ElementId(OST_GenericModel))` |
| MEDIUM | decode_revit_geometry_for_preview_not_mutation | Keep decoded geometry separate from write payloads | `preview_brep = revit_solid.ToBrep()` |

---

## Revit Host Troubleshooting

Context: `revit_host`

| Priority | ID | Rule | Example |
|----------|-----|------|---------|
| CRITICAL | diagnose_missing_tab_from_addin_manifest | Inspect `.addin` manifest path first | Check `userpc_revit_addins/<year>/` |
| CRITICAL | opennurbs_conflict_load_order | Restart Revit, load RIR first | Restart Revit, launch RIR before 3DM content |
| HIGH | gh_addons_missing_check_rhino_history | Check Rhino command history (F2) | Open Rhino in Revit, press F2 |
| HIGH | network_loaded_gha_needs_loadfromremotesources | Check `Revit.exe.config` for `loadFromRemoteSources` | `<loadFromRemoteSources enabled="true"/>` |
| HIGH | error_200_or_json_is_plugin_conflict | Inspect add-in manifests for DLL conflicts | Review machine/user addin manifests |
| HIGH | older_pyrevit_can_conflict_with_rir | Verify pyRevit version/loader DLL | Compare pyRevit.addin loader path vs installed version |
