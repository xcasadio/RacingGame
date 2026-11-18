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

## Update after the light and ambient slice (2026-11-14)

Plan `ai-agent/tasks/rgce-3d-lighting-shadows-tasks.md` (S2a), port at `46f2013`, CasaEngine at `b56ed923` (branch `chantier/racinggame-lighting`), reference unchanged (seed 1). The comparison tool now samples 46 probes on 5 views (`scripts/TrackCaptureCompare/probes.json`).

Causes found and fixed:

- The engine multiplied the ambient term by the texture twice (CasaEngine ADR-0098), and used the reflection cube as ambient source whenever one was bound; a world can now ask for a flat ambient (ADR-0099).
- The race world had three hard-coded lights, the key one about 19 degrees from the original's, and an ambient of 0.05. It now has the original's single light and the original's ambient and diffuse per material.
- The port raised the ambient of signs, banners and windmills to 128/255 after an override that is dead code in the original.
- The port had added an emissive term to its generated materials.

Mean absolute difference on the red channel, before (after the tangents) and after this slice:

| View | Before | After |
|---|---|---|
| Beginner-start | 30.65 | 21.92 |
| Beginner-sector-20 | 50.03 | 34.65 |
| Beginner-sector-50 | 28.43 | 20.74 |
| Beginner-sector-80 | 37.27 | 29.34 |
| Beginner-overview | 24.53 | 19.09 |
| Advanced-start | 26.70 | 22.51 |
| Advanced-sector-20 | 41.38 | 28.41 |
| Advanced-sector-50 | 26.29 | 18.98 |
| Advanced-sector-80 | 27.64 | 17.63 |
| Advanced-overview | 26.38 | 18.40 |
| Expert-start | 27.21 | 17.57 |
| Expert-sector-20 | 28.90 | 24.49 |
| Expert-sector-50 | 25.58 | 20.20 |
| Expert-sector-80 | 24.46 | 16.34 |
| Expert-overview | 26.42 | 21.15 |

Probes: lit and facing-away points within 12 of the original per channel: 19 of 34 (10 before). The slice's target, all of them, is not reached. The 15 left:

| Probes | Cause | Evidence | Where it goes |
|---|---|---|---|
| 7, lit terrain (+12 to +19) | The port's terrain has a specular term (0.05, power 6); the original's terrain shader has none. | All pass in a run with that specular at zero. | Materials slice |
| 5, lit road (+13 to +37) | The port's road has no normal map and no tangents; the original computes `texture × (A + D × bump)`. | Read in the code; not measured. | Materials slice |
| 3, city ground | The port casts a shadow there, the original does not. | All pass in a run without shadows. | Shadow slice |
| 1, casino wall (+77) | Likely: a glass panel of `Casino01` (`ReflectionSimpleGlass`, no texture) that the original draws opaque and dark through a technique fallback, and the port draws as reflective glass. | Zoom on both pictures; not proven on the geometry. | Materials slice |

Status of the earlier findings: M5 (buildings brighter) is reduced, not closed (casino wall above); M2 (signs and banners almost black) is closed for its lighting part, their look now depends on the reflection technique of the original (materials slice); L1 (shadows) is untouched except for the direction, which now follows the original's light: the gantry and building shadows fall where the original's do, still black-edged and untinted.

Not measured: the frame-time cost of the engine changes (the game has no tool for it). The car selection screen shows no visible change (its capture differs only at byte level).

## Update after the material base slice (2026-11-14)

Plan `ai-agent/tasks/rgce-3d-materials-base-tasks.md`, port at `4c058ad`, CasaEngine at `2dea831a` (branch `chantier/racinggame-lighting`), reference unchanged (seed 1). Direction decided by the author: a common base now, a PBR material in the engine later; the original's per-technique specular and reflection formulas are no longer reproduced.

Causes found and fixed:

- No texture of the port had mipmaps: the engine's loader created a single level, while the original builds its 199 textures with mipmaps. The loader can now build the chain (CasaEngine ADR-0101) and the race world uses it. The noise on far ground and far road is gone.
- The terrain had no detail texture (M1). The engine's lit material gains one (ADR-0100); the terrain binds `LandscapeDetail` tiled 24 times, as the original.
- The terrain had a specular term the original does not have.
- The road had no tangents and no normal map (M4, for its relief).
- The sky was the mirror image of the original's (G7): the sky cube is converted to the port's axes (ADR-0017), which also puts the glass reflections on the original's convention.
- Imported diffuse colours were truncated instead of rounded.

