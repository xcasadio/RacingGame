#!/usr/bin/env bash
# Usage: framestats.sh <outdir> <name> <demo title> [extra env assignments...] -> runs the demo with CASAENGINE_DEMO_FRAME_STATS=1 (BIN = build to measure) and prints the [FrameStats] line.
OUT="$1"; NAME="$2"; TITLE="$3"; shift 3
mkdir -p "$OUT"
BIN="${BIN:-/d/development/repo/RacingGame/CasaEngine/CasaEngine.Demos/bin/Release/net9.0-windows}"
SRC="${CONTENT_SRC:-/d/development/repo/RacingGame/CasaEngine/CasaEngine.Demos/Content}"
cp -u "$SRC/demos-input.buttonsMapping" "$BIN/Content/"; mkdir -p "$BIN/Content/EditorWorld"; cp -ru "$SRC/EditorWorld/." "$BIN/Content/EditorWorld/"
cd "$BIN" || exit 1
rm -f log.txt
env CASAENGINE_START_DEMO="$TITLE" CASAENGINE_DEMO_FRAME_STATS=1 "$@" timeout 180 ./CasaEngine.Demos.exe > /dev/null 2>&1
cp log.txt "$OUT/$NAME.log" 2>/dev/null
echo "$NAME: $(grep -h '\[FrameStats\]' log.txt | sed 's/^.*\[FrameStats\]/[FrameStats]/') exceptions=$(grep -ci exception log.txt)"
