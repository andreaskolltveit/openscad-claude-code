# OpenSCAD Claude Code Project

## Workflow
1. User describes a 3D model to Claude Code
2. Claude Code writes a `.scad` file to `output/`
3. Claude Code runs `bash scripts/render-stl.sh output/model.scad` to generate STL
4. STL file ready for 3D printing in `output/model.stl`
5. Optional: `bash scripts/render-png.sh output/model.scad` for visual preview

### Reverse engineering workflow (STL → SCAD)

**Choose approach based on goal:**

| Goal | Approach | Command |
|------|----------|---------|
| **Exact replica** (default) | `polyhedron` — perfect fidelity, ~1s render | `python scripts/stl-to-scad.py file.stl` |
| **Editable replica** | `sliced` — cross-sections, approximate | `python scripts/stl-to-scad.py file.stl --mode sliced` |
| **Parametric recreation** | Manual — guided by analyze-stl.py | `python scripts/analyze-stl.py file.stl` |

**Primary workflow (polyhedron):**
1. `python scripts/stl-to-scad.py file.stl` → `output/file.scad`
2. `bash scripts/validate-scad.sh output/file.scad` → must say VALID
3. `bash scripts/render-stl.sh output/file.scad` → `output/file.stl`
4. Verify: `python scripts/analyze-stl.py output/file.stl` — dimensions must match original

**Sliced workflow (editable cross-sections):**
1. `python scripts/stl-to-scad.py file.stl --mode sliced`
2. The output uses `polygon()` + `linear_extrude()` per Z-slice — editable in OpenSCAD

**Manual parametric workflow (simple geometric shapes):**
1. `python scripts/analyze-stl.py file.stl` — read the recommendation at the bottom
2. Write parametric OpenSCAD code using dimensions from the report
3. Best for models with <500 faces and mostly flat/vertical surfaces

**Decision tree (also printed by analyze-stl.py):**
- Complex/organic (>1000 faces, high curve %) → `polyhedron`
- Medium complexity → `sliced`
- Simple geometric (<500 faces, >80% flat/vertical) → manual parametric

**Options:**
- `--reduce 0.5` — simplify mesh to 50% of faces (needs `fast-simplification`)
- `--z-step 0.5` — Z resolution for sliced mode (default 0.5mm)
- `--tolerance 0.1` — polygon simplification for sliced mode
- `-o path.scad` — custom output path

**Dependencies:** `pip install -r requirements.txt` (first time only)

#### CRITICAL GUARDRAILS — STL recreation
- **NEVER** infer what a model is from its filename, user description, or any textual context
- **ONLY** use the geometric analysis data (dimensions, cross-sections, Z-levels, pockets) to determine shape
- The filename is for output naming only — it tells you NOTHING about the geometry
- If text appears in the geometric analysis (e.g. embossed/debossed letters), reproduce it as literal text geometry
- Text in filenames or metadata is NOT content to reproduce — it is just a label
- Always verify the recreation by comparing analyze-stl.py output of original vs recreation
- When in doubt about a feature, describe what the geometry shows and ask the user

## Conventions
- All generated `.scad` files go in `output/`
- Use descriptive filenames: `output/phone-stand.scad`, not `output/model.scad`
- Always validate before rendering: `bash scripts/validate-scad.sh output/file.scad`
- Use `$fn` for resolution control (32 for preview, 64-128 for final)
- All dimensions in millimeters
- Only 3D geometry at top level — extrude all 2D shapes
- No `echo()`, `assert()`, or debug statements in output
- Output must be a complete, valid OpenSCAD script
- **Prefer BOSL2** over manual implementations for rounding, threading, gears, and attachments
- Use MCAD when BOSL2 does not cover the need (e.g. stepper motors, servos)

## OpenSCAD Path
```
C:\Program Files (x86)\OpenSCAD\openscad.exe
```

