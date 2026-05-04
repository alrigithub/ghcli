#! python 3
"""Spiral stair generator: individual tread top surfaces only.

Units: millimetres.
"""

import math
import Rhino.Geometry as rg

debug_lines = []
warnings = []


def log(message):
    debug_lines.append(str(message))


def as_float(value, fallback):
    if value is None:
        return fallback
    try:
        return float(value)
    except Exception:
        return fallback


def point_at(radius, angle, z):
    return rg.Point3d(radius * math.cos(angle), radius * math.sin(angle), z)


def arc_curve(radius, start_angle, sweep_angle, z):
    center = rg.Point3d(0.0, 0.0, z)
    xaxis = rg.Vector3d(math.cos(start_angle), math.sin(start_angle), 0.0)
    yaxis = rg.Vector3d(-math.sin(start_angle), math.cos(start_angle), 0.0)
    plane = rg.Plane(center, xaxis, yaxis)
    return rg.Arc(plane, radius, sweep_angle).ToNurbsCurve()


try:
    inner_radius = max(0.0, as_float(inner_radius, 200.0))
    outer_radius = max(inner_radius + 1.0, as_float(outer_radius, 3000.0))
    level_height = max(1.0, as_float(level_height, 2500.0))
    tread_depth = max(1.0, as_float(tread_depth, 260.0))
    comfort_target = as_float(comfort_target, 620.0)

    mid_radius = (inner_radius + outer_radius) / 2.0
    initial_riser = (comfort_target - tread_depth) / 2.0
    if initial_riser <= 0.0:
        warnings.append("comfort_target <= tread_depth; using one step fallback")
        initial_riser = level_height

    num_steps = max(1, int(round(level_height / initial_riser)))
    actual_riser = level_height / num_steps
    angle_per_step = tread_depth / mid_radius
    total_rotation = angle_per_step * num_steps
    total_rotation_deg = math.degrees(total_rotation)
    comfort_check = 2.0 * actual_riser + tread_depth

    if comfort_check < 600.0 or comfort_check > 650.0:
        warnings.append(
            "comfort_check {:.2f}mm outside 600-650mm range".format(comfort_check)
        )

    tread_surfaces = []
    tolerance = 0.01

    for i in range(num_steps):
        start_angle = i * angle_per_step
        end_angle = (i + 1) * angle_per_step
        z_top = (i + 1) * actual_riser

        p0 = point_at(inner_radius, start_angle, z_top)
        p1 = point_at(outer_radius, start_angle, z_top)
        p2 = point_at(outer_radius, end_angle, z_top)
        p3 = point_at(inner_radius, end_angle, z_top)

        curves = [
            rg.LineCurve(p0, p1),
            arc_curve(outer_radius, start_angle, angle_per_step, z_top),
            rg.LineCurve(p2, p3),
        ]

        if inner_radius > tolerance:
            inner = arc_curve(inner_radius, start_angle, angle_per_step, z_top)
            inner.Reverse()
            curves.append(inner)
        else:
            curves.append(rg.LineCurve(p3, p0))

        joined = rg.Curve.JoinCurves(curves, tolerance)
        if joined and len(joined) > 0:
            breps = rg.Brep.CreatePlanarBreps(joined[0], tolerance)
            if breps and len(breps) > 0:
                tread_surfaces.append(breps[0])
                continue

        fallback = rg.Brep.CreateFromCornerPoints(p0, p1, p2, p3, tolerance)
        if fallback is not None:
            tread_surfaces.append(fallback)
        else:
            warnings.append("failed to create tread surface {}".format(i))

    log("units: mm")
    log("inner_radius: {:.2f}".format(inner_radius))
    log("outer_radius: {:.2f}".format(outer_radius))
    log("mid_radius: {:.2f}".format(mid_radius))
    log("level_height: {:.2f}".format(level_height))
    log("tread_depth: {:.2f}".format(tread_depth))
    log("comfort_target: {:.2f}".format(comfort_target))
    log("num_steps: {}".format(num_steps))
    log("actual_riser: {:.2f}".format(actual_riser))
    log("comfort_check: {:.2f}".format(comfort_check))
    log("angle_per_step_deg: {:.2f}".format(math.degrees(angle_per_step)))
    log("total_rotation_deg: {:.2f}".format(total_rotation_deg))
    log("tread_surfaces: {}".format(len(tread_surfaces)))
    if warnings:
        log("warnings: {}".format("; ".join(warnings)))
except Exception as exc:
    tread_surfaces = []
    num_steps = 0
    actual_riser = 0.0
    total_rotation_deg = 0.0
    comfort_check = 0.0
    log("ERROR: {}".format(exc))

dbg = "\n".join(debug_lines)
