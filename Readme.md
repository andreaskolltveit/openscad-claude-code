# OpenSCAD Claude Code Generator

Generate 3D-printable STL files from text descriptions using [Claude Code](https://claude.com/claude-code) and [OpenSCAD](https://openscad.org/). Also reverse-engineer existing STL files back to editable OpenSCAD code.

## What can it do?

**Text → 3D model:** Describe what you want ("a 20mm cube with a 5mm hole through the center"), and Claude Code generates OpenSCAD code, renders it to STL — ready for your slicer.

**STL → OpenSCAD:** Got an STL you want to modify? The converter turns it into editable `.scad` code. Perfect fidelity, renders in ~1 second.

## Requirements

- Windows 10/11
- [OpenSCAD](https://openscad.org/downloads.html) installed at `C:\Program Files (x86)\OpenSCAD\`
- [Claude Code](https://claude.com/claude-code) CLI
- [Python 3.10+](https://python.org) (for STL analysis/conversion scripts)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (optional, for tests)

## Quick Start

```bash
# Clone and install
git clone <repo-url>
cd OpenSCAD_OpenAI_Generator
pip install -r requirements.txt   # Python deps for STL conversion

# Generate a model from text
# Open Claude Code and ask: "Create a phone stand that fits an iPhone 15"

# Or reverse-engineer an existing STL
python scripts/stl-to-scad.py path/to/model.stl
# → output/model.scad (ready to edit, validate, and render)
```

## Scripts

### Generate 3D models
```bash
bash scripts/render-stl.sh output/model.scad       # Render .scad → .stl
bash scripts/render-png.sh output/model.scad        # Render .scad → .png preview
bash scripts/validate-scad.sh output/model.scad     # Validate syntax only
```

### Reverse engineer STL files
```bash
# Convert STL to OpenSCAD (perfect replica)
python scripts/stl-to-scad.py model.stl

# Convert with cross-sections (editable but approximate)
python scripts/stl-to-scad.py model.stl --mode sliced

# Simplify a complex mesh (50% fewer faces)
python scripts/stl-to-scad.py model.stl --reduce 0.5

# Analyze STL geometry (dimensions, features, recommendations)
python scripts/analyze-stl.py model.stl
```

## How Claude Code Uses This

Everything is in `CLAUDE.md` — Claude Code reads it automatically. It contains:

- Complete OpenSCAD syntax reference
- Decision tree for STL reverse engineering (polyhedron vs sliced vs manual)
- BOSL2 and MCAD library documentation
- 3D printing best practices
- Guardrails to prevent common mistakes

**You don't need to teach Claude Code how to use the tools.** Just say "recreate this STL" or "design a bracket for X" and it follows the workflow in CLAUDE.md.

## Examples

See `examples/` for reference models:

- `basic-cube.scad` — calibration cube with axis labels
- `phone-stand.scad` — practical model with difference/translate
- `threaded-bolt.scad` — parametric design with modules and for-loops
- `gear.scad` — trigonometry and polygon/linear_extrude

## Tests

```bash
dotnet test "STL Generator.Tests"
```

Tests call OpenSCAD directly — no mocks, no API dependencies.

## Project Structure

```
├── CLAUDE.md                    # OpenSCAD reference + workflow for Claude Code
├── requirements.txt             # Python deps for STL conversion
├── scripts/
│   ├── stl-to-scad.py           # STL → .scad converter (polyhedron/sliced)
│   ├── analyze-stl.py           # STL geometry analyzer (zero deps)
│   ├── render-stl.sh            # .scad → .stl
│   ├── render-png.sh            # .scad → .png preview
│   └── validate-scad.sh         # syntax validation
├── examples/                    # reference .scad files
├── output/                      # generated models (STL/PNG gitignored)
├── STL Generator.Tests/         # xUnit integration tests
└── STL Generator.sln
```

## License

MIT

## Credits

- [Claude Code](https://claude.com/claude-code) by Anthropic
- [OpenSCAD](https://openscad.org/)
