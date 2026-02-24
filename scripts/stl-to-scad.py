#!/usr/bin/env python3
"""Convert an STL file to an OpenSCAD .scad file.

Two modes:
  polyhedron (default) — Direct mesh-to-polyhedron. Fast render (~1s), perfect
                         fidelity, valid manifold. Optionally simplify with --reduce.
  sliced              — Cross-section approach. Approximate but more editable.
                         Configurable Z-resolution and simplification tolerance.

Usage:
  python scripts/stl-to-scad.py input.stl                            # polyhedron
  python scripts/stl-to-scad.py input.stl --mode sliced              # cross-section
  python scripts/stl-to-scad.py input.stl --reduce 0.5               # simplify 50%
  python scripts/stl-to-scad.py input.stl -o output/custom-name.scad # custom output
"""

import argparse
import os
import sys
import time


def check_dependencies(mode, reduce):
    """Check that required Python packages are installed."""
    missing = []

    try:
        import trimesh  # noqa: F401
    except ImportError:
        missing.append("trimesh")

    try:
        import numpy  # noqa: F401
    except ImportError:
        missing.append("numpy")

    if mode == "sliced":
        try:
            import shapely  # noqa: F401
        except ImportError:
            missing.append("shapely")
        try:
            import rtree  # noqa: F401
        except ImportError:
            missing.append("rtree")

    if reduce is not None:
        try:
            import fast_simplification  # noqa: F401
        except ImportError:
            missing.append("fast-simplification")

    if missing:
        print(f"Error: Missing dependencies: {', '.join(missing)}")
        print(f"Install with: pip install {' '.join(missing)}")
        print(f"Or install all: pip install -r requirements.txt")
        sys.exit(1)


def load_and_center(path, reduce):
    """Load STL mesh, center at XY origin with Z starting at 0."""
    import trimesh
    import numpy as np

    mesh = trimesh.load(path)

    # Center XY at origin, Z starts at 0
    bounds = mesh.bounds  # [[min_x,min_y,min_z],[max_x,max_y,max_z]]
    center_x = (bounds[0][0] + bounds[1][0]) / 2
    center_y = (bounds[0][1] + bounds[1][1]) / 2
    min_z = bounds[0][2]
    mesh.apply_translation([-center_x, -center_y, -min_z])

    if reduce is not None:
        import fast_simplification

        target = int(len(mesh.faces) * reduce)
        points, faces = fast_simplification.simplify(
            mesh.vertices.astype(np.float32),
            mesh.faces.astype(np.int32),
            target_count=target,
        )
        mesh = trimesh.Trimesh(vertices=points, faces=faces)
        mesh.fix_normals()

    return mesh


def polyhedron_to_scad(mesh):
    """Convert trimesh to OpenSCAD polyhedron source code."""
    lines = []
    lines.append("// Auto-generated from STL — polyhedron mode")
    lines.append(f"// Vertices: {len(mesh.vertices)}, Faces: {len(mesh.faces)}")
    lines.append("")
    lines.append("polyhedron(")

    # Points
    lines.append("  points=[")
    for i, v in enumerate(mesh.vertices):
        comma = "," if i < len(mesh.vertices) - 1 else ""
        lines.append(f"    [{v[0]:.6g},{v[1]:.6g},{v[2]:.6g}]{comma}")
    lines.append("  ],")

    # Faces
    lines.append("  faces=[")
    for i, f in enumerate(mesh.faces):
        comma = "," if i < len(mesh.faces) - 1 else ""
        lines.append(f"    [{f[0]},{f[1]},{f[2]}]{comma}")
    lines.append("  ]")

    lines.append(");")
    return "\n".join(lines)


