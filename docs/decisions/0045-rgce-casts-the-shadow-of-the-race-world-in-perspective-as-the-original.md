# ADR-0045: RacingGameCasaEngine casts the shadow of the race world in perspective, as the original does

- **Status**: Proposed
- **Date**: 2026-11-22
- **Source**: this chantier: `ai-agent/tasks/rgce-render-fidelity-tasks.md`, slice F6 (decision D2 of the author of 2026-11-21: the engine reproduces the shadow projection of the original; decision of the author of 2026-11-22: a second grid of candidates with the same rule; rule of choice written before each measurement in the frozen scripts `measure_shadow.py` and `measure_shadow2.py`); engine ADR-0147

## Context

Facts verified on 2026-11-22:

- The original draws its shadow from a virtual light placed 129.25 m from an aimed point, against the direction of the light, with a perspective that covers `atan2(129.25, 171.6) * 129.25` = 83.44 m by 129.25 m at that point, between the planes 1 and 218.75 (`RacingGameOriginal/Core/Shaders/ShadowMapShader.cs:208-266`). During a race the aimed point is the car plus its direction times 129.25 / 6. Its shadows spread out as they move away from the virtual light. It reads the map with 10 reads within one texel, fades it with a texture (full up to 80 % of the half-width, a rounded square), multiplies the image by the shadow colour and blurs the result on screen; it biases the caster and the receiver by 0.00065 each and has no normal bias (`Core/Content/Shaders/ShadowMap.fx:100-126`, `:172-240`).
- The same objects cast and receive in the original and in the port: the road (both sides), the tunnels, the guard rails, the objects near the track and the car cast; the terrain only receives (`Core/Tracks/Track.cs:1294-1339`, `Core/Landscapes/Landscape.cs:1284-1326`; the flags of the port are those of `LegacyTrackSceneFactory`).
- The port cast its shadow from an orthographic square of 200 units centred on the camera (4096 texels, parallel shadows). On `Beginner-sector-20` the shadows of the city did not fall where the original's do: the shadow to lit luminance ratio of the city was 1.961 for 0.276 in the original, and that view was the farthest of the 15 from the original (E 31.281).
- The engine gives the directional shadow a perspective projection from a virtual light, with a linear depth, and a runtime aimed point `ShadowSettings.FocusPosition` (engine ADR-0147). Its `DepthBias` is then a fraction of the interval between the planes: the two biases of the original are worth 0.00118.
- ADR-0036 keeps the mean M of the median luminances of the three tunnel views inside the author's window, 28.882 to 31.882 (the original is at 30.38). Before this decision M was 30.860, made of the medians 27.50 / 26.57 / 38.50 for 11.07 / 39.29 / 40.79 in the original: one view too bright, two too dark. The original's capture of `Advanced-tunnel` contains the sun and its halo, which the port does not draw.

## Decision

