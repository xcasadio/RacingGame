# ADR-0034: RacingGameCasaEngine gives the road the original's sun sheen and takes the environment light off it

- **Status**: Proposed
- **Date**: 2026-11-18
- **Source**: this chantier: `ai-agent/tasks/rgce-road-tunnel-shadow-tasks.md`, slice S2 (rule of choice written before the measurement, applied mechanically by a script frozen before it; plan approved by the author on 2026-11-18 in autonomous mode, the use of the occlusion keys approved by the author the same day on the presentation of a trial); the analysis: point O9 of `ai-agent/tasks/rgce-render-gaps-tasks.md`; the measurement: `artifacts/pbr-handover/s22/s2/` (`measure_s2.json`, `measure_s2.txt`, one folder per candidate) and the frozen script `artifacts/pbr-handover/s22/scripts/measure_s2.py`; ADR-0018, ADR-0028

## Context

Facts verified on 2026-11-18, in the code and in the measurement of this slice:

- **The original's road** (`RacingGameOriginal/Core/Content/Shaders/NormalMapping.fx`, `PS_SpecularRoad20`; `Graphics/Material.cs:30-37`) is its texture times (40/255 + N.L x 210/255), plus N.L times a white Phong highlight of power 24 masked by the alpha of `Road.tga`, which is 255 everywhere: a full sun highlight and no reflection of the sky. The shadow multiplies the whole.
- **The port's road** was the `Road` family of ADR-0028 (metallic 0, roughness 0.85), shared with the road underside, the columns and the tunnel: a matt surface, with the image-based light of the sky on it. Measured on 423 track points projected into the 12 non-overview reference views, on the 223 points that the original does not darken: median luminance 84.0 in the original, 101.8 in the port; the box of the highlight of `Beginner-sector-50` (x 450-660, y 560-700): mean luminance 209.9 in the original, 110.5 in the port.
- A lower roughness alone restores the highlight but makes the road lighter and bluer, because the reflection of the sky grows with it (trial of 2026-11-18, roughness 0.35: lit road 112.7, B - R 26.0 for 11.5 in the original). A darker base colour (170, 150) does worse than the candidate below.
- The engine's `lit-pbr` material has `occlusion_texture` (red channel) and `occlusion_strength`: the occlusion is 1 + strength x (red - 1) and multiplies the image-based light only, diffuse and specular, never the direct light (`CasaEngine/CasaEngine/Content/Shaders/LitPbr.fx`). No engine change is needed.
- At the start of this slice (state of S1, engine `8ddcb31b0`), E, the mean difference per channel with the original over the 15 views, is 17.762, and the probes are 17 of 34, 0 of 4, 27 of 33, 3 of 3 (criteria 1 to 4) in `linear`.

## Decision

- **The top of the road is a new family `RoadSurface`: metallic 0, roughness 0.30** (`RacingGamePbrMaterialMapping.RoadSurface`, used by `RaceMaterialRecipes.ForRoad` only). The road underside, the columns and the tunnel keep `Road` (0, 0.85).
- **The road receives no environment light**: `Material.Track.Road` gets `occlusion_texture` = `Texture.NoEnvironmentLight` (a 4 x 4 opaque black image, `Content/Textures/NoEnvironmentLight.png`) and `occlusion_strength` 1 (`RaceMaterialRecipe.OcclusionTextureFilePath` and `OcclusionStrength`; the cooker writes neither key for a recipe without an occlusion texture, so the 98 other cooked materials are unchanged). This is a choice of material data, the original giving the road no sky reflection; it is not a workaround of an engine defect.
- The base colour of the road stays 210.

The candidates were roughness 0.30, 0.35, 0.40, 0.45 crossed with occlusion strength 0.5, 0.75, 1, measured in `linear` on a copy of the build folder. Quantities (script `measure_s2.py`, luminance 0.299 R + 0.587 G + 0.114 B): Lr, median luminance of the 223 lit road points; G, mean luminance of the highlight box; E; Tb / Ta / Te, mean luminance of the road box (x 520-760, y 600-700) of the three tunnel views; Sat, share of the pixels of the 15 views with a channel at 255. Rule, written before the measurement: eligible if |Lr - Lr of the original| <= 6, G >= 160, each tunnel box at least the start state's minus 3, Sat at most the start state's plus 0.5, E below the start state's; among the eligible candidates the smallest E, and within 0.02 of it the highest roughness, then the lowest occlusion strength.

