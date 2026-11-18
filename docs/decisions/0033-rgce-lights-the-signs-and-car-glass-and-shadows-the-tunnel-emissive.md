# ADR-0033: RacingGameCasaEngine lights the signs and the car glass as the original does and shadows the tunnel emissive

- **Status**: Accepted; amended by ADR-0038 (the window glass of the buildings changes: received shadow and base colour, per material), ADR-0039 (the base colour of the car glass is no longer grey 224)
- **Date**: 2026-11-17
- **Source**: this chantier: `ai-agent/tasks/rgce-render-gaps-tasks.md`, slice R4 (rules of choice written before the measurement, applied mechanically; plan approved by the author on 2026-11-17 in AUTO mode, to be confirmed with this record; the shadow factor of the emissive exists in the engine since slice R2, ADR-0126 of CasaEngine); the measurement: `artifacts/pbr-handover/s20/r4/` (`table.md`, `step1-rule.txt` to `step3-rule.txt`, one folder per candidate, `sheet-*.png`, `scripts/`); open points O14 and O16 of `ai-agent/tasks/rgce-pbr-handover-tasks.md`; ADR-0028, ADR-0030, ADR-0032

## Context

Facts verified on 2026-11-17, in the code and in the measurement of this slice:

- **Car glass.** The cooked glass of the car (`Models/Car/Standard 19.material`, family `CarGlass`: metallic 1, roughness 0.05) took the original's diffuse colour of the glass, 13, as its base colour. For a metal the reflectance at normal incidence is the base colour, so the glass reflected almost nothing and came out black (open point O14). The engine does what it should; the original's glass reflects about 0.75 at normal incidence. The glass of the buildings (`BuildingGlass`, the casino wall) is black in the original too (13 of 255) and must not change.
- **Signs.** The original lights a sign with the ambient of its own material: `texture x (0.3137 + relief x 0.835) x (0.85 + 0.75 x reflection)` for the chevron signs of the Beginner tunnel. `RaceMaterialRecipes.ForScenery` did not use the `AmbientColor` that `LegacyGltfModelReader` assigns to the imported material, and `lit-pbr` has no ambient per material, so the signs (family `Signage`, 18 cooked materials) were nearly black on the faces turned away from the sun, and the chevrons of the Beginner tunnel stayed black.
- **Tunnel.** ADR-0030 gives the tunnel the original's ambient as an emissive texture, at the author's level of 0.5, and the engine never shadowed an emissive: the factor `emissive_shadow_factor` (0 leaves the emissive unshadowed, 1 shadows it like the rest of the pixel) exists since slice R2. The tunnel recipe has no occlusion texture. The criterion that the author kept in open point O16 is the median of the top band of the tunnel views, rows 0 to 251, aggregated as the mean over the three views of each view's median (the original's value is 30.382 with the script of this slice; O16 had read 30.3 with the script of its time).
- At the start of this slice (state of R3, exposure 0.9, ADR-0032), E, the mean difference per channel with the original over the 15 views, is 17.947, and the probes are 17 of 34, 0 of 4, 27 of 33, 3 of 3 (criteria 1 to 4).

## Decision

Three decisions, each chosen by a rule written before the measurement and applied mechanically (the whole table is below). A candidate that did not pass its rule was not applied.

