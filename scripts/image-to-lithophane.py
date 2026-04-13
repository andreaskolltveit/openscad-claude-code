#!/usr/bin/env python3
"""
image-to-lithophane.py — convert an image to a 3D-printable lithophane STL.

Lithophane = darker pixels become thicker, lighter pixels become thinner.
Backlit, the image becomes visible through the variations in light transmission.

Generates an OpenSCAD .scad file using surface() with a heightmap, then renders to STL.

Usage:
    python image-to-lithophane.py photo.jpg
    python image-to-lithophane.py photo.jpg --width 100 --thickness 3 --base 0.6
    python image-to-lithophane.py photo.jpg --shape curved --diameter 80
    python image-to-lithophane.py photo.jpg --invert  # for non-backlit reliefs

Output:
    output/<name>.scad — heightmap-based OpenSCAD model
    output/<name>.dat  — surface data file (referenced by .scad)
    output/<name>.stl  — final mesh (if --render or default)

Dependencies: Pillow, numpy. Install: pip install pillow numpy
"""
from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path

try:
    import numpy as np
    from PIL import Image, ImageOps
except ImportError as e:
    sys.exit(f"Missing dependency: {e.name}. Run: pip install pillow numpy")


SCAD_FLAT_TEMPLATE = """// Lithophane generated from {src}
// Size: {width:.2f} x {height:.2f} x ({base} + {relief}) mm
// Pixels: {px} x {py} ({mode})

scale([{sx}, {sy}, 1])
    union() {{
        // Base plate
        translate([0, 0, 0])
            cube([{px_minus_1}, {py_minus_1}, {base}]);
        // Heightmap relief
        translate([0, 0, {base}])
            surface(file = "{datfile}", center = false, convexity = 5);
    }}
"""

SCAD_CURVED_TEMPLATE = """// Curved lithophane generated from {src}
// Cylinder diameter: {diameter} mm, height: {height:.2f} mm
// Wall: {base} (base) + up to {relief} mm relief outward

$fn = 200;
radius = {diameter} / 2;
arc_deg = {arc_deg};
height = {height};
base = {base};
relief = {relief};
px = {px};
py = {py};

// Inner cylinder (the base wall)
difference() {{
    cylinder(h = height, r = radius + base, $fn = 200);
    translate([0, 0, -1])
        cylinder(h = height + 2, r = radius, $fn = 200);
}}

// Wrap the heightmap around the cylinder
// Each pixel column becomes a thin wedge.
module heightmap_wedge() {{
    scale([1, height / (py - 1), 1])
        translate([0, 0, radius + base])
            rotate([90, 0, 0])
                surface(file = "{datfile}", center = false, convexity = 5);
}}

step = arc_deg / (px - 1);
for (i = [0 : px - 1]) {{
    rotate([0, 0, i * step - arc_deg / 2])
        translate([-0.51, 0, 0])
            // Slice one column out of the heightmap and place it at this angle
            intersection() {{
                heightmap_wedge();
                translate([-0.5, -height/2 - 1, 0])
                    cube([1.02, height + 2, radius + base + relief + 1]);
            }}
}}
"""


def load_image(path: Path, max_pixels: int, invert: bool) -> np.ndarray:
    img = Image.open(path)
    img = ImageOps.exif_transpose(img)  # honor EXIF rotation
    img = img.convert("L")  # 8-bit grayscale

    # Resample so the longest side is at most max_pixels
    w, h = img.size
    scale = max_pixels / max(w, h)
    if scale < 1:
        new_size = (max(1, int(w * scale)), max(1, int(h * scale)))
        img = img.resize(new_size, Image.LANCZOS)

    arr = np.asarray(img, dtype=np.float32)
    # In a backlit lithophane, dark pixels = thick, light pixels = thin.
    # surface() uses the value as Z-height directly, so we invert the grayscale.
    if not invert:
        arr = 255.0 - arr
    return arr


def write_dat(arr: np.ndarray, path: Path, base: float, relief: float) -> None:
    # OpenSCAD surface() expects whitespace-separated rows, one per Y line.
    # Normalize 0..255 -> 0..relief (mm).
    height = (arr / 255.0) * relief
    with path.open("w") as f:
        for row in height:
            f.write(" ".join(f"{v:.4f}" for v in row))
            f.write("\n")


