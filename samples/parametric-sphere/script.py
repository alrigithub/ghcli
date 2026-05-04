#! python 3
"""Parametric sphere generator for a Grasshopper CPython 3 node.

Inputs:
    radius  : float
    u_count : int
    v_count : int

Outputs:
    sphere     : Brep
    iso_curves : list[Curve]
    pts        : list[Point3d]
    dbg        : str
"""

import math
import Rhino.Geometry as rg

debug_lines = []


def log(message):
    debug_lines.append(str(message))


try:
    if radius is None:
        radius = 5.0
    radius = float(radius)
    if radius <= 0.0:
        radius = 5.0

    if u_count is None:
        u_count = 12
    if v_count is None:
        v_count = 6
    u_count = max(3, int(round(float(u_count))))
    v_count = max(2, int(round(float(v_count))))

    center = rg.Point3d(0.0, 0.0, 0.0)
    rg_sphere = rg.Sphere(center, radius)
    sphere = rg_sphere.ToBrep()

    iso_curves = []
    pts = []

    # Longitude curves.
    for i in range(u_count):
        angle = (2.0 * math.pi * i) / u_count
        x_axis = rg.Vector3d(math.cos(angle), math.sin(angle), 0.0)
        plane = rg.Plane(center, x_axis, rg.Vector3d.ZAxis)
        iso_curves.append(rg.Circle(plane, radius).ToNurbsCurve())

    # Latitude curves, excluding the poles.
    for j in range(1, v_count):
        t = -0.5 * math.pi + (math.pi * j) / v_count
        z = radius * math.sin(t)
        lat_radius = radius * math.cos(t)
        plane = rg.Plane(rg.Point3d(0.0, 0.0, z), rg.Vector3d.XAxis, rg.Vector3d.YAxis)
        iso_curves.append(rg.Circle(plane, lat_radius).ToNurbsCurve())

    for j in range(v_count + 1):
        theta = -0.5 * math.pi + (math.pi * j) / v_count
        for i in range(u_count):
            phi = (2.0 * math.pi * i) / u_count
            pts.append(
                rg.Point3d(
                    radius * math.cos(theta) * math.cos(phi),
                    radius * math.cos(theta) * math.sin(phi),
                    radius * math.sin(theta),
                )
            )

    log("radius: {:.3f}".format(radius))
    log("u_count: {}".format(u_count))
    log("v_count: {}".format(v_count))
    log("curves: {}".format(len(iso_curves)))
    log("points: {}".format(len(pts)))
except Exception as exc:
    sphere = None
    iso_curves = []
    pts = []
    log("ERROR: {}".format(exc))

dbg = "\n".join(debug_lines)