- **D1: the base colour of the car glass is 224 grey** (`RaceMaterialRecipes.LegacyCarGlassBaseColor`, was the original's diffuse colour 13); metallic 1, roughness 0.05 and the opaque queue are unchanged, and so is `BuildingGlass`. On the rear window of `Beginner-start` (box x 608-626, y 388-391, glass only on the original and on the port) the mean of the three absolute differences of the box means falls from 132.851 to 39.548. The glass is no longer black but it is not the original's: the port's mean R/G/B over the box is 114.7 / 175.7 / 196.2 for 136.9 / 137.6 / 137.9 in the original, bluer and brighter in G and B (the original's box is a neutral grey, the port's is tinted blue). **Open gap.** The windshield of the car selection screen, reported only (box x 920-1019, y 510-529): mean R/G/B 127 / 183 / 228 before the PBR (`s1m`), 7 / 10 / 12 in the state of R3, 110 / 163 / 190 now.
- **D2: the 18 materials of the `Signage` family carry the original's material ambient as an emissive** (`RaceMaterialRecipes.ForScenery`, `SignageEmissiveLevel` 1): `emissive_texture` is the material's own base colour texture, `emissive_color` is the `AmbientColor` imported for that material (read from the model's glTF, `extras.legacyEffect.floats.ambientColor`, between 0.098 and 0.392 grey, per material; the recipe computes it from the imported material, no list is written in the code) times the level 1.0, the same on the three channels; `emissive_shadow_factor` stays at its default. The rule's first branch (a level that puts both the most red pixel and the lightest pixel of the sign region of the Beginner tunnel view within +-15 of the original's) found no level (0.5, 0.75, 1.0, 1.3); the fallback branch took the level that minimises the sum of the absolute differences of the two colours, 1.0: the sum falls from 158 to 91 and E goes from 17.945 to 17.760. The most red pixel is [79, 19, 19] for [87, 0, 0] in the original: the original's red is deeper. This is a choice of material data on the precedent of ADR-0030 (the author's decision to carry the tunnel's ambient as an emissive texture), not a workaround of an engine defect (plan D5).
- **D3: the tunnel has no occlusion texture, `emissive_shadow_factor` 1, and an emissive level of 0.6 grey instead of the author's 0.5** (`RaceMaterialRecipes.LegacyTunnelEmissiveFactor` and `LegacyTunnelEmissiveShadowFactor`). Candidates: the start state (no occlusion, factor 0, level 0.5) and every triplet of occlusion (none or a 1 x 1 black texture at strength 1), factor (0.5 or 1) and level (0.5 to 1.0 by 0.1), plus the factor 0 for both occlusion settings, and levels 1.5 and 2.0 for the factor 1 since `emissive_color` accepts values above 1 (checked: levels 1.5 and 2.0 load without error and give brighter images than 1.0), 40 candidates in all. The rule: among the candidates whose top band median mean is within 1.5 of the original's 30.382 (window 28.882 to 31.882), the one with the lowest mean difference per channel over the three views, retained only if it is lower than the start state's by at least 0.3. Two candidates are in the window, both without occlusion: factor 0.5 with level 0.6 (top band 31.836, difference 15.684) and factor 1 with level 0.6 (top band 29.338, difference 15.030); the second is retained, 1.225 below the start state. No candidate with occlusion is in the window: the occlusion texture, which removes the sky reflection and the diffuse ambient from the tunnel, is not retained, and none of its files (PNG, catalogue entries, recipe field, cooker keys) is added.

### The author's decision of ADR-0030 is replaced (plan D4)

**This record replaces the level of the tunnel's emissive that the author chose in ADR-0030 (0.5 on the three channels, open point O16, 2026-11-17) by 0.6, and adds the shadow factor 1 that the engine did not offer then.** It does so with the criterion that the author had retained in O16, the median of the top band of the three tunnel views, and with the lowest mean difference per channel among the candidates that hold it; the author's decision is replaced only because this measurement retains another value. The two states, on the three tunnel views in `linear`, from the state after the glass and the signs of this slice:

| State | Emissive level | Shadow factor | Top band median, Beginner / Advanced / Expert | Mean of the three (window 28.882 to 31.882) | Mean difference per channel, Beginner / Advanced / Expert | Mean |
|---|---|---|---|---|---|---|
| ADR-0030 (the author's) | 0.5 | 0 | 22.43 / 24.08 / 38.43 | 28.314 (outside) | 11.28 / 21.13 / 16.36 | 16.255 |
| this record | 0.6 | 1 | 24.36 / 26.64 / 37.01 | 29.338 (inside) | 10.68 / 21.29 / 13.11 | 15.030 |

(The author's 0.5 had a top band median of 30.0 for 30.3 when it was measured, before the exposure of ADR-0032 and the signs of this slice; with the script of this slice it is 28.314 after the glass and the signs of this slice, and 28.051 in the state of R3.) ADR-0030 stays as written, `Accepted`, and gets one status line that points here.

### Measurements

Mode `linear`; items 1 and 2 on the 15 views against `s1c/ref` (variant `nopost`, probes without terrain-detail correction), item 3 on the three tunnel views against `s3/ref-tunnel`; every candidate cooked in a copy of the build folder outside the repository, starting from the state retained by the previous item. The tables are copied by `artifacts/pbr-handover/s20/r4/scripts/gen_adr.py` from `artifacts/pbr-handover/s20/r4/table.md`; the arithmetic of the rules is in `step1-rule.txt` to `step3-rule.txt`.

#### Item 1: car glass (CarGlass, `Car/Standard 19.material`: base_color 13 grey -> 224 grey, metallic 1, roughness 0.05 unchanged)

Rear-window box on `Beginner-start`: x 608-626, y 388-391 inclusive ([608,627) x [388,392)), 19 x 4 = 76 pixels, glass only in both images. Original mean R/G/B over the box: 136.91 / 137.57 / 137.95. Metric = mean of the three absolute differences of the box means (port minus original).

| Candidate | Folder | Box mean R/G/B (port) | Diff R/G/B (port - original) | Metric | building-away-1 (Beginner-overview): reference, port, diff, crit 1 | Lum (15 views) | E | Crit 1 | Crit 2 | Crit 3 | Crit 4 | Pick |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| start (R3 state, base 13) | base | 3.12 / 4.91 / 5.84 | -133.79 / -132.66 / -132.11 | 132.851 | 14,13,12 (0.1), 23,22,21 (0.2), +10,+9,+9, PASS | 98.72 | 17.947 | 17/34 | 0/4 | 27/33 | 3/3 | no |
| base 224 grey | glass-224 | 114.66 / 175.74 / 196.17 | -22.25 / +38.17 / +58.22 | 39.548 | 14,13,12 (0.1), 23,22,21 (0.2), +10,+9,+9, PASS | 98.73 | 17.945 | 17/34 | 0/4 | 27/33 | 3/3 | **yes** |


#### Item 2: Signage (18 materials: emissive_texture = the material's own base texture, emissive_color = imported AmbientColor x level)

Region x 770-850, y 140-220 ([770,850) x [140,220)) of the Beginner tunnel view; port = `linear`, original = `s3/ref-tunnel/Beginner-tunnel-nopost.png`. Most red pixel = argmax R-(G+B)/2; lightest pixel = argmax 0.299 R + 0.587 G + 0.114 B (`scripts/TrackCaptureCompare/Program.cs:205-208`, `Luminance`); first in row-major order on ties; searched separately in each image. Start state = after item 1 (`glass-224`, tunnel image `glass-224-tunnel`).

Original: most red pixel (816, 163) = [87, 0, 0]; lightest pixel (825, 157) = [87, 100, 111].

| Candidate | Level | Folder | Most red px (xy) rgb | Diff R/G/B | Lightest px (xy) rgb | Diff R/G/B | Sum of abs diffs | Both within +-15 | E (15 views) | E - E start | Crit 1/2/3/4 | Lum (15 views) | Eligible (E rise <= 0.1) | Pick |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| start | - | glass-224 | (835, 147) [19, 17, 15] | [-68, 17, 15] | (788, 140) [73, 81, 86] | [-14, -19, -25] | 158 | no | 17.945 | +0.000 | 17/0/27/3 | 98.73 | - | no |
| level 0.5 | 0.5 | signs-0.5 | (817, 150) [34, 18, 18] | [-53, 18, 18] | (788, 140) [73, 81, 86] | [-14, -19, -25] | 147 | no | 17.882 | -0.063 | 17/0/27/3 | 98.79 | yes | no |
| level 0.75 | 0.75 | signs-0.75 | (816, 141) [54, 19, 19] | [-33, 19, 19] | (788, 140) [73, 81, 86] | [-14, -19, -25] | 129 | no | 17.811 | -0.134 | 17/0/27/3 | 98.89 | yes | no |
| level 1.0 | 1.0 | signs-1.0 | (816, 144) [79, 19, 19] | [-8, 19, 19] | (772, 205) [82, 83, 88] | [-5, -17, -23] | 91 | no | 17.760 | -0.185 | 17/0/27/3 | 99.03 | yes | **yes** |
| level 1.3 | 1.3 | signs-1.3 | (806, 160) [114, 21, 22] | [27, 21, 22] | (773, 202) [114, 114, 121] | [27, 14, 10] | 121 | no | 17.803 | -0.142 | 16/0/27/3 | 99.25 | yes | no |

Imported AmbientColor per material (read in the model's glTF, `extras.legacyEffect.floats.ambientColor`, as `LegacyGltfModelReader.ApplyLegacyEffect` does; `scripts/signambient.py`); emissive_color = ambient x level on the three channels:

| Cooked material | glTF | Ambient |
|---|---|---|
| Models/Banner2/orig 09 - Default.material | Models/Banner2.gltf (material `orig_09_-_Default`) | 0.156863 |
| Models/Banner3/orig 09 - Default.material | Models/Banner3.gltf (material `orig_09_-_Default`) | 0.325490 |
| Models/Banner4/orig 09 - Default.material | Models/Banner4.gltf (material `orig_09_-_Default`) | 0.392157 |
| Models/Banner5/orig 09 - Default.material | Models/Banner5.gltf (material `orig_09_-_Default`) | 0.156863 |
| Models/Banner6/orig 09 - Default.material | Models/Banner6.gltf (material `orig_09_-_Default`) | 0.325490 |
| Models/Banner/orig 09 - Default.material | Models/Banner.gltf (material `orig_09_-_Default`) | 0.392157 |
| Models/Casino01/153.material | Models/Casino01.gltf (material `Material__153`) | 0.100000 |
| Models/Goal/orig 09 - Default.material | Models/Goal.gltf (material `orig_09_-_Default`) | 0.313726 |
| Models/Hotel02/163.material | Models/Hotel02.gltf (material `Material__163`) | 0.100000 |
| Models/Hotel02/casino.material | Models/Hotel02.gltf (material `casino`) | 0.100000 |
| Models/Hotel02/casino2.material | Models/Hotel02.gltf (material `casino2`) | 0.098039 |
| Models/Roadsign2/PDX19 - Default.material | Models/Roadsign2.gltf (material `PDX19_-_Default`) | 0.203922 |
| Models/Roadsign/PDX21 - Default.material | Models/Roadsign.gltf (material `PDX21_-_Default`) | 0.180392 |
| Models/Sign2/orig 09 - Default.material | Models/Sign2.gltf (material `orig_09_-_Default`) | 0.313726 |
| Models/SignCurveLeft/orig 09 - Default.material | Models/SignCurveLeft.gltf (material `orig_09_-_Default`) | 0.313726 |
| Models/SignCurveRight/orig 09 - Default.material | Models/SignCurveRight.gltf (material `orig_09_-_Default`) | 0.313726 |
| Models/SignWarning/orig 09 - Default.material | Models/SignWarning.gltf (material `orig_09_-_Default`) | 0.266667 |
| Models/Sign/orig 09 - Default.material | Models/Sign.gltf (material `orig_09_-_Default`) | 0.313726 |


#### Item 3: tunnel (RoadTunnel.material), three tunnel views in `linear`, from the state after item 2 (glass 224 + signs level 1.0)

Per view: median luminance (0.2126 R + 0.7152 G + 0.0722 B, 0-255 values) of the pixels of rows 0-251; aggregate = mean of the three medians. Centre of the window = the original's value computed with the same script (`tunnelmeas.py`, same definition as `r3/scripts/gen_bench.py`): per view 11.07, 39.29, 40.79, mean 30.382 (O16 reported 30.3, the R3 doc 30.4); window +-1.5 = [28.882, 31.882]. Difference = mean over R,G,B of the mean absolute difference per channel, per view, averaged over the 3 views. `emissive_color` above 1 is accepted and effective (levels 1.5 and 2.0 loaded without error and the images are brighter than at 1.0). Occlusion = `occlusion_texture` (1x1 black texture added to the copy) with `occlusion_strength` 1.

| # | Occlusion | Shadow factor | Level | Folder | Top median Beginner / Advanced / Expert | Mean | In window | Diff Beginner / Advanced / Expert | Mean diff | Start diff - diff | Pick |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | without | 0 | 0.5 (start state) | T-o0-f0-l0.5 | 22.43 / 24.08 / 38.43 | 28.314 | no | 11.28 / 21.13 / 16.36 | 16.255 | +0.000 | no |
| 2 | without | 0 | 0.6 | T-o0-f0-l0.6 | 24.36 / 26.64 / 47.50 | 32.836 | no | 13.50 / 21.15 / 16.45 | 17.033 | -0.779 | no |
| 3 | without | 0 | 0.7 | T-o0-f0-l0.7 | 26.72 / 29.37 / 57.92 | 38.002 | no | 16.51 / 21.85 / 19.05 | 19.138 | -2.884 | no |
| 4 | without | 0 | 0.8 | T-o0-f0-l0.8 | 29.43 / 32.37 / 69.28 | 43.695 | no | 19.79 / 22.94 / 23.23 | 21.984 | -5.729 | no |
| 5 | without | 0 | 0.9 | T-o0-f0-l0.9 | 32.57 / 35.73 / 81.30 | 49.868 | no | 23.12 / 24.26 / 27.96 | 25.115 | -8.860 | no |
| 6 | without | 0 | 1.0 | T-o0-f0-l1.0 | 36.01 / 39.29 / 93.72 | 56.339 | no | 26.48 / 25.77 / 32.64 | 28.295 | -12.041 | no |
| 7 | without | 0.5 | 0.5 | T-o0-f0.5-l0.5 | 22.43 / 24.08 / 35.93 | 27.481 | no | 9.98 / 20.99 / 15.05 | 15.338 | +0.917 | no |
| 8 | without | 0.5 | 0.6 | T-o0-f0.5-l0.6 | 24.36 / 26.64 / 44.50 | 31.836 | yes | 11.88 / 20.67 / 14.50 | 15.684 | +0.570 | no |
| 9 | without | 0.5 | 0.7 | T-o0-f0.5-l0.7 | 26.72 / 29.37 / 54.28 | 36.787 | no | 14.62 / 21.05 / 16.02 | 17.231 | -0.976 | no |
| 10 | without | 0.5 | 0.8 | T-o0-f0.5-l0.8 | 29.43 / 32.37 / 64.86 | 42.220 | no | 17.66 / 21.82 / 19.55 | 19.675 | -3.421 | no |
| 11 | without | 0.5 | 0.9 | T-o0-f0.5-l0.9 | 32.57 / 35.73 / 76.21 | 48.172 | no | 20.79 / 22.83 / 23.89 | 22.503 | -6.249 | no |
| 12 | without | 0.5 | 1.0 | T-o0-f0.5-l1.0 | 36.01 / 39.29 / 87.94 | 54.413 | no | 23.97 / 24.00 / 28.29 | 25.421 | -9.167 | no |
| 13 | without | 1 | 0.5 | T-o0-f1-l0.5 | 22.43 / 24.08 / 30.80 | 25.772 | no | 9.65 / 21.72 / 14.52 | 15.294 | +0.960 | no |
| 14 | without | 1 | 0.6 | T-o0-f1-l0.6 | 24.36 / 26.64 / 37.01 | 29.338 | yes | 10.68 / 21.29 / 13.11 | 15.030 | +1.225 | **yes** |
| 15 | without | 1 | 0.7 | T-o0-f1-l0.7 | 26.72 / 29.37 / 44.35 | 33.478 | no | 12.50 / 21.40 / 12.99 | 15.633 | +0.622 | no |
| 16 | without | 1 | 0.8 | T-o0-f1-l0.8 | 29.43 / 32.37 / 52.51 | 38.102 | no | 14.61 / 21.79 / 15.03 | 17.141 | -0.887 | no |
| 17 | without | 1 | 0.9 | T-o0-f1-l0.9 | 32.57 / 35.73 / 61.51 | 43.271 | no | 16.85 / 22.34 / 18.16 | 19.117 | -2.863 | no |
| 18 | without | 1 | 1.0 | T-o0-f1-l1.0 | 36.01 / 39.29 / 71.00 | 48.765 | no | 19.26 / 22.99 / 21.60 | 21.283 | -5.029 | no |
| 19 | without | 1 | 1.5 | T-o0-f1-l1.5 | 57.58 / 60.50 / 121.24 | 79.775 | no | 32.36 / 27.85 / 38.29 | 32.836 | -16.581 | no |
| 20 | without | 1 | 2.0 | T-o0-f1-l2.0 | 82.87 / 86.70 / 164.21 | 111.262 | no | 45.02 / 34.92 / 52.04 | 43.993 | -27.739 | no |
| 21 | with | 0 | 0.5 | T-o1-f0-l0.5 | 11.79 / 10.79 / 30.22 | 17.599 | no | 8.63 / 23.25 / 17.61 | 16.497 | -0.243 | no |
| 22 | with | 0 | 0.6 | T-o1-f0-l0.6 | 14.72 / 13.43 / 40.14 | 22.762 | no | 10.50 / 22.43 / 17.21 | 16.713 | -0.458 | no |
| 23 | with | 0 | 0.7 | T-o1-f0-l0.7 | 17.50 / 16.43 / 51.08 | 28.338 | no | 12.96 / 22.18 / 18.79 | 17.975 | -1.720 | no |
| 24 | with | 0 | 0.8 | T-o1-f0-l0.8 | 20.00 / 19.72 / 63.07 | 34.261 | no | 15.83 / 22.32 / 21.74 | 19.964 | -3.710 | no |
| 25 | with | 0 | 0.9 | T-o1-f0-l0.9 | 23.28 / 23.29 / 75.70 | 40.759 | no | 19.05 / 22.81 / 25.60 | 22.486 | -6.232 | no |
| 26 | with | 0 | 1.0 | T-o1-f0-l1.0 | 27.15 / 27.28 / 88.62 | 47.685 | no | 22.45 / 23.66 / 29.86 | 25.322 | -9.068 | no |
| 27 | with | 0.5 | 0.5 | T-o1-f0.5-l0.5 | 11.79 / 10.79 / 28.21 | 16.928 | no | 7.31 / 23.16 / 16.30 | 15.590 | +0.664 | no |
| 28 | with | 0.5 | 0.6 | T-o1-f0.5-l0.6 | 14.72 / 13.43 / 37.28 | 21.810 | no | 8.86 / 22.00 / 15.39 | 15.416 | +0.838 | no |
| 29 | with | 0.5 | 0.7 | T-o1-f0.5-l0.7 | 17.50 / 16.43 / 47.51 | 27.147 | no | 11.05 / 21.40 / 15.96 | 16.136 | +0.119 | no |
| 30 | with | 0.5 | 0.8 | T-o1-f0.5-l0.8 | 20.00 / 19.72 / 58.71 | 32.807 | no | 13.69 / 21.22 / 18.30 | 17.737 | -1.482 | no |
| 31 | with | 0.5 | 0.9 | T-o1-f0.5-l0.9 | 23.28 / 23.29 / 70.50 | 39.025 | no | 16.70 / 21.39 / 21.61 | 19.902 | -3.647 | no |
| 32 | with | 0.5 | 1.0 | T-o1-f0.5-l1.0 | 27.15 / 27.28 / 82.76 | 45.730 | no | 19.92 / 21.90 / 25.52 | 22.448 | -6.193 | no |
| 33 | with | 1 | 0.5 | T-o1-f1-l0.5 | 11.79 / 10.79 / 22.50 | 15.025 | no | 7.37 / 24.35 / 16.19 | 15.969 | +0.285 | no |
| 34 | with | 1 | 0.6 | T-o1-f1-l0.6 | 14.72 / 13.43 / 29.43 | 19.192 | no | 8.01 / 23.05 / 14.37 | 15.142 | +1.113 | no |
| 35 | with | 1 | 0.7 | T-o1-f1-l0.7 | 17.50 / 16.43 / 37.34 | 23.759 | no | 9.24 / 22.18 / 13.42 | 14.945 | +1.310 | no |
| 36 | with | 1 | 0.8 | T-o1-f1-l0.8 | 20.00 / 19.72 / 46.08 | 28.598 | no | 10.91 / 21.59 / 14.31 | 15.603 | +0.651 | no |
| 37 | with | 1 | 0.9 | T-o1-f1-l0.9 | 23.28 / 23.29 / 55.50 | 34.026 | no | 12.99 / 21.26 / 16.34 | 16.866 | -0.612 | no |
| 38 | with | 1 | 1.0 | T-o1-f1-l1.0 | 27.15 / 27.28 / 65.43 | 39.955 | no | 15.40 / 21.21 / 19.11 | 18.574 | -2.319 | no |
| 39 | with | 1 | 1.5 | T-o1-f1-l1.5 | 49.29 / 50.87 / 117.86 | 72.673 | no | 29.13 / 25.41 / 35.85 | 30.126 | -13.872 | no |
| 40 | with | 1 | 2.0 | T-o1-f1-l2.0 | 76.72 / 79.44 / 162.40 | 106.185 | no | 42.52 / 32.51 / 50.17 | 41.734 | -25.479 | no |

Original's three medians: Beginner 11.07, Advanced 39.29, Expert 40.79; mean 30.382.


## Resulting state (glass 224 + signs level 1.0 + tunnel pick), measured for information

15 views (`final-state`): lum 99.05 (original 96.29), E 17.759 (R3 state 17.947, rise -0.188), crit 17/34, 0/4, 27/33, 3/3; tunnel (`final-state-tunnel`): top medians 24.36, 26.64, 37.01, mean 29.338, mean diff 15.030; identical to `T-o0-f1-l0.6`: True.

After the application (build of the committed code, no environment override), `docs/racinggame-3d-capture-bench-local-baseline.md`, "After the signs, car glass and tunnel materials": the replays of the 15 views in `linear`, twice, are identical to each other and to the final state of the measurement (`s20/r4/final-state`) on 15 of 15, and the tunnel views on 3 of 3 (`s20/r4/final-state-tunnel`); in `legacy`, twice, identical to each other; criterion 3 is 27 of 33 in both modes; E of the 15 views is 17.759 (the state of R3: 17.947).

## Consequences

- E of the 15 views in `linear` goes from 17.947 to 17.759; the tunnel mean difference per channel from 16.255 to 15.030; criterion 3 stays at 27 of 33 in both modes. The cooked content changes in 20 materials only: `Models/Car/Standard 19` (base colour), the 18 `Signage` materials (`emissive_color`, `emissive_texture`) and `Track/RoadTunnel` (`emissive_color`, `emissive_shadow_factor`); no catalogue entry, texture or `BuildingGlass` material changes. The cooker writes `emissive_shadow_factor` only for a recipe that asks for a non-default value, as it does `emissive_texture`, and its verifier checks it.
- Editing these materials in the editor changes the game, and running the cooker again reverts them: the values live in `RaceMaterialRecipes`. ADR-0030 is amended by this record (status line); the tunnel's level and its shadow factor are here.
- The probe `building-away-1` (the black wall of the casino tower) moves from +10, +9, +9 to +12, +11, +11 with the signs' emissive (the signage family includes the materials of `Casino01` and `Hotel02`): the criterion 1 tolerance is 12, so it passes with no margin on the red channel, and the level 1.3 would have failed it. The `BuildingGlass` material is not changed and the glass alone leaves the probe as it was.
- Left open:
  - Glass: the rear window is bluer and brighter in G and B than the original's (see D1); the original's box is a neutral grey, the port's is tinted blue, and the cause is not investigated here.
  - Signs: the red of the chevrons is deeper in the original (most red pixel [79, 19, 19] against [87, 0, 0]); no level of the ambient emissive puts both measured colours within +-15.
  - Tunnel: the Beginner ceiling is still bluish and brighter than the original's (top band median 24.4 for 11.1); the sky reflection inside the tunnel stays, since no occlusion candidate was ever in the window.
  - **Jagged shadow edge in the tunnel.** Now that the shadow reaches the emissive, the right wall of the Expert tunnel shows a jagged, wavy edge of shadow (shadow-map aliasing on a wall seen at a grazing angle) where the original has a smooth dark wall (`artifacts/pbr-handover/s20/r4/sheet-tunnel-Expert.png`, seen by the main session of this slice and on the sheet again by the executor). The candidate with factor 0.5 and level 0.6 (in the window, difference 15.684 against 15.030) shows it less. Left to the author: keep the factor 1 chosen by the rule, or take 0.5 at a cost of about 0.65 of mean difference per channel (15.684 against 15.030).
  - **Beginner tunnel, left wall.** With the shadow reaching the emissive, the left wall of the Beginner tunnel is black where the state before this change (and, faintly, the original) shows a lit lamp panel (`artifacts/pbr-handover/s20/r4/sheet-tunnel-Beginner.png`, seen by the executor on the sheet; the candidate with factor 0.5 keeps the panel lit).
  - City pavement probes of `Beginner-sector-20` (lit and shadow, shadow over lit luminance ratio 3.171 for 0.276 in the original, 3.171 at the end of R3): not attainable. The original casts its shadows from a virtual point light seen in perspective, of narrow coverage and attenuated at its edges (`ShadowMapShader.cs:235-266`, `ShadowMap.fx:224-232`); the port has a directional shadow, and reproducing the original's would need an engine change that the author did not authorise (A4 of the plan, D5). The note is in `scripts/TrackCaptureCompare/probes.json`, with no probe value or criterion changed.
- The `legacy` mode changes too: its criteria are 5 of 34 (was 6), 2 of 4, 27 of 33, 3 of 3, and E 20.570 (was 21.000); `legacy` has no criterion of its own besides criterion 3 (plan D6).
- Reversible: `git revert` of the commit of this slice puts the cooked materials back with it.
