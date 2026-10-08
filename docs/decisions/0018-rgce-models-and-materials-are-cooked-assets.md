# ADR-0018: RacingGameCasaEngine's models, materials and textures are cooked assets

- **Status**: Accepted
- **Date**: 2026-10-08
- **Source**: this chantier: `ai-agent/tasks/rgce-editor-project-tasks.md`, decisions D2 and D3 (author answers, 2026-10-08), proposal P8 (validated with the plan), slice S5 and tasks T5.1 to T5.6; CasaEngine commit `359a0a98b`

## Context

- The game read the 56 legacy glTF models by path and built every material in code:
  - scenery, rail posts, column segments and start lights went through `LegacyTrackSceneFactory.LoadLegacyModel`. Their values came from rules on texture names (`RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning`) and from `LegacyImportedMaterialPresentationResolver`. A root node rotated by pi/2 about X was added at load time;
  - the car (`LegacyCarVisualFactory`) and the selection plate (`CarSelectionCarousel.LoadPlateModel`) had their own rules: paint, reflection blend, normal map;
  - the road, ground, terrain, rail and column materials were written in code.
- The editor could not open or edit any of these: only catalogued `.staticModel`, `.material` and `.texture` assets are editable, and the game's PNG images were catalogued but referenced by no id.
- A `lit-diffuse` `.material` holds every value the game used. `MaterialCompiler` always names the blend, depth, rasterizer and sampler states, while the game left several of them `null`, which the renderer draws as `Opaque`, `Default`, `CullCounterClockwise` and `AnisotropicClamp` (CasaEngine `RenderStateCache.cs:51-54`).
- A compiled material's render features were computed from the material alone. A catalogued material with a normal map drawn on a mesh without tangents (none of the 56 glTF has tangents) would have chosen a normal-map technique without the data it needs.
- The author decided that the race worlds become editable (D2) and that engine gaps are fixed in CasaEngine (D3). P8 proposed cooking the models with a repository tool into standard assets with deterministic ids.

## Decision

- **Cooked assets.** `scripts/RgceAssetCooker` cooks, with the editor serializers:
  - one `Models/<Name>.staticModel` per glTF (`StaticModel.<Name>`), with the geometry the legacy reader reads. The rotated root is cooked into the models that `LoadLegacyModel` loaded, not into the car or the selection plate;
  - one `lit-diffuse` `.material` per model material (`Material.<Name>.<Slot>`) and per track material (`Material.Track.<Role>`), with the values of `RaceMaterialRecipes`, the pure functions extracted from the code that built the materials;
  - one `Textures/<Name>.texture` per image a cooked material uses (`Texture.<Name>`). The PNG entries are renamed `Image.<Name>`.
  - Ids are uuid5 of the asset name, so a second run changes nothing. `--check` lists what is missing and `--verify` compares every cooked file with the glTF and the recipes.
- **Render states.** A state the game left `null` is cooked as the state the renderer applies to `null`. Parity is measured under that rule (`compare_race_scene_exports.py --effective-states`).
- **The game loads by id.** Scenery, posts, segments and start lights are `StaticModelComponent`s on their `StaticModel.<Name>`, shared through the engine's handles; model sizes for placements come from the cooked models. The track generators take their material from a serialized `material_asset_id`, a cooked `Material.Track.<Role>` by default. Name-based material rules no longer run in the game.
- **Run-time state stays in code.**
  - **Car paint**: each car index loads a private copy of `StaticModel.Car` (`LoadCopy`). The slots whose base texture is `Texture.RacerCar` get, in that copy only, a copy of their material with a new id and the painted or unpainted texture of `CarPaint`. The painted-or-not rule is the legacy one, on the slot name; `--verify` checks it against the recipes.
  - **Start light**: `RaceStartLight` switches its component between three cooked models.
- **Engine fix (D3).** CasaEngine `359a0a98b` resolves a compiled material's render features against the drawn mesh (`RenderFeatureResolver.AdjustToMesh`, `ResolveForMesh`): `NormalMap` is dropped without tangents and `VertexColor` is added with vertex colours, as for a material built in code. `RenderItem.Features` returns the value set by the renderer.
- **Parity tooling.** The scene export records each drawn `SubMesh` with the material the renderer takes and the render features it draws with. The cooked game is compared with references taken before S5, by export, track audit, car audits and UI captures.

## Consequences

- Complements ADR-0016 and ADR-0017. Every model, material and texture of the race opens in the editor; the walk of the whole catalogue is recorded with T5.7.
- Editing a cooked `.material`, `.staticModel` or `.texture` in the editor changes the game. Running the cooker again rewrites every cooked file whose bytes differ from the glTF and the recipes, so it reverts such edits, and `--verify` reports them as mismatches. The cooker has no way to keep an edited asset.
- 81 cooked materials name a normal texture. The game draws them without a normal map, as before, because the meshes have no tangents.
- Car paint is not in the assets. The private material copies keep the cooked `MaterialAssetId`, so a hot reload of a car material would draw it unpainted. The copies and the paint textures live until the process ends, as before.
- `EvaluateRuntimeTuning` stays only in `LegacyImportProfileVerifier`, a validation tool.
