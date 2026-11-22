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

## Update after the shadow colour (2026-11-18)

Decisions: see ADR-0036. Plan `ai-agent/tasks/rgce-shadow-colour-decor-tasks.md`, slice G2; outputs under `artifacts/pbr-handover/s24/g2/`.

The shadow colour of the race world is (0.38, 0.3952, 0.4104) instead of the original's (0.25, 0.26, 0.27): the engine applies it in linear light before the tone curve, where 0.25 left the shadows about half as bright as the original's. Measured in `linear` with `artifacts/pbr-handover/s23/scripts/measure_tint.py` (r1, r2: shadow over lit ratio of the terrain on `Beginner-start` and `Expert-sector-80`, original 0.284 and 0.267; S: sum of their differences with the original; E: mean difference per channel over the 15 views; M: median of the top band of the tunnel views, window 28.882 to 31.882; Dt: mean difference per channel of the tunnel views):

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

Applied state: E 15.734 -> 15.312 in `linear`, 20.245 -> 20.009 in `legacy`; probes 20 of 34, 2 of 4, 27 of 33, 3 of 3 in `linear` (criterion 2 was 0 of 4) and 4 of 34, 0 of 4, 27 of 33, 3 of 3 in `legacy` (criterion 2 was 2 of 4: there the colour is not decoded). The road of the tunnels is at 11.5 / 20.2 / 27.9 for 11.7 / 21.8 / 31.1 in the original.

## Update after the palm foliage (2026-11-18)

Decisions: see ADR-0037. Plan `ai-agent/tasks/rgce-shadow-colour-decor-tasks.md`, slice G3; outputs under `artifacts/pbr-handover/s24/g3/`.

The four leaf materials of the palms take a base grey of 170 and the original's alpha cutoff of 0.5. Measured in `linear` with `artifacts/pbr-handover/s23/scripts/measure_leaves.py` on the green-dominant pixels of four boxes (D: mean colour difference of the leaves with the original; area: leaf pixels of the candidate over the original's; E: mean difference per channel over the 15 views):

| Base grey | Alpha cutoff | D | d per box | Leaf area per box | E | Eligible |
|---|---|---|---|---|---|---|
| 255 | 0.35 | 26.99 | 41.9 / 27.3 / 18.4 / 20.3 | 3.17 / 1.62 / 1.26 / 1.23 | 15.312 | yes |
| 255 | 0.5 | 28.57 | 41.8 / 28.3 / 20.3 / 23.9 | 3.10 / 1.58 / 1.20 / 1.11 | 15.287 | yes |
| 220 | 0.35 | 19.83 | 35.2 / 20.4 / 9.5 / 14.3 | 3.04 / 1.56 / 1.24 / 1.13 | 15.260 | yes |
| 220 | 0.5 | 21.24 | 34.9 / 21.1 / 11.1 / 17.8 | 3.00 / 1.52 / 1.19 / 1.03 | 15.232 | yes |
| 190 | 0.35 | 14.96 | 29.8 / 14.9 / 5.9 / 9.3 | 2.84 / 1.45 / 1.21 / 0.98 | 15.236 | yes |
| 190 | 0.5 | 15.46 | 29.5 / 15.3 / 4.9 / 12.2 | 2.84 / 1.43 / 1.16 / 0.91 | 15.205 | yes |
| 170 | 0.35 | 14.19 | 26.6 / 11.8 / 9.7 / 8.6 | 2.62 / 1.33 / 1.17 / 0.86 | 15.232 | yes |
| 170 | 0.5 | 13.88 | 26.2 / 11.7 / 8.9 / 8.7 | 2.64 / 1.34 / 1.13 / 0.83 | 15.199 | yes |
| 150 | 0.35 | 14.79 | 23.8 / 12.4 / 13.2 / 9.7 | 2.27 / 1.17 / 1.10 / 0.73 | 15.235 | yes |
| 150 | 0.5 | 14.39 | 23.4 / 12.4 / 12.6 / 9.2 | 2.32 / 1.19 / 1.08 / 0.70 | 15.201 | yes |
| 130 | 0.35 | 15.77 | 21.4 / 13.1 / 16.3 / 12.2 | 1.76 / 0.96 / 0.99 / 0.57 | 15.243 | yes |
| 130 | 0.5 | 15.43 | 21.2 / 13.1 / 15.9 / 11.6 | 1.81 / 0.98 / 0.98 / 0.57 | 15.207 | yes |