Mean absolute difference on the red channel and share of the original's edges found in the port, before and after this slice:

| View | Difference before | after | Edges before | after |
|---|---|---|---|---|
| Beginner-start | 21.92 | 15.84 | 61.5 % | 99.1 % |
| Beginner-sector-20 | 34.65 | 32.92 | 68.2 % | 67.0 % |
| Beginner-sector-50 | 20.74 | 16.25 | 61.6 % | 87.3 % |
| Beginner-sector-80 | 29.34 | 24.49 | 64.7 % | 96.1 % |
| Beginner-overview | 19.09 | 10.48 | 93.1 % | 99.3 % |
| Advanced-start | 22.51 | 16.43 | 46.1 % | 96.1 % |
| Advanced-sector-20 | 28.41 | 23.13 | 50.1 % | 92.0 % |
| Advanced-sector-50 | 18.98 | 15.12 | 62.2 % | 89.3 % |
| Advanced-sector-80 | 17.63 | 13.04 | 51.2 % | 98.7 % |
| Advanced-overview | 18.40 | 8.93 | 95.2 % | 100.0 % |
| Expert-start | 17.57 | 13.04 | 60.0 % | 96.8 % |
| Expert-sector-20 | 24.49 | 18.43 | 41.8 % | 97.4 % |
| Expert-sector-50 | 20.20 | 14.43 | 58.0 % | 93.3 % |
| Expert-sector-80 | 16.34 | 11.37 | 49.8 % | 98.1 % |
| Expert-overview | 21.15 | 10.01 | 98.0 % | 99.9 % |

`Beginner-sector-20` is the view the shadows dominate: the port still shadows the city ground where the original does not (shadow slice).