- **The race world casts its shadow in perspective with the projection of the original** (`RaceWorldTemplates.ApplyRaceEnvironment`): `Projection` `Perspective`, `LightDistance` 129.25, `ViewWidth` 83.4372, `ViewHeight` 129.25, `NearPlane` 1, `FarPlane` 218.75.
- **The aimed point follows the player's car as in the original**: the new component `RaceShadowFocusComponent` of the entity `RaceRoot` sets `Shadows.FocusPosition` every update to the position of the car plus its visual forward direction times 129.25 / 6; with an orthographic projection it sets `null`. It saves nothing with the world. The capture bench sets its runtime `FocusOverride` to the point the original aimed at for the view it replays (the car of the views file, in the original's coordinates, then converted).
- **Settings**: map of 2048 texels; fade over the last 8.34 m of the half-width (a fifth of it, the band of the original's fade texture); **depth bias 0.002** (0.44 m); **normal bias 0**; filter radius 3 texels and 64 reads (ADR-0035, unchanged); shadow colour (0.38, 0.3952, 0.4104) (ADR-0036, unchanged, measured again). `MaxDistance` and `DepthRange` keep their values: the perspective projection does not use them, and `MaxDistance` only bounds the fade distance.

Candidates, measured in `linear` on a copy of the build folder whose race worlds receive the settings, three replays each (15 views, 3 tunnel views, 15 views in `legacy`). E = mean absolute difference per channel with the original (`nopost`) over the 15 views; H = probes held among the nine the slice aims at (criterion 1 of `Beginner-sector-20/city-lit-1..3`, criterion 3 of `city-shadow-1..3` and of `Beginner-sector-50/road-shadow-1..3`). Rule, frozen before the measurement: a candidate is eligible when H rises, E falls, E of the tunnels does not rise, criterion 3 does not fall and stays at 27 at least in `legacy`, criteria 1, 2 and 4 do not fall, no probe is lost, and M stays in the window; retained: the eligible candidate of largest H, then of smallest E.

First grid: the projection and the settings of the original (`orig`: depth bias 0.00118, normal bias 0.1 of the port, radius 3, 64 reads, colour level 0.38), and one setting changed at a time.

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

No candidate of the first grid is eligible: all leave the window of the tunnels. Asked, the author chose a second grid with the same rule, without the normal bias, which the original does not have, around the filter radius and the depth bias.

| Candidate | Setting (normal bias 0) | H | E | E tunnels | M | Medians `Beginner` / `Advanced` / `Expert` tunnel | Criteria 1 / 2 / 3 / 4 | Eligible |
|---|---|---|---|---|---|---|---|---|
| `nb0` | radius 3, depth bias 0.00118 | 3 | 12.765 | 12.089 | 27.885 | 24.72 / 25.08 / 33.86 | 27 / 3 / 30 / 3 | no: M outside the window |
| `n-r1.5` | radius 1.5, depth bias 0.00118 | 3 | 12.830 | 12.472 | 29.195 | 27.43 / 26.43 / 33.72 | 27 / 3 / 30 / 3 | yes |
| `n-db0.002` | radius 3, depth bias 0.002 | 3 | 12.772 | 12.471 | 29.150 | 26.87 / 26.22 / 34.36 | 27 / 3 / 30 / 3 | yes, **retained** |
| `n-db0.003` | radius 3, depth bias 0.003 | 3 | 12.774 | 12.785 | 29.982 | 28.65 / 26.58 / 34.72 | 27 / 3 / 30 / 3 | yes |
| `n-r1.5-db0.002` | radius 1.5, depth bias 0.002 | 3 | 12.826 | 12.561 | 29.481 | 27.87 / 26.57 / 34.00 | 27 / 3 / 30 / 3 | yes |
| `n-r1.5-db0.003` | radius 1.5, depth bias 0.003 | 3 | 12.823 | 12.605 | 29.839 | 28.36 / 26.58 / 34.57 | 27 / 3 / 30 / 3 | yes |

Applied state (replayed twice, identical; the 15 views and the 3 tunnel views in `linear` and the 15 views in `legacy` are byte-identical to the captures of `n-db0.002`): E 14.131 -> 12.772 in `linear` and 15.355 -> 14.134 in `legacy`; E of the tunnel views 12.835 -> 12.471 in `linear` and 13.742 -> 13.471 in `legacy`; M 30.860 -> 29.150; probes held 27 / 2 / 27 / 3 -> 27 / 3 / 30 / 3 in `linear` and 16 / 0 / 27 / 3 -> 19 / 0 / 30 / 3 in `legacy` (criteria 1 / 2 / 3 / 4). On the three start views the point computed from the car of the port is the point of the views file (distance 0.000).

## Consequences

- The shadows of the city of `Beginner-sector-20` fall where the original's do: E of that view 31.281 -> 18.325, its three `city-shadow` probes are held, and the shadow to lit ratio of the city is 0.275 for 0.276 in the original. It is the largest gain of the 15 views; `Beginner-sector-80` goes 15.304 -> 14.502 and `Advanced-start` 11.496 -> 11.148.
- **Six of the nine probes aimed at stay out of reach**: the three `city-lit` probes, and the three `road-shadow` probes of `Beginner-sector-50`, where the shadow the original casts across the road does not appear in the port (ratio of the road 0.769 for 0.228; it was 0.734). Its cause is not known.
- **The depth bias and the normal bias are not the original's.** With the original's bias and the normal bias of the port (`orig`) the views are as close (E 12.803) and the tunnels closer on average (E of the tunnels 11.502), but M falls to 24.976, out of the window: the medians of the tunnel views are 19.21 / 22.00 / 33.72. The retained setting gives 26.87 / 26.22 / 34.36, for 11.07 / 39.29 / 40.79 in the original. `Beginner-tunnel` stays brighter than the original and the two others darker, as before this decision; the window is held by the same compensation. The tunnel walls are one layer thick and almost parallel to the rays, and their shadow follows the biases and the filter radius (second grid); that this is the cause of the darkening is an inference from those measures, not a proof.
- Differences with the original that remain: the fade is a square and not the original's rounded square; there is no screen blur, the softness comes from 64 reads over 3 texels instead of 10 reads within one texel and a blur; the shadow colour is applied in linear light (ADR-0036).
- **Not checked in motion.** The bench captures still views. The perspective map is not snapped to its texels (engine ADR-0147), so the edge of a shadow can crawl when the car moves; the orthographic map was snapped. This is to be looked at in play.
- The map is 2048 texels instead of 4096: a quarter of the video memory. 4096 measures the same (first grid). Not timed.
- ADR-0035 (64 reads) and ADR-0036 (shadow colour) stand: 32 reads measure the same on the views and 64 is kept; the colour levels 0.30 and 0.46 both lose the shadow to lit ratios of the bench.
- The three race worlds change (the `shadows` object and the new component of `RaceRoot`); the scene export does not (the shadow settings are not in it). 3 of the 10 interface captures change, those with a 3D race world; the six screens without a 3D world and the carousel are byte-identical. 20 of the 30 track audit captures change.
- Against the original with its post-process the difference goes 30.183 -> 28.920: that variant is the reference of the glow slice (F7), not of this one.
- Not covered: the lens flare and the sun (F5), the glow (F7).
