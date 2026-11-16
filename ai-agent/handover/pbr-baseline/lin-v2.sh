#!/usr/bin/env bash
# Usage: lin-v2.sh <outdir> <name> [extra env assignments...]  -> captures "Colour pipeline demo" in LinearHdr (AcesApprox, exposure 1, intensity 1 unless overridden)
OUT="$1"; NAME="$2"; shift 2
mkdir -p "$OUT"
BIN="${BIN:-/d/development/repo/RacingGame/CasaEngine/CasaEngine.Demos/bin/Debug/net9.0-windows}"
OUTW=$(cygpath -w "$OUT")
cd "$BIN" || exit 1
rm -f log.txt "$OUT/$NAME.png"
env CASAENGINE_START_DEMO="Colour pipeline demo" CASAENGINE_DEMO_COLOR_PIPELINE=LinearHdr CASAENGINE_DEMO_TONEMAPPER=AcesApprox CASAENGINE_DEMO_EXPOSURE=1 CASAENGINE_COLOR_DEMO_LIGHT_INTENSITY=1 "$@" CASAENGINE_CAPTURE_SCREENSHOT_PATH="${OUTW}\\${NAME}.png" timeout 90 ./CasaEngine.Demos.exe > /dev/null 2>&1
cp log.txt "$OUT/$NAME.log" 2>/dev/null
echo "$NAME $(sha256sum "$OUT/$NAME.png" | cut -c1-16) exceptions=$(grep -ci exception "$OUT/$NAME.log")"
