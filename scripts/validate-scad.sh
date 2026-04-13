#!/usr/bin/env bash
set -euo pipefail

# shellcheck source=_openscad-path.sh
source "$(dirname "$0")/_openscad-path.sh"

if [ $# -lt 1 ]; then
    echo "Usage: $0 <file.scad>"
    exit 1
fi

SCAD_FILE="$1"

if [ ! -f "$SCAD_FILE" ]; then
    echo "Error: File not found: $SCAD_FILE"
    exit 1
fi

# Render to null output — OpenSCAD validates syntax during export
# (BSD mktemp on macOS does not support --suffix)
TEMP_STL="$(mktemp -t scadval).stl"
trap 'rm -f "$TEMP_STL"' EXIT

echo "Validating: $SCAD_FILE"
OUTPUT=$("$OPENSCAD" -o "$TEMP_STL" "$SCAD_FILE" 2>&1) || {
    echo "INVALID: $SCAD_FILE"
    echo "$OUTPUT"
    exit 1
}

# Match real OpenSCAD diagnostics ("ERROR:" / "WARNING:") — not status fields like "NoError".
if echo "$OUTPUT" | grep -E "^(ERROR|WARNING):" >/dev/null; then
    echo "WARNINGS/ERRORS:"
    echo "$OUTPUT" | grep -E "^(ERROR|WARNING):"
    exit 1
fi

echo "VALID: $SCAD_FILE"
exit 0