## Scripts
```bash
# Generation
bash scripts/render-stl.sh output/model.scad       # → output/model.stl
bash scripts/render-png.sh output/model.scad        # → output/model.png
bash scripts/validate-scad.sh output/model.scad     # syntax check only

# Reverse engineering (STL → SCAD)
python scripts/stl-to-scad.py file.stl              # polyhedron (perfect replica)
python scripts/stl-to-scad.py file.stl --mode sliced # cross-section (editable)
python scripts/stl-to-scad.py file.stl --reduce 0.5  # simplify 50%

# Analysis
python scripts/analyze-stl.py file.stl              # analyze existing STL geometry
```

## Running Tests
```bash
dotnet test "STL Generator.Tests"
```

---

# OpenSCAD Reference

## 3D Primitives

```scad
cube([x, y, z], center=false);
cube(size, center=false);                  // shorthand for cube([s,s,s])
sphere(r=radius, $fn=fragments);
sphere(d=diameter);
cylinder(h=height, r=radius, $fn=fragments, center=false);
cylinder(h=height, r1=bottom_r, r2=top_r); // cone
cylinder(h=height, d=diameter);
cylinder(h=height, d1=bottom_d, d2=top_d);
polyhedron(points=[[x,y,z],...], faces=[[i,j,k],...]);
```

## 2D Primitives (must be extruded for 3D output)

```scad
circle(r=radius, $fn=fragments);
circle(d=diameter);
square([x, y], center=false);
square(size, center=false);
polygon(points=[[x,y],...], paths=[[0,1,2,...]]);
text("string", size=10, font="Liberation Sans", halign="left", valign="baseline");
```

## Transformations

```scad
translate([x, y, z]) child();
rotate([x_deg, y_deg, z_deg]) child();
rotate(a=angle, v=[x,y,z]) child();        // axis-angle
scale([x, y, z]) child();
mirror([x, y, z]) child();                  // mirror across plane
resize([x, y, z], auto=false) child();
color("name", alpha) child();               // "red", "blue", "#FF8800"
color([r, g, b, a]) child();
offset(r=radius) 2d_child();                // round offset
offset(delta=d, chamfer=false) 2d_child();  // sharp offset
multmatrix(m) child();                       // 4x4 transformation matrix
```

## Boolean Operations

```scad
union() { a(); b(); }           // combine shapes
difference() { base(); cut(); } // subtract: first child minus rest
intersection() { a(); b(); }   // keep only overlapping volume
```

## Hull & Minkowski

```scad
hull() { a(); b(); }            // convex hull wrapping all children
minkowski() { a(); b(); }       // Minkowski sum (rounded edges etc.)
```

## Extrusion

```scad
linear_extrude(height=h, center=false, twist=0, slices=20, scale=1.0)
    2d_child();
rotate_extrude(angle=360, $fn=fragments)
    2d_child();                  // 2D profile must be in positive X half
```

## Special Variables

```scad
$fn = 64;    // number of fragments (overrides $fa/$fs when set)
$fa = 12;    // minimum angle per fragment
$fs = 2;     // minimum size per fragment
$t           // animation variable [0..1)
$vpr         // viewport rotation [x,y,z]
$vpt         // viewport translation [x,y,z]
$vpd         // viewport distance
$vpf         // viewport field of view
```

## Mathematical Functions

```scad
// Trigonometry (degrees)
sin(deg)  cos(deg)  tan(deg)
asin(x)   acos(x)   atan(x)   atan2(y, x)

// Arithmetic
abs(x)  ceil(x)  floor(x)  round(x)
max(a, b, ...)  min(a, b, ...)
pow(base, exp)  sqrt(x)  exp(x)  ln(x)  log(x)   // log = log10

// Vector/list
len(v)  norm(v)  cross(u, v)
concat(a, b, ...)
lookup(key, [[k,v],...])

// String
str("a", 1, "b")  chr(n)  ord("c")  search("needle", "haystack")

// Other
sign(x)  rands(min, max, count, seed)
```

## Control Flow