def main() -> int:
    ap = argparse.ArgumentParser(description="Convert image to lithophane STL via OpenSCAD")
    ap.add_argument("image", type=Path, help="input image (jpg/png/heic etc.)")
    ap.add_argument("-o", "--output", type=Path, help="output .scad path (default: output/<name>.scad)")
    ap.add_argument("--width", type=float, default=100.0, help="lithophane width in mm (flat mode), default 100")
    ap.add_argument("--thickness", type=float, default=3.0, help="max relief thickness in mm (default 3)")
    ap.add_argument("--base", type=float, default=0.6, help="solid base thickness in mm (default 0.6)")
    ap.add_argument("--max-pixels", type=int, default=400, help="resample image so longest side = N pixels (default 400)")
    ap.add_argument("--invert", action="store_true", help="dark = thin (for non-backlit reliefs / signs)")
    ap.add_argument("--shape", choices=("flat", "curved"), default="flat", help="flat panel or curved (cylindrical) lampshade-style")
    ap.add_argument("--diameter", type=float, default=80.0, help="cylinder diameter in mm (curved mode, default 80)")
    ap.add_argument("--arc-deg", type=float, default=180.0, help="degrees of cylinder occupied by image (curved mode, default 180)")
    ap.add_argument("--no-render", action="store_true", help="only write .scad/.dat, skip STL render")
    args = ap.parse_args()

    if not args.image.exists():
        sys.exit(f"Image not found: {args.image}")

    out_dir = Path(__file__).resolve().parent.parent / "output"
    out_dir.mkdir(exist_ok=True)
    name = args.image.stem
    scad_path = args.output or (out_dir / f"{name}-lithophane.scad")
    dat_path = scad_path.with_suffix(".dat")

    print(f"Loading {args.image}...")
    arr = load_image(args.image, args.max_pixels, args.invert)
    py, px = arr.shape  # numpy: rows=Y, cols=X
    print(f"  Resampled to {px} x {py} pixels (grayscale)")

    relief = args.thickness - args.base
    if relief <= 0:
        sys.exit(f"--thickness ({args.thickness}) must be greater than --base ({args.base})")

    print(f"Writing heightmap: {dat_path}")
    write_dat(arr, dat_path, args.base, relief)

    if args.shape == "flat":
        # Each surface() unit = 1mm in X/Y. Scale so width matches request.
        sx = args.width / (px - 1)
        # Preserve aspect ratio
        sy = sx
        height_mm = sy * (py - 1)
        scad = SCAD_FLAT_TEMPLATE.format(
            src=args.image.name,
            width=args.width,
            height=height_mm,
            base=args.base,
            relief=relief,
            px=px,
            py=py,
            mode="flat",
            sx=sx,
            sy=sy,
            px_minus_1=px - 1,
            py_minus_1=py - 1,
            datfile=dat_path.name,
        )
    else:  # curved
        # Image height in mm = real-world height; X wraps around the cylinder.
        # Approximate: each pixel column is one wedge.
        # Choose mm-per-pixel so vertical aspect roughly matches.
        circ_per_pixel = (args.diameter * 3.14159 * args.arc_deg / 360.0) / (px - 1)
        height_mm = circ_per_pixel * (py - 1)
        scad = SCAD_CURVED_TEMPLATE.format(
            src=args.image.name,
            diameter=args.diameter,
            arc_deg=args.arc_deg,
            height=height_mm,
            base=args.base,
            relief=relief,
            px=px,
            py=py,
            datfile=dat_path.name,
        )

    scad_path.write_text(scad)
    print(f"Wrote {scad_path}")

    if args.no_render:
        return 0

    if args.shape == "curved":
        print("NOTE: curved mode is experimental and slow to render — consider --no-render and tune in OpenSCAD GUI first.")

    print("Rendering STL...")
    render_script = Path(__file__).parent / "render-stl.sh"
    result = subprocess.run(["bash", str(render_script), str(scad_path)])
    return result.returncode


if __name__ == "__main__":
    sys.exit(main())
