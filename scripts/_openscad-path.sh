#!/usr/bin/env bash
# Sourced helper — sets $OPENSCAD to a working openscad binary across platforms.
# Override by exporting OPENSCAD before calling.

if [ -n "${OPENSCAD:-}" ] && [ -x "$OPENSCAD" ]; then
    return 0 2>/dev/null || exit 0
fi

# 1. PATH
if command -v openscad >/dev/null 2>&1; then
    OPENSCAD="$(command -v openscad)"
    return 0 2>/dev/null || exit 0
fi

# 2. Known locations
CANDIDATES=(
    "/Applications/OpenSCAD.app/Contents/MacOS/OpenSCAD"
    "/Applications/OpenSCAD-2021.01.app/Contents/MacOS/OpenSCAD"
    "/opt/homebrew/bin/openscad"
    "/usr/local/bin/openscad"
    "/usr/bin/openscad"
    "/c/Program Files/OpenSCAD/openscad.exe"
    "/c/Program Files (x86)/OpenSCAD/openscad.exe"
)

for c in "${CANDIDATES[@]}"; do
    if [ -x "$c" ]; then
        OPENSCAD="$c"
        return 0 2>/dev/null || exit 0
    fi
done

echo "Error: OpenSCAD not found. Install it or set OPENSCAD=/path/to/openscad" >&2
exit 1
