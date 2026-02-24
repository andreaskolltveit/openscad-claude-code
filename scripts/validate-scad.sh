#!/usr/bin/env bash
set -euo pipefail

OPENSCAD="/c/Program Files (x86)/OpenSCAD/openscad.exe"

if [ $# -lt 1 ]; then
    echo "Usage: $0 <file.scad>"
    exit 1
fi

SCAD_FILE="$1"

if [ ! -f "$SCAD_FILE" ]; then
    echo "Error: File not found: $SCAD_FILE"
    exit 1
fi

if [ ! -f "$OPENSCAD" ]; then
    echo "Error: OpenSCAD not found at: $OPENSCAD"
    exit 1
fi

# Render to null output — OpenSCAD validates syntax during export
TEMP_STL=$(mktemp --suffix=.stl)
trap 'rm -f "$TEMP_STL"' EXIT

echo "Validating: $SCAD_FILE"
OUTPUT=$("$OPENSCAD" -o "$TEMP_STL" "$SCAD_FILE" 2>&1) || {
    echo "INVALID: $SCAD_FILE"
    echo "$OUTPUT"
    exit 1
}

if echo "$OUTPUT" | grep -qi "error\|warning"; then
    echo "WARNINGS/ERRORS:"
    echo "$OUTPUT"
    exit 1
fi

echo "VALID: $SCAD_FILE"
exit 0
