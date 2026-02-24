#!/usr/bin/env python3
"""Analyze a binary or ASCII STL file and report geometry details.

Usage: python scripts/analyze-stl.py <file.stl>

Reports:
  - Dimensions (bounding box)
  - Triangle count
  - Z-level distribution (layer structure)
  - Face normal analysis (flat/vertical/angled)
  - Cross-section analysis at key Z levels
  - ASCII cross-section visualizations
  - Floor/ceiling surface detection (pocket/void identification)
  - Internal void/pocket detection
"""

import struct
import math
import sys
import os
from collections import defaultdict


def read_binary_stl(filepath):
    """Read a binary STL and return (header, triangles).
    Each triangle is (normal, v1, v2, v3) as tuples of (x,y,z)."""
    triangles = []
    with open(filepath, "rb") as f:
        header = f.read(80)
        num_tri = struct.unpack("<I", f.read(4))[0]

        for _ in range(num_tri):
            data = f.read(50)
            if len(data) < 50:
                break
            vals = struct.unpack("<12fH", data)
            normal = (vals[0], vals[1], vals[2])
            v1 = (vals[3], vals[4], vals[5])
            v2 = (vals[6], vals[7], vals[8])
            v3 = (vals[9], vals[10], vals[11])
            triangles.append((normal, v1, v2, v3))

    return header, triangles


def is_ascii_stl(filepath):
    """Check if an STL file is ASCII format."""
    with open(filepath, "rb") as f:
        start = f.read(80).lstrip()
        return start.startswith(b"solid") and b"\x00" not in start


def read_ascii_stl(filepath):
    """Read an ASCII STL and return (header, triangles)."""
    triangles = []
    with open(filepath, "r") as f:
        lines = f.readlines()

    header = lines[0].strip().encode() if lines else b""
    normal = None
    verts = []

    for line in lines:
        line = line.strip()
        if line.startswith("facet normal"):
            parts = line.split()
            normal = (float(parts[2]), float(parts[3]), float(parts[4]))
            verts = []
        elif line.startswith("vertex"):
            parts = line.split()
            verts.append((float(parts[1]), float(parts[2]), float(parts[3])))
        elif line.startswith("endfacet") and normal and len(verts) == 3:
            triangles.append((normal, verts[0], verts[1], verts[2]))

    return header, triangles


def ascii_cross_section(verts_2d, width, height, grid_res=2.0, label=""):
    """Generate ASCII art cross-section from 2D vertex coordinates."""
    if not verts_2d:
        return []

    min_x = min(v[0] for v in verts_2d)
    min_y = min(v[1] for v in verts_2d)

    cols = int(width / grid_res) + 2
    rows = int(height / grid_res) + 2

    grid = [[' '] * cols for _ in range(rows)]

    for x, y in verts_2d:
        c = int((x - min_x) / grid_res)
        r = int((y - min_y) / grid_res)
        if 0 <= r < rows and 0 <= c < cols:
            grid[r][c] = '#'

    lines = []
    if label:
        lines.append(label)
    for r in range(rows - 1, -1, -1):
        y_val = min_y + r * grid_res
        line = ''.join(grid[r])
        if '#' in line:
            lines.append(f"  y={y_val:6.1f} |{line}|")

    return lines


def detect_surfaces(triangles, min_y):
    """Detect floor (top-facing) and ceiling (bottom-facing) surfaces."""
    top_faces = defaultdict(lambda: defaultdict(int))
    bot_faces = defaultdict(lambda: defaultdict(int))

    for tri in triangles:
        avg_z = round((tri[1][2] + tri[2][2] + tri[3][2]) / 3, 1)
        avg_y = (tri[1][1] + tri[2][1] + tri[3][1]) / 3 - min_y

        if tri[0][2] > 0.9:
            top_faces[avg_z]["total"] += 1
        elif tri[0][2] < -0.9:
            bot_faces[avg_z]["total"] += 1

    return top_faces, bot_faces


def detect_pockets(all_verts, min_x, min_y, z_target, tol=0.3, grid_size=2.0):
    """Detect internal voids/pockets at a given Z level."""
    verts_2d = [(v[0] - min_x, v[1] - min_y) for v in all_verts
                if abs(v[2] - z_target) < tol]
    if len(verts_2d) < 10:
        return None

    grid = {}
    for x, y in verts_2d:
        gx = int(x / grid_size)
        gy = int(y / grid_size)
        grid[(gx, gy)] = grid.get((gx, gy), 0) + 1

    all_gx = [k[0] for k in grid.keys()]
    all_gy = [k[1] for k in grid.keys()]
    if not all_gx:
        return None

    gx_range = range(min(all_gx), max(all_gx) + 1)
    gy_range = range(min(all_gy), max(all_gy) + 1)

    empty_interior = []
    for gx in gx_range:
        for gy in gy_range:
            if (gx, gy) not in grid or grid[(gx, gy)] < 3:
                neighbors = sum(1 for dx in [-1, 0, 1] for dy in [-1, 0, 1]
                                if (gx + dx, gy + dy) in grid
                                and grid[(gx + dx, gy + dy)] >= 3)
                if neighbors >= 4:
                    rx = gx * grid_size + grid_size / 2
                    ry = gy * grid_size + grid_size / 2
                    empty_interior.append((rx, ry))

    if not empty_interior:
        return None

    ex = [p[0] for p in empty_interior]
    ey = [p[1] for p in empty_interior]
    return {
        "count": len(empty_interior),
        "x_range": (min(ex), max(ex)),
        "y_range": (min(ey), max(ey)),
        "width": max(ex) - min(ex),
        "height": max(ey) - min(ey),
    }


