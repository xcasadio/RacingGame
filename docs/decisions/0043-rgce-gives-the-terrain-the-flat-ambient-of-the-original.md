# ADR-0043: RacingGameCasaEngine gives the terrain the flat ambient of the original

- **Status**: Proposed
- **Date**: 2026-11-21
- **Source**: this chantier: `ai-agent/tasks/rgce-render-fidelity-tasks.md`, slice F2 (decision D1 of the author of 2026-11-21: the ambient per material is a key of the engine; rule of choice written before the measurement in a frozen script, applied mechanically; plan approved by the author on 2026-11-21, AUTO mode); the measurement: `artifacts/render-fidelity/f2/` of the worktree of the chantier (`measure_terrain.json`, `measure_terrain.txt`, one folder per candidate, `applied/`, `f2_check.txt`) and the frozen script `artifacts/render-fidelity/scripts/measure_terrain.py` (SHA-256 `6288f619b409c3dd…`); the engine key: `CasaEngine/docs/decisions/0146-a-lit-pbr-material-can-carry-its-own-flat-ambient-colour.md`

## Context

Facts verified on 2026-11-21:

- The original draws its terrain with `texture * (ambient + saturate(N.L) * diffuse) * 2 * detail` (`RacingGameOriginal/Core/Content/Shaders/LandscapeNormalMapping.fx:498-520`, technique `DiffuseWithDetail20`), ambient (88, 88, 88) and diffuse (234, 234, 234) (`Core/Landscapes/Landscape.cs:597-607`). A face turned away from the sun keeps `0.345 * texture * detail`: brown and textured. Its shadow multiplies the rendered image, that ambient included.
- In the port the terrain was a `lit-pbr` material without an ambient of its own. A face turned away from the sun received the light of the sky only (image-based light, ambient of the world 0.05 in `Auto`, ADR-0031, ADR-0032): grey or blue, darker than the original on some faces and lighter on others. On the 12 probes of class `away` of the terrain, the mean absolute difference with the original was 11.583 per channel and 8 probes failed criterion 1. The same pixels are identical in the captures taken without shadows: the shadow is not the cause.
- A trial on the ambient of the world (2026-11-18, open point O4 of `rgce-shadow-colour-decor-tasks.md`) had shown a weak lever with a cost on the buildings: the ambient of the world reaches every material.
- The engine now gives `lit-pbr` the key `ambient_color` (engine ADR-0146, decision D1 of the author): base colour x ambient colour added to the pixel, detail texture included, without the occlusion of the material, with the shadow tint of the `MultiplyLitColor` mode, which is the mode of the race world. The occlusion of a `lit-pbr` material scales the image-based light only, so a black occlusion texture at full strength takes the light of the sky off a material (ADR-0034, the road).

## Decision

- **The terrain carries the flat ambient 0.42**: `ambient_color` (0.42, 0.42, 0.42) on the material of the terrain (`RaceMaterialRecipes.ForTerrain`, constant `TerrainAmbientLevel`), white like the original's.
- **The terrain no longer receives the light of the sky**: its occlusion texture is the black `NoEnvironmentLight.png` of the road at strength 1.
- `RaceMaterialRecipe` gains the field `AmbientColor`; the cooker writes `ambient_color` only when it is not (0, 0, 0), and its verifier reads an absent key as (0, 0, 0). The other materials keep their files as they are. The city ground, which has its own recipe, is not changed here.

Candidates, measured in `linear` on a copy of the build folder (`matcand.py`), each replayed on the 15 views, the 3 tunnel views and the 15 views in `legacy`: seven levels without the light of the sky and five with it, around the original's 0.345. Quantities (`measure_terrain.py`): A, the mean absolute difference per channel with the original on the 12 `away` probes of the terrain; E, the mean difference per channel over the 15 views; Et, the same over the 3 tunnel views; the probes held on the four criteria. Rule: eligible when E and Et do not rise, criterion 3 stays at 27 or more in `linear` and in `legacy`, criteria 1, 2 and 4 do not fall and no `lit` probe of the terrain that holds criterion 1 loses it; retained: the smallest A below the one of the start state (between two candidates closer than 0.05, the smallest E).

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

The level 0.50 has the smallest A but a sunlit probe of the terrain leaves the tolerance of criterion 1; the rule retains 0.42. With the light of the sky kept, no level comes close. The level is above the original's 0.345 because the tone curve of the race world darkens the low values.

Applied state (replayed twice, identical; the 15 views and the 3 tunnel views in `linear` and the 15 views in `legacy` are byte-identical to the captures of the candidate): A 11.583 -> 8.056; E 15.039 -> 14.374 in `linear` and 19.793 -> 15.509 in `legacy`; E of the tunnel views 12.844 -> 12.844; probes 20 / 2 / 27 / 3 -> 27 / 2 / 27 / 3 in `linear`, 4 / 0 / 27 / 3 -> 16 / 0 / 27 / 3 in `legacy`.

## Consequences

- The faces of the terrain turned away from the sun are brown and textured again: 11 of the 12 `away` probes of the terrain hold criterion 1 (4 before); the one that does not is on the right-hand slope of `Expert-sector-80`.
- The level is a measured calibration under the tone curve and the exposure of the race world (ADR-0032): it must be measured again if they change.
- The terrain is lit by the sun and by its own ambient only. A change of the sky or of the ambient of the world no longer reaches it.
- The scene export gains the parameter `AmbientColor` on every `lit-pbr` record (engine key); only the three terrain records change their values (`AmbientColor` and the occlusion texture).
- Three interface captures with a 3D world change (`ui-race-hud`, `ui-pause`, `ui-race-finished`) and 21 of the 30 track audit captures; the seven other interface captures, the carousel included, are byte-identical.
- Not covered: the city ground and the scenery materials (slice F3 of the plan), the position of the shadows (slice F6).
