# 3D capture bench: baseline of the local version, before PBR

First run of the capture bench (ADR-0015) on the game code of this machine, taken on 2026-11-16 before any change to the engine format, the content or the materials. Plan: `ai-agent/tasks/rgce-pbr-handover-tasks.md`, slice S1c.

This is a different measurement from `racinggame-3d-capture-bench-findings.md`. That document records the other machine's version of the game, whose race world code is not in this tree. The figures below describe the version of this machine: generator components, cooked `lit-diffuse` assets, legacy colour pipeline.

Every figure cites its output file under `artifacts/pbr-handover/s1c/` (not in git).

The object placement was then aligned on the original; the second run, which is the reference taken before PBR, is in the last section. Decisions: see ADR-0027.

## Run

- Game `40fb7c1`, engine `b429b79ec`, Debug builds.
- Original: `RacingGame.exe --capture-track-audit --seed 1 --size 1280x720 --out ref`. Its LAN multiplayer is removed in this copy.
- Port: `RacingGameCasaEngine.exe --capture-reference-views ref/views.json --reference-capture-out cand-a`, then a second run to `cand-b`.
- Comparison: `scripts/TrackCaptureCompare`, variants `nopost` and `full`, then the probes of `probes.json` with `--no-terrain-detail-correction`.
- The port logs `pipeline legacy fxaaPasses 0` for the 15 pictures (`cand-a/run.log`).

## Alignment and repeatability

- The cameras are replayed at the right place. For each view, the distance between the original's car position, converted, and the nearest anchor of the port (`cand-a/run.log`, `alignment` lines):

| Track | start | sector-20 | sector-50 | sector-80 | overview |
|---|---|---|---|---|---|
| Beginner | 0.00 m | 1.07 m | 0.00 m | 1.57 m | 0.00 m |
| Advanced | 0.00 m | 1.14 m | 0.00 m | 1.72 m | 0.00 m |
| Expert | 0.00 m | 0.53 m | 3.68 m | 1.83 m | 0.00 m |

- The conversion is exact: a point of the original converted to the race world and back moves by 0.000061 m at most, and the placed transforms come back unchanged (`cand-a/run.log`, `round trip` lines).
- Two runs give 14 identical files of 15. `Expert-start.png` differs by its hash, with a mean difference of 0.00 on every channel (`compare-repeat/report.md`). A third run, made by the independent verification, differs from the first by the hash of two files, `Expert-start.png` and `Expert-overview.png` (`verify/cand`); the size of that difference was not measured.

## Pictures against the original

Mean absolute difference per channel (0 to 255) and share of the original's edges found within 2 pixels. `nopost` is the original without its post-processing, the fair comparison for a port without glow (`compare-nopost/report.md`, `compare-full/report.md`).

| View | R, G, B `nopost` | Edges `nopost` | R, G, B `full` | Edges `full` |
|---|---|---|---|---|
| Beginner-start | 30.78, 27.89, 24.51 | 49.0 % | 39.29, 35.86, 31.42 | 58.9 % |
| Beginner-sector-20 | 54.14, 55.10, 56.49 | 31.9 % | 68.95, 66.86, 67.95 | 44.7 % |
| Beginner-sector-50 | 25.46, 22.92, 20.05 | 50.6 % | 44.34, 36.93, 31.42 | 69.0 % |
| Beginner-sector-80 | 33.22, 31.57, 26.89 | 48.6 % | 51.13, 45.39, 37.05 | 63.3 % |
| Beginner-overview | 22.78, 20.34, 18.57 | 80.9 % | 38.05, 34.73, 32.61 | 85.4 % |
| Advanced-start | 24.61, 21.76, 19.26 | 42.1 % | 49.71, 40.36, 33.91 | 62.1 % |
| Advanced-sector-20 | 47.27, 39.92, 32.56 | 42.6 % | 68.10, 54.67, 45.50 | 66.5 % |
| Advanced-sector-50 | 26.01, 23.50, 20.84 | 51.5 % | 46.44, 38.85, 33.33 | 65.6 % |
| Advanced-sector-80 | 23.80, 21.39, 18.40 | 39.0 % | 42.07, 33.77, 27.84 | 48.2 % |
| Advanced-overview | 23.10, 20.02, 17.56 | 85.5 % | 43.60, 37.50, 32.73 | 87.7 % |
| Expert-start | 29.04, 26.39, 21.96 | 39.8 % | 39.16, 35.45, 30.13 | 56.7 % |
| Expert-sector-20 | 26.99, 25.03, 21.41 | 32.3 % | 43.92, 36.29, 28.98 | 47.1 % |
| Expert-sector-50 | 31.53, 30.15, 30.17 | 43.5 % | 51.81, 44.59, 41.61 | 60.4 % |
| Expert-sector-80 | 19.76, 17.99, 15.34 | 40.8 % | 30.60, 25.00, 21.65 | 69.1 % |
| Expert-overview | 24.64, 22.56, 21.57 | 88.4 % | 43.71, 39.19, 36.97 | 88.4 % |

`Beginner-sector-20` is the worst view: the ground of the city is black in the port (mean luminance 52.1 against 79.7), because the city ground does not exist in this version.

## Light and shadow probes

`compare-probes/probes.md`, criteria of the tool's README:

| Criterion | Passed | Failed | Judged |
|---|---|---|---|
| 1. `lit` and `away` within 12 of the original | 10 | 24 | 34 |
| 2. shadow over lit ratio within 0.05 | 0 | 4 | 4 |
| 3. a shadowed point is shadowed, a lit point is lit | 21 | 12 | 33 |
| 4. `away` and shadowed within 12 | 0 | 3 | 3 |

The shadow over lit ratio of the original is 0.23 to 0.28. In the port it is 0.67 to 1.13: the shadows are far lighter, and several points the original shadows are lit in the port.

## Scene against the original

`cand-a/scene-measure-<Track>.txt`, in the original's coordinates.

- **Road points: identical.** 1332, 2172 and 4466 points on both sides, position difference 0.000 m, right vector within 0.04 degree, up vector within 0.03 degree, road width identical.
- **Placed objects:**

| Track | Original | Port | Matched within 1 m | Missing in the port | Extra in the port |
|---|---|---|---|---|---|
| Beginner | 1147 | 821 | 712 | 435 | 109 |
| Advanced | 1693 | 1226 | 1007 | 686 | 219 |
| Expert | 3404 | 2595 | 2140 | 1264 | 455 |

- **Shadow flags:** of the matched objects, 620 of 712 (Beginner), 864 of 1007 (Advanced) and 1771 of 2140 (Expert) cast a shadow in the port and not in the original. The port sets cast and receive on almost every placed object.

## Divergences by topic

Topics both machines implemented keep this machine's version as the reference (decision D7 of the plan). The measure shows where that version differs from the original. Figures are original / port / matched, for Beginner, Advanced, Expert.

