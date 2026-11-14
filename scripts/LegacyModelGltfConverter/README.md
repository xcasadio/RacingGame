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
dotnet run --project scripts/LegacyModelGltfConverter -- RacingGame/Content/Models RacingGameCasaEngine/Content/Models RacingGameCasaEngine/Content/Textures --skip Cube --normal-map RacingGame/Content/Textures/LandscapeNormal.tga --normal-map RacingGame/Content/Textures/CityGroundNormal.tga --normal-map RacingGame/Content/Textures/RoadCementNormal.tga --normal-map RacingGame/Content/Textures/RoadBackNormal.tga --normal-map RacingGame/Content/Textures/RoadTunnelNormal.tga --normal-map RacingGame/Content/Textures/RoadNormal.tga --sky-cube RacingGame/Content/Textures/SkyCubeMap.dds
```

Arguments: `<x models directory> <gltf output directory> <png output directory> [--skip <model name>]... [--normal-map <tga file>]... [--sky-cube <dds file>]`.
`--normal-map` converts a normal map no model references (the meshes RacingGameCasaEngine generates: terrain, road structures) into the png output directory.
`--sky-cube` converts the legacy sky cube (a DXT1 cube map without mipmaps) to the axes of the converted world and writes it under its own file name into the png output directory (`RacingGameCasaEngine/Content/Textures/SkyCubeMap.dds`); see Output and ADR-0017. The source must not be in the png output directory.
The tool rewrites every output file: close anything that keeps a `.png` of the output directory open, or it stops on a file access error and must be run again.
`Cube` is skipped because RacingGameCasaEngine does not load it (the legacy game uses it through MGCB).

The exit code is `0` when every model converts; any error (unmatched material names, missing texture, non-decomposable node transform) is printed and gives exit code `1`.

## Output

For each `<Name>.x`:

- `<gltf dir>/<Name>.gltf` and `<Name>.bin`: geometry from `AssimpToGltfConverter`, node transforms decomposed to translation/rotation/scale;
- `<png dir>/<Texture>.png`: each `diffuseTexture` / `normalTexture` `.tga` of an effect instance, converted with the same base name: colour textures losslessly (RGBA), normal maps to the standard channel layout (ADR-0016: red from the legacy green, green from the legacy alpha, opaque alpha); referenced from the glTF as an external image (`BaseColor` and `Normal` channels);
- `<png dir>/SkyCubeMap.dds` (only with `--sky-cube`): the sky cube with `new(P) = old(P.x, P.y, -P.z)` for every direction `P` (the original samples its cube with `(x, z, y)` of its Z-up direction, and the conversion maps legacy `(x, y, z)` to `(x, z, -y)`; ADR-0017). The mirror is a permutation of the texels: the +Z and -Z faces swap, the +-X and +-Z faces flip horizontally, the +-Y faces vertically, and the DXT1 blocks are reordered with the rows or columns of their 2-bit indices reversed, **without recompression**, so no texel value changes. The tool decodes both cubes and checks `new(P) == old(P.x, P.y, -P.z)` at the centre of the 1 572 864 texels, then converts its own result again and requires the source back byte for byte (a mirror is its own inverse); any difference is an error. Only DXT1 cube maps with six faces and no mipmaps are accepted (the engine loader reads those and uncompressed ones);
- a `TANGENT` attribute (VEC4, `w` = +1) on every primitive whose material has a normal texture; `GltfStaticModelReader` reads it, and CasaEngine only enables normal mapping on meshes that have tangents. The tangents are generated like the legacy content processor does (`MeshHelper.CalculateTangentFrames` on the UV channel 0, no binormal): per triangle the direction of increasing U, accumulated on the triangle's vertices without weighting, made orthogonal to the vertex normal and normalised; a vertex whose triangles all have a degenerate UV mapping gets `cross(normal, +X)` like the legacy models. The legacy shader builds its tangent frame as `(cross(normal, tangent), tangent, normal)` without correcting mirrored UVs, so `w` is a constant `+1` (CasaEngine builds `cross(normal, tangent) * w`) and not the glTF handedness of the UV mapping. This relies on the legacy model space and the glTF one being the same up to a rotation or scale (measured: same world positions, normals and UVs, no mirrored node transform); a model with a mirrored world transform would need `w = -1`;
- material `extras`, used by RacingGameCasaEngine to restore what the old runtime importer read:
  - `legacyMaterial`: `diffuse`, `specular`, `emissive`, `shininess` from the Assimp material (`null` when absent);
  - `legacyEffect`: raw copy of the material `EffectInstance` (`file`, `dwords`, `floats`, `strings`), parsed with the rules of the old importer (ported in `LegacyXFileParser.cs`).

Materials are matched by name between the `.x` file, the Assimp import and the glTF file.

## Validation

The conversion was checked against the output of the old `StaticModelImporter` (CasaEngine `295db0c6`) on every converted model: material metadata (effect file, technique, colors, specular power, diffuse/normal/reflection texture file names) and geometry. See `docs/racinggame-casaengine-update-tasks.md`, task T2.1.

The tangents were checked against the vertex data of the built legacy models (`RacingGameOriginal/Desktop/bin/Debug/net10.0/Content/Models/*.xnb`, tangent channel of the vertex buffers): same vertex positions, UVs and normals, and the tangent and `cross(normal, tangent) * w` of the glTF files match the legacy ones within 2 degrees for at least 99.96 % of the vertices of every model.
