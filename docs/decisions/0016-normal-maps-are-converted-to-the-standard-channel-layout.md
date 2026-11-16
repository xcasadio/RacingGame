# ADR-0016: The legacy normal maps are converted to the standard channel layout

- **Status**: Accepted
- **Date**: 2026-11-14
- **Source**: the author's decision of 2026-11-14 ("Convertir les textures"), after the 3D capture bench (`docs/racinggame-3d-capture-bench-findings.md`) and the review of `ai-agent/tasks/rgce-3d-lighting-shadows-tasks.md` (R1)

## Context

Facts verified on 2026-11-14:

- Every `*Normal*` texture of the original game has a saturated red channel (mean 255.0) and keeps a component around 127 in alpha. Measured on all the `.tga` files of `RacingGame/Content/Textures` and on the 49 `.png` files `scripts/LegacyModelGltfConverter` had written "losslessly (RGBA)".
- The original shaders read a normal map as `.agb` (`RacingGameOriginal/Core/Content/Shaders/NormalMapping.fx:203, 769`, `LandscapeNormalMapping.fx:192, 502`) and build the tangent space as rows `(cross(normal, tangent), tangent, normal)` (`NormalMapping.fx:163-172`). So alpha goes along `cross(normal, tangent)`, green along the vertex tangent, blue along the normal.
- CasaEngine reads `.rgb * 2 - 1` and builds `TBN = (T, cross(N, T) * w, N)` (`CasaEngine/CasaEngine/Content/Shaders/LitForward.fx:583-584, 596-602`), which is the glTF convention: red along the tangent, green along the bitangent.
- With the unconverted textures, the engine decoded a flat texel as about `normalize(1, 0, 1)`: every normal-mapped surface was lit as if tilted by about 45 degrees. The city ground was almost black for that reason.
- CasaEngine enables normal mapping only on meshes that have tangents (`RenderFeatureResolver.cs:146-152`). The meshes the port generates have them (terrain, guard rails, columns, road underside, tunnels, city ground). The imported glTF models have none: `GltfStaticModelReader` does not read a `TANGENT` attribute and `AssimpToGltfConverter` does not write one, so their normal maps are not used today.

## Decision

The assets are converted; the engine is not changed. `LegacyModelGltfConverter` writes every normal map as `(red, green, blue, alpha) = (legacy green, legacy alpha, legacy blue, 255)`. With the same vertex tangent and `w = 1`, CasaEngine then computes exactly the original's perturbed normal.

Normal maps used only by generated meshes are converted by the same tool (`--normal-map <tga file>`), and the port loads the converted `.png` files instead of the legacy `.tga` files.

## Alternatives rejected

- A decoding option in the engine ("X in alpha"): keeps a non-standard layout in assets that glTF declares as standard normal textures, and adds a shader variant to maintain.
- Writing `(alpha, green, blue)`: right for a tangent space built as `(tangent, cross(normal, tangent), normal)`, wrong for the original's, which swaps the two axes.

## Consequences

- The 49 converted normal maps change, and 6 are added (`LandscapeNormal`, `CityGroundNormal`, `RoadCementNormal`, `RoadBackNormal`, `RoadTunnelNormal`, `RoadNormal`).
- The mapping is derived from the two shaders. The bench pictures do not prove it yet: the port's light direction and ambient term still differ from the original's (next slice), so a picture comparison cannot separate a right decoding from a wrong one.
- Imported models still ignore their normal maps. Giving them tangents is an engine change (glTF reader and converter), to plan in the engine's repository. Those tangents must follow the original's processor (`RacingGameModelProcessor.GenerateTangents`: tangent along increasing U) with `w = 1`, so that this conversion stays right for them.

## Follow-up: tangents of the imported models (2026-11-14)

Done the same day, at the author's request.

- CasaEngine reads the optional glTF `TANGENT` attribute (CasaEngine `9c57de77`, branch `chantier/gltf-static-tangents`).
- `LegacyModelGltfConverter` writes a `TANGENT` on every primitive whose material has a normal texture: direction of increasing U accumulated per triangle without weighting, made orthogonal to the vertex normal, `w = +1`. A vertex whose triangles all have degenerate UVs gets `cross(normal, +X)`.
- Measured against the tangent channel of the original's built models (`.xnb`): the glTF model space equals the original's (no mirror), and on every model at least 99.96 % of the matched vertices have their tangent and their `cross(normal, tangent)` within 2 degrees of the original's. An independent check on all 56 models found 100 % and no flipped bitangent.
- No node, combo or track placement matrix has a negative determinant. A mirrored placement would need `w = -1`.
