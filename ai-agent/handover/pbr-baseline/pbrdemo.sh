#!/usr/bin/env bash
# Usage: pbrdemo.sh <outdir> <name> [extra env assignments...] -> captures "PBR material demo" in validation configuration (delay 12000 ms = past frame 600)
OUT="$1"; NAME="$2"; shift 2
mkdir -p "$OUT"
BIN="${BIN:-/d/development/repo/RacingGame/CasaEngine/CasaEngine.Demos/bin/Debug/net9.0-windows}"
OUTW=$(cygpath -w "$OUT")
SRC="$(dirname "$BIN")/../../Content"
cp -u "$SRC/demos-input.buttonsMapping" "$BIN/Content/"; mkdir -p "$BIN/Content/EditorWorld"; cp -ru "$SRC/EditorWorld/." "$BIN/Content/EditorWorld/"
cd "$BIN" || exit 1
rm -f log.txt "$OUT/$NAME.png"
env CASAENGINE_START_DEMO="PBR material demo" CASAENGINE_PBR_DEMO_VALIDATION=1 CASAENGINE_CAPTURE_SCREENSHOT_DELAY_MS=${DELAY:-12000} "$@" CASAENGINE_CAPTURE_SCREENSHOT_PATH="${OUTW}\\${NAME}.png" timeout 120 ./CasaEngine.Demos.exe > /dev/null 2>&1
cp log.txt "$OUT/$NAME.log" 2>/dev/null
echo "$NAME $(sha256sum "$OUT/$NAME.png" | cut -c1-16) exceptions=$(grep -ci exception "$OUT/$NAME.log")"
