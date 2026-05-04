# Eto UI Reference

Rules for building cross-platform UI dialogs and forms in Rhino/Grasshopper using the Eto.Forms framework.

---

## Required imports

Every Eto dialog needs all three:

```python
import Rhino.UI
import Eto.Drawing as drawing
import Eto.Forms as forms
```

---

## Dialog lifecycle

### Creating a dialog

```python
class MyDialog(forms.Dialog):
    def __init__(self):
        super().__init__()  # MUST be first line
        self.Title = "My Dialog"
        self.Padding = drawing.Padding(10)
        # ... build layout ...
```

### Showing dialogs

| Mode | Method | Notes |
|------|--------|-------|
| Modal | `dialog.ShowModal(parent)` | Parent = `RhinoEtoApp.MainWindowForDocument(sc.doc)` (Mac-safe) |
| Non-modal (Form) | `EtoExtensions.Show(form, sc.doc)` | Static method call |
| Semi-modal | `EtoExtensions.ShowSemiModal(dialog, sc.doc, parent)` | Static method call |

Python cannot call C# extension methods as instance methods — always use the static `EtoExtensions.*` form.

**Never use** `RhinoEtoApp.MainWindow` as parent — it fails on Mac. Always use `MainWindowForDocument(sc.doc)`.

### OK/Cancel with keyboard support

```python
self.DefaultButton = ok_btn     # Enter key
self.AbortButton = cancel_btn   # Escape key
# For Dialog[bool]:
self.Close(True)   # OK
self.Close(False)   # Cancel
# Result is returned by ShowModal()
```

`DefaultButton` and `AbortButton` must be assigned before `ShowSemiModal`.

---

## Layout patterns

### DynamicLayout (most flexible)

```python
layout = forms.DynamicLayout()
layout.Padding = drawing.Padding(10)
layout.Spacing = drawing.Size(5, 5)
layout.BeginVertical()
layout.AddRow(label, textbox)
layout.AddRow(None, button)  # None = spacer
layout.EndVertical()
self.Content = layout
```

Always bracket with `BeginVertical()`/`EndVertical()` or `BeginHorizontal()`/`EndHorizontal()`.

### TableLayout

```python
table = forms.TableLayout()
table.Spacing = drawing.Size(5, 5)  # Must be drawing.Size, not bare int
table.Rows.Add(forms.TableRow(
    forms.TableCell(label),
    forms.TableCell(textbox)
))
```

### StackLayout

```python
stack = forms.StackLayout()
stack.Items.Add(forms.StackLayoutItem(button))  # Must wrap in StackLayoutItem
```

### Important: type wrappers

- `Padding` → `drawing.Padding(n)` (not bare int)
- `Spacing` → `drawing.Size(w, h)` (not bare int)
- StackLayout items → `forms.StackLayoutItem(control)`
- TableLayout → `forms.TableRow(forms.TableCell(control))`

---

## GridView

```python
from System.Collections.ObjectModel import ObservableCollection

grid = forms.GridView()
grid.DataStore = ObservableCollection[object](initial_items)

# To update data — NEVER replace the collection instance:
grid.DataStore.Clear()
for item in new_items:
    grid.DataStore.Add(item)
```

### CustomCell

- `CreateCell` is called once per column (cells are reused) — don't set data here
- `ConfigureCell` is called per row — set per-row data here

---

## TreeGridView

Set `DataStore` **after** all columns and items are configured:

```python
tree.Columns.Add(col1)
tree.Columns.Add(col2)
tree.DataStore = collection  # Last
```

---

## Events

- When overriding events in subclasses, call `base.<EventName>(e)` or external `+=` subscribers stop working
- In `KeyDown` handlers, set `e.Handled = True` if you want `KeyUp` to fire
- For `Drawable`, call `self.Invalidate(True)` after any state change to trigger repaint

---

## Python-specific limitations

- **No ViewModel/Binding** — use `ObservableCollection` for list data and manual property updates
- **Extension methods are static** — `EtoExtensions.UseRhinoStyle(form)`, `EtoExtensions.PushPickButton(dialog, cb)`