Applied state: D 26.99 -> 13.88; E 15.312 -> 15.199 in `linear`, 20.009 -> 19.959 in `legacy`; probes unchanged. The cause of the leaves' excess brightness is not established: the base level is a calibration.

## Update after the window glass of the buildings (2026-11-18)

Decisions: see ADR-0038. Plan `ai-agent/tasks/rgce-shadow-colour-decor-tasks.md`, slice G4; outputs under `artifacts/pbr-handover/s24/g4/`.

Measured in `linear` with `artifacts/pbr-handover/s23/scripts/measure_windows.py`, each material alone (M: mean difference per channel with the original over the pixels of the material in the 15 views; E: mean difference per channel over the 15 views, 15.199 at the start):

| Material | Mask pixels | Receives the shadow | Base colour | M | E | Eligible | Retained |
|---|---|---|---|---|---|---|---|
| Hotel01 / 39 | 10848 | yes | 255 / 255 / 255 | 79.97 | 15.199 | yes |  |
| Hotel01 / 39 | 10848 | no | 255 / 255 / 255 | 74.64 | 15.195 | yes | **yes** |
| Hotel01 / 39 | 10848 | yes | 224 grey | 90.33 | 15.209 | no (E) |  |
| Hotel01 / 39 | 10848 | no | 224 grey | 85.93 | 15.206 | no (E) |  |
| Hotel02 / fenster | 11064 | yes | 94 / 162 / 231 | 146.27 | 15.199 | yes |  |
| Hotel02 / fenster | 11064 | no | 94 / 162 / 231 | 87.65 | 15.150 | yes |  |
| Hotel02 / fenster | 11064 | yes | 224 grey | 132.31 | 15.187 | yes |  |
| Hotel02 / fenster | 11064 | no | 224 grey | 47.35 | 15.116 | yes | **yes** |
| Casino01 / 39 | 8187 | yes | 64 / 64 / 64 | 133.17 | 15.199 | yes |  |
| Casino01 / 39 | 8187 | no | 64 / 64 / 64 | 133.09 | 15.199 | yes |  |
| Casino01 / 39 | 8187 | yes | 224 grey | 46.08 | 15.135 | yes | **yes** |
| Casino01 / 39 | 8187 | no | 224 grey | 45.87 | 15.135 | yes |  |

Applied together: E 15.199 -> 15.047 in `linear`, 19.959 -> 19.806 in `legacy`; probes unchanged. The statement of the PBR switch section above, that the original's dark opaque glass is replaced by a mirror of the sky and that the black casino wall was not inspected, is completed here: measured over all its pixels, the casino glass is closer to the original with a 224 grey base.

## Update after the car glass (2026-11-18)

Decisions: see ADR-0039. Plan `ai-agent/tasks/rgce-shadow-colour-decor-tasks.md`, slice G5; outputs under `artifacts/pbr-handover/s24/g5/`.

The base colour of the car glass is (255, 174, 148) instead of grey 224: it compensates the blue of the reflected sky. Iteration of `artifacts/pbr-handover/s23/scripts/measure_glass.py` on the rear window of `Beginner-start` (box x 608-626, y 388-391; original 136.9 / 137.6 / 137.9):

| Step | Base colour R / G / B | Box R / G / B | Mean error | Largest error | E | Criterion 3 |
|---|---|---|---|---|---|---|
| g0 | 224 / 224 / 224 | 114.7 / 175.7 / 196.2 | 39.50 | 58.22 | 15.047 | 27 |
| g1 | 255 / 175 / 158 | 134.4 / 138.7 / 147.4 | 4.33 | 9.41 | 15.046 | 27 |
| g2 | 255 / 174 / 148 | 134.4 / 137.7 / 137.3 | 1.09 | 2.47 | 15.046 | 27 |

E and the probes are unchanged. Side effect: the reflections that are not the sky turn warm; in the car selection carousel the headlights turn orange.

## Update after the leaf of the small palm (2026-11-18)

Decisions: see ADR-0040. Plan `ai-agent/tasks/rgce-shadow-colour-decor-tasks.md`, slice G7; outputs under `artifacts/pbr-handover/s25/g7/`.

