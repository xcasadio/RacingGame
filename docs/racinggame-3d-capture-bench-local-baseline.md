# 3D capture bench: baseline of the local version, before PBR

First run of the capture bench (ADR-0015) on the game code of this machine, taken on 2026-11-16 before any change to the engine format, the content or the materials. Plan: `ai-agent/tasks/rgce-pbr-handover-tasks.md`, slice S1c.

This is a different measurement from `racinggame-3d-capture-bench-findings.md`. That document records the other machine's version of the game, whose race world code is not in this tree. The figures below describe the version of this machine: generator components, cooked `lit-diffuse` assets, legacy colour pipeline.

Every figure cites its output file under `artifacts/pbr-handover/s1c/` (not in git).

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
- Two runs give 14 identical files of 15. `Expert-start.png` differs by its hash, with a mean difference of 0.00 on every channel (`compare-repeat/report.md`).

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
| Expert-sector-80 | 19.76, 17.99, 15.34 | 44.8 % | 30.60, 25.00, 21.65 | 69.1 % |
| Expert-overview | 24.64, 22.56, 21.57 | 88.4 % | 43.71, 39.19, 36.97 | 88.4 % |

`Beginner-sector-20` is the worst view: the ground of the city is black in the port (mean luminance 52.1 against 79.7), because the city ground does not exist in this version.

## Light and shadow probes

`compare-probes/probes.md`, criteria of the tool's README:

| Criterion | Pass | Fail |
|---|---|---|
| 1. `lit` and `away` within 12 of the original | 10 | 34 |
| 2. shadow over lit ratio within 0.05 | 0 | 4 |
| 3. a shadowed point is shadowed, a lit point is lit | 21 | 33 |
| 4. `away` and shadowed within 12 | 0 | 3 |

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
| Guard rail posts | Same count and position (455/455/455, 742/743/742, 1529/1529/1529; 0.518 m at most), but a scale ratio difference of 0.167 on every post: the port scales them by 1.125, the original by 1.35. |
| Column segments | About twice too many: 46/101/43, 50/160/47, 137/337/130. The extra segments have no counterpart in the original. |
| Lanterns | All of the original's are matched, and the port adds a third more: 39/53/39, 62/89/62, 228/330/228. |
| Palms | Far fewer than the original and few matched: `AlphaPalm` 80/17/14, 101/31/18, 219/54/34; `AlphaPalmSmall` 49/6/5, 52/2/0, 81/9/0. Matched palms are rotated by up to 177 degrees. |
| Signs and banners | Counts differ and few match: `Sign` 18/7/1, 22/16/3, 36/22/2; `Sign2` 6/7/0, 15/12/0, 37/32/5. |
| Buildings, hotels, start light | Matched one for one on Beginner; `Hotel01` 5/3/3 and `Casino01` 9/5/5 on Expert. |
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