| Roughness / occlusion strength | Lr | G | E | Tb / Ta / Te | Sat % | Eligible |
|---|---|---|---|---|---|---|
| original | 84.0 | 209.9 | - | 11.7 / 21.8 / 31.1 | - | - |
| start state (0.85, none) | 101.8 | 110.5 | 17.762 | 8.8 / 12.5 / 15.4 | 0.000 | - |
| 0.30 / 0.5 | 101.5 | 190.6 | 16.466 | 8.6 / 12.7 / 16.1 | 0.000 | no (Lr) |
| 0.30 / 0.75 | 93.9 | 190.0 | 16.050 | 7.9 / 12.1 / 15.8 | 0.000 | no (Lr) |
| 0.30 / 1 | 83.8 | 189.3 | 15.732 | 7.0 / 11.6 / 15.4 | 0.000 | yes |
| 0.35 / 0.5 | 100.7 | 187.2 | 16.537 | 8.5 / 12.9 / 16.5 | 0.000 | no (Lr) |
| 0.35 / 0.75 | 92.6 | 186.5 | 16.128 | 7.8 / 12.4 / 16.1 | 0.000 | no (Lr) |
| 0.35 / 1 | 84.3 | 185.8 | 15.791 | 7.0 / 11.9 / 15.8 | 0.000 | yes |
| 0.40 / 0.5 | 99.3 | 178.7 | 16.714 | 8.4 / 13.1 / 16.7 | 0.000 | no (Lr) |
| 0.40 / 0.75 | 92.4 | 178.0 | 16.326 | 7.8 / 12.7 / 16.4 | 0.000 | no (Lr) |
| 0.40 / 1 | 85.3 | 177.2 | 15.987 | 7.1 / 12.2 / 16.1 | 0.000 | yes |
| 0.45 / 0.5 | 97.7 | 167.6 | 16.927 | 8.4 / 13.3 / 16.9 | 0.000 | no (Lr) |
| 0.45 / 0.75 | 92.7 | 166.7 | 16.570 | 7.8 / 12.9 / 16.6 | 0.000 | no (Lr) |
| 0.45 / 1 | 86.4 | 165.7 | 16.243 | 7.2 / 12.5 / 16.3 | 0.000 | yes |

The rule retains roughness 0.30 with occlusion strength 1: it is the only candidate within 0.02 of the smallest E. The four candidates at strength 1 are eligible; the eight others leave the lit road too light.

## Consequences

- On the applied state (replays identical byte for byte to the retained candidate's captures): E 17.762 -> 15.732 in `linear` and 20.572 -> 20.249 in `legacy`; lit road 101.8 -> 83.8 for 84.0 in the original; highlight box 110.5 -> 189.3 for 209.9; probes 20 of 34, 0 of 4, 27 of 33, 3 of 3 in `linear` (17 of 34 before) and 4 of 34, 2 of 4, 27 of 33, 3 of 3 in `legacy` (5 of 34 before).
- Every image that shows the road changes: the 15 views, the race HUD, pause and finish screens, 26 of the 30 audit captures (the four others, two tunnel helpers and two terrain overviews, are identical). The six screens without a 3D world and the car selection are unchanged.
- **Known limits.** Roughness 0.30 is the lowest value measured with the occlusion: 0.25 was tried only without it (trial of 2026-11-18, `artifacts/pbr-handover/s21/road-r0.25`: lit road 119.5, highlight box 188.7, E 17.426, a state the rule would have rejected). The highlight stays below the original's (189.3 for 209.9). The road in the tunnels stays darker than the original's (7.0 / 11.6 / 15.4 for 11.7 / 21.8 / 31.1; 8.8 / 12.5 / 15.4 before): it has no sky to reflect and the ambient is the open point O6 of `rgce-render-gaps-tasks.md`. The original also darkens the road near the car on stretches tilted away from the sun (73 of the 423 points, at about the shadow colour, with no visible caster; mechanism not established): this is not imitated.
- Editing `Road.material` in the editor changes the game, and running the cooker again reverts it: the values live in `RacingGamePbrMaterialMapping.RoadSurface` and `RaceMaterialRecipes.ForRoad`.
- A new texture needs its `Image.<stem>` catalogue entry, written by `scripts/generate_rgce_content_assets.py`, before the cooker writes its `Texture.<stem>` entry.
- ADR-0028 listed the road in the `Road` family: its status line now points here. ADR-0028 is not rewritten.
