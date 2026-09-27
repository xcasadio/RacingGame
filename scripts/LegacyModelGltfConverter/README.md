# LegacyModelGltfConverter

Converts the legacy RacingGame DirectX `.x` models to `.gltf` files that the CasaEngine runtime reads with `GltfStaticModelReader`, without any change to CasaEngine.

## Why

CasaEngine removed its runtime Assimp importer (`StaticModelImporter`, CasaEngine commit `726b2f5a1`, ADR-0019): the runtime only reads glTF. The editor converter (`AssimpToGltfConverter`) alone is not enough for the RacingGame models:

- their textures are only declared in the `.x` `EffectInstance` blocks, which Assimp does not import;
- the converter writes node transforms as matrices, which `GltfStaticModelReader` cannot read;
- `GltfStaticModelReader` only reports texture paths for external images, and glTF only accepts PNG/JPEG images while the legacy textures are `.tga`.

## Usage

Run from the repository root (the project references `CasaEngine/CasaEngine.EditorServices`):

```
dotnet run --project scripts/LegacyModelGltfConverter -- RacingGame/Content/Models RacingGameCasaEngine/Content/Models RacingGameCasaEngine/Content/Textures --skip Cube
```

Arguments: `<x models directory> <gltf output directory> <png output directory> [--skip <model name>]...`.
`Cube` is skipped because RacingGameCasaEngine does not load it (the legacy game uses it through MGCB).

The exit code is `0` when every model converts; any error (unmatched material names, missing texture, non-decomposable node transform) is printed and gives exit code `1`.

## Output

For each `<Name>.x`:

- `<gltf dir>/<Name>.gltf` and `<Name>.bin`: geometry from `AssimpToGltfConverter`, node transforms decomposed to translation/rotation/scale;
- `<png dir>/<Texture>.png`: each `diffuseTexture` / `normalTexture` `.tga` of an effect instance, converted losslessly (RGBA), same base name; referenced from the glTF as an external image (`BaseColor` and `Normal` channels);
- material `extras`, used by RacingGameCasaEngine to restore what the old runtime importer read:
  - `legacyMaterial`: `diffuse`, `specular`, `emissive`, `shininess` from the Assimp material (`null` when absent);
  - `legacyEffect`: raw copy of the material `EffectInstance` (`file`, `dwords`, `floats`, `strings`), parsed with the rules of the old importer (ported in `LegacyXFileParser.cs`).

Materials are matched by name between the `.x` file, the Assimp import and the glTF file.

## Validation

The conversion was checked against the output of the old `StaticModelImporter` (CasaEngine `295db0c6`) on every converted model: material metadata (effect file, technique, colors, specular power, diffuse/normal/reflection texture file names) and geometry. See `docs/racinggame-casaengine-update-tasks.md`, task T2.1.
