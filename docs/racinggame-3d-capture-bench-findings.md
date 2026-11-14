# 3D capture bench: first findings (2026-11-14)

First run of the capture bench (ADR-0015, plan `ai-agent/tasks/rgce-3d-capture-bench-tasks.md`): the original RacingGame at `b4374f4` against RacingGameCasaEngine at `b4642e8`, CasaEngine working tree at `778f6aa`, 1280x720, seed 1, original variant `nopost`.

Everything below was observed on the 15 comparison sheets (`artifacts/capture-bench/report/`, not versioned; regenerate with `scripts/TrackCaptureCompare/README.md`). Causes are not investigated here unless a line says so.

## What already matches

- The two worlds coincide: the port's start grid is at 0.00 m of the original's car position on the three tracks, and on the start views the road edges, the lane markings, the banner and the relief overlap.
- The terrain relief overlaps on the three overviews (79 to 88 % of the original's edges found in the port).
- Both captures are repeatable: two runs give identical images on each side.

## Measures

Mean absolute difference per channel (0-255), share of the original's edges found within 2 pixels in the port.

| View | R | G | B | Edges matched |
|---|---|---|---|---|
| Beginner-start | 30.94 | 28.04 | 24.62 | 48.9 % |
| Beginner-sector-20 | 57.68 | 58.13 | 59.04 | 30.3 % |
| Beginner-sector-50 | 32.16 | 30.19 | 26.13 | 38.2 % |
| Beginner-sector-80 | 36.58 | 34.54 | 29.63 | 40.8 % |
| Beginner-overview | 23.06 | 20.54 | 18.67 | 79.4 % |
| Advanced-start | 27.44 | 24.14 | 21.27 | 39.6 % |
| Advanced-sector-20 | 47.32 | 39.91 | 32.44 | 43.8 % |
| Advanced-sector-50 | 26.65 | 24.02 | 21.23 | 51.9 % |
| Advanced-sector-80 | 29.64 | 25.79 | 20.85 | 26.7 % |
| Advanced-overview | 23.14 | 20.06 | 17.53 | 85.3 % |
| Expert-start | 31.58 | 28.84 | 24.32 | 36.8 % |
| Expert-sector-20 | 25.62 | 23.79 | 20.05 | 26.3 % |
| Expert-sector-50 | 31.89 | 28.43 | 25.47 | 28.5 % |
| Expert-sector-80 | 21.14 | 19.10 | 16.18 | 34.4 % |
| Expert-overview | 24.79 | 22.69 | 21.62 | 87.9 % |

## Divergences, most severe first

### 1. Position, rotation, scale

| # | Divergence | Seen on |
|---|---|---|
| G1 | The road leaves the original's path away from the start: its edges are offset sideways by up to about one lane in curves and on raised sections, and the columns under it are offset with it. The port's checkpoints are 0.5 to 3.7 m from the original's track points (alignment log). | Expert-sector-50, Expert-sector-80, Advanced-sector-80, Beginner-sector-50 |
| G2 | A black ribbon runs beside the road, on the ground, along its whole length. The original has nothing there. | Every sector view, Beginner-start, Expert-start |
| G3 | Many objects of the original are absent: palms, ruins, stone blocks, oil tanks, oil pump, the casino tower, street lamps, cactus, small signs. Some come from the original's random scenery (plan O2), others stand at fixed places. | Beginner-start, Beginner-sector-80, Advanced-sector-50, Advanced-sector-80, Expert-start, Expert-sector-20 |
| G4 | The paved city ground is missing: the ground between the buildings is black, and the grey city plane of the overview is absent. | Beginner-sector-20, Beginner-overview, Expert-overview |
| G5 | Road signs do not stand as in the original: the curve sign is larger in the picture and lower, the row of four signs is shifted and turned. | Beginner-sector-50, Advanced-sector-50, Advanced-sector-80 |
| G6 | The start gantry differs on Advanced: the original shows a bare metal gantry, the port a banner. | Advanced-start |
| G7 | The sky is not oriented as in the original: the distant mountains and the clouds of the background do not line up. | Every view |

### 2. Textures and materials

| # | Divergence | Seen on |
|---|---|---|
| M1 | The terrain is blurred: the original shows a fine grain close to the camera, the port only the stretched base texture. | Every view |
| M2 | Signs, the billboard and the start banner are almost black in the port, textured and lit in the original. | Beginner-sector-50, Beginner-sector-80, Advanced-sector-80, Advanced-start |
| M3 | The car does not have the original's paint: white with dark stripes in the original, orange in the port (first car, first colour on both sides). | The three start views |
| M4 | The road has no sun glint: the original shows a bright specular band on the asphalt. | Beginner-sector-50, Advanced-sector-20, Advanced-sector-50 |
| M5 | Buildings are much brighter in the port than in the original at the same place. | Beginner-sector-20 |

### 3. Lighting and effects

| # | Divergence | Seen on |
|---|---|---|
| L1 | Shadows are black and hard-edged in the port, softer and lighter in the original; their direction and length differ too (banner, columns). | Start views, Expert-sector-50, Expert-sector-80 |
| L2 | No lens flare and no sun disc in the port. | Beginner-sector-20, Advanced-sector-20 |
| L3 | No glow or radial blur (compare with the `full` variant, `artifacts/capture-bench/report-full/`). | Every view |

## Found while building the bench

- The race world of the port always draws the checkpoint course as light blue debug lines (`RaceCourseDebugComponent`, added in `RaceWorldFactory.cs:248`). The capture removes them; the game still shows them.
- The chase camera leaves the camera's orientation tilted, and the look-at view matrix takes its up vector from it. The existing `--capture-track-audit` of the port is therefore rolled; the new capture resets the orientation.
- The existing `--capture-track-audit` does not force a size and keeps the HUD in its pictures.

## Update after the geometry slice (2026-11-14)

Plan `ai-agent/tasks/rgce-3d-geometry-fidelity-tasks.md`, port at `0ac894d`, same reference (seed 1). The original now exports its track points and placed objects (`scene.json`) and the port measures itself against them (`scene-measure-<Track>.txt`).

Measured: road points at 0.000 m, road width delta 0.000, and the same objects on both sides (Beginner 1147, Advanced 1693, Expert 3404, none unmatched, distance and scale delta 0.000, rotation within 0.04 degrees).

| # | Status | Cause found |
|---|---|---|
| G1 | Closed | The road path was right; the road width was not. The track files' width helpers were never read (XML item name not declared), so the road kept its default width, up to about 10 m away from the original's edges. |
| G2 | Not a geometry divergence | No extra geometry lies beside the road. What was read as a ribbon is the port's black shadow of the guard rail and of the road. Moved to lighting (L1). |
| G3 | Closed | Combo models were never expanded (same XML cause), guard rail holders and column segments bypassed the original's add path, and the random scenery was not ported. |
| G4 | Closed for the geometry | The city ground plane is built. It looks almost black because it lies in the port's black building shadows (L1). |
| G5 | Closed | Signs were placed with the normalized right vector and a different random order; they now match the original's matrices. |
| G6 | Closed | The "bare gantry" of the original is a street lamp close to the camera, which the port did not place (road helpers never read). |
| G7 | Open | Sky orientation, not touched. |

Also added: the road underside and the tunnels. No view of the bench shows a tunnel; they are checked by their ranges and vertex counts only.

Share of the original's edges found within 2 pixels in the port, before and after:

| View | Before | After |
|---|---|---|
| Beginner-start | 48.9 % | 60.2 % |
| Beginner-sector-20 | 30.3 % | 42.6 % |
| Beginner-sector-50 | 38.2 % | 59.1 % |
| Beginner-sector-80 | 40.8 % | 55.4 % |
| Beginner-overview | 79.4 % | 87.0 % |
| Advanced-start | 39.6 % | 42.9 % |
| Advanced-sector-20 | 43.8 % | 44.8 % |
| Advanced-sector-50 | 51.9 % | 56.6 % |
| Advanced-sector-80 | 26.7 % | 45.8 % |
| Advanced-overview | 85.3 % | 87.1 % |
| Expert-start | 36.8 % | 54.3 % |
| Expert-sector-20 | 26.3 % | 41.3 % |
| Expert-sector-50 | 28.5 % | 52.9 % |
| Expert-sector-80 | 34.4 % | 50.2 % |
| Expert-overview | 87.9 % | 93.9 % |

What keeps these numbers from going higher is no longer placement: the terrain's fine grain (M1), the shadows (L1) and the materials (M2 to M5) make most of the remaining edge differences.

## Update: normal maps (2026-11-14)

Found while reviewing the lighting plan, decision in ADR-0016.

- The original's normal maps keep their components in (alpha, green, blue), with alpha along `cross(normal, tangent)` and green along the tangent; red is unused. They had been converted unchanged, and the engine reads (red, green, blue): every normal-mapped surface of the port was lit as if tilted by about 45 degrees. This, not a shadow, is why the city ground looked black (`Beginner-sector-20`: 87.6 % of the original's edges found now, 42.6 % before).
- Only the meshes the port generates were affected (terrain, guard rails, columns, road underside, tunnels, city ground). The imported models have no tangents in their glTF files, so the engine does not use their normal maps at all: a new divergence, M6, whose fix is in the engine (glTF reader and converter).
- The 55 normal maps are now written in the standard layout. The mapping comes from reading the two shaders; the pictures cannot confirm it until the light direction and the ambient term match the original (lighting slice).
- Not checked: the car selection carousel (its plate has a normal map).

## Update: tangents of the imported models (2026-11-14)

M6 is closed (ADR-0016, follow-up). The engine reads the glTF `TANGENT` attribute, the converter writes the original's tangents, and the 56 models are regenerated: imported models are now normal-mapped, with a tangent space measured equal to the original's. Signs, ruins and building faces are no longer flat. What still differs on them is the light itself (direction, ambient, shadows): the lighting slice.

Left as reported by the independent check, not fixed: the engine reader drops a `TANGENT` accessor of the wrong size without a warning, and keeps a zero-length tangent as it is. Neither can happen with the converter's output.
