# ADR-0028: RacingGameCasaEngine cooks the race world materials as PBR, from the surface families of their recipes

- **Status**: Accepted; amended by ADR-0034 (the top of the road leaves the `Road` family: `RoadSurface`, roughness 0.30, no environment light)
- **Date**: 2026-11-17
- **Source**: this chantier: `ai-agent/tasks/rgce-pbr-handover-tasks.md`, decision D6 (author answer, 2026-11-16: keep the cooked assets and the PBR, the cooker and its recipes produce `lit-pbr` materials), slice S1f, tasks T10.1 and T10.2; the other machine's commits `d0db72f`, `09f1c54`, `d35a25a`, `f309d6b`, `fb7daa2`, `b5391fe` and `46f2013` (`origin/remaster`)

## Context

- The two machines made incompatible choices for the materials of the race world, both recorded by an ADR numbered 0018 until the merge:
  - ADR-0018 (the other machine) builds every material in code with `RacingGamePbrMaterialMapping.Create`: a `LitPbrMaterial` whose metallic, roughness and clear coat come from a family table;
  - ADR-0026 (this machine) cooks the models, materials and textures into catalogued assets with `scripts/RgceAssetCooker`, as `lit-diffuse` materials with the values of `RaceMaterialRecipes`. The car paint copies the cooked material at run time and decides painted or not by the slot name and by the reflection settings of the cooked material.
- Before S1f, the 98 cooked materials were `lit-diffuse`, tuned by `RacingGameLegacyMaterialTuning`, with an ambient colour raised on signs, banners and windmills and a per-material reflection cube. The engine's `lit-pbr` definition has no ambient, specular or reflection property (`BuiltInMaterialDefinitions.cs`), so a cooked `lit-pbr` material cannot carry them.
- `LandscapeDetail.png` had an `Image.LandscapeDetail` catalogue entry but no `.texture` asset.
- The author decided (D6) to keep the assets cooked and the PBR.

## Decision

- **The assets stay cooked, as `lit-pbr`.** `scripts/RgceAssetCooker` writes the 98 materials with the `lit-pbr` definition: base and normal textures, detail texture and tiling, base colour, metallic, roughness, clear coat and clear coat roughness, emissive colour, alpha cut-off, queue and render states. The ambient, specular and reflection settings are not cooked. `--verify` compares every one of these values with the recipe.
- **The recipes carry the PBR values.** `RaceMaterialRecipe` holds a PBR family (`RacingGamePbrFamily`: name, metallic, roughness, clear coat, clear coat roughness) and a base colour. The family table and the rules that pick a family are ported from the other machine's `RacingGamePbrMaterialMapping` into `RacingGame.Gameplay/Import` with the same values; the family has a name here, so that two families with the same values (the glass of the buildings and of the car) stay distinguishable.
  - Scenery: by the legacy effect `ReflectionSimpleGlass.fx` (building glass), else by the file name of the diffuse texture, ignoring case and extension; anything else is `Default`.
  - Car: glass by the material name (`glass`, `fenster`) or by the effect (the cooked glass is called `Standard 19` and only its effect names it), then chrome, tyre (`gummi`), paint, else `Default`.
  - Carousel plate `CarouselPlate`; road, road underside, tunnel and columns `Road`; terrain and flat ground `Terrain`; guard rails `GuardRail`.
  - Base colour: the diffuse colour with its alpha forced to 255; 210 on every car part but the glass (`Model.RenderCar` of the original).
- **The terrain has its detail texture.** `Texture.LandscapeDetail` is cooked (with `generate_mipmaps`) and catalogued, and the terrain material uses it, tiled 24 times (`DiffuseWithDetail20` of the original, `f309d6b`).
- **The car paint keeps its place in code, on `lit-pbr`.** The slots whose base texture is `Texture.RacerCar` still get, in the private copy of the model, a copy of their material with the painted or unpainted texture, now a copy of a `LitPbrMaterial`. A slot is painted when its cooked material has a clear coat (`clear_coat` above 0): the car paint is the only family with one. The slot-name rule and the reflection test are gone.
- **The legacy tuning is deleted.** `RacingGameLegacyMaterialTuning`, the ambient raise of the import profile (`46f2013`), the lit-diffuse members of the recipes (ambient, specular, reflection) and `RaceSkySystem.ShouldUseSceneReflectionCube` are removed. The reflection hint of the import profile is set in place from the legacy glass effect (`b5391fe`). `LegacyImportProfileVerifier` checks the expected PBR family of each probed material instead of the specular and reflection tuning.
- **Light, colour pipeline and shadows are not part of this decision.** The race world stays in the `Legacy` colour pipeline under its current light until the next slice (S1g): the render after S1f is an intermediate state, measured and not tuned.

## Consequences

- Amends ADR-0018: its materials are no longer built in code by `RacingGamePbrMaterialMapping.Create`; the mapping holds the family table and the family rules, and the cooker writes what the recipes compute. The values of the families are those of ADR-0018. Its decisions on the colour pipeline, the light and the carousel are not changed here.
- Amends ADR-0026: the cooked materials are `lit-pbr`, not `lit-diffuse`; the paint rule is the clear coat of the cooked material, not the slot name; `EvaluateRuntimeTuning` no longer exists, not even in `LegacyImportProfileVerifier`.
- Editing a cooked `.material` in the editor changes the game as before, and running the cooker again reverts the edit. The values of a family are tuned in `RacingGamePbrMaterialMapping`, then the content is cooked again.
- A material of the race world must go through the recipes and the cooker. A `LitDiffuseMaterial` would be pi times brighter than its neighbours at equal light.
- A `lit-pbr` surface lit by the unchanged light is darker than the `lit-diffuse` one it replaces: the bench figures are in `docs/racinggame-3d-capture-bench-local-baseline.md`, section "After the PBR materials". Setting the light is the next slice.
- 29 materials are `Default`: the scenery that no texture list of the other machine names (trees, palms, train, barriers, cacti, rocks, sand castle, column segments). They keep the values of the other machine's `Default` family.
