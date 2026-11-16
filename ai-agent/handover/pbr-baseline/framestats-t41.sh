#!/usr/bin/env bash
# Usage: framestats-t41.sh <outdir> <name> <aa: Fxaa|None> <ssao: 1|0> <clearcoat row: 1|0> -> runs "PBR material demo" (validation) with CASAENGINE_DEMO_FRAME_STATS=1 in the Release build
# and prints the [FrameStats] line and the [DemoConfig] line of the same run, then checks the activation counters against the variables (plan T4.1, validity rule).
OUT="$1"; NAME="$2"; AA="$3"; SSAO="$4"; ROW="$5"
HERE="$(cd "$(dirname "$0")" && pwd)"
"$HERE/framestats.sh" "$OUT" "$NAME" "PBR material demo" CASAENGINE_PBR_DEMO_VALIDATION=1 CASAENGINE_DEMO_AA="$AA" CASAENGINE_DEMO_SSAO="$SSAO" CASAENGINE_PBR_DEMO_CLEARCOAT="$ROW" > /dev/null
LOG="$OUT/$NAME.log"
FS=$(grep -h '\[FrameStats\]' "$LOG" | sed 's/^.*\[FrameStats\]/[FrameStats]/')
DC=$(grep -h '\[DemoConfig\]' "$LOG" | sed 's/^.*\[DemoConfig\]/[DemoConfig]/')
echo "$NAME: $FS"
echo "$NAME: $DC"
get() { echo "$FS" | grep -o "$1=[0-9n/a]*" | cut -d= -f2; }
FX=$(get fxaaDraws); SS=$(get ssaoStaticDraws); SK=$(get ssaoSkinnedDraws); SP=$(get ssaoPasses)
EXPAA=None; [ "$AA" = "Fxaa" ] && EXPAA=Fxaa
EXPSSAO=Off; [ "$SSAO" = "1" ] && EXPSSAO=On
EXPROW=Off; [ "$ROW" = "1" ] && EXPROW=Graded
OK=yes
echo "$DC" | grep -q "aa=$EXPAA ssao=$EXPSSAO clearCoatRow=$EXPROW" || OK=NO
if [ "$AA" = "Fxaa" ]; then [ "${FX:-0}" -ge 1 ] 2>/dev/null || OK=NO; else [ "$FX" = "0" ] || OK=NO; fi
if [ "$SSAO" = "1" ]; then
  [ "${SS:-0}" -ge 1 ] 2>/dev/null || OK=NO
  [ "${SP:-0}" -ge 1 ] 2>/dev/null || OK=NO
  # the demo draws a skinned character: its pre-pass count is expected >= 1 too
  [ "${SK:-0}" -ge 1 ] 2>/dev/null || OK=NO
else
  [ "$SS" = "0" ] && [ "$SK" = "0" ] && [ "$SP" = "0" ] || OK=NO
fi
echo "$NAME: valid=$OK (fxaaDraws=$FX ssaoStaticDraws=$SS ssaoSkinnedDraws=$SK ssaoPasses=$SP; expected aa=$EXPAA ssao=$EXPSSAO clearCoatRow=$EXPROW) exceptions=$(grep -ci exception "$LOG")"