The leaf of the small palm (`PalmLeave2.tga`) takes a base grey of 220 instead of the 170 of the other palms: the boxes of the foliage rule did not contain it, and 170 took it away from the original. Measured in `linear` with `artifacts/pbr-handover/s25/scripts/measure_smallpalm.py` on the 21601 pixels of the material in the 15 views (original mean colour 83.8 / 98.3 / 57.7; first row: start state):

| Base grey | Alpha cutoff | M | Mean R / G / B over the mask | E | Criterion 3 |
|---|---|---|---|---|---|
| 170 | 0.5 | 26.80 | 62.8 / 76.4 / 55.2 | 15.046 | 27 |
| 255 | 0.35 | 25.72 | 85.1 / 106.5 / 64.5 | 15.047 | 27 |
| 255 | 0.5 | 25.94 | 86.2 / 107.5 / 65.2 | 15.044 | 27 |
| 220 | 0.35 | 22.58 | 74.9 / 93.4 / 60.0 | 15.043 | 27 |
| 220 | 0.5 | 22.63 | 76.0 / 94.4 / 60.7 | 15.039 | 27 |
| 190 | 0.5 | 24.38 | 67.8 / 83.4 / 57.2 | 15.042 | 27 |

Applied state: M 26.80 -> 22.63; E 15.046 -> 15.039 in `linear`, 19.806 -> 19.793 in `legacy`; probes unchanged.

## Update after the scenery seed (2026-11-18)

Decisions: see ADR-0041. Plan `ai-agent/tasks/rgce-worlds-as-assets-tasks.md`, slice M0; outputs under `artifacts/worlds-as-assets/m0/`.

The game now draws its random scenery with the seed of the reference capture (1) in every mode; the reference capture no longer sets a seed, it checks the one of its `views.json`. Checked with `artifacts/worlds-as-assets/scripts/m0_check.py`, every run replayed twice and identical byte for byte:

| Runs | Against `artifacts/pbr-handover/s25/g7/ref/` |
|---|---|
| 15 reference views, `linear` and `legacy` | byte-identical; scene measures identical |
| 3 tunnel views, `linear` and `legacy` | byte-identical; scene measures identical |
| 30 track audit captures | 23 change, 7 identical (the six road-only captures and the terrain overview of Beginner) |
| 10 interface screens | `ui-race-hud`, `ui-pause`, `ui-race-finished` change; the 7 others identical |

Placement digests logged by the race worlds (`Track placements <track>: ... digest`), the same in the reference views, the track audit, the interface captures and the scene export: Beginner `197D7EF3A381BD78` (1147 placements), Advanced `C6D4C53FF70AEEB1` (1693), Expert `EF69E08B051B8297` (3404). Before, outside the reference views: `6EBF84F81D01AC88` (1274), `FD0564E1183D98FE` (1757), `29A2403A3F0CDCFA` (3432).

Reference of the track audit and of the interface from now on: `artifacts/worlds-as-assets/m0/ref/`.

## Update after the terrain ambient (2026-11-21)

Decisions: see ADR-0043. Plan `ai-agent/tasks/rgce-render-fidelity-tasks.md`, slice F2; outputs under `artifacts/render-fidelity/f2/` of the worktree of the chantier. From this slice on the bench runs from that worktree; its first replay (slice F0) is byte-identical to `artifacts/worlds-as-assets/m1/ref/`.

The terrain carries the flat ambient of the original through the engine key `ambient_color` (0.42) and no longer receives the light of the sky. A = mean absolute difference per channel with the original on the 12 `away` probes of the terrain.

