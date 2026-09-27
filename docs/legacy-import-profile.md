# Legacy Import Profile

This workspace keeps project-specific legacy material interpretation out of CasaEngine while still preserving compatibility with RacingGame content.

## Engine side

- `CasaEngine.Framework.Assets.Loaders.ILegacyMaterialImportProfile` is the neutral extension point for reinterpreting preserved legacy import metadata.
- `NeutralLegacyMaterialImportProfile` is the default fallback. It only consumes explicit metadata already present on imported materials.
- `LegacyImportedMaterialPresentationResolver` is the shared generic mapping from imported hints to render-ready presentation data used by both the editor import path and the RacingGame runtime compatibility path.
- `GltfStaticModelReader.ReadWithMetadata(...)` and `EditorAssetImportService.ImportFile(...)` both accept an optional legacy import profile.
- The runtime only reads glTF models: CasaEngine removed its runtime Assimp importer (`StaticModelImporter`), and `GltfStaticModelReader` does not read the legacy `.x` `EffectInstance` metadata.

## RacingGame converted models

- The legacy `.x` models of `RacingGame/Content/Models` are converted once by `scripts/LegacyModelGltfConverter` (see its README) into `RacingGameCasaEngine/Content/Models/*.gltf` and `*.bin`, with their `.tga` textures converted to `RacingGameCasaEngine/Content/Textures/*.png`.
- The converter stores the legacy material metadata in the glTF material `extras`: `legacyMaterial` (diffuse, specular, emissive, shininess) and `legacyEffect` (raw `EffectInstance`: file, dwords, floats, strings).
- `RacingGameCasaEngine/Bootstrap/LegacyGltfModelReader.cs` reads a converted model with `GltfStaticModelReader`, restores the legacy metadata from the `extras` with the rules of the old importer, then applies the legacy import profile.

## RacingGame bootstrap

- `RacingGameCasaEngine/Bootstrap/RacingGameImportProfiles.cs` exposes the project-owned profile instance.
- `RacingGameCasaEngine/Bootstrap/RacingGameLegacyMaterialImportProfile.cs` owns the RacingGame-specific rules:
  - exact `LegacyTechniqueIndex` reflection mapping,
  - `Sign` / `Banner` / `Windmill` bright-ambient conventions,
  - `Alpha` / `Palm` / `Leave` / `Ast` / `plants` alpha-cutout conventions.
- `RacingGameCasaEngine/Worlds/LegacyTrackSceneFactory.cs` and `RacingGameCasaEngine/Components/LegacyCarVisualFactory.cs` load the converted models through `LegacyGltfModelReader` with that profile.
- `RacingGameCasaEngine/Bootstrap/LegacyImportProfileVerifier.cs` is the bounded regression harness for representative assets.

## Isolation guarantees

- CasaEngine preserves raw legacy metadata such as effect path, technique index, reflection textures, and imported hints.
- CasaEngine does not hardcode RacingGame asset-name heuristics or RacingGame-specific technique tables.
- CasaEngine only applies generic consequences of imported hints:
  - alpha-cutout queue, alpha cutoff, and cull-none behavior,
  - bright ambient floor,
  - ambient and emissive color clamping.
- Any future game-specific naming convention or technique interpretation must stay in a project-owned profile, not in `CasaEngine/CasaEngine/**` or `CasaEngine/CasaEngine.EditorServices/**`.

## How to plug another game profile

1. Implement `ILegacyMaterialImportProfile` inside the game project.
2. Expose it from a project bootstrap class similar to `RacingGameImportProfiles`.
3. Pass it to `GltfStaticModelReader.ReadWithMetadata(...)` (or to a project reader that restores the legacy metadata first, like `LegacyGltfModelReader`) and `EditorAssetImportService.ImportFile(...)` where legacy content is imported.
4. Add a bounded verifier on representative assets before deleting any compatibility fallback.

## Bounded verification

```powershell
dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -c Debug --no-restore
$process = Start-Process -FilePath RacingGameCasaEngine/bin/Debug/net9.0-windows/RacingGameCasaEngine.exe `
  -ArgumentList '--verify-legacy-import-profile' -NoNewWindow -Wait -PassThru
$process.ExitCode
```

Expected result: `0`.