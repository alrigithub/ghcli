#! python 3
"""Template for a Grasshopper CPython 3 node created through graph.apply.

Inputs are variables named by the node schema.
Outputs are assigned by setting variables with matching output names.
"""

import Rhino
import Rhino.Geometry as rg
import System

debug_lines = []


def log(message):
    debug_lines.append(str(message))


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


try:
    if count is None:
        count = 1
    count = max(1, int(count))

    result = []
    log("count: {}".format(count))
except Exception as exc:
    result = []
    log("ERROR: {}".format(exc))

dbg = "\n".join(debug_lines)