| Candidate | A | E | Et | Probes 1 / 2 / 3 / 4 | Criterion 3, `legacy` | Eligible |
|---|---|---|---|---|---|---|
| start state (no ambient of its own, sky light) | 11.583 | 15.039 | 12.844 | 20 / 2 / 27 / 3 | 27 | - |
| 0.345, without the sky light | 9.500 | 14.579 | 12.844 | 25 / 2 / 27 / 3 | 27 | yes |
| 0.42, without the sky light | 8.056 | 14.374 | 12.844 | 27 / 2 / 27 / 3 | 27 | yes |
| 0.50, without the sky light | 6.889 | 15.025 | 12.844 | 24 / 2 / 27 / 3 | 27 | no (1 lit probe(s) lost) |
| 0.58, without the sky light | 7.833 | 16.788 | 12.844 | 15 / 2 / 27 / 3 | 27 | no (E, c1, 8 lit probe(s) lost) |
| 0.66, without the sky light | 11.694 | 19.330 | 12.844 | 13 / 2 / 27 / 3 | 27 | no (E, c1, 9 lit probe(s) lost) |
| 0.74, without the sky light | 16.417 | 22.273 | 12.844 | 9 / 2 / 27 / 3 | 27 | no (E, c1, 11 lit probe(s) lost) |
| 0.82, without the sky light | 21.444 | 25.428 | 12.844 | 4 / 1 / 27 / 3 | 27 | no (E, c1, c2, 12 lit probe(s) lost) |
| 0.25, with the sky light | 11.000 | 14.521 | 12.844 | 20 / 2 / 27 / 3 | 27 | yes |
| 0.345, with the sky light | 10.583 | 14.655 | 12.844 | 21 / 2 / 27 / 3 | 27 | yes |
| 0.42, with the sky light | 11.056 | 15.470 | 12.844 | 20 / 2 / 27 / 3 | 27 | no (E, 1 lit probe(s) lost) |
| 0.50, with the sky light | 13.556 | 17.207 | 12.844 | 17 / 2 / 27 / 3 | 27 | no (E, c1, 4 lit probe(s) lost) |
| 0.58, with the sky light | 17.167 | 19.497 | 12.844 | 11 / 2 / 27 / 3 | 27 | no (E, c1, 8 lit probe(s) lost) |

Applied state: A 11.583 -> 8.056; E 15.039 -> 14.374 in `linear`, 19.793 -> 15.509 in `legacy`; E against the original with its post-process 31.648 -> 30.116; tunnel views 12.844 -> 12.844; probes 20 / 2 / 27 / 3 -> 27 / 2 / 27 / 3 in `linear`, 4 / 0 / 27 / 3 -> 16 / 0 / 27 / 3 in `legacy`.

Reference of the bench from now on: `artifacts/render-fidelity/f2/ref/` of the worktree.

## Update after the scenery ambient (2026-11-21)

Decisions: see ADR-0044. Plan `ai-agent/tasks/rgce-render-fidelity-tasks.md`, slice F3; outputs under `artifacts/render-fidelity/f3/` of the worktree of the chantier.

Five scenery families and the city ground carry the ambient of the original through the engine key `ambient_color` and no longer receive the light of the sky. M = mean absolute difference per channel with the original on the pixels of a group (mask taken with the materials of the group drawn as markers).

| Group | Materials | Mask (pixels) | M start | x 1.00, sky | x 1.22, sky | x 1.00, no sky | x 1.22, no sky | x 1.45, no sky | Retained | M applied |
|---|---|---|---|---|---|---|---|---|---|---|
| `Masonry` | 13 | 222482 | 14.546 | 13.315 | 13.142 | 12.800 | 11.829 | 11.137 | x 1.45, without the sky light | 11.136 |
| `Vegetation` | 1 | 32 | - | - | - | - | - | - | left aside (mask below 500 pixels) | - |
| `LampPost` | 11 | 94829 | 24.715 | 26.101 | 26.893 | 27.263 | 25.357 | 23.132 | x 1.45, without the sky light | 23.132 |
| `GuardRail` | 1 | 5740 | 19.576 | 22.081 | 23.667 | 17.942 | 16.434 | 15.991 | x 1.45, without the sky light | 15.987 |
| `MatteIndustrial` | 6 | 209154 | 31.949 | 29.294 | 28.382 | 26.120 | 24.816 | 23.883 | x 1.45, without the sky light | 23.883 |
| `Default` | 28 | 94106 | 12.435 | 12.870 | 13.486 | 9.551 | 9.292 | 9.262 | x 1.45, without the sky light | 9.261 |
| city ground | 1 | 410871 | 42.047 | 42.440 | 42.654 | 41.088 | 41.307 | 41.592 | x 1.00, without the sky light | 41.091 |