```scad
// Conditional
if (condition) { ... } else { ... }

// For loop (union of all iterations)
for (i = [0 : step : end]) { ... }
for (i = [0, 5, 10]) { ... }
for (x = [0:2], y = [0:2]) { ... }

// Intersection for loop
intersection_for(i = [0:2]) { ... }

// Let (local variables)
let (a = 1, b = a + 2) { ... }

// Assign (deprecated — use let)
```

## Modules & Functions

```scad
// Module (produces geometry)
module name(param1, param2=default) {
    // geometry here
    children();   // pass-through children
}
name(10, param2=20);

// Function (returns a value)
function name(param1, param2=default) = expression;
```

## Import / Export

```scad
import("file.stl");
import("file.svg");
import("file.dxf", layer="layername");
surface("file.dat", center=false, convexity=1);
// Export is done via CLI: openscad -o output.stl input.scad
```

## Modifier Characters

```scad
*  child();   // disable (comment out)
!  child();   // show only this
#  child();   // highlight (transparent red)
%  child();   // transparent (background)
```

## 3D Printing Tips

- **Wall thickness:** minimum 1.2mm (2-3 perimeters)
- **Tolerances:** 0.2-0.3mm clearance for press-fit, 0.4-0.5mm for sliding fit
- **Overhang:** max 45° without supports; use chamfers instead of fillets on bottom edges
- **Bridging:** up to ~50mm unsupported span for most printers
- **Minimum feature:** 0.4mm (typical nozzle diameter)
- **Hole compensation:** add 0.2mm to hole radius for accurate fit
- **Layer adhesion:** orient strongest axis perpendicular to print bed
- **Text:** minimum 8mm height for readability, use `linear_extrude(1)` for emboss/deboss
- **Screw holes:** use `cylinder(h, r=nominal_radius + 0.15)` for M3, M4, M5
- **Resolution:** `$fn=64` for final prints, `$fn=32` during development

---

# Libraries

## BOSL2 (Belfry OpenSCAD Library v2)

**Installed at:** `My Documents\OpenSCAD\libraries\BOSL2\`

Always include with:
```scad
include <BOSL2/std.scad>
```

For specific modules, add extra includes:
```scad
include <BOSL2/std.scad>
include <BOSL2/screws.scad>
include <BOSL2/threading.scad>
include <BOSL2/gears.scad>
include <BOSL2/joiners.scad>
```

### BOSL2 Enhanced Shapes

```scad
// Cube with rounded edges (replaces manual minkowski)
cuboid([x,y,z], rounding=r, chamfer=c, anchor=CENTER);

// Cylinder with rounding
cyl(h=height, r=radius, rounding1=r1, rounding2=r2, anchor=BOTTOM);

// Sphere with style options
sphere(r=radius, style="aligned", anchor=CENTER);

// Prismoid (tapered box)
prismoid([bot_x,bot_y], [top_x,top_y], h=height, rounding=r);

// Tube (hollow cylinder)
tube(h=height, or=outer_r, ir=inner_r, anchor=BOTTOM);
```

### BOSL2 Attachments (relative positioning)

```scad
// Instead of manual translate, use anchor/attach:
cuboid([20,20,10], anchor=BOTTOM)
    attach(TOP) cyl(h=5, r=3);

// Common anchors: CENTER, TOP, BOTTOM, LEFT, RIGHT, FRONT, BACK
// Edge anchors: TOP+LEFT, BOTTOM+FRONT, etc.
// Shorthands:
up(z)       // translate([0,0,z])
down(z)     // translate([0,0,-z])
left(x)     // translate([-x,0,0])
right(x)    // translate([x,0,0])
fwd(y)      // translate([0,-y,0])
back(y)     // translate([0,y,0])
```

### BOSL2 Screws & Threading

```scad
include <BOSL2/screws.scad>

// Standard metric screw
screw("M6", length=20, head="hex", drive="hex");
screw("M3", length=10, head="socket", drive="hex");
screw("M4", length=12, head="flat");

// Nut
nut("M6", thickness=5);

// Screw hole (for difference())
screw_hole("M4", length=10, head="socket", oversize=0.3);