| Topic | What the measure shows |
|---|---|
| Road spline and widths | No difference. |
| Guard rail posts | Every post of the original is matched (455/455/455, 742/743/742, 1529/1529/1529; 0.518 m at most; one extra post on Advanced), but a scale ratio difference of 0.167 on every post: the port scales them by 1.125, the original by 1.35. |
| Column segments | Two to three times too many: 46/101/43, 50/160/47, 137/337/130 (2.2, 3.2 and 2.5 times the original's count). The extra segments have no counterpart in the original. |
| Lanterns | Every `Laterne` of the original is matched, and the port adds more (about 1.4 times the original's count): 39/53/39, 62/89/62, 228/330/228. The two-sided lanterns are all missing: `Laterne2Sides` 1/0/0, 3/0/0, 9/0/0. |
| Palms | Far fewer than the original and few matched: `AlphaPalm` 80/17/14, 101/31/18, 219/54/34; `AlphaPalmSmall` 49/6/5, 52/2/0, 81/9/0. Matched palms are rotated by up to 177 degrees. |
| Signs and banners | Counts differ and few match: `Sign` 18/7/1, 22/16/3, 36/22/2; `Sign2` 6/7/0, 15/12/0, 37/32/5. |
| Buildings, hotels, start light | `Building` to `Building5`, `Hotel01`, `Hotel02` and `StartLight` are matched one for one on Beginner. `Casino01` is 3/0/0, 6/2/2, 9/5/5 and `Hotel01` is 5/3/3 on Expert. |
| Shadows | The port's objects cast where the original's do not, and the shadows are much lighter (probes above). |

What is missing because it exists only on the other machine, to be ported by slice S1h:

- the random filler scenery: most of the missing rocks, ruins, stones, cactuses and small palms (`Ruin` 75/7/7, `SharpRock` 105/36/36, `Stone5` 41/14/14 on Beginner);
- the city ground (black ground of `Beginner-sector-20`);
- the road underside.

Other differences seen in the pictures:

- The sky is the mirror image of the original's. The other machine corrected it by converting the sky cube (ADR-0017), to be ported by slice S1e.
- The car is orange with stripes; the original's capture draws a white car (`carColor` 255, 255, 255 in `views.json`). The port's first colour is orange.
- The captures use the front-end options saved by the user, as the other capture modes do.

## Limits

- One run on one machine. The picture figures depend on the GPU.
- The object match uses the tool's fixed 1 m distance. A palm drawn from another random sequence counts as missing and extra at once.
- Nothing here says which side is right for the gameplay. It says how far this version is from the original.

## After the alignment on the original's object placement

Second run, on 2026-11-16, after slice S1a of the plan: the objects are placed through the original's add path, in its draw order, and cast and receive shadows as the original's do (ADR-0027). This run is the reference taken before PBR. The figures above stay as the record of the version before the alignment.

Outputs under `artifacts/pbr-handover/s1a/`; the original's export is the same one, `artifacts/pbr-handover/s1c/ref`. The tables of this section are generated from the output files by a script.

### Scene against the original

`cand-a/scene-measure-<Track>.txt`.

| Track | Road points | Objects original / port | Matched | Missing | Extra | Shadow cast / receive mismatches |
|---|---|---|---|---|---|---|
| Beginner | 1332 / 1332, 0.000 m at most | 1147 / 1147 | 1147 | 0 | 0 | 0 / 0 |
| Advanced | 2172 / 2172, 0.000 m at most | 1693 / 1693 | 1693 | 0 | 0 | 0 / 0 |
| Expert | 4466 / 4466, 0.000 m at most | 3404 / 3404 | 3404 | 0 | 0 | 0 / 0 |

Largest difference of a matched object, per track, with the model that carries it:

| Track | Distance | Scale ratio | Rotation | Rotation without `GuardRailHolder` (one of the models at that value) |
|---|---|---|---|---|
| Beginner | 0.000 m | 0.000 | 1.08 degrees (`GuardRailHolder`) | 0.04 degrees (`AlphaPalm2`) |
| Advanced | 0.000 m | 0.000 | 6.81 degrees (`GuardRailHolder`) | 0.04 degrees (`AlphaPalm`) |
| Expert | 0.000 m | 0.000 | 11.91 degrees (`GuardRailHolder`) | 0.04 degrees (`AlphaPalm`) |

The rotation of the guard rail posts is read on the entities the generators built. The original's post matrix is sheared on a slope (its right, direction and up vectors are not at a right angle); an entity holds a scale, a rotation and a translation, so it keeps the closest rotation. Position and scale are exact. The scenery is read from the layout, not from the entities: lanterns, banners, signs and the start light carry the same shear in their entities, which this column does not show. Open point O9 of the plan.

### Pictures against the original

`compare-nopost/report.md`, `compare-full/report.md`; the `nopost` edges before the alignment are repeated from `artifacts/pbr-handover/s1c/compare-nopost/report.md`.

| View | R, G, B `nopost` | Edges `nopost` | Edges `nopost` before | R, G, B `full` | Edges `full` |
|---|---|---|---|---|---|
| Beginner-start | 29.69, 27.03, 23.51 | 58.9 % | 49.0 % | 39.69, 36.01, 31.20 | 73.0 % |
| Beginner-sector-20 | 55.09, 56.25, 56.78 | 33.2 % | 31.9 % | 69.24, 67.67, 68.40 | 47.5 % |
| Beginner-sector-50 | 25.32, 22.78, 20.00 | 53.2 % | 50.6 % | 44.71, 37.16, 31.74 | 75.1 % |
| Beginner-sector-80 | 28.76, 27.44, 23.88 | 55.4 % | 48.6 % | 49.28, 43.14, 35.37 | 74.3 % |
| Beginner-overview | 22.03, 19.60, 17.81 | 86.9 % | 80.9 % | 38.02, 34.62, 32.34 | 94.3 % |
| Advanced-start | 24.46, 21.60, 19.11 | 42.3 % | 42.1 % | 49.71, 40.30, 33.85 | 62.9 % |
| Advanced-sector-20 | 46.59, 39.27, 32.00 | 42.9 % | 42.6 % | 67.92, 54.38, 45.27 | 68.2 % |
| Advanced-sector-50 | 26.14, 23.64, 21.06 | 55.4 % | 51.5 % | 46.78, 39.20, 33.77 | 76.7 % |
| Advanced-sector-80 | 21.10, 18.82, 16.44 | 45.0 % | 39.0 % | 39.51, 31.40, 25.93 | 61.1 % |
| Advanced-overview | 22.85, 19.82, 17.40 | 87.3 % | 85.5 % | 43.89, 37.70, 32.88 | 92.3 % |
| Expert-start | 28.15, 25.56, 21.73 | 56.4 % | 39.8 % | 38.87, 35.28, 30.23 | 69.5 % |
| Expert-sector-20 | 25.72, 23.26, 19.71 | 40.7 % | 32.3 % | 43.00, 34.73, 27.74 | 58.0 % |
| Expert-sector-50 | 27.48, 24.74, 22.63 | 51.6 % | 43.5 % | 46.69, 38.91, 33.75 | 71.0 % |
| Expert-sector-80 | 19.61, 17.84, 15.15 | 46.5 % | 40.8 % | 30.82, 25.14, 21.77 | 77.2 % |
| Expert-overview | 24.44, 22.43, 21.49 | 93.7 % | 88.4 % | 44.52, 39.94, 37.64 | 94.1 % |

### Light and shadow probes

`compare-probes/probes.md`.

| Criterion | Passed | Failed | Judged |
|---|---|---|---|
| 1. lit and away within +-12 | 11 | 23 | 34 |
| 2. shadow/lit ratio within +-0.05 (per view and surface) | 0 | 4 | 4 |
| 3. class agreement (shadow shadowed, lit lit) | 21 | 12 | 33 |
| 4. away-shadowed within +-12 | 0 | 3 | 3 |

Shadow over lit luminance ratio, original then port: terrain 0.284 then 1.128; city 0.276 then 0.682; road 0.228 then 0.673; terrain 0.267 then 1.024. The lighting is not part of this slice, so these stay as before.

### Repeatability

Two runs (`cand-a`, `cand-b`, `compare-repeat/report.md`): 12 identical files of 15. Different by their hash: `Advanced-overview.png` (mean difference 0.00, 0.00, 0.00), `Expert-overview.png` (mean difference 0.00, 0.00, 0.00), `Expert-start.png` (mean difference 0.00, 0.00, 0.00).

Outside the bench the seed is fixed per track. Placement digests logged by two runs of `--capture-track-audit` (`audit-run-1.log`, `audit-run-2.log`):

| Track | Placements | Digest, run 1 | Digest, run 2 |
|---|---|---|---|
| Beginner | 1274 | `6EBF84F81D01AC88` | `6EBF84F81D01AC88` |
| Advanced | 1757 | `FD0564E1183D98FE` | `FD0564E1183D98FE` |
| Expert | 3432 | `29A2403A3F0CDCFA` | `29A2403A3F0CDCFA` |


## After the tangents, the normal maps and the sky cube

Third run, on 2026-11-17, after slice S1e of the plan: the models carry the original's tangents, the normal maps are converted to the standard channel layout and the sky cube to the port's axes (ADR-0016, ADR-0017), imported colours are rounded and materials without a legacy effect get the original's defaults. The `lit-diffuse` materials that name a normal map now draw with it. This run is the reference before the next slices. The two previous runs above stay as their record.

Outputs under `artifacts/pbr-handover/s1e/`; the original's export is the same one, `artifacts/pbr-handover/s1c/ref`. The tables of this section are generated from the output files by a script.

### Scene against the original

`cand-a/scene-measure-<Track>.txt`, unchanged by this slice:

| Track | Objects original / port | Matched | Missing | Extra | Shadow cast / receive mismatches |
|---|---|---|---|---|---|
| Beginner | 1147 / 1147 | 1147 | 0 | 0 | 0 / 0 |
| Advanced | 1693 / 1693 | 1693 | 0 | 0 | 0 / 0 |
| Expert | 3404 / 3404 | 3404 | 0 | 0 | 0 / 0 |

### Pictures against the original

`compare-nopost/report.md`, `compare-full/report.md`; the `nopost` edges of the previous run come from `artifacts/pbr-handover/s1a/compare-nopost/report.md`. The last column is the mean difference with the previous run's picture of the same view (`compare-vs-s1a/report.md`), the size of this slice's change on screen.

| View | R, G, B `nopost` | Edges `nopost` | Edges `nopost` before | R, G, B `full` | Edges `full` | Change from the previous run, R, G, B |
|---|---|---|---|---|---|---|
| Beginner-start | 26.32, 24.50, 21.38 | 61.3 % | 58.9 % | 39.65, 35.45, 30.51 | 77.0 % | 14.36, 12.02, 9.30 |
| Beginner-sector-20 | 47.57, 49.21, 50.41 | 34.1 % | 33.2 % | 61.96, 60.85, 62.29 | 48.1 % | 13.33, 11.89, 9.76 |
| Beginner-sector-50 | 28.23, 25.31, 21.49 | 53.8 % | 53.2 % | 48.75, 41.13, 34.05 | 75.7 % | 14.98, 12.58, 9.17 |
| Beginner-sector-80 | 27.29, 25.97, 22.46 | 56.5 % | 55.4 % | 49.65, 43.09, 34.97 | 75.3 % | 15.23, 13.96, 11.02 |
| Beginner-overview | 16.06, 15.07, 13.60 | 86.4 % | 86.9 % | 37.10, 31.77, 28.35 | 98.5 % | 24.94, 20.84, 16.28 |
| Advanced-start | 23.78, 21.42, 18.69 | 41.8 % | 42.3 % | 50.92, 41.05, 33.93 | 62.5 % | 12.45, 10.08, 7.45 |
| Advanced-sector-20 | 39.65, 34.54, 28.63 | 45.4 % | 42.9 % | 63.36, 50.97, 42.67 | 70.5 % | 17.15, 12.54, 8.36 |
| Advanced-sector-50 | 23.67, 21.08, 18.68 | 56.5 % | 55.4 % | 45.84, 37.42, 31.89 | 77.3 % | 13.00, 11.87, 10.20 |
| Advanced-sector-80 | 25.17, 22.69, 19.53 | 47.5 % | 45.0 % | 42.63, 36.23, 30.33 | 61.3 % | 16.42, 14.30, 11.25 |
| Advanced-overview | 18.13, 16.23, 13.50 | 91.1 % | 87.3 % | 38.86, 33.92, 29.15 | 99.1 % | 26.28, 22.12, 17.62 |
| Expert-start | 23.68, 21.97, 19.03 | 58.8 % | 56.4 % | 38.52, 33.78, 28.83 | 73.2 % | 13.94, 11.73, 9.04 |
| Expert-sector-20 | 27.47, 25.09, 21.56 | 40.3 % | 40.7 % | 47.29, 39.44, 32.01 | 57.5 % | 15.91, 14.02, 10.96 |
| Expert-sector-50 | 25.20, 24.09, 22.31 | 54.3 % | 51.6 % | 44.33, 38.87, 34.43 | 72.9 % | 15.25, 11.79, 9.02 |
| Expert-sector-80 | 21.59, 19.11, 16.03 | 42.4 % | 46.5 % | 40.62, 31.21, 25.01 | 75.4 % | 23.11, 19.73, 15.35 |
| Expert-overview | 16.90, 16.33, 15.57 | 96.8 % | 93.7 % | 38.04, 33.89, 31.47 | 99.0 % | 24.68, 21.36, 17.79 |

### Light and shadow probes

`compare-probes/probes.md`.

| Criterion | Passed | Failed | Judged |
|---|---|---|---|
| 1. lit and away within +-12 | 11 | 23 | 34 |
| 2. shadow/lit ratio within +-0.05 (per view and surface) | 0 | 4 | 4 |
| 3. class agreement (shadow shadowed, lit lit) | 21 | 12 | 33 |
| 4. away-shadowed within +-12 | 0 | 3 | 3 |

Shadow over lit luminance ratio, original then port: terrain 0.284 then 1.049; city 0.276 then 0.965; road 0.228 then 0.673; terrain 0.267 then 1.010.

### Repeatability

Two runs (`cand-a`, `cand-b`, `compare-repeat/report.md`): 12 identical files of 15. Different by their hash: `Advanced-overview.png` (mean difference 0.00, 0.00, 0.00), `Expert-overview.png` (mean difference 0.00, 0.00, 0.00), `Expert-start.png` (mean difference 0.00, 0.00, 0.00).

### Other references of this machine

UI captures (`ui/`) against the previous reference `artifacts/pbr-handover/s1a/ui` (`ui-compare/report.txt`). The 3D states change with the sky and the normal maps:

| State | R | G | B |
|---|---|---|---|
| splash | 0.00 | 0.00 | 0.00 |
| main-menu | 0.00 | 0.00 | 0.00 |
| highscores | 0.00 | 0.00 | 0.00 |
| options | 0.00 | 0.00 | 0.00 |
| help | 0.00 | 0.00 | 0.00 |
| car-selection | 1.56 | 1.55 | 1.51 |
| track-selection | 0.00 | 0.00 | 0.00 |
| race-hud | 15.02 | 14.96 | 13.97 |
| pause | 4.89 | 4.87 | 4.51 |
| race-finished | 5.24 | 4.83 | 4.13 |

Track audit (`audit/`, 30 captures) against `artifacts/pbr-handover/s1a/audit-1`: largest channel difference 28.136 (`audit-Advanced-road-only-start`), smallest 1.671 (`audit-Expert-helper-tunnel`).

## After the mipmaps

Fourth run, on 2026-11-17, after slice S1m of the plan: every cooked `.texture` asks for its mipmap chain (`generate_mipmaps`, ADR-0124 of the engine) and the two car paint textures are created with theirs, rewritten after each paint. The run before it is the previous section, `artifacts/pbr-handover/s1e/`. This run is the reference before the next slices. The previous runs above stay as their record.

Outputs under `artifacts/pbr-handover/s1m/`; the original's export is the same one, `artifacts/pbr-handover/s1c/ref`. The tables of this section are generated from the output files by a script.

### Mipmap chains

The lines `[CaptureBench] mipmaps` of `cand-a/run.log`, written once the first race world is ready (`Levels` is the level count read from the texture, `Expected` the full chain of its size, `Chain` compares level 1 read back with the box average of level 0 read back):

| Texture | Levels | Expected | Chain |
|---|---|---|---|
| Texture.Road | 11 | 11 | - |
| Texture.Landscape | 12 | 12 | - |
| Texture.Building | 10 | 10 | - |
| CarPaint.Painted | 12 | 12 | ok |
| CarPaint.Unpainted | 12 | 12 | ok |

`cand-b/run.log` has the same lines.

### Scene against the original

`cand-a/run.log`, scene measure of each track:

| Track | Objects original / port | Matched | Missing | Extra | Shadow cast / receive mismatches |
|---|---|---|---|---|---|
| Beginner | 1147 / 1147 | 1147 | 0 | 0 | 0 / 0 |
| Advanced | 1693 / 1693 | 1693 | 0 | 0 | 0 / 0 |
| Expert | 3404 / 3404 | 3404 | 0 | 0 | 0 / 0 |

### Pictures against the original

`compare-nopost/report.md`, `compare-full/report.md`; the `nopost` edges of the previous run come from `artifacts/pbr-handover/s1e/compare-nopost/report.md`. The last column is the mean difference with the previous run's picture of the same view (`compare-vs-s1e/report.md`), the size of this slice's change on screen.

| View | R, G, B `nopost` | Edges `nopost` | Edges `nopost` before | R, G, B `full` | Edges `full` | Change from the previous run, R, G, B |
|---|---|---|---|---|---|---|
| Beginner-start | 26.20, 24.37, 21.29 | 60.7 % | 61.3 % | 39.53, 35.35, 30.43 | 76.6 % | 1.08, 1.08, 0.92 |
| Beginner-sector-20 | 47.59, 49.24, 50.48 | 33.8 % | 34.1 % | 61.76, 60.67, 62.18 | 47.7 % | 1.46, 1.47, 1.24 |
| Beginner-sector-50 | 28.15, 25.22, 21.41 | 48.1 % | 53.8 % | 48.60, 40.95, 33.88 | 71.3 % | 1.30, 1.32, 1.23 |
| Beginner-sector-80 | 27.17, 25.86, 22.39 | 56.3 % | 56.5 % | 49.52, 42.94, 34.84 | 75.0 % | 1.50, 1.47, 1.35 |
| Beginner-overview | 15.90, 14.90, 13.45 | 86.3 % | 86.4 % | 36.99, 31.64, 28.23 | 98.4 % | 0.79, 0.77, 0.67 |
| Advanced-start | 23.78, 21.40, 18.69 | 40.6 % | 41.8 % | 50.90, 41.01, 33.89 | 62.3 % | 0.78, 0.80, 0.69 |
| Advanced-sector-20 | 39.58, 34.46, 28.55 | 41.5 % | 45.4 % | 63.31, 50.90, 42.61 | 67.2 % | 0.62, 0.63, 0.59 |
| Advanced-sector-50 | 23.55, 20.96, 18.58 | 52.7 % | 56.5 % | 45.61, 37.20, 31.70 | 75.3 % | 1.31, 1.29, 1.22 |
| Advanced-sector-80 | 25.18, 22.70, 19.54 | 46.9 % | 47.5 % | 42.47, 36.10, 30.20 | 60.9 % | 1.17, 1.17, 1.12 |
| Advanced-overview | 18.04, 16.14, 13.43 | 90.9 % | 91.1 % | 38.82, 33.88, 29.10 | 99.0 % | 0.46, 0.43, 0.34 |
| Expert-start | 23.65, 21.93, 19.01 | 57.9 % | 58.8 % | 38.36, 33.59, 28.70 | 72.0 % | 1.33, 1.38, 1.16 |
| Expert-sector-20 | 27.55, 25.13, 21.63 | 39.5 % | 40.3 % | 47.32, 39.43, 32.03 | 56.7 % | 1.12, 1.20, 0.94 |
| Expert-sector-50 | 25.24, 24.15, 22.37 | 53.3 % | 54.3 % | 44.28, 38.83, 34.40 | 72.5 % | 1.11, 1.12, 1.09 |
| Expert-sector-80 | 21.56, 19.07, 16.00 | 42.1 % | 42.4 % | 40.54, 31.13, 24.95 | 75.3 % | 0.80, 0.81, 0.74 |
| Expert-overview | 16.74, 16.16, 15.43 | 96.4 % | 96.8 % | 37.95, 33.79, 31.38 | 98.7 % | 0.99, 0.95, 0.80 |

Change from the previous run, mean of the three channels: largest 1.44 (`Beginner-sector-80`), smallest 0.41 (`Advanced-overview`).

Change of the `nopost` edges matched since the previous run, in points: highest -0.1 (`Beginner-overview`), lowest -5.7 (`Beginner-sector-50`); 15 of 15 views are lower, 0 higher.

### Light and shadow probes

`compare-probes/probes.md`; the column before comes from `artifacts/pbr-handover/s1e/compare-probes/probes.md`.

| Criterion | Passed | Failed | Judged | Passed before |
|---|---|---|---|---|
| 1. lit and away within +-12 | 11 | 23 | 34 | 11 |
| 2. shadow/lit ratio within +-0.05 (per view and surface) | 0 | 4 | 4 | 0 |
| 3. class agreement (shadow shadowed, lit lit) | 21 | 12 | 33 | 21 |
| 4. away-shadowed within +-12 | 0 | 3 | 3 | 0 |

Shadow over lit luminance ratio, original then port: terrain 0.284 then 1.049; city 0.276 then 0.965; road 0.228 then 0.679; terrain 0.267 then 1.010.

### Repeatability

Two runs (`cand-a`, `cand-b`, `compare-repeat/report.md`): 12 identical files of 15. Different by their hash: `Advanced-overview.png` (mean difference 0.00, 0.00, 0.00), `Expert-overview.png` (mean difference 0.00, 0.00, 0.00), `Expert-start.png` (mean difference 0.00, 0.00, 0.00).

### Other references of this machine

UI captures (`ui/`) against the previous reference `artifacts/pbr-handover/s1e/ui` (`ui-compare/report.txt`):

| State | R | G | B |
|---|---|---|---|
| splash | 0.00 | 0.00 | 0.00 |
| main-menu | 0.00 | 0.00 | 0.00 |
| highscores | 0.00 | 0.00 | 0.00 |
| options | 0.00 | 0.00 | 0.00 |
| help | 0.00 | 0.00 | 0.00 |
| car-selection | 0.22 | 0.20 | 0.20 |
| track-selection | 0.00 | 0.00 | 0.00 |
| race-hud | 0.79 | 0.79 | 0.75 |
| pause | 0.17 | 0.17 | 0.16 |
| race-finished | 0.19 | 0.20 | 0.18 |

Track audit (`audit/`, 30 captures) against `artifacts/pbr-handover/s1e/audit`: largest channel difference 1.589 (`audit-Expert-sector-80`), smallest 0.005 (`audit-Beginner-terrain-overview`).

## After the PBR materials

Fifth run, on 2026-11-17, after slice S1f of the plan: the 98 cooked materials of the race world are `lit-pbr` (metallic, roughness and clear coat by surface family, ported from the other machine's `RacingGamePbrMaterialMapping`), the terrain material has its detail texture, and the car paint copies a cooked `LitPbrMaterial`. The run before it is the previous section, `artifacts/pbr-handover/s1m/`. The colour pipeline (`legacy`), the light and the shadows are unchanged: this run is an intermediate state, measured and not tuned, to be set by the next slice. The previous runs above stay as their record.

Outputs under `artifacts/pbr-handover/s1f/`; the original's export is the same one, `artifacts/pbr-handover/s1c/ref`. The tables of this section are generated from the output files and from the cooked `.material` files by a script.

### Cooked materials

Definitions of the 98 cooked `.material` files: `lit-pbr` 98. Files without a planned material: 0.

Materials by family (family from the cook plan, values read back from the cooked files):

| Family | Materials | Metallic | Roughness | Clear coat | Clear coat roughness |
|---|---|---|---|---|---|
| Default | 29 | 0 | 0.8 | 0 | 0.1 |
| Signage | 18 | 0 | 0.6 | 0 | 0.1 |
| Masonry | 13 | 0 | 0.9 | 0 | 0.1 |
| LampPost | 11 | 1 | 0.5 | 0 | 0.1 |
| MatteIndustrial | 6 | 0 | 0.7 | 0 | 0.1 |
| Vegetation | 4 | 0 | 0.95 | 0 | 0.1 |
| BuildingGlass | 3 | 1 | 0.05 | 0 | 0.1 |
| Road | 3 | 0 | 0.85 | 0 | 0.1 |
| CarChrome | 2 | 1 | 0.1 | 0 | 0.1 |
| CarTyre | 2 | 0 | 0.9 | 0 | 0.1 |
| GuardRail | 2 | 1 | 0.45 | 0 | 0.1 |
| Terrain | 2 | 0 | 1 | 0 | 0.1 |
| CarGlass | 1 | 1 | 0.05 | 0 | 0.1 |
| CarPaint | 1 | 0 | 0.5 | 1 | 0.05 |
| CarouselPlate | 1 | 1 | 0.3 | 0 | 0.1 |
| Total | 98 | | | | |

`Material.Car.Standard 19`: family CarGlass, metallic 1, roughness 0.05, base colour 13, 13, 13, 255. `Material.Track.Terrain`: detail texture tiled 24 times (`detail_texture` afabb175-1413-5bd8-b522-afcecd9c9a9e).

### Car paint slots

The line `[CaptureBench] car paint slots` of the first car, once the first race world is ready: the number of material slots (mesh and sub-meshes) that sample the painted texture and the unpainted one. Before: the replay of S1f's first task, on the cooked `lit-diffuse` content of the previous run (`before/run.log`).

| Run | Painted | Unpainted |
|---|---|---|
| Before (`before/run.log`) | 1 | 10 |
| `cand-a/run.log` | 1 | 10 |
| `cand-b/run.log` | 1 | 10 |

### Mipmap chains

The lines `[CaptureBench] mipmaps` of `cand-a/run.log`, written once the first race world is ready (`Levels` is the level count read from the texture, `Expected` the full chain of its size, `Chain` compares level 1 read back with the box average of level 0 read back):

| Texture | Levels | Expected | Chain |
|---|---|---|---|
| Texture.Road | 11 | 11 | - |
| Texture.Landscape | 12 | 12 | - |
| Texture.Building | 10 | 10 | - |
| CarPaint.Painted | 12 | 12 | ok |
| CarPaint.Unpainted | 12 | 12 | ok |

`cand-b/run.log` has the same lines; the lines of the previous run (`artifacts/pbr-handover/s1m/cand-a/run.log`) are the same.

### Scene against the original

`cand-a/run.log`, scene measure of each track:

| Track | Objects original / port | Matched | Missing | Extra | Shadow cast / receive mismatches |
|---|---|---|---|---|---|
| Beginner | 1147 / 1147 | 1147 | 0 | 0 | 0 / 0 |
| Advanced | 1693 / 1693 | 1693 | 0 | 0 | 0 / 0 |
| Expert | 3404 / 3404 | 3404 | 0 | 0 | 0 / 0 |

The three `scene-measure-*.txt` files are identical to those of the previous run, byte for byte; the 15 `alignment` lines of the log are the same as the previous run's.

The log has 15 `[CaptureBench] view` lines, 15 of them `pipeline legacy fxaaPasses 0`.

### Pictures against the original

`compare-nopost/report.md`, `compare-full/report.md`; the `nopost` edges of the previous run come from `artifacts/pbr-handover/s1m/compare-nopost/report.md`. The last column is the mean difference with the previous run's picture of the same view (`compare-vs-s1m/report.md`), the size of this slice's change on screen.

| View | R, G, B `nopost` | Edges `nopost` | Edges `nopost` before | R, G, B `full` | Edges `full` | Change from the previous run, R, G, B |
|---|---|---|---|---|---|---|
| Beginner-start | 41.17, 36.23, 29.20 | 41.7 % | 60.7 % | 63.11, 49.63, 39.53 | 66.4 % | 39.02, 35.59, 28.88 |
| Beginner-sector-20 | 51.13, 51.19, 51.08 | 15.5 % | 33.8 % | 68.64, 63.65, 63.15 | 30.5 % | 25.22, 23.93, 19.63 |
| Beginner-sector-50 | 60.76, 57.25, 48.36 | 24.1 % | 48.1 % | 88.98, 77.01, 64.05 | 48.2 % | 52.60, 50.93, 42.97 |
| Beginner-sector-80 | 65.87, 60.31, 46.40 | 34.3 % | 56.3 % | 91.98, 78.10, 58.61 | 57.3 % | 57.32, 53.50, 41.91 |
| Beginner-overview | 32.76, 29.64, 25.05 | 32.6 % | 86.3 % | 57.37, 46.90, 39.25 | 58.6 % | 26.25, 23.10, 17.76 |
| Advanced-start | 66.72, 59.19, 47.34 | 22.7 % | 40.6 % | 95.93, 79.28, 62.34 | 50.3 % | 51.62, 47.61, 38.51 |
| Advanced-sector-20 | 63.13, 54.91, 45.29 | 22.2 % | 41.5 % | 90.28, 72.50, 59.53 | 46.4 % | 38.53, 34.08, 27.30 |
| Advanced-sector-50 | 62.69, 56.02, 48.15 | 23.5 % | 52.7 % | 91.27, 74.57, 62.88 | 51.5 % | 54.20, 49.59, 41.92 |
| Advanced-sector-80 | 54.45, 49.33, 41.25 | 36.5 % | 46.9 % | 80.83, 66.63, 54.73 | 65.5 % | 56.44, 52.09, 43.46 |
| Advanced-overview | 35.62, 30.20, 23.01 | 22.6 % | 90.9 % | 62.17, 48.96, 37.68 | 55.7 % | 32.49, 26.92, 18.54 |
| Expert-start | 41.48, 37.89, 31.46 | 39.6 % | 57.9 % | 62.51, 51.14, 42.59 | 63.8 % | 36.32, 34.11, 28.27 |
| Expert-sector-20 | 55.55, 48.22, 36.85 | 27.0 % | 39.5 % | 83.34, 65.98, 48.92 | 48.9 % | 50.55, 45.96, 34.99 |
| Expert-sector-50 | 57.37, 52.70, 44.16 | 37.4 % | 53.3 % | 83.39, 71.05, 58.76 | 65.7 % | 49.57, 46.03, 38.26 |
| Expert-sector-80 | 44.03, 38.44, 30.75 | 26.6 % | 42.1 % | 66.28, 51.28, 39.77 | 61.2 % | 31.48, 27.98, 22.29 |
| Expert-overview | 33.92, 30.82, 26.29 | 44.2 % | 96.4 % | 58.82, 48.93, 41.55 | 65.0 % | 28.13, 24.53, 18.61 |

Change from the previous run, mean of the three channels: largest 50.91 (`Beginner-sector-80`), smallest 22.37 (`Beginner-overview`).

Change of the `nopost` edges matched since the previous run, in points: highest -10.4 (`Advanced-sector-80`), lowest -68.3 (`Advanced-overview`); 15 of 15 views are lower, 0 higher.

Mean luminance of the 15 pictures: original 96.3, port 56.0, port in the previous run 93.6 (`compare-nopost/report.md`, `artifacts/pbr-handover/s1m/compare-nopost/report.md`).

### Light and shadow probes

`compare-probes/probes.md`; the column before comes from `artifacts/pbr-handover/s1m/compare-probes/probes.md`.

| Criterion | Passed | Failed | Judged | Passed before |
|---|---|---|---|---|
| 1. lit and away within +-12 | 10 | 24 | 34 | 11 |
| 2. shadow/lit ratio within +-0.05 (per view and surface) | 0 | 4 | 4 | 0 |
| 3. class agreement (shadow shadowed, lit lit) | 21 | 12 | 33 | 21 |
| 4. away-shadowed within +-12 | 0 | 3 | 3 | 0 |

Shadow over lit luminance ratio, original then port: terrain 0.284 then 1.038; city 0.276 then 1.037; road 0.228 then 0.804; terrain 0.267 then 1.015.

### Repeatability

Two runs (`cand-a`, `cand-b`, `compare-repeat/report.md`): 13 identical files of 15. Different by their hash: `Expert-overview.png` (mean difference 0.00, 0.00, 0.00), `Expert-start.png` (mean difference 0.00, 0.00, 0.00).

### Other references of this machine

UI captures (`ui/`) against the previous reference `artifacts/pbr-handover/s1m/ui` (`ui-compare/report.txt`):

| State | R | G | B |
|---|---|---|---|
| splash | 0.00 | 0.00 | 0.00 |
| main-menu | 0.00 | 0.00 | 0.00 |
| highscores | 0.00 | 0.00 | 0.00 |
| options | 0.00 | 0.00 | 0.00 |
| help | 0.00 | 0.00 | 0.00 |
| car-selection | 4.86 | 4.21 | 4.23 |
| track-selection | 0.00 | 0.00 | 0.00 |
| race-hud | 30.02 | 28.72 | 26.38 |
| pause | 8.96 | 8.50 | 7.71 |
| race-finished | 23.31 | 22.47 | 20.38 |

Track audit (`audit/`, 30 captures) against `artifacts/pbr-handover/s1m/audit`: largest channel difference 56.691 (`audit-Advanced-sector-50`), smallest 14.910 (`audit-Expert-helper-tunnel`).


## After the race world lighting

Sixth run, on 2026-11-17, after slice S1g of the plan: the race world renders in linear HDR by default (`--color-pipeline legacy` keeps the previous chain), lit by the original's single key light with an image-based ambient from the sky cube and the original's soft tinted shadows, the road and the tunnels have tangents so that their normal maps apply, and the generated track materials have no emissive. The run before it is the previous section, `artifacts/pbr-handover/s1f/cand-a` (cooked PBR materials, `legacy` colour pipeline, three lights). The state just before this slice, taken again on the engine it ends on, is `artifacts/pbr-handover/s1g/base` (`legacy` pipeline). The previous runs above stay as their record.

Outputs under `artifacts/pbr-handover/s1g/`; the original's export is the same one, `artifacts/pbr-handover/s1c/ref`. The tables of this section are generated from the output files and from the cooked `.material` files by a script. **Every figure below is for one colour pipeline mode, stated in its table: `legacy` is the engine's legacy colour chain (no tonemapping, no anti-aliasing), the mode of the earlier runs, and `linear` is the default of the game, linear HDR with the ACES approximation, exposure 1, FXAA, bloom and SSAO off.** The colours of `legacy` are no longer comparable to the original's now that the light is PBR-scaled (see the luminance below); its use is the geometry and shadow-placement checks.

### Run

- Game `RacingGameCasaEngine.exe --capture-reference-views artifacts/pbr-handover/s1c/ref/views.json --reference-capture-out <folder> --color-pipeline <mode>`, Debug build, engine `8a3933b36`. Folders: `legacy-a`, `legacy-b`, `linear-a`, `linear-b` (two runs per mode), each with its `run.log`.
- `legacy`: lines `[CaptureBench] view ... pipeline legacy fxaaPasses 0` in `legacy-a/run.log` and `legacy-b/run.log`: 15 and 15 of 15; lines with a warning, an error or an exception: 0 and 0.
- `linear`: lines `[CaptureBench] view ... pipeline linear fxaaPasses 1` in `linear-a/run.log` and `linear-b/run.log`: 15 and 15 of 15; lines with a warning, an error or an exception: 0 and 0.

### Race world settings

Read from `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs` at the commit of this run:

| Setting | Value |
|---|---|
| Key light direction | normalize(-8500, -15000, -7250) |
| Key light intensity | MathF.PI |
| Key light specular colour | new(235, 235, 235) |
| ShadowMapResolution | 4096 |
| ShadowMaxDistance | 100f |
| ShadowFadeDistance | 15f |
| ShadowDepthRange | 150f |
| ShadowFilterRadius | 3f |
| ShadowColor | new(0.25f, 0.26f, 0.27f) |
| ShadowDepthBias | 0.001f |
| ShadowNormalBias | 0.1f |
| Shadows.Mode | ShadowMode.MultiplyLitColor |
| AmbientSource | AmbientSource.Auto |
| AmbientColor | Vector3.One |
| AmbientIntensity | 1f |
| SpecularIntensity | 1f |
| Colour pipeline (`linear`) | Mode = ColorPipelineMode.LinearHdr, Tonemapper = TonemapOperator.AcesApprox, Exposure = 1.0f, BloomEnabled = false, SsaoEnabled = false, AntiAliasing = AntiAliasingMode.Fxaa |

### Road and tunnel meshes

Control run by a throwaway tool (not in git) that rebuilds the road and tunnel meshes of the three tracks with the game's own code (`t3-geometry-before.txt` before the change, `t3-geometry-after.txt` after):

| Mesh | Vertices | Hash of positions, normals, UVs, indices before | after | Tangent check after |
|---|---|---|---|---|
| Beginner road | 6660 | 4CCD29EF95BE1220 | 4CCD29EF95BE1220 | 0 failure(s) |
| Beginner tunnel | 404 | 8E67E1C88327E007 | 8E67E1C88327E007 | 0 failure(s) |
| Advanced road | 10860 | E42D99EF7B5262C3 | E42D99EF7B5262C3 | 0 failure(s) |
| Advanced tunnel | 860 | F4F96C47C74CD791 | F4F96C47C74CD791 | 0 failure(s) |
| Expert road | 22330 | 8A4E39D94005A7E1 | 8A4E39D94005A7E1 | 0 failure(s) |
| Expert tunnel | 896 | EBA77D793F9950E5 | EBA77D793F9950E5 | 0 failure(s) |

The check, for each vertex: tangent of unit length, orthogonal to the normal and equal to the expected vector (the road point's right vector with the port's axes, its opposite for the tunnel), to 1e-4, with `w` = 1. Before the change the meshes have no tangents, which is why the engine ignored the normal maps of the road and the tunnel.

### Cooked track materials

Values read back from `RacingGameCasaEngine/Content/Materials/Track/*.material`:

| Material | Normal map | Emissive colour |
|---|---|---|
| Road | yes | 0, 0, 0 |
| Ground | no | 0, 0, 0 |
| Terrain | yes | 0, 0, 0 |
| GuardRail | yes | 0, 0, 0 |
| RoadColumn | yes | 0, 0, 0 |
| RoadTunnel | yes | 0, 0, 0 |

Cooker: `--verify` before the recook: 8 mismatches; recook: 6 files written, 257 unchanged; `--verify` after: 0 mismatches; second pass: 0 files written (`t3-cook-*.txt`).

### Scene against the original

`legacy-a/run.log`, scene measure of each track (the `linear` runs give the same lines):

| Track | Objects original / port | Matched | Missing | Extra | Shadow cast / receive mismatches |
|---|---|---|---|---|---|
| Beginner | 1147 / 1147 | 1147 | 0 | 0 | 0 / 0 |
| Advanced | 1693 / 1693 | 1693 | 0 | 0 | 0 / 0 |
| Expert | 3404 / 3404 | 3404 | 0 | 0 | 0 / 0 |

12 of the 12 `scene-measure-*.txt` files (3 tracks, 2 modes, 2 runs) are identical, byte for byte, to those of `base` (the state before this slice).

The five `[CaptureBench] mipmaps` lines are the same as the previous run's in both modes; `car paint slots`: legacy painted 1 unpainted 10, linear painted 1 unpainted 10.

### Pictures against the original

`compare-<mode>-nopost/report.md`, `compare-<mode>-full/report.md` (mean difference per channel R, G, B, and share of the original's edges found within 2 pixels in the port; `nopost` is the original without its post screen shaders). The edges of the previous run come from `artifacts/pbr-handover/s1f/compare-nopost/report.md`.

| View | `legacy` R, G, B `nopost` | Edges | `linear` R, G, B `nopost` | Edges | Edges previous run (`legacy`) | `legacy` R, G, B `full` | `linear` R, G, B `full` |
|---|---|---|---|---|---|---|---|
| Beginner-start | 40.74, 53.16, 53.37 | 99.8 % | 37.21, 45.89, 45.78 | 99.3 % | 41.7 % | 33.45, 48.65, 52.57 | 31.98, 40.18, 42.79 |
| Beginner-sector-20 | 43.71, 52.40, 50.58 | 72.9 % | 44.52, 49.60, 48.00 | 65.3 % | 15.5 % | 44.28, 51.84, 51.46 | 46.62, 50.11, 49.97 |
| Beginner-sector-50 | 43.48, 57.96, 58.20 | 95.7 % | 39.32, 48.52, 48.68 | 88.6 % | 24.1 % | 31.62, 46.08, 49.79 | 32.51, 39.05, 41.80 |
| Beginner-sector-80 | 49.56, 65.05, 63.34 | 99.0 % | 39.21, 49.79, 50.11 | 98.5 % | 34.3 % | 38.12, 52.72, 55.61 | 37.16, 41.28, 43.01 |
| Beginner-overview | 28.18, 36.05, 34.41 | 99.9 % | 34.07, 37.70, 33.94 | 99.7 % | 32.6 % | 27.47, 38.52, 40.03 | 24.91, 33.58, 34.99 |
| Advanced-start | 41.84, 56.90, 57.98 | 96.2 % | 33.75, 43.50, 45.70 | 94.7 % | 22.7 % | 29.92, 44.37, 49.75 | 29.21, 32.54, 36.25 |
| Advanced-sector-20 | 42.86, 49.32, 46.31 | 96.6 % | 36.44, 40.47, 39.25 | 91.5 % | 22.2 % | 43.39, 48.34, 47.41 | 38.95, 39.32, 39.74 |
| Advanced-sector-50 | 41.63, 52.72, 52.12 | 96.0 % | 34.48, 41.71, 41.22 | 89.7 % | 23.5 % | 28.30, 39.52, 42.60 | 30.35, 32.57, 34.04 |
| Advanced-sector-80 | 50.76, 68.81, 71.95 | 98.3 % | 41.49, 54.94, 58.65 | 97.5 % | 36.5 % | 33.79, 54.76, 61.67 | 33.23, 43.23, 48.48 |
| Advanced-overview | 28.42, 36.71, 33.72 | 99.8 % | 33.72, 37.89, 33.30 | 98.9 % | 22.6 % | 28.76, 40.32, 41.39 | 26.47, 34.66, 35.38 |
| Expert-start | 34.23, 45.54, 44.62 | 99.1 % | 28.54, 36.19, 35.60 | 98.6 % | 39.6 % | 24.70, 39.34, 41.50 | 21.91, 28.91, 30.83 |
| Expert-sector-20 | 51.38, 69.65, 67.35 | 99.0 % | 44.31, 57.77, 57.63 | 97.2 % | 27.0 % | 36.37, 56.18, 59.53 | 36.61, 45.68, 49.33 |
| Expert-sector-50 | 47.10, 64.64, 67.38 | 95.4 % | 42.64, 55.24, 58.14 | 91.1 % | 37.4 % | 34.71, 53.85, 60.49 | 34.67, 43.98, 49.74 |
| Expert-sector-80 | 38.05, 51.55, 52.02 | 99.1 % | 39.62, 49.76, 49.60 | 98.7 % | 26.6 % | 27.64, 45.70, 50.04 | 28.72, 42.50, 46.45 |
| Expert-overview | 25.95, 33.20, 31.49 | 99.9 % | 31.97, 34.84, 31.63 | 99.3 % | 44.2 % | 29.60, 38.70, 39.90 | 26.15, 32.74, 34.37 |

Change from the picture of the same view in another state, mean of the three channels (`compare-<mode>-vs-base/report.md`, `compare-<mode>-vs-s1f/report.md`):

| View | `legacy` vs `base` | `linear` vs `base` | `legacy` vs previous run | `linear` vs previous run |
|---|---|---|---|---|
| Beginner-start | 77.37 | 71.46 | 77.73 | 71.74 |
| Beginner-sector-20 | 66.98 | 58.93 | 66.98 | 58.92 |
| Beginner-sector-50 | 100.93 | 91.08 | 100.92 | 91.07 |
| Beginner-sector-80 | 111.27 | 98.22 | 111.28 | 98.22 |
| Beginner-overview | 51.44 | 54.12 | 51.43 | 54.12 |
| Advanced-start | 102.87 | 91.20 | 102.86 | 91.20 |
| Advanced-sector-20 | 78.34 | 75.37 | 78.34 | 75.37 |
| Advanced-sector-50 | 98.58 | 85.92 | 98.57 | 85.91 |
| Advanced-sector-80 | 106.75 | 94.39 | 106.77 | 94.40 |
| Advanced-overview | 52.30 | 54.63 | 52.30 | 54.63 |
| Expert-start | 70.71 | 62.87 | 70.94 | 63.04 |
| Expert-sector-20 | 104.79 | 95.42 | 104.76 | 95.39 |
| Expert-sector-50 | 98.89 | 90.77 | 98.89 | 90.77 |
| Expert-sector-80 | 79.68 | 78.78 | 79.65 | 78.75 |
| Expert-overview | 48.54 | 51.42 | 48.54 | 51.42 |

`legacy` against `base`: largest 111.27 (`Beginner-sector-80`), smallest 48.54 (`Expert-overview`).
`linear` against `base`: largest 98.22 (`Beginner-sector-80`), smallest 51.42 (`Expert-overview`).

Mean luminance of the 15 pictures: original 96.3; port in `legacy` 140.5; port in `linear` 133.5; port before this slice (`base`, `legacy`) 55.9; port in the previous run 56.0.

Saturated pixels (the three channels at 255, and at 250 or more) of the full picture, which is where the specular glints of the rails and lamp posts show:

| Run | `Beginner-start` at 255 | at 250 or more | `Expert-start` at 255 | at 250 or more |
|---|---|---|---|---|
| `base` (`legacy`, before this slice) | 728 | 791 | 691 | 760 |
| `legacy-a` | 6062 | 6824 | 6500 | 6800 |
| `linear-a` | 0 | 0 | 0 | 0 |

### Light and shadow probes

`compare-<mode>-probes/probes.md`; the base column is `compare-base-probes/probes.md`, the state before this slice in `legacy`. The criterion 3 of the base is the figure this slice must not lower.

| Criterion | `legacy` passed | `linear` passed | Judged | `base` passed |
|---|---|---|---|---|
| 1. lit and away within +-12 | 0 | 1 | 34 | 10 |
| 2. shadow/lit ratio within +-0.05 (per view and surface) | 2 | 0 | 4 | 0 |
| 3. class agreement (shadow shadowed, lit lit) | 27 | 27 | 33 | 21 |
| 4. away-shadowed within +-12 | 0 | 3 | 3 | 0 |

Shadow over lit luminance ratio, in the order of the report (terrain, city, road, terrain): original 0.284, 0.276, 0.228, 0.267; port in `legacy` 0.282, 1.921, 0.835, 0.263; port in `linear` 0.183, 3.133, 0.905, 0.141; `base` 1.038, 1.037, 0.804, 1.015. Only the two terrain ratios in `legacy` were a target of this slice (within 0.03 of the original).

Road over terrain luminance of the lit probes of `Beginner-start` (`road-lit-1` over `terrain-lit-1`): original 0.348; port in `legacy` 0.756; port in `linear` 0.806; `base` 0.815.

### Repeatability

`legacy`: two runs (`legacy-a`, `legacy-b`, `compare-legacy-repeat/report.md`): 14 identical files of 15. Different by their hash: `Expert-overview.png` (mean difference 0.00, 0.00, 0.00).

`linear`: two runs (`linear-a`, `linear-b`, `compare-linear-repeat/report.md`): 13 identical files of 15. Different by their hash: `Expert-overview.png` (mean difference 0.00, 0.00, 0.00), `Expert-start.png` (mean difference 0.00, 0.00, 0.00).

### Other references of this machine

UI captures (`ref-ui/`, default colour pipeline, `linear`) against the state before this slice `artifacts/pbr-handover/s1g/base-ui` (`ui-compare/report.txt`). The race states change because the race world below the interface changes; the car selection carousel has its own world and is not touched by this slice.

| State | R | G | B |
|---|---|---|---|
| splash | 0.00 | 0.00 | 0.00 |
| main-menu | 0.00 | 0.00 | 0.00 |
| highscores | 0.00 | 0.00 | 0.00 |
| options | 0.00 | 0.00 | 0.00 |
| help | 0.00 | 0.00 | 0.00 |
| car-selection | 0.00 | 0.00 | 0.00 |
| track-selection | 0.00 | 0.00 | 0.00 |
| race-hud | 62.77 | 69.11 | 68.43 |
| pause | 18.36 | 19.97 | 19.54 |
| race-finished | 56.24 | 62.67 | 60.56 |

Track audit (`ref-audit/`, 30 captures, default colour pipeline, `linear`) against `artifacts/pbr-handover/s1g/base-audit` (`legacy`): largest channel difference 110.816 (`audit-Advanced-sector-80`), smallest 24.087 (`audit-Beginner-helper-tunnel`). The new references of this machine are `artifacts/pbr-handover/s1g/ref-ui` and `ref-audit`.


## After the road underside and the city ground

Seventh run, on 2026-11-17, after slice S1h of the plan: the road generator builds the underside of the road and the terrain generator builds the city ground plane, both as the original builds them (ADR-0029). The run before it is the previous section, `artifacts/pbr-handover/s1g/` (`legacy-a`, `linear-a`, after the race world lighting). The state just before this slice, taken again on the same engine, is `artifacts/pbr-handover/s1h/base-legacy-a` and `base-linear-a` (the previous section's content, code at `4b2d4f8`). The previous runs above stay as their record.

Outputs under `artifacts/pbr-handover/s1h/`; the original's export is the same one, `artifacts/pbr-handover/s1c/ref`. The tables of this section are generated from the output files, from the cooked `.material` files and from the source by a script. **Every figure below that depends on the colour pipeline is for one mode, stated in its table: `legacy` is the engine's legacy colour chain (no tonemapping, no anti-aliasing), and `linear` is the default of the game, linear HDR with the ACES approximation, exposure 1, FXAA, bloom and SSAO off.** The colours of both are brighter than the original's (open point O13 of the plan); the scene measure and the geometry checks do not depend on them.

### Run

- Game `RacingGameCasaEngine.exe --capture-reference-views artifacts/pbr-handover/s1c/ref/views.json --reference-capture-out <folder> --color-pipeline <mode>`, Debug build, engine `8a3933b36`. Folders: `legacy-a`, `legacy-b`, `linear-a`, `linear-b` (two runs per mode), each with its `run.log`.
- `legacy`: lines `[CaptureBench] view ... pipeline legacy fxaaPasses 0` in `legacy-a/run.log` and `legacy-b/run.log`: 15 and 15 of 15; lines with a warning, an error or an exception: 0 and 0.
- `linear`: lines `[CaptureBench] view ... pipeline linear fxaaPasses 1` in `linear-a/run.log` and `linear-b/run.log`: 15 and 15 of 15; lines with a warning, an error or an exception: 0 and 0.

### Road underside

Control run by a throwaway tool (not in git, `t1-geometry.txt`) that rebuilds the road and its underside for the three tracks with the game's own code and compares every vertex and index with the formulas of the original (`TrackVertex.cs:49` and `:171-197`, `Track.cs:33`, `:48` and `:408-486`) recomputed in double precision from the road points. Each cell is the result of the check and its largest deviation. Failures in total: 0.

| Check | Beginner | Advanced | Expert |
|---|---|---|---|
| Road points | 1332 | 2172 | 4466 |
| Vertices (4 per point) | 5328 OK | 8688 OK | 17864 OK |
| Indices (18 per segment) | 23958 OK | 39078 OK | 80370 OK |
| Upper vertex positions equal the road mesh edge vertices (1e-5) | OK (max 0.00E+000) | OK (max 0.00E+000) | OK (max 0.00E+000) |
| Upper vertex normals equal the road mesh's (1e-5) | OK (max 0.00E+000) | OK (max 0.00E+000) | OK (max 0.00E+000) |
| Upper vertex tangents equal the road mesh's (1e-5) | OK (max 0.00E+000) | OK (max 0.00E+000) | OK (max 0.00E+000) |
| Texture coordinates equal (TextureU x 1, V), V = 0, 0.135, 0.865, 1 (1e-6) | OK (max 9.54E-009) | OK (max 9.54E-009) | OK (max 9.54E-009) |
| Normals equal the expected vectors (1e-5) | OK (max 1.24E-007) | OK (max 1.26E-007) | OK (max 1.29E-007) |
| Tangents equal the expected vectors (1e-5) | OK (max 1.30E-007) | OK (max 1.46E-007) | OK (max 1.34E-007) |
| Normals and tangents of unit length (1e-5) | OK (max 1.19E-007) | OK (max 1.79E-007) | OK (max 1.19E-007) |
| Normal orthogonal to tangent (1e-5) | OK (max 8.94E-008) | OK (max 8.94E-008) | OK (max 9.87E-008) |
| Upper and lower positions equal the original's own float expression `pos -/+ RoadWidthScale * roadWidth * right / 2 - up * RoadThickness * roadWidth`, converted, minus the origin | max 0.00E+000 (upper), 0.00E+000 (lower) | max 0.00E+000 (upper), 0.00E+000 (lower) | max 0.00E+000 (upper), 0.00E+000 (lower) |
| Same positions against double precision (informative: float rounding of coordinates of the order of 1e3 m) | upper 1.23E-004, lower 2.39E-004 | upper 1.23E-004, lower 2.36E-004 | upper 1.22E-004, lower 2.41E-004 |
| Tangent `w` = 1 | OK (0 different) | OK (0 different) | OK (0 different) |
| Indices equal the pattern of `Track.cs:464-486` (step 4) | OK (0 different) | OK (0 different) | OK (0 different) |
| Bottom triangles whose cross product is along the normal (0 means front face seen from below under `CullCounterClockwise`) | 0 of 2662 | 0 of 4342 | 0 of 8930 |
| Model casts / receives shadows | false / false | false / false | false / false |

### City ground plane

Anchor, the constant `LegacyCityGroundAnchor` of `RacingGame.Gameplay/Track/LegacyTrackSceneFactory.CityGround.cs`, in the original's coordinates: `new(994.9773f, 426.16977f, 28.235294f)`; half size of the plane `min(x, y)` = 426.1697692871094. The plane is made once, for Beginner, and kept for the three tracks.

Two independent derivations:

| Method | Output | Result |
|---|---|---|
| (a) throwaway tool on this port's placement of Beginner (`t2-anchor-port.txt`): first placement, in the order the objects were added, whose model name contains `hotel` or `building` | 1274 placements, placement #0 `Building` | (994.9773, 426.16977, 28.235294), size 426.16977 |
| (b) a log line added for the occasion to `CreateCityPlane` of the original, run by `--capture-track-audit --seed 1` on the three levels, then removed (`t2-anchor-original.txt`) | 1 line | level Beginner object count 1155 first big building index 0 position 994.9773 426.16977 28.235294 size min(x,y) 426.16977 |

The constant against (a): largest deviation 0.00E+000 (tolerance 1e-3); against (b): the digits of the line above (the same position, to the last digit of a float). Failures in total: 0.

Control by a second throwaway tool (`t2-geometry.txt`) of the plane the terrain generator builds, against the formulas of `PlaneRenderer.cs` and `Landscape.CreateCityPlane` (the four vertices `(+-right +-dir) * size + up * 0.1` of the strip, the anchor as translation, `up` = (0, 0, 1), `right` = (1, 0, 0), UV `+-size / 20`), converted with the port's point conversion minus the origin of each track:

| Check | Beginner | Advanced | Expert |
|---|---|---|---|
| Vertices, indices | 4, 6 (OK) | 4, 6 (OK) | 4, 6 (OK) |
| Indices are the strip triangles (0, 1, 2), (2, 1, 3) | OK | OK | OK |
| Positions equal the original's own float expression | OK (max 0.00E+000, tolerance 1E-005) | OK (max 0.00E+000, tolerance 1E-005) | OK (max 0.00E+000, tolerance 1E-005) |
| Positions against double precision (informative) | OK (max 3.05E-005, tolerance 1E-003) | OK (max 3.05E-005, tolerance 1E-003) | OK (max 3.05E-005, tolerance 1E-003) |
| Texture coordinates equal +-size / 20 | OK (max 3.81E-007, tolerance 1E-004) | OK (max 3.81E-007, tolerance 1E-004) | OK (max 3.81E-007, tolerance 1E-004) |
| Normal equals `up` | OK (max 0.00E+000, tolerance 1E-005) | OK (max 0.00E+000, tolerance 1E-005) | OK (max 0.00E+000, tolerance 1E-005) |
| Tangent equals `right` | OK (max 0.00E+000, tolerance 1E-005) | OK (max 0.00E+000, tolerance 1E-005) | OK (max 0.00E+000, tolerance 1E-005) |
| Normals and tangents of unit length | OK (max 0.00E+000, tolerance 1E-005) | OK (max 0.00E+000, tolerance 1E-005) | OK (max 0.00E+000, tolerance 1E-005) |
| Normal orthogonal to tangent | OK (max 0.00E+000, tolerance 1E-005) | OK (max 0.00E+000, tolerance 1E-005) | OK (max 0.00E+000, tolerance 1E-005) |
| Tangent `w` = 1 | OK (0 different) | OK (0 different) | OK (0 different) |
| Triangles whose cross product is along the normal (0 of 2: front face seen from above, like the road) | 0 of 2 | 0 of 2 | 0 of 2 |
| Model casts / receives shadows | false / true | false / true | false / true |

### Cooked track materials

Values read back from `RacingGameCasaEngine/Content/Materials/Track/*.material`:

| Material | Base texture | Normal map | Rasterizer state | Emissive colour |
|---|---|---|---|---|
| Road | Texture.Road | Texture.RoadNormal | CullCounterClockwise | 0, 0, 0 |
| RoadBack | Texture.RoadBack | Texture.RoadBackNormal | CullCounterClockwise | 0, 0, 0 |
| Ground | Texture.CityGround | Texture.CityGroundNormal | CullCounterClockwise | 0, 0, 0 |
| Terrain | Texture.Landscape | Texture.LandscapeNormal | CullCounterClockwise | 0, 0, 0 |
| GuardRail | Texture.Leitplanke | Texture.LeitplankeNormal | CullCounterClockwise | 0, 0, 0 |
| RoadColumn | Texture.RoadCement | Texture.RoadCementNormal | CullCounterClockwise | 0, 0, 0 |
| RoadTunnel | Texture.RoadTunnel | Texture.RoadTunnelNormal | CullNone | 0, 0, 0 |

Cooker, T12.1 (road underside): `--verify` before the recook: 6 mismatches; recook: 3 files written, 263 unchanged, catalogue 609 -> 612 entries; `--verify` after: 0 mismatches; second pass: 0 files written (`t1-cook-*.txt`).

Cooker, T12.2 (city ground): `--verify` before the recook: 6 mismatches; recook: 3 files written, 265 unchanged, catalogue 612 -> 614 entries; `--verify` after: 0 mismatches; second pass: 0 files written (`t2-cook-*.txt`).

`Material.Track.Terrain` is unchanged between `4b2d4f8` and the commit of this run (`git diff`).

### Scene against the original

`legacy-a/run.log`, scene measure of each track (the `linear` runs give the same lines):

| Track | Objects original / port | Matched | Missing | Extra | Shadow cast / receive mismatches |
|---|---|---|---|---|---|
| Beginner | 1147 / 1147 | 1147 | 0 | 0 | 0 / 0 |
| Advanced | 1693 / 1693 | 1693 | 0 | 0 | 0 / 0 |
| Expert | 3404 / 3404 | 3404 | 0 | 0 | 0 / 0 |

12 of the 12 `scene-measure-*.txt` files (3 tracks, 2 modes, 2 runs) are identical, byte for byte, to those of the state before this slice (`base-<mode>-a`). The underside and the plane are not objects the scene measure counts.

The five `[CaptureBench] mipmaps` lines are the same as the previous run's in both modes; `car paint slots`: legacy painted 1 unpainted 10, linear painted 1 unpainted 10.

### Pictures against the original

`compare-<mode>-nopost/report.md`, `compare-<mode>-full/report.md` (mean difference per channel R, G, B, and share of the original's edges found within 2 pixels in the port; `nopost` is the original without its post screen shaders). The edges of the previous run come from `artifacts/pbr-handover/s1g/compare-<mode>-nopost/report.md`.

| View | `legacy` R, G, B `nopost` | Edges | `linear` R, G, B `nopost` | Edges | Edges previous run (`legacy`) | Edges previous run (`linear`) | `legacy` R, G, B `full` | `linear` R, G, B `full` |
|---|---|---|---|---|---|---|---|---|
| Beginner-start | 40.80, 53.23, 53.45 | 99.8 % | 37.24, 45.93, 45.84 | 99.3 % | 99.8 % | 99.3 % | 33.50, 48.72, 52.65 | 32.01, 40.22, 42.85 |
| Beginner-sector-20 | 53.52, 63.56, 62.45 | 92.3 % | 50.91, 56.41, 55.44 | 77.6 % | 72.9 % | 65.3 % | 50.47, 61.68, 61.63 | 48.70, 54.50, 54.31 |
| Beginner-sector-50 | 43.46, 57.96, 58.25 | 95.7 % | 39.31, 48.52, 48.72 | 88.6 % | 95.7 % | 88.6 % | 31.63, 46.10, 49.85 | 32.51, 39.05, 41.85 |
| Beginner-sector-80 | 50.38, 66.02, 64.51 | 99.1 % | 40.24, 50.91, 51.32 | 98.7 % | 99.0 % | 98.5 % | 38.94, 53.69, 56.78 | 38.15, 42.41, 44.24 |
| Beginner-overview | 30.16, 38.62, 37.35 | 99.8 % | 35.55, 39.40, 36.18 | 99.7 % | 99.9 % | 99.7 % | 29.26, 41.09, 42.89 | 25.92, 35.13, 36.86 |
| Advanced-start | 41.84, 56.89, 57.98 | 96.2 % | 33.75, 43.50, 45.70 | 94.7 % | 96.2 % | 94.7 % | 29.92, 44.36, 49.75 | 29.21, 32.54, 36.25 |
| Advanced-sector-20 | 42.82, 49.29, 46.31 | 96.6 % | 36.41, 40.45, 39.25 | 91.5 % | 96.6 % | 91.5 % | 43.42, 48.37, 47.44 | 38.97, 39.33, 39.76 |
| Advanced-sector-50 | 41.56, 52.66, 52.11 | 96.0 % | 34.40, 41.65, 41.20 | 89.7 % | 96.0 % | 89.7 % | 28.25, 39.49, 42.60 | 30.29, 32.52, 34.02 |
| Advanced-sector-80 | 50.82, 68.94, 72.15 | 98.6 % | 41.62, 55.11, 58.87 | 97.8 % | 98.3 % | 97.5 % | 33.83, 54.88, 61.87 | 33.27, 43.35, 48.66 |
| Advanced-overview | 28.37, 36.67, 33.70 | 99.8 % | 33.70, 37.88, 33.30 | 98.9 % | 99.8 % | 98.9 % | 28.78, 40.32, 41.40 | 26.48, 34.66, 35.39 |
| Expert-start | 34.65, 46.02, 45.11 | 99.4 % | 28.93, 36.62, 36.04 | 98.9 % | 99.1 % | 98.6 % | 25.13, 39.85, 42.02 | 22.25, 29.34, 31.31 |
| Expert-sector-20 | 51.29, 69.56, 67.30 | 99.2 % | 44.22, 57.69, 57.58 | 97.5 % | 99.0 % | 97.2 % | 36.37, 56.18, 59.57 | 36.58, 45.65, 49.33 |
| Expert-sector-50 | 45.96, 63.23, 65.81 | 96.4 % | 41.18, 53.58, 56.39 | 92.1 % | 95.4 % | 91.1 % | 33.80, 52.68, 59.19 | 33.46, 42.55, 48.23 |
| Expert-sector-80 | 38.05, 51.66, 52.36 | 99.2 % | 39.71, 49.93, 49.96 | 98.4 % | 99.1 % | 98.7 % | 27.65, 45.83, 50.40 | 28.82, 42.69, 46.82 |
| Expert-overview | 29.46, 37.92, 36.56 | 99.9 % | 34.60, 37.97, 35.28 | 99.4 % | 99.9 % | 99.3 % | 32.45, 43.27, 44.67 | 27.66, 35.33, 37.28 |

Change from the picture of the same view in another state, mean of the three channels (`compare-<mode>-vs-base/report.md` for the state before this slice, `compare-<mode>-vs-s1g/report.md` for the previous run):

| View | `legacy` vs before this slice | `linear` vs before this slice | `legacy` vs previous run | `linear` vs previous run |
|---|---|---|---|---|
| Beginner-start | 0.07 | 0.05 | 0.07 | 0.05 |
| Beginner-sector-20 | 19.52 | 15.54 | 19.52 | 15.54 |
| Beginner-sector-50 | 0.11 | 0.08 | 0.11 | 0.08 |
| Beginner-sector-80 | 1.02 | 1.16 | 1.02 | 1.16 |
| Beginner-overview | 2.55 | 1.85 | 2.55 | 1.85 |
| Advanced-start | 0.01 | 0.01 | 0.01 | 0.01 |
| Advanced-sector-20 | 0.09 | 0.05 | 0.09 | 0.05 |
| Advanced-sector-50 | 0.18 | 0.16 | 0.18 | 0.16 |
| Advanced-sector-80 | 0.23 | 0.24 | 0.23 | 0.24 |
| Advanced-overview | 0.06 | 0.03 | 0.06 | 0.03 |
| Expert-start | 0.57 | 0.54 | 0.57 | 0.54 |
| Expert-sector-20 | 0.18 | 0.14 | 0.18 | 0.14 |
| Expert-sector-50 | 1.57 | 1.67 | 1.57 | 1.67 |
| Expert-sector-80 | 0.52 | 0.43 | 0.52 | 0.43 |
| Expert-overview | 4.79 | 3.69 | 4.79 | 3.69 |

`legacy` against the state before this slice: largest 19.52 (`Beginner-sector-20`), smallest 0.01 (`Advanced-start`); 7 of 15 views change by 0.5 or more.
`linear` against the state before this slice: largest 15.54 (`Beginner-sector-20`), smallest 0.01 (`Advanced-start`); 6 of 15 views change by 0.5 or more.

Mean luminance of the 15 pictures: original 96.3; port in `legacy` 142.2 (before this slice 140.5); port in `linear` 134.8 (before this slice 133.5).

Saturated pixels (the three channels at 255, and at 250 or more) of the full picture:

| Run | `Beginner-start` at 255 | at 250 or more | `Expert-start` at 255 | at 250 or more |
|---|---|---|---|---|
| `base-legacy-a` (before this slice) | 6062 | 6824 | 6500 | 6800 |
| `legacy-a` | 6062 | 6824 | 6500 | 6800 |
| `base-linear-a` (before this slice) | 0 | 0 | 0 | 0 |
| `linear-a` | 0 | 0 | 0 | 0 |

### Light and shadow probes

`compare-<mode>-probes/probes.md`; the base columns are `compare-base-<mode>-probes/probes.md`, the state before this slice. The criterion 3 of the base is the figure this slice must not lower.

| Criterion | `legacy` passed | `linear` passed | Judged | `base` `legacy` passed | `base` `linear` passed |
|---|---|---|---|---|---|
| 1. lit and away within +-12 | 0 | 1 | 34 | 0 | 1 |
| 2. shadow/lit ratio within +-0.05 (per view and surface) | 2 | 0 | 4 | 2 | 0 |
| 3. class agreement (shadow shadowed, lit lit) | 27 | 27 | 33 | 27 | 27 |
| 4. away-shadowed within +-12 | 0 | 3 | 3 | 0 | 3 |

Shadow over lit luminance ratio, in the order of the report (terrain, city, road, terrain): original 0.284, 0.276, 0.228, 0.267; port in `legacy` 0.282, 1.850, 0.835, 0.263 (before this slice 0.282, 1.921, 0.835, 0.263); port in `linear` 0.183, 2.556, 0.905, 0.141 (before this slice 0.183, 3.133, 0.905, 0.141).

Road over terrain luminance of the lit probes of `Beginner-start` (`road-lit-1` over `terrain-lit-1`): original 0.348; port in `legacy` 0.756 (before this slice 0.756); port in `linear` 0.806 (before this slice 0.806).

Probes of the city ground in the shadow (`Beginner-sector-20`, criterion 3, mean colour of the 9x9 square, R,G,B), the original's colour, then the port before and after this slice in each mode:

| Probe | Original | `legacy` before | `legacy` after | `linear` before | `linear` after | Verdict `legacy` after / `linear` after |
|---|---|---|---|---|---|---|
| city-shadow-1 | 31,34,37 | 124,139,132 | 195,225,234 | 120,131,123 | 184,204,213 | FAIL (cand. lit) / FAIL (cand. lit) |
| city-shadow-2 | 29,33,35 | 39,45,44 | 69,87,99 | 26,30,28 | 56,72,84 | FAIL (cand. lit) / FAIL (cand. lit) |
| city-shadow-3 | 30,33,35 | 40,46,46 | 56,71,79 | 20,24,24 | 32,43,51 | FAIL (cand. lit) / FAIL (cand. lit) |

### Repeatability

`legacy`: two runs (`legacy-a`, `legacy-b`, `compare-legacy-repeat/report.md`): 13 identical files of 15. Different by their hash: `Advanced-overview.png` (mean difference 0.00, 0.00, 0.00), `Expert-overview.png` (mean difference 0.00, 0.00, 0.00).

`linear`: two runs (`linear-a`, `linear-b`, `compare-linear-repeat/report.md`): 13 identical files of 15. Different by their hash: `Advanced-overview.png` (mean difference 0.00, 0.00, 0.00), `Expert-start.png` (mean difference 0.00, 0.00, 0.00).

### Other references of this machine

UI captures (`ref-ui/`, default colour pipeline, `linear`) against the state before this slice `artifacts/pbr-handover/s1h/base-ui` (`ui-compare/report.txt`). The race states change because the race world below the interface changes (the city ground plane and the road underside); the other screens are not touched by this slice.

| State | R | G | B |
|---|---|---|---|
| splash | 0.00 | 0.00 | 0.00 |
| main-menu | 0.00 | 0.00 | 0.00 |
| highscores | 0.00 | 0.00 | 0.00 |
| options | 0.00 | 0.00 | 0.00 |
| help | 0.00 | 0.00 | 0.00 |
| car-selection | 0.00 | 0.00 | 0.00 |
| track-selection | 0.00 | 0.00 | 0.00 |
| race-hud | 0.04 | 0.05 | 0.06 |
| pause | 0.01 | 0.01 | 0.02 |
| race-finished | 0.04 | 0.05 | 0.06 |

Track audit (`ref-audit/`, 30 captures, default colour pipeline, `linear`) against `artifacts/pbr-handover/s1h/base-audit` (the state before this slice): largest channel difference 23.808 (`audit-Expert-helper-laterns`), smallest 0.000 (`audit-Advanced-helper-laterns`). The new references of this machine are `artifacts/pbr-handover/s1h/ref-ui` and `ref-audit`.