Applied state: E 14.374 -> 14.131 in `linear`, 15.509 -> 15.355 in `legacy`; E against the original with its post-process 30.116 -> 30.183; tunnel views 12.844 -> 12.835; difference outside the masks 13.130 -> 13.125; probes 27 / 2 / 27 / 3 -> 27 / 2 / 27 / 3 in `linear`, 16 / 0 / 27 / 3 -> 16 / 0 / 27 / 3 in `legacy`.

Reference of the bench from now on: `artifacts/render-fidelity/f3/ref/` of the worktree.

## Update after the perspective shadow (2026-11-22)

Decisions: see ADR-0046. Plan `ai-agent/tasks/rgce-render-fidelity-tasks.md`, slice F6; outputs under `artifacts/render-fidelity/f6/` of the worktree of the chantier.

The race world casts its shadow in perspective from a virtual light, with the projection of the original (engine ADR-0155), aimed ahead of the player's car. E = mean absolute difference per channel with the original (`nopost`) over the 15 views; H = probes held among the nine the slice aimed at; M = mean of the median luminances of the three tunnel views, to hold between 28.882 and 31.882 (ADR-0036). First grid, around the settings of the original:

| Candidate | Setting | H | E | E tunnels | M | Criteria 1 / 2 / 3 / 4 | Criterion 3 `legacy` | E `Beginner-sector-20` | Eligible |
|---|---|---|---|---|---|---|---|---|---|
| `base` | settings before (orthographic) | 0 | 14.131 | 12.835 | 30.860 | 27 / 2 / 27 / 3 | 27 | 31.281 | - |
| `orig` | projection and settings of the original | 3 | 12.803 | 11.502 | 24.976 | 27 / 3 / 30 / 3 | 30 | 18.481 | no: M outside the window |
| `res4096` | map of 4096, radius 6 | 3 | 12.814 | 11.454 | 24.953 | 27 / 3 / 30 / 3 | 30 | 18.566 | no: M outside the window |
| `r1.5` | filter radius 1.5 | 3 | 12.862 | 11.223 | 25.308 | 27 / 3 / 30 / 3 | 30 | 18.614 | no: M outside the window |
| `r6` | filter radius 6 | 3 | 12.726 | 11.845 | 24.648 | 27 / 3 / 30 / 3 | 30 | 18.416 | no: M outside the window |
| `nb0` | normal bias 0 | 3 | 12.765 | 12.089 | 27.885 | 27 / 3 / 30 / 3 | 30 | 18.297 | no: M outside the window |
| `db0.0006` | depth bias 0.0006 | 3 | 12.800 | 11.734 | 21.336 | 27 / 3 / 30 / 3 | 30 | 18.458 | no: M outside the window |
| `db0.002` | depth bias 0.002 | 3 | 12.803 | 11.773 | 27.529 | 27 / 3 / 30 / 3 | 30 | 18.504 | no: M outside the window |
| `fade0` | no fade | 3 | 12.840 | 11.500 | 24.976 | 27 / 2 / 29 / 3 | 29 | 18.623 | no: a probe lost, M outside the window |
| `taps32` | 32 reads | 3 | 12.805 | 11.605 | 24.928 | 27 / 3 / 30 / 3 | 30 | 18.496 | no: M outside the window |
| `c0.30` | shadow colour level 0.30 | 3 | 12.959 | 12.626 | 23.787 | 27 / 0 / 30 / 3 | 30 | 18.975 | no: criterion 2 falls, M outside the window |
| `c0.46` | shadow colour level 0.46 | 3 | 12.927 | 13.981 | 26.312 | 27 / 0 / 30 / 3 | 30 | 19.166 | no: E of the tunnels rises, criterion 2 falls, M outside the window |

Second grid, asked by the author after the first retained nothing, without normal bias:

| Candidate | Setting (normal bias 0) | H | E | E tunnels | M | Medians `Beginner` / `Advanced` / `Expert` tunnel | Criteria 1 / 2 / 3 / 4 | Eligible |
|---|---|---|---|---|---|---|---|---|
| `nb0` | radius 3, depth bias 0.00118 | 3 | 12.765 | 12.089 | 27.885 | 24.72 / 25.08 / 33.86 | 27 / 3 / 30 / 3 | no: M outside the window |
| `n-r1.5` | radius 1.5, depth bias 0.00118 | 3 | 12.830 | 12.472 | 29.195 | 27.43 / 26.43 / 33.72 | 27 / 3 / 30 / 3 | yes |
| `n-db0.002` | radius 3, depth bias 0.002 | 3 | 12.772 | 12.471 | 29.150 | 26.87 / 26.22 / 34.36 | 27 / 3 / 30 / 3 | yes, **retained** |
| `n-db0.003` | radius 3, depth bias 0.003 | 3 | 12.774 | 12.785 | 29.982 | 28.65 / 26.58 / 34.72 | 27 / 3 / 30 / 3 | yes |
| `n-r1.5-db0.002` | radius 1.5, depth bias 0.002 | 3 | 12.826 | 12.561 | 29.481 | 27.87 / 26.57 / 34.00 | 27 / 3 / 30 / 3 | yes |
| `n-r1.5-db0.003` | radius 1.5, depth bias 0.003 | 3 | 12.823 | 12.605 | 29.839 | 28.36 / 26.58 / 34.57 | 27 / 3 / 30 / 3 | yes |

Applied state (`n-db0.002`): E 14.131 -> 12.772 in `linear`, 15.355 -> 14.134 in `legacy`; tunnel views 12.835 -> 12.471; M 30.860 -> 29.150; E against the original with its post-process 30.183 -> 28.920; probes 27 / 2 / 27 / 3 -> 27 / 3 / 30 / 3 in `linear`, 16 / 0 / 27 / 3 -> 19 / 0 / 30 / 3 in `legacy`. `Beginner-sector-20`: E 31.281 -> 18.325, shadow to lit ratio of the city 1.961 -> 0.275 for 0.276 in the original. Still out of reach: the three `city-lit` probes and the three `road-shadow` probes of `Beginner-sector-50` (ratio of the road 0.769 for 0.228).

The bench now sets the aimed point of the shadow for each view from the car of the views file (`car.position` and `car.direction`), as the original's capture does, and logs on each view the point computed from the car of the port next to it (`[CaptureBench] view <name> shadowFocus ...`): on the three start views the distance is 0.000.

Reference of the bench from now on: `artifacts/render-fidelity/f6/ref/` of the worktree.

## State at the end of the render fidelity plan (2026-11-22)

Plan `ai-agent/tasks/rgce-render-fidelity-tasks.md` (slices F0 to F8), with the engine plan `CasaEngine/ai-agent/tasks/render-fidelity-tasks.md`. Decisions: ADR-0043, ADR-0044, ADR-0046. Outputs under `artifacts/render-fidelity/` of the worktree of the chantier, copied to the main checkout at the closing.

| Measure against the original | Start (F0) | After the terrain ambient (F2) | After the scenery ambient (F3) | After the perspective shadow (F6), final |
|---|---|---|---|---|
| E, 15 views, `linear`, `nopost` | 15.039 | 14.374 | 14.131 | 12.772 |
| E, 3 tunnel views, `linear`, `nopost` | 12.844 | 12.844 | 12.835 | 12.471 |
| Probes held, `linear` (criteria 1 / 2 / 3 / 4, of 34 / 4 / 33 / 3) | 20 / 2 / 27 / 3 | 27 / 2 / 27 / 3 | 27 / 2 / 27 / 3 | 27 / 3 / 30 / 3 |
| E, 15 views, `legacy`, `nopost` | 19.793 | 15.509 | 15.355 | 14.134 |
| Probes held, `legacy` | 4 / 0 / 27 / 3 | 16 / 0 / 27 / 3 | 16 / 0 / 27 / 3 | 19 / 0 / 30 / 3 |
| E, 15 views, `linear`, against the original with its post-process (`full`) | 31.648 | 30.116 | 30.183 | 28.920 |

Measured and not applied: the per-pixel rotation of the shadow filter (slice F4: no gain; it does not compile as a guarded option on the `ps_3_0` profile) and the glow (slice F7: eleven candidates of brightness, contrast, colour offset and bloom against `full`; the best, a brightness of 1.2, takes the views from 28.920 to 27.174 but the tunnel views from 14.744 to 14.836, and none is eligible under the rule). Not done: the sun and its lens flare (slice F5), deferred by the author until an audit of the component life cycle of the engine.

Reference of the bench: `artifacts/render-fidelity/f6/ref/` of the worktree.