def analyze(filepath):
    """Analyze an STL file and print a comprehensive report."""
    filesize = os.path.getsize(filepath)

    if is_ascii_stl(filepath):
        header, triangles = read_ascii_stl(filepath)
        fmt = "ASCII"
    else:
        header, triangles = read_binary_stl(filepath)
        fmt = "Binary"

    if not triangles:
        print("Error: No triangles found in file.")
        return

    # Bounding box
    all_verts = [v for t in triangles for v in t[1:]]
    xs = [v[0] for v in all_verts]
    ys = [v[1] for v in all_verts]
    zs = [v[2] for v in all_verts]

    min_x, max_x = min(xs), max(xs)
    min_y, max_y = min(ys), max(ys)
    min_z, max_z = min(zs), max(zs)
    w, h, d = max_x - min_x, max_y - min_y, max_z - min_z

    print(f"File: {os.path.basename(filepath)}")
    print(f"Format: {fmt}")
    print(f"Size: {filesize:,} bytes")
    print(f"Triangles: {len(triangles):,}")
    print()
    print(f"Bounding box:")
    print(f"  X: {min_x:.2f} to {max_x:.2f}  (width:  {w:.2f} mm)")
    print(f"  Y: {min_y:.2f} to {max_y:.2f}  (height: {h:.2f} mm)")
    print(f"  Z: {min_z:.2f} to {max_z:.2f}  (depth:  {d:.2f} mm)")
    print(f"  Center: ({(min_x+max_x)/2:.2f}, {(min_y+max_y)/2:.2f}, "
          f"{(min_z+max_z)/2:.2f})")
    print()

    # Face normal analysis
    top = bottom = vertical = angled = 0
    for tri in triangles:
        nz = tri[0][2]
        if nz > 0.9:
            top += 1
        elif nz < -0.9:
            bottom += 1
        elif abs(nz) < 0.1:
            vertical += 1
        else:
            angled += 1

    print(f"Face normals:")
    print(f"  Top-facing (flat):    {top:6d}  ({100*top/len(triangles):.0f}%)")
    print(f"  Bottom-facing (flat): {bottom:6d}  "
          f"({100*bottom/len(triangles):.0f}%)")
    print(f"  Vertical (walls):     {vertical:6d}  "
          f"({100*vertical/len(triangles):.0f}%)")
    print(f"  Angled (curves/bevel):{angled:6d}  "
          f"({100*angled/len(triangles):.0f}%)")
    print()

    # Z-level distribution
    z_hist = {}
    for v in all_verts:
        zr = round(v[2], 1)
        z_hist[zr] = z_hist.get(zr, 0) + 1

    max_count = max(z_hist.values())
    bar_scale = 40 / max_count if max_count > 0 else 1

    print(f"Z-level distribution ({len(z_hist)} distinct levels):")
    for z in sorted(z_hist.keys()):
        bar = "#" * int(z_hist[z] * bar_scale)
        print(f"  z={z:6.1f}: {z_hist[z]:6d} {bar}")
    print()

    # Cross-section analysis at multiple Z levels
    z_range = max_z - min_z
    z_levels = sorted(set([
        min_z,
        min_z + z_range * 0.1,
        min_z + z_range * 0.25,
        min_z + z_range * 0.5,
        min_z + z_range * 0.75,
        min_z + z_range * 0.9,
        max_z,
    ]))

    def analyze_slice(target_z, tolerance=0.25):
        verts = [(v[0], v[1]) for v in all_verts
                 if abs(v[2] - target_z) < tolerance]
        if len(verts) < 10:
            return None

        sx = [v[0] for v in verts]
        sy = [v[1] for v in verts]
        cx = (min(sx) + max(sx)) / 2
        cy = (min(sy) + max(sy)) / 2
        dists = [math.sqrt((x - cx) ** 2 + (y - cy) ** 2) for x, y in verts]
        avg_r = sum(dists) / len(dists)
        std_r = (sum((dd - avg_r) ** 2 for dd in dists) / len(dists)) ** 0.5
        variation = std_r / avg_r if avg_r > 0 else 0

        if variation < 0.1:
            shape = "circular"
        elif variation < 0.25:
            shape = "roughly circular/oval"
        else:
            shape = "irregular"

        return {
            "verts": len(verts),
            "x_range": (min(sx), max(sx)),
            "y_range": (min(sy), max(sy)),
            "width": max(sx) - min(sx),
            "height": max(sy) - min(sy),
            "center": (cx, cy),
            "avg_radius": avg_r,
            "variation": variation,
            "shape": shape,
        }

    print("Cross-section analysis:")
    for z_level in z_levels:
        info = analyze_slice(z_level)
        if info:
            print(f"  z={z_level:.1f}: {info['shape']} "
                  f"({info['width']:.1f} x {info['height']:.1f} mm, "
                  f"r~{info['avg_radius']:.1f} mm, "
                  f"variation={info['variation']:.3f}, "
                  f"pts={info['verts']})")
        else:
            print(f"  z={z_level:.1f}: insufficient data")
    print()

    # ASCII cross-sections at key Z levels
    print("ASCII cross-sections (# = geometry present):")
    ascii_z_levels = [min_z + 0.2, min_z + z_range * 0.25,
                      min_z + z_range * 0.5,
                      min_z + z_range * 0.75, max_z - 0.2]
    for z_target in ascii_z_levels:
        tol = max(0.25, z_range * 0.03)
        verts_2d = [(v[0], v[1]) for v in all_verts
                    if abs(v[2] - z_target) < tol]
        if len(verts_2d) < 10:
            continue
        lines = ascii_cross_section(verts_2d, w, h, grid_res=2.0,
                                    label=f"\n  --- z={z_target:.1f}mm ---")
        for line in lines:
            print(f"  {line}")
    print()

    # Floor/ceiling surface detection
    top_faces, bot_faces = detect_surfaces(triangles, min_y)

    print("Floor surfaces (top-facing flat faces by Z level):")
    for z in sorted(top_faces.keys()):
        count = top_faces[z]["total"]
        if count > 5:
            print(f"  z={z:5.1f}: {count:4d} faces")
    print()

    print("Ceiling surfaces (bottom-facing flat faces by Z level):")
    for z in sorted(bot_faces.keys()):
        count = bot_faces[z]["total"]
        if count > 5:
            print(f"  z={z:5.1f}: {count:4d} faces")
    print()

    # Pocket/void detection
    print("Pocket/void detection:")
    pocket_z_levels = [min_z + z_range * f for f in
                       [0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9]]
    found_pockets = False
    for z_target in pocket_z_levels:
        pocket = detect_pockets(all_verts, min_x, min_y, z_target)
        if pocket:
            found_pockets = True
            print(f"  z={z_target:.1f}: {pocket['count']} void cells, "
                  f"region: {pocket['width']:.0f} x {pocket['height']:.0f} mm "
                  f"at X=[{pocket['x_range'][0]:.0f},"
                  f"{pocket['x_range'][1]:.0f}] "
                  f"Y=[{pocket['y_range'][0]:.0f},"
                  f"{pocket['y_range'][1]:.0f}]")
    if not found_pockets:
        print("  No internal voids detected (solid object)")
    print()

    # Reconstruction recommendation
    flat_vertical_pct = (top + bottom + vertical) / len(triangles) * 100
    curve_pct = angled / len(triangles) * 100

    print("=" * 60)
    print("Recommended reconstruction:")
    if len(triangles) < 500 and flat_vertical_pct > 80:
        print(f"  >>Manual parametric (simple geometry: {len(triangles)} faces, "
              f"{flat_vertical_pct:.0f}% flat/vertical)")
        print("    Write OpenSCAD by hand using dimensions above.")
        print("    Best for: boxes, plates, simple brackets, geometric shapes")
    elif len(triangles) > 1000 or curve_pct > 30:
        print(f"  >>Polyhedron (complex: {len(triangles):,} faces, "
              f"{curve_pct:.0f}% curved)")
        print("    python scripts/stl-to-scad.py <file>")
        print("    Perfect fidelity, ~1s render, valid manifold")
        if len(triangles) > 50000:
            ratio = max(0.1, 20000 / len(triangles))
            print(f"    Consider: --reduce {ratio:.1f} to reduce "
                  f"from {len(triangles):,} to ~{int(len(triangles)*ratio):,} faces")
    else:
        print(f"  >>Sliced cross-section (medium: {len(triangles):,} faces, "
              f"{curve_pct:.0f}% curved)")
        print("    python scripts/stl-to-scad.py <file> --mode sliced")
        print("    Approximate but editable in OpenSCAD")
    print("=" * 60)


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(f"Usage: {sys.argv[0]} <file.stl>")
        sys.exit(1)

    path = sys.argv[1]
    if not os.path.exists(path):
        print(f"Error: File not found: {path}")
        sys.exit(1)

    analyze(path)
