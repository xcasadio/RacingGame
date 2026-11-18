# ADR-0036: RacingGameCasaEngine sets the shadow colour of the race world against the original

- **Status**: Proposed
- **Date**: 2026-11-18
- **Source**: this chantier: `ai-agent/tasks/rgce-shadow-colour-decor-tasks.md`, slice G2 (rule of choice written before the measurement in a frozen script, applied mechanically; author's request of 2026-11-18 to continue in autonomous mode, subject "ambient of the tunnels and of the terrain"); the measurement: `artifacts/pbr-handover/s24/g2/` (`measure_tint.json`, `measure_tint.txt`, one folder per candidate) and the frozen script `artifacts/pbr-handover/s23/scripts/measure_tint.py`; open points O6 of `ai-agent/tasks/rgce-render-gaps-tasks.md` and O5 of `ai-agent/tasks/rgce-road-tunnel-shadow-tasks.md`; ADR-0032, ADR-0033

## Context

Facts verified on 2026-11-18:

- The original multiplies the final image by its shadow colour (0.25, 0.26, 0.27) where a shadow falls (`RacingGameOriginal/Core/Content/Shaders/ShadowMap.fx:36`, `:240`; `Core/Shaders/ShadowMapBlur.cs`).
- The race world used the same three numbers in the engine's `MultiplyLitColor` mode. The engine decodes the colour to linear light (`CasaEngine/CasaEngine/Framework/Rendering/Shaders/ForwardLightBinder.cs:243` at the engine commit `46acdf6fd`, power 2.2: 0.25 becomes about 0.047) and multiplies the pixel by it, the emissive excepted, before the tone curve (`Lighting.fxh:310-324`, `LitPbr.fx:565-572`, same commit). Without a tone curve that is the original's multiply; under the ACES curve of the race world (ADR-0018, exposure 0.9 of ADR-0032) dark values are compressed further.
- Measured on the probes of the bench, the shadow over lit luminance ratio of the terrain was 0.152 on `Beginner-start` and 0.139 on `Expert-sector-80` in the port, for 0.284 and 0.267 in the original: shadows about half as bright as they should be. The road of the tunnels, which is in the shadow of the tunnel, was at 7.0 / 11.6 / 15.4 (mean luminance of a box) for 11.7 / 21.8 / 31.1 in the original.
- ADR-0032 had measured the tone curve, the exposure and the ambient against these shadows and retained none of them for it; the shadow colour had not been measured.

## Decision

- **The shadow colour of the race world is (0.38, 0.3952, 0.4104)** (`RaceWorldFactory.ShadowColor`): the level 0.38 with the hue of the original, the three channels staying in its 25 : 26 : 27 ratio. The car selection carousel keeps its own settings.

Candidates: the levels 0.30 to 0.46 by 0.02, measured in `linear` with a temporary build reading an environment variable, removed before the measurement. Quantities (script `measure_tint.py`): r1, r2, the shadow over lit ratio of the terrain on the two probed views (original 0.284 and 0.267); S, the sum of their two differences with the original; E, the mean difference per channel with the original over the 15 views; M, the median of the top band of the three tunnel views (the author's criterion for the tunnel, window 28.882 to 31.882); Dt, the mean difference per channel of the three tunnel views; the probes. Rule, written before the measurement: eligible if M is in the window, criterion 3 is at least 27 and E is at most the start state's; the smallest S is retained and, within 0.01 of it, the smallest Dt.

| Level | r1 | r2 | S | E | M | Dt | Probes 1 / 2 / 3 / 4 | Eligible |
|---|---|---|---|---|---|---|---|---|
| 0.25 (start state) | 0.152 | 0.139 | 0.260 | 15.734 | 30.053 | 15.155 | 20 / 0 / 27 / 3 | yes |
| 0.30 | 0.198 | 0.176 | 0.177 | 15.573 | 30.382 | 13.911 | 20 / 0 / 27 / 3 | yes |
| 0.32 | 0.218 | 0.193 | 0.140 | 15.504 | 30.505 | 13.489 | 20 / 0 / 27 / 3 | yes |
| 0.34 | 0.239 | 0.209 | 0.103 | 15.436 | 30.600 | 13.174 | 20 / 1 / 27 / 3 | yes |
| 0.36 | 0.261 | 0.227 | 0.063 | 15.371 | 30.741 | 12.940 | 20 / 2 / 27 / 3 | yes |
| 0.38 | 0.284 | 0.246 | 0.021 | 15.312 | 30.885 | 12.844 | 20 / 2 / 27 / 3 | yes |
| 0.40 | 0.307 | 0.265 | 0.025 | 15.264 | 31.053 | 13.016 | 20 / 2 / 27 / 3 | yes |
| 0.42 | 0.332 | 0.284 | 0.065 | 15.233 | 31.267 | 13.552 | 20 / 2 / 27 / 3 | yes |
| 0.44 | 0.357 | 0.305 | 0.111 | 15.219 | 31.481 | 14.336 | 20 / 1 / 27 / 3 | yes |
| 0.46 | 0.383 | 0.326 | 0.158 | 15.212 | 31.647 | 15.246 | 20 / 0 / 27 / 3 | yes |

The rule retains the level 0.38: 0.38 and 0.40 are within 0.01 of the smallest S, and 0.38 has the smaller Dt.

## Consequences

- On the applied state (15 views and 3 tunnel views byte-identical to the captures of the retained candidate): terrain ratios 0.284 and 0.246 for 0.284 and 0.267 in the original (0.152 and 0.139 before); E 15.734 -> 15.312 in `linear`; the tunnels' difference with the original 15.155 -> 12.844, and their walls stay in the author's window (30.885); the road of the tunnels 11.5 / 20.2 / 27.9 for 11.7 / 21.8 / 31.1 in the original (7.0 / 11.6 / 15.4 before); criterion 2 of the probes goes from 0 to 2 of 4.
- The emissive of the tunnel, which receives the shadow (ADR-0033, setting kept by the author on 2026-11-18), is lighter in the shade with this colour; the rule kept the author's criterion for it.
- **`legacy` mode.** There the colour is not decoded, so 0.38 is lighter than the original's shadow: E goes from 20.245 to 20.009, criterion 2 from 2 to 0 of 4, criterion 3 stays at 27 of 33. The `legacy` mode is a bench mode (it is not what the game shows).
- Known limits: the level is tied to the tone curve and the exposure of the race world; changing either calls for a new measurement. The shadows that the original casts on the road near the car, and its perspective shadow, are still not reproduced.
- No ADR had recorded the shadow colour before; the comment of `RaceWorldFactory` now points here.
