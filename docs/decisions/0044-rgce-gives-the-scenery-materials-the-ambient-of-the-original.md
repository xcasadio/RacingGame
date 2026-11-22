# ADR-0044: RacingGameCasaEngine gives the scenery materials the ambient of the original

- **Status**: Proposed
- **Date**: 2026-11-21
- **Source**: this chantier: `ai-agent/tasks/rgce-render-fidelity-tasks.md`, slice F3 (decision D1 of the author of 2026-11-21: the ambient per material is a key of the engine; rule of choice written before the measurement in a frozen script, applied mechanically; plan approved by the author on 2026-11-21, AUTO mode); the measurement: `artifacts/render-fidelity/f3/` of the worktree of the chantier

## Context

Facts verified on 2026-11-21:

- On the scenery path of the original (`RacingGameOriginal/Core/Graphics/MeshRenderManager.cs:415-420`) the shader receives, per material, the ambient, diffuse and specular colours of the `EffectInstance` block of its `.X` file, through the tag set by the model processor (`PipelineExtension/RacingGameModelProcessor.cs:94-118`, `Core/Graphics/Material.cs:221-254`). The scenery techniques of `NormalMapping.fx` multiply that ambient by the texture. The values read in the `.X` files go from 0.10 (palms, train) to 0.486 (windmill); a material without an effect block keeps the default 40 of 255. The forcing to (128, 128, 128) of `Model.cs:271-275` is overwritten by the value of the tag and does not reach the scenery shader. The city ground has the ambient (32, 32, 32) (`Core/Landscapes/Landscape.cs:614-619`).
- In the port the ambient of each material reaches the recipe (`.X`, then `extras.legacyEffect.floats.ambientColor` of the glTF, then `LegacyGltfModelReader.cs:94-96`, then `RaceMaterialRecipes.ForScenery`), which used it only for the emissive of the signs (ADR-0033). A scenery material had no ambient of its own: a face turned away from the sun received the light of the sky only.
- The engine gives `lit-pbr` the key `ambient_color` (engine ADR-0154): base colour x ambient colour added to the pixel, with the shadow tint of the `MultiplyLitColor` mode. ADR-0043 used it for the terrain, with a black occlusion texture that takes the light of the sky off the material.
- The scenery materials were measured by group: the six families of `RacingGamePbrMaterialMapping` that `ForScenery` gives a matte or default material (`Masonry`, `Vegetation`, `LampPost`, `GuardRail`, `MatteIndustrial`, `Default`; the leaves of the palms left out) and the city ground (`ForGround`). The mask of a group is the set of pixels of the 15 reference views where its materials, drawn as magenta markers, are visible (the method of ADR-0038); M is the mean absolute difference per channel with the original on that mask.

## Decision

- **Five families carry the ambient of their model file, times 1.45, and no longer receive the light of the sky**: `Masonry`, `LampPost`, `GuardRail`, `MatteIndustrial` and `Default`. `RaceMaterialRecipes.ForScenery` sets `AmbientColor` to the imported ambient colour times the factor of the family (`SceneryAmbientOf`) and gives the material the black occlusion texture `NoEnvironmentLight.png` of the road at strength 1.
- **The city ground carries the ambient 32 of 255 of the original, at factor 1, and no longer receives the light of the sky** (`RaceMaterialRecipes.ForGround`).
- **Left as they are**: the family `Vegetation` (one material, the plants of `Hotel01`: its mask has too few pixels to be measured), the signs (their ambient is an emissive, ADR-0033), the window glass (ADR-0038), the leaves of the palms (their base level was calibrated without an ambient, ADR-0037 and ADR-0040), the car, the plate of the carousel and the meshes built for the track (road, tunnel, columns, guard rails of the track).
- `ForScenery` and `ForGround` gain the parameter `occlusionTexturePath`; the cooker passes the path of `NoEnvironmentLight.png`. The cooked key `ambient_color` and its verification exist since ADR-0043.

Candidates, measured in `linear` on a copy of the build folder, cooked by a temporary build of the cooker that is not committed: five uniform settings (factor 1.00 or 1.22 with the light of the sky; factor 1.00, 1.22 or 1.45 without it) applied to every group measured, replayed on the 15 views. The rule (`measure_decor.py`, frozen before the measurement) composes one candidate from them: per group, the setting of smallest M when it is at least 0.25 below the M of the start state, otherwise no change. That candidate is replayed (15 views, 3 tunnel views, 15 views in `legacy`) and is retained only if E and E of the tunnel views do not rise, no probe loses a criterion it holds, the difference on the pixels outside every mask (U) does not rise, and the M of every group it changes, measured on its own capture, is below the M of the start state.

