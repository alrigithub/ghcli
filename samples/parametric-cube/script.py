#! python 3
"""Parametric cube generator for a Grasshopper CPython 3 node."""

import Rhino.Geometry as rg

debug_lines = []


def log(message):
    debug_lines.append(str(message))


try:
    if size is None:
        size = 6.0
    size = max(0.01, float(size))

    if count is None:
        count = 1
    count = max(1, int(round(float(count))))

    if gap is None:
        gap = size * 0.25
    gap = max(0.0, float(gap))

    cubes = []
    centers = []
    edges = []
    step = size + gap
    start = -0.5 * step * (count - 1)

    for i in range(count):
        center = rg.Point3d(start + step * i, 0.0, size * 0.5)
        centers.append(center)

        plane = rg.Plane(center, rg.Vector3d.XAxis, rg.Vector3d.YAxis)
        box = rg.Box(
            plane,
            rg.Interval(-size * 0.5, size * 0.5),
            rg.Interval(-size * 0.5, size * 0.5),
            rg.Interval(-size * 0.5, size * 0.5),
        )
        brep = box.ToBrep()
        cubes.append(brep)
        edges.extend(brep.DuplicateEdgeCurves())

    log("size: {:.3f}".format(size))
    log("count: {}".format(count))
    log("gap: {:.3f}".format(gap))
    log("cubes: {}".format(len(cubes)))
    log("edge curves: {}".format(len(edges)))
except Exception as exc:
    cubes = []
    centers = []
    edges = []
    log("ERROR: {}".format(exc))

dbg = "\n".join(debug_lines)