include <BOSL2/threading.scad>

// Custom threaded rod
threaded_rod(d=10, pitch=1.5, l=30, blunt_start=true, $fn=64);

// Trapezoidal thread (ACME-style, for lead screws)
trapezoidal_threaded_rod(d=10, pitch=2, l=30, $fn=64);
```

### BOSL2 Gears

```scad
include <BOSL2/gears.scad>

// Spur gear (mod = metric module)
spur_gear(mod=2, teeth=20, thickness=8, pressure_angle=20);

// Rack
rack(mod=2, teeth=10, thickness=8, height=10);

// Bevel gear
bevel_gear(mod=2, teeth=20, mate_teeth=20, thickness=8);
```

### BOSL2 Joiners

```scad
include <BOSL2/joiners.scad>

// Dovetail joint
dovetail("male", width=10, height=8, slide=30);
dovetail("female", width=10, height=8, slide=30);

// Snap pin
snap_pin(r=2.5, l=15, d=1, nub_depth=0.5);
snap_pin_socket(r=2.5, l=15, d=1);
```

### BOSL2 Rounding

```scad
include <BOSL2/rounding.scad>

// Round corners of a 2D path
path = square(20, center=true);
rounded = round_corners(path, radius=3, $fn=32);
linear_extrude(10) polygon(rounded);

// Rounded prism (3D with rounded top/bottom edges)
rounded_prism(square(20, center=true), height=10, joint_top=2, joint_bot=2);
```

---

## MCAD (Bundled with OpenSCAD)

**Installed at:** `C:\Program Files (x86)\OpenSCAD\libraries\MCAD\`

Use `use` (not `include`) for MCAD modules:

```scad
use <MCAD/involute_gears.scad>
use <MCAD/nuts_and_bolts.scad>
use <MCAD/stepper.scad>
use <MCAD/servos.scad>
use <MCAD/bearing.scad>
use <MCAD/boxes.scad>
use <MCAD/shapes.scad>
use <MCAD/regular_shapes.scad>
use <MCAD/triangles.scad>
use <MCAD/teardrop.scad>
use <MCAD/polyholes.scad>
```

### MCAD Key Modules

```scad
// Involute gear
use <MCAD/involute_gears.scad>
gear(number_of_teeth=20, circular_pitch=200, gear_thickness=5, rim_thickness=5,
     hub_thickness=5, bore_diameter=5);

// Nuts and bolts
use <MCAD/nuts_and_bolts.scad>
nutHole(size=3);       // M3 nut cutout for difference()
boltHole(size=3, length=10);  // M3 bolt hole

// Stepper motors
use <MCAD/stepper.scad>
motor(Nema17);         // NEMA 17 stepper motor outline
motor(Nema23);

// Servo motors
use <MCAD/servos.scad>
alignds420(position=[0,0,0], rotation=[0,0,0]);

// Bearings
use <MCAD/bearing.scad>
bearing(model=608);    // 608 skateboard bearing

// Rounded box
use <MCAD/boxes.scad>
roundedBox([30, 20, 10], radius=3);

// Teardrop (better for 3D printing overhangs)
use <MCAD/teardrop.scad>
teardrop(radius=5, length=10);

// Polyhole (accurate printed holes)
use <MCAD/polyholes.scad>
polyhole(h=10, d=5);   // more accurate than cylinder for 3D printing
```

### When to use MCAD vs BOSL2

| Need | Use |
|------|-----|
| Rounded edges/chamfers | BOSL2 `cuboid()` |
| Standard screws/nuts | BOSL2 `screw()` |
| Spur/bevel gears | BOSL2 `spur_gear()` |
| Stepper motor outlines | MCAD `motor()` |
| Servo outlines | MCAD `alignds420()` |
| Bearings (608, 6200, etc.) | MCAD `bearing()` |
| Teardrop holes for printing | MCAD `teardrop()` |
| Snap joints/dovetails | BOSL2 `dovetail()` |
| Bézier curves/paths | BOSL2 `bezier_curve()` |