Probes: lit and facing-away points within 12 of the original: 24 of 34 (19 before). The 10 left: 5 lit road probes (the original's road specular, left to the PBR work), 3 city ground probes (shadows), the casino wall (glass panel, PBR work), and one terrain probe that misses by one level.

Left open:

- The port's terrain is about 7 % brighter than the original's, uniformly, lit or not. The detail texture is not the cause (the gain was there before it). Cause not found.
- Enabling mipmaps raised the mean difference slightly on two views (`Beginner-sector-80` +0.14, `Expert-sector-20` +0.18) before the detail texture brought both well below; on `Expert-sector-20` the change sits on palms, road and rails. Cause not isolated.
- The original reads the sky cube with two conventions depending on the technique: glass follows the displayed sky, signs and car paint do not. Only the first is reproduced; the second is one of the original's accidental behaviours, to be decided case by case with the PBR work.
- The car selection screen: the windscreen reflection changes with the converted sky cube; nothing else visibly.

## Update after the shadow slice (2026-11-14)

Plan `ai-agent/tasks/rgce-3d-lighting-shadows-tasks.md` (S2b), port at `d40b020`, CasaEngine at `585af17d` (branch `chantier/racinggame-lighting`). The reference is now `artifacts/capture-bench/original-s2b`: same 30 pictures as before, byte for byte, and a `scene.json` that also says which objects cast and receive shadows in the original.

What was done:

- The engine's shadows gain a mode that multiplies the lit colour by a shadow colour, a filter radius for soft edges, a fade before the shadow distance, and a depth range separate from the lateral extent (CasaEngine ADR-0102). Defaults keep the previous output.
- The race world uses the original's shadow colour (0.25, 0.26, 0.27) in that mode, a filter radius of 3, and a map of 100 m with a 15 m fade.
- Casters and receivers follow the original object by object: only its "near track" objects cast; among them banners, signs and alpha models do not receive. Measured: no mismatch on either flag on the three tracks (they differed on 1015 and 1059 of Beginner's 1147 objects before). Road, guard rails and tunnels cast and receive; the road underside and the generated columns do neither; the terrain and the city ground only receive.

What the bench shows (L1, G2):

- Shadows have the original's grey-blue tint and fall where the original's do. On terrain, the shadow/lit ratio matches the original (0.281 against 0.284, 0.264 against 0.267); it was 1.0 before.
- Edges are graded over 3 to 4 pixels, still crisper than the original's screen-space blur; thin shadows stay thin where the original smears them.
- The mean difference falls on every non-overview view (three-channel mean), slightly: for example `Beginner-sector-80` 23.73 to 22.20, `Expert-sector-80` 10.84 to 9.87.

Probes: 24 of 34 lit and facing-away points within 12 (unchanged); shadow/lit ratio right on 2 of 4 surfaces (0 before); same points lit and shadowed on both sides: 27 of 33 (unchanged); shadowed faces of near buildings: 3 of 3.

What does not match, and why:

| Difference | Cause | Status |
|---|---|---|
| The original is 6 to 8 % darker everywhere, even outside its shadow map. | The weights of its shadow blur sum to 0.965, not 1 (`Core/Content/Shaders/PostScreenShadowBlur.fx`), and the blurred mask is multiplied over the whole frame after two passes: every pixel is multiplied by 0.965 x 0.965 = 0.931. With that pass off, the original's lit terrain equals the port's to the level. This is the "terrain 7 % brighter" left open by the material base slice. | Measured, cause found by the independent verification. Not reproduced: it is an accident of the original (see the PBR preparation, A11). |
| The city ground left of the road in `Beginner-sector-20` is shadowed in the port, lit in the original. | The shadow of Hotel02, about 75 m high: the engine's orthographic map casts it; the original's perspective map, with its light 129 m away, clips the top of the building. | The Hotel shadow as the cause of the 80-level drop: measured. Without it these points still miss by 15 to 23: about 9 from the darkening above, the rest unexplained. The clipping itself: computed, not measured. |
| A soft dark patch on the road in `Beginner-sector-50`, and a darker road at the start of Beginner, are only thin bands in the port. | Cast by the track meshes in the original. Back-face culling and the map extent are ruled out by measurement. Candidates left: the original's screen-space blur, its perspective map. | Not isolated. |
| Palms cast leaf-shaped shadows in the port, card-shaped ones in the original. | The original draws every caster two-sided and without alpha test. | Known, listed with the original's accidental behaviours. |
| In the original, anything drawn in front of a shadowed receiver is darkened too, receiver or not. | Its shadows are a screen-space mask multiplied over the whole frame. | Approximated per object in the port. |
| On the overview views the port's shadows sit under the camera, the original's near the start grid. | The engine centres its map on the camera, the original on the car. | Known limit; an engine setting for the map centre would lift it. |

Not measured: the render cost of the 12-tap filter and of the multiply mode.

## Update after the PBR switch (2026-11-16)

Plan `ai-agent/tasks/rgce-pbr-race-world-tasks.md`, decision ADR-0018, CasaEngine at `48d720a6`. The race world and the car now use `LitPbrMaterial`, light the scene with the key light at pi and the sky cube (image-based ambient and reflections), and render in `LinearHdr` (tonemapper `AcesApprox`, exposure 1, FXAA). The bench takes `--color-pipeline legacy|linear` (default `linear`); both modes use the same PBR materials, only the colour chain differs (`scripts/TrackCaptureCompare/README.md`).

What the bench still proves, and what it no longer does:

- **Still valid, in both modes.** Geometry and placement: the three `scene-measure-*.txt` are byte for byte identical to those recorded before the switch (road points at 0.000 m, objects matched, no difference of shadow flags). Shadow placement: the shadow or light class of the probes is unchanged from before the switch (27 of 33 agree with the original; the six that do not are the same `shadow` probes of `city` and `road`, which the port renders lit). Repeatability: two captures in a row give 0.00 on every channel and 0.0 % of different pixels on the 15 views in each mode (0.01 on the three channels of `Expert-overview` in `linear`, the flicker already seen before the switch). `[CaptureBench]` logs `fxaaPasses 0` in `legacy` and `fxaaPasses 1` in `linear` for every view.
- **No longer comparable.** Colours. The bench compares against the original captured without its post-process, and the port's materials, light and colour chain are different by design (D1, D2, P7). Probe criterion 1 (lit and away within 12) falls from 24 of 34 to 0 in `legacy` and 2 in `linear`; this is the expected effect of the switch, not a regression to chase. Mean differences of the sheets are no longer a progress measure.

Numbers at the end of the chantier (probes of `scripts/TrackCaptureCompare/probes.json` against `artifacts/capture-bench/original-s2b`, `--no-terrain-detail-correction`):

| Criterion | `legacy` | `linear` |
|---|---|---|
| 1. lit and away within 12 | 0 of 34 | 2 of 34 |
| 2. shadow/lit ratio within 0.05 | 2 of 4 | 0 of 4 |
| 3. shadow or light class | 27 of 33 | 27 of 33 |
| 4. shadowed faces of near buildings within 12 | 0 of 3 | 3 of 3 |

Left for the author to judge by eye, with the values that stay starting points (plan point O4):

- The lit road over the lit terrain luminance ratio of `Beginner-start` (`road-lit-1`, `terrain-lit-1`): 0.6853 in `legacy` and 0.7116 in `linear`, against 0.5610 before the switch and 0.3509 in the original. The road is lighter than the original's: its roughness (0.85) and base colour are starting values.
- The terrain shadow/lit ratio in `linear`: 0.172 and 0.138 against 0.284 and 0.267 in the original (0.282 and 0.263 in `legacy`). The cause was not found; the hypothesis noted when the pipeline was switched (the shadow colour applied to the linear light, then encoded) was not checked.
- The six `shadow` probes of the city ground and the road that the original shades and the port lights (ratios 1.86 and 0.84 in `legacy`, 2.80 and 0.90 in `linear`, against 0.275 and 0.227): unchanged by the switch, the shadow slice's limits remain (the Hotel02 shadow, the original's screen-space blur).
- The faces turned away from the sun are lit by the sky: `building-away-1` is 159,174,182 in `legacy` and 161,169,175 in `linear`, against 14,13,12 in the original. In `legacy` the shadowed faces of near buildings are 12 to 17 levels too light (0 of 3).
- The image-based lighting is derived from the 512 pixel sky cube without mipmaps (O1): not judged on a race. In the car selection carousel, which stays in `Legacy` (no tonemapping), the white car is very light.

The original's dark opaque glass (A5) is replaced: the glass of the buildings is a mirror of the sky (the black casino wall, plan point O2, was not inspected). The original's glint on the road (M4) has no PBR counterpart here: the road is a rough dielectric (0 / 0.85).

## Update after the road material (2026-11-18)

Decisions: see ADR-0034. Plan `ai-agent/tasks/rgce-road-tunnel-shadow-tasks.md`, slices S1 and S2; outputs under `artifacts/pbr-handover/s22/`.

- **Engine fix taken first (S1).** The engine now offsets the shadow lookup along the vertex normal instead of the normal-mapped one (CasaEngine `cdfc21b0e`). On the 15 views at most 0.220 % of the pixels change by more than 8 levels (shadow edges); E stays 17.762 in `linear` (17.759 before). On the right wall of the Expert tunnel the ragged, speckled shadow edge is gone: the speckle measure of `artifacts/pbr-handover/s22/scripts/speckle.py` falls from 0.863 % to 0.352 % (0.363 % in the original).
- **Road material (S2).** The top of the road is the family `RoadSurface` (metallic 0, roughness 0.30) and receives no environment light (`occlusion_texture` black, `occlusion_strength` 1). The statement of the PBR switch section above, that the original's glint on the road has no PBR counterpart, no longer holds: the sun highlight of the road is back.

Measured in `linear` with `artifacts/pbr-handover/s22/scripts/measure_s2.py` (Lr: median luminance of the 223 lit road points; G: mean luminance of the highlight box of `Beginner-sector-50`; E: mean difference per channel with the original over the 15 views; Tb / Ta / Te: road box of the three tunnel views; Sat: share of pixels with a channel at 255):

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

Applied state: E 17.762 -> 15.732 in `linear`, 20.572 -> 20.249 in `legacy`; probes 20 of 34, 0 of 4, 27 of 33, 3 of 3 in `linear` (17 of 34 before) and 4 of 34, 2 of 4, 27 of 33, 3 of 3 in `legacy`. Remaining gaps on the road: the highlight is below the original's (189.3 for 209.9), the road in the tunnels is darker than the original's, and the original's darkening of the road near the car (73 of 423 points) is not imitated.

## Update after the shadow filter taps (2026-11-18)

Decisions: see ADR-0035. Plan `ai-agent/tasks/rgce-shadow-colour-decor-tasks.md`, slice G1; outputs under `artifacts/pbr-handover/s24/g1/`.

The race world filters its shadow edges with 64 taps instead of 12 (engine setting `FilterTaps`, CasaEngine ADR-0127). On the right wall of the Expert tunnel the fan of bands at the edge of the shadow becomes a gradient. Against the previous reference only shadow edges change: at most 1.231 % of the pixels of a view outside the tunnels, 2.171 % on the Expert tunnel. E is unchanged: 15.734 in `linear` (15.732 before), 20.245 in `legacy` (20.249); the probes are unchanged. The cost in frame time was not measured.
