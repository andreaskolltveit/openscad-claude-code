# OpenSCAD Claude Code Generator

Generate 3D-printable STL files from text descriptions using [Claude Code](https://claude.com/claude-code) and [OpenSCAD](https://openscad.org/).

Describe what you want ("a 20mm cube with a 5mm hole through the center"), and Claude Code generates OpenSCAD code, writes a `.scad` file, and renders it to STL — ready for your slicer.

## Requirements

- Windows 10/11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for tests)
- [OpenSCAD](https://openscad.org/downloads.html) installed at `C:\Program Files (x86)\OpenSCAD\`
- [Claude Code](https://claude.com/claude-code) CLI

## How It Works

1. Describe a 3D model to Claude Code
2. Claude Code writes a `.scad` file to `output/`
3. Claude Code runs `bash scripts/render-stl.sh output/model.scad`
4. STL file ready for 3D printing in `output/model.stl`

No API keys, no GUI, no config files — Claude Code is the engine.

## Quick Start

```bash
# Clone and open with Claude Code
git clone <repo-url>
cd OpenSCAD_OpenAI_Generator

# Ask Claude Code to generate a model
# Example: "Create a phone stand that fits an iPhone 15"
# Claude Code will write the .scad file and render the STL
```

## Scripts

```bash
bash scripts/render-stl.sh output/model.scad       # Render .scad to .stl
bash scripts/render-png.sh output/model.scad        # Render .scad to .png preview
bash scripts/validate-scad.sh output/model.scad     # Validate syntax only
```

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
├── CLAUDE.md                    # OpenSCAD reference for Claude Code
├── scripts/
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