def sliced_to_scad(mesh, z_step, tolerance):
    """Convert trimesh to OpenSCAD using cross-section slicing."""
    import numpy as np

    bounds = mesh.bounds
    z_min = bounds[0][2]
    z_max = bounds[1][2]
    total_height = z_max - z_min

    z_levels = np.arange(z_min, z_max + z_step / 2, z_step)

    lines = []
    lines.append("// Auto-generated from STL — sliced mode")
    lines.append(f"// Z-step: {z_step}mm, tolerance: {tolerance}mm")
    lines.append(f"// Height: {total_height:.2f}mm, slices: {len(z_levels)}")
    lines.append("")
    lines.append("union() {")

    slice_count = 0
    for z in z_levels:
        try:
            section = mesh.section(
                plane_origin=[0, 0, z], plane_normal=[0, 0, 1]
            )
            if section is None:
                continue

            # to_2D() returns (Path2D, transform_matrix)
            # The transform contains the XY offset — we must apply its inverse
            planar, transform = section.to_2D()

            if planar is None or len(planar.polygons_full) == 0:
                continue

            # Extract the XY offset from the transform so we can correct it
            offset_x = transform[0][3]
            offset_y = transform[1][3]

            for polygon in planar.polygons_full:
                simplified = polygon.simplify(tolerance, preserve_topology=True)
                if simplified.is_empty:
                    continue

                coords = list(simplified.exterior.coords)[:-1]  # drop closing dup

                # Correct for to_planar() offset
                corrected = [
                    (x + offset_x, y + offset_y) for x, y in coords
                ]

                pts = ",".join(f"[{x:.4f},{y:.4f}]" for x, y in corrected)
                lines.append(f"  translate([0,0,{z:.4f}])")
                lines.append(f"    linear_extrude(height={z_step:.4f})")
                lines.append(f"      polygon(points=[{pts}]);")

                # Handle holes
                for interior in simplified.interiors:
                    hole_coords = list(interior.coords)[:-1]
                    hole_corrected = [
                        (x + offset_x, y + offset_y) for x, y in hole_coords
                    ]
                    hole_pts = ",".join(
                        f"[{x:.4f},{y:.4f}]" for x, y in hole_corrected
                    )
                    lines.append(f"  // hole at z={z:.1f}")
                    lines.append(f"  translate([0,0,{z:.4f}])")
                    lines.append(f"    linear_extrude(height={z_step:.4f})")
                    lines.append(f"      polygon(points=[{hole_pts}]);")

                slice_count += 1

        except Exception:
            continue

    lines.append("}")
    lines.append(f"// Total slices generated: {slice_count}")
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(
        description="Convert STL to OpenSCAD (.scad)"
    )
    parser.add_argument("input", help="Input .stl file")
    parser.add_argument(
        "-o", "--output", help="Output .scad file (default: output/<name>.scad)"
    )
    parser.add_argument(
        "--mode",
        choices=["polyhedron", "sliced"],
        default="polyhedron",
        help="Conversion mode (default: polyhedron)",
    )
    parser.add_argument(
        "--reduce",
        type=float,
        default=None,
        help="Reduce mesh to this fraction (e.g. 0.5 = 50%% of faces). "
        "Requires fast-simplification package.",
    )
    parser.add_argument(
        "--z-step",
        type=float,
        default=0.5,
        help="Z-step for sliced mode in mm (default: 0.5)",
    )
    parser.add_argument(
        "--tolerance",
        type=float,
        default=0.1,
        help="Polygon simplification tolerance for sliced mode (default: 0.1)",
    )
    args = parser.parse_args()

    if not os.path.isfile(args.input):
        print(f"Error: File not found: {args.input}")
        sys.exit(1)

    check_dependencies(args.mode, args.reduce)

    # Determine output path
    if args.output:
        out_path = args.output
    else:
        base = os.path.splitext(os.path.basename(args.input))[0]
        base = "-".join(base.lower().split())
        out_path = os.path.join("output", f"{base}.scad")

    os.makedirs(os.path.dirname(out_path) or ".", exist_ok=True)

    # Load and process
    print(f"Loading: {args.input}")
    t0 = time.time()
    mesh = load_and_center(args.input, args.reduce)
    load_time = time.time() - t0

    bounds = mesh.bounds
    dims = bounds[1] - bounds[0]

    print(f"  Dimensions: {dims[0]:.2f} x {dims[1]:.2f} x {dims[2]:.2f} mm")
    print(f"  Faces: {len(mesh.faces):,}")
    print(f"  Loaded in {load_time:.2f}s")

    # Convert
    print(f"Converting ({args.mode} mode)...")
    t0 = time.time()

    if args.mode == "polyhedron":
        scad_code = polyhedron_to_scad(mesh)
    else:
        scad_code = sliced_to_scad(mesh, args.z_step, args.tolerance)

    convert_time = time.time() - t0

    # Write output
    with open(out_path, "w") as f:
        f.write(scad_code)

    file_size = os.path.getsize(out_path)
    print(f"  Converted in {convert_time:.2f}s")
    print(f"  Output: {out_path} ({file_size:,} bytes)")
    print("Done.")


if __name__ == "__main__":
    main()
