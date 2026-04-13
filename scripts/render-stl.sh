#!/usr/bin/env bash
set -euo pipefail

# shellcheck source=_openscad-path.sh
source "$(dirname "$0")/_openscad-path.sh"

if [ $# -lt 1 ]; then
    echo "Usage: $0 <file.scad> [output.stl]"
    exit 1
fi

SCAD_FILE="$1"
STL_FILE="${2:-${SCAD_FILE%.scad}.stl}"

if [ ! -f "$SCAD_FILE" ]; then
    echo "Error: File not found: $SCAD_FILE"
    exit 1
fi

echo "Rendering: $SCAD_FILE → $STL_FILE"
# Use manifold backend if supported (OpenSCAD 2023.05+ snapshot) — significantly faster than CGAL.
BACKEND_FLAG=()
if "$OPENSCAD" --help 2>&1 | grep -q -- "--backend"; then
    BACKEND_FLAG=(--backend=manifold)
fi
"$OPENSCAD" "${BACKEND_FLAG[@]}" -o "$STL_FILE" "$SCAD_FILE" 2>&1

if [ -f "$STL_FILE" ] && [ -s "$STL_FILE" ]; then
    echo "Success: $STL_FILE ($(wc -c < "$STL_FILE") bytes)"
else
    echo "Error: STL file was not created or is empty"
    exit 1
fi
