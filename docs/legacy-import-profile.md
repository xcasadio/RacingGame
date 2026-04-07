# Legacy Import Profile

This workspace keeps project-specific legacy material interpretation out of CasaEngine while still preserving compatibility with RacingGame content.

## Engine side

- `CasaEngine.Framework.Assets.Loaders.ILegacyMaterialImportProfile` is the neutral extension point for reinterpreting preserved legacy import metadata.
- `NeutralLegacyMaterialImportProfile` is the default fallback. It only consumes explicit metadata already present on imported materials.
- `LegacyImportedMaterialPresentationResolver` is the shared generic mapping from imported hints to render-ready presentation data used by both the editor import path and the RacingGame runtime compatibility path.
- `StaticModelImporter.ImportWithMetadata(...)` and `EditorAssetImportService.ImportFile(...)` both accept an optional legacy import profile.

## RacingGame bootstrap

- `RacingGameCasaEngine/Bootstrap/RacingGameImportProfiles.cs` exposes the project-owned profile instance.
- `RacingGameCasaEngine/Bootstrap/RacingGameLegacyMaterialImportProfile.cs` owns the RacingGame-specific rules:
  - exact `LegacyTechniqueIndex` reflection mapping,
  - `Sign` / `Banner` / `Windmill` bright-ambient conventions,
  - `Alpha` / `Palm` / `Leave` / `Ast` / `plants` alpha-cutout conventions.
- `RacingGameCasaEngine/Worlds/LegacyTrackSceneFactory.cs` imports runtime legacy models through that profile.
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
3. Pass it to `StaticModelImporter.ImportWithMetadata(...)` and `EditorAssetImportService.ImportFile(...)` where legacy content is imported.
4. Add a bounded verifier on representative assets before deleting any compatibility fallback.

## Bounded verification

```powershell
dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -c Debug --no-restore
$process = Start-Process -FilePath RacingGameCasaEngine/bin/Debug/net9.0-windows/RacingGameCasaEngine.exe `
  -ArgumentList '--verify-legacy-import-profile' -NoNewWindow -Wait -PassThru
$process.ExitCode
```

Expected result: `0`.