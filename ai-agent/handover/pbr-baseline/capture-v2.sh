#!/usr/bin/env bash
# Usage: capture-v2.sh (same as capture.sh, kept for the post-origin/main harness; hints/help are hidden by the harness when CASAENGINE_CAPTURE_SCREENSHOT_PATH is set)
# Usage: pbr-capture.sh <outdir> [extra env assignments...]
# Captures every baseline demo twice by exact title and prints "slug sha1 sha2 notfound".
OUT="$1"; shift
BIN="${BIN:-/d/development/repo/RacingGame/CasaEngine/CasaEngine.Demos/bin/Debug/net9.0-windows}"
mkdir -p "$OUT"
OUTW=$(cygpath -w "$OUT")
declare -a DEMOS=(
  "material|Material system demo|"
  "environment|Environment showcase|"
  "static-model|Static model demo|"
  "static-shadow|Static shadow validation demo|CASAENGINE_SHADOW_DEMO_VALIDATION=1"
  "skinned|Skinned mesh demo|"
  "particles|Particle system demo|"
  "split-screen|Split-screen demo (2 views)|"
  "render-to-texture|Render-to-texture demo|"
  "ui-overlay|MGUI UI Overlay Demo (HUD + Modal Screens)|"
  "frozen-frame|Frozen frame demo|"
  "tile-map|Tile map demo|"
)
# origin/main registers these demo assets in AssetInfos.json but neither Content.mgcb nor the csproj copies them to the output: sync them.
SRC="$(dirname "$BIN")/../../Content"
cp -u "$SRC/demos-input.buttonsMapping" "$BIN/Content/" && mkdir -p "$BIN/Content/EditorWorld" && cp -ru "$SRC/EditorWorld/." "$BIN/Content/EditorWorld/"
cd "$BIN" || exit 1
for entry in "${DEMOS[@]}"; do
  IFS='|' read -r slug title extra <<< "$entry"
  for n in 1 2; do
    rm -f log.txt "$OUT/$slug-$n.png"
    env CASAENGINE_START_DEMO="$title" CASAENGINE_CAPTURE_SCREENSHOT_PATH="$OUTW\\$slug-$n.png" $extra "$@" timeout 90 ./CasaEngine.Demos.exe > /dev/null 2>&1
    cp log.txt "$OUT/$slug-$n.log" 2>/dev/null
  done
  s1=$(sha256sum "$OUT/$slug-1.png" 2>/dev/null | cut -c1-16)
  s2=$(sha256sum "$OUT/$slug-2.png" 2>/dev/null | cut -c1-16)
  nf=$(grep -c "was not found" "$OUT/$slug-1.log" "$OUT/$slug-2.log" 2>/dev/null | tr '\n' ' ')
  ex=$(grep -ci "exception" "$OUT/$slug-1.log" 2>/dev/null)
  [ -s "$OUT/$slug-1.png" ] && [ -s "$OUT/$slug-2.png" ] && ok=yes || ok=NOFILE
  echo "$slug $s1 $s2 notfound=[$nf] exceptions=$ex files=$ok"
done
