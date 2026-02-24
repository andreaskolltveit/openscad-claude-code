#!/usr/bin/env bash
set -euo pipefail

OPENSCAD="/c/Program Files (x86)/OpenSCAD/openscad.exe"

if [ $# -lt 1 ]; then
    echo "Usage: $0 <file.scad> [output.png]"
    exit 1
fi

SCAD_FILE="$1"
PNG_FILE="${2:-${SCAD_FILE%.scad}.png}"

if [ ! -f "$SCAD_FILE" ]; then
    echo "Error: File not found: $SCAD_FILE"
    exit 1
fi

if [ ! -f "$OPENSCAD" ]; then
    echo "Error: OpenSCAD not found at: $OPENSCAD"
    exit 1
fi

echo "Rendering preview: $SCAD_FILE → $PNG_FILE"
"$OPENSCAD" -o "$PNG_FILE" --imgsize=1024,768 "$SCAD_FILE" 2>&1

if [ -f "$PNG_FILE" ] && [ -s "$PNG_FILE" ]; then
    echo "Success: $PNG_FILE ($(wc -c < "$PNG_FILE") bytes)"
else
    echo "Error: PNG file was not created or is empty"
    exit 1
fi
