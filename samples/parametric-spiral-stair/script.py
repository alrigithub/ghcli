#! python 3
"""Parametric spiral stair generator for a Grasshopper CPython 3 node."""

import math
import Rhino.Geometry as rg

debug_lines = []


def log(message):
    debug_lines.append(str(message))


def clamp(value, minimum, fallback):
    if value is None:
        return fallback
    try:
        value = float(value)
    except Exception:
        return fallback
    return max(minimum, value)


try:
    step_count = int(round(clamp(step_count, 3, 18)))
    radius = clamp(radius, 0.5, 6.0)
    total_height = clamp(total_height, 0.5, 8.0)
    turns = clamp(turns, 0.1, 1.35)
    tread_depth = clamp(tread_depth, 0.1, 1.25)
    tread_width = clamp(tread_width, 0.1, 2.4)
    thickness = clamp(thickness, 0.05, 0.28)

    treads = []
    post_lines = []
    rail_points = []
    center_points = []

    angle_total = 2.0 * math.pi * turns
    step_rise = total_height / max(1, step_count - 1)
    rail_height = 1.1

    for i in range(step_count):
        t = i / max(1.0, step_count - 1.0)
        angle = angle_total * t
        z = step_rise * i

        radial = rg.Vector3d(math.cos(angle), math.sin(angle), 0.0)
        tangent = rg.Vector3d(-math.sin(angle), math.cos(angle), 0.0)
        center = rg.Point3d(radius * radial.X, radius * radial.Y, z)
        center_points.append(center)

        plane = rg.Plane(center, radial, tangent)
        box = rg.Box(
            plane,
            rg.Interval(-tread_depth * 0.25, tread_depth * 0.75),
            rg.Interval(-tread_width * 0.5, tread_width * 0.5),
            rg.Interval(-thickness * 0.5, thickness * 0.5),
        )
        treads.append(box.ToBrep())

        outer = center + radial * (tread_depth * 0.75)
        post_top = rg.Point3d(outer.X, outer.Y, z + rail_height)
        post_lines.append(rg.LineCurve(outer, post_top))
        rail_points.append(post_top)

    centerline = None
    if len(center_points) >= 2:
        centerline = rg.Polyline(center_points).ToNurbsCurve()

    handrail = None
    if len(rail_points) >= 2:
        handrail = rg.Curve.CreateInterpolatedCurve(rail_points, 3)
        if handrail is None:
            handrail = rg.Polyline(rail_points).ToNurbsCurve()

    log("step_count: {}".format(step_count))
    log("radius: {:.3f}".format(radius))
    log("height: {:.3f}".format(total_height))
    log("turns: {:.3f}".format(turns))
    log("rise per step: {:.3f}".format(step_rise))
    log("treads: {}".format(len(treads)))
    log("posts: {}".format(len(post_lines)))
except Exception as exc:
    treads = []
    post_lines = []
    centerline = None
    handrail = None
    log("ERROR: {}".format(exc))

dbg = "\n".join(debug_lines)