| Group | Materials | Mask (pixels) | M start | x 1.00, sky | x 1.22, sky | x 1.00, no sky | x 1.22, no sky | x 1.45, no sky | Retained | M applied |
|---|---|---|---|---|---|---|---|---|---|---|
| `Masonry` | 13 | 222482 | 14.546 | 13.315 | 13.142 | 12.800 | 11.829 | 11.137 | x 1.45, without the sky light | 11.136 |
| `Vegetation` | 1 | 32 | - | - | - | - | - | - | left aside (mask below 500 pixels) | - |
| `LampPost` | 11 | 94829 | 24.715 | 26.101 | 26.893 | 27.263 | 25.357 | 23.132 | x 1.45, without the sky light | 23.132 |
| `GuardRail` | 1 | 5740 | 19.576 | 22.081 | 23.667 | 17.942 | 16.434 | 15.991 | x 1.45, without the sky light | 15.987 |
| `MatteIndustrial` | 6 | 209154 | 31.949 | 29.294 | 28.382 | 26.120 | 24.816 | 23.883 | x 1.45, without the sky light | 23.883 |
| `Default` | 28 | 94106 | 12.435 | 12.870 | 13.486 | 9.551 | 9.292 | 9.262 | x 1.45, without the sky light | 9.261 |
| city ground | 1 | 410871 | 42.047 | 42.440 | 42.654 | 41.088 | 41.307 | 41.592 | x 1.00, without the sky light | 41.091 |

Applied state (replayed twice, identical; the 15 views and the 3 tunnel views in `linear` and the 15 views in `legacy` are byte-identical to the captures of the candidate): E 14.374 -> 14.131 in `linear` and 15.509 -> 15.355 in `legacy`; E of the tunnel views 12.844 -> 12.835; U 13.130 -> 13.125; probes 27 / 2 / 27 / 3 -> 27 / 2 / 27 / 3 in `linear`, 16 / 0 / 27 / 3 -> 16 / 0 / 27 / 3 in `legacy`.

## Consequences

- The faces of the scenery turned away from the sun are lit by the ambient of their own material, times their texture, instead of the light of the sky. The largest gain is on `MatteIndustrial` (the windmills, the oil tanks): the pillars of the windmills, darker than the original before, are close to it on the judge board (`artifacts/render-fidelity/f3/judge-decor.jpg`). Elsewhere the change is slight, and small on the whole image (E 14.374 -> 14.131).
- **1.45 is the largest factor of the grid**, and M was still falling there for `Masonry`, `LampPost`, `GuardRail` and `MatteIndustrial` (it is flat for `Default`): a higher factor has not been measured. The factor is above 1, as for the terrain (ADR-0043), because the tone curve of the race world darkens the low values; it is a measured calibration and must be measured again if the tone curve or the exposure change (ADR-0032).
- With the light of the sky kept, the ambient takes `LampPost`, `GuardRail`, `Default` and the city ground away from the original. Without it every group comes closer at the retained setting; `LampPost` does so at the factor 1.45 only. These materials are now lit by the sun and by their own ambient only: a change of the sky or of the ambient of the world no longer reaches them.
- The city ground gains little (its M stays above 41). On `Beginner-sector-20`, which holds most of its pixels, a large part of the ground is in shadow in the port and lit in the original: an ambient does not change that (the shadows are the subject of slice F6).
- 60 material files change (`ambient_color`, `occlusion_texture`, `occlusion_strength`). In the scene export only `MaterialParameters.AmbientColor` and `TextureHashes.OcclusionTexture` change, on the records of those materials.
- Against the original with its post-process the difference rises slightly (30.116 -> 30.183): that variant is the reference of the glow slice (F7), not of this one.
- Three interface captures with a 3D world change (`ui-race-hud`, `ui-pause`, `ui-race-finished`) and 22 of the 30 track audit captures; the seven other interface captures, the carousel included, are byte-identical.
- Not covered: the position and the softness of the shadows (slices F4 and F6), the lens flare (F5), the glow (F7), the reflection of the sky on the scenery (deferred, decision D6 of the plan).
