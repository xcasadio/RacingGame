# ADR-0017: The legacy sky cube is converted to the axes of the converted world

- **Status**: Accepted
- **Date**: 2026-11-14
- **Source**: this chantier: `ai-agent/tasks/rgce-3d-materials-base-tasks.md` (D2, G4, A7), following the author's direction of 2026-11-14 ("Socle commun puis PBR") and ADR-0016

## Context

Facts verified on 2026-11-14:

- The original samples its sky cube with `In.pos.xzy` of the box vertex, in its Z-up world (`RacingGameOriginal/Core/Content/Shaders/PreScreenSkyCubeMapping.fx:53`): the cube direction is `(x, z, y)` of the world direction.
- The port converts a legacy point `(x, y, z)` to `(x, z, -y)` and samples the cube with the world direction as it is (`CasaEngine/CasaEngine/Content/Shaders/LegacySkyCube.fx`, `LitForward.fx` for reflections). A converted world direction `P` is the legacy direction `(P.x, -P.z, P.y)`, whose cube direction is `(P.x, P.y, -P.z)`: the port displayed the mirror along Z of the original's sky. Measured (diagnostic capture, uncommitted, `LegacySkyCube.fx` sampling `(x, y, -z)`): the mean difference to the original drops by 6.6 / 4.6 / 3.9 (red / green / blue) on `Beginner-overview`, 8.1 / 6.2 / 5.6 on `Advanced-overview`, 8.9 / 7.2 / 6.7 on `Expert-overview`, and the contours of the far mountains and of the clouds overlay the original's on every view with sky (`Beginner-sector-20`, `Advanced-start`, `Beginner-sector-50`, `Expert-overview`).
- The same cube object is the sky and the reflection cube of the race world and of the car selection carousel. `SkyCubeMap.dds` is a DXT1 cube of 512 x 512 per face, six faces, no mipmaps.
- The original's reflections all read this same cube, but with two conventions. With the `(x, z, y)` swap, like the sky: `ReflectionSpecular` and `ReflectionSpecular20` (glass, `NormalMapping.fx:452, 555, 634`). Without the swap, hence inconsistent with the sky it displays: `SpecularWithReflection20` (signs, lamp posts, `NormalMapping.fx:495-496, 533-534`) and `SpecularWithReflectionForCar20` (car paint, `:695-696`).
- The engine reads a cube with the world direction (`LitForward.fx`), like any engine material, including the planned PBR material.

## Decision

- The asset is converted, not the engine's shader, as for the normal maps (ADR-0016): assets are converted to the engine's conventions, so that the files the port loads follow the engine's own (a world direction reads the texel seen in that direction) for the sky, for the engine's reflections and for any later material.
- `scripts/LegacyModelGltfConverter --sky-cube <dds>` writes `RacingGameCasaEngine/Content/Textures/SkyCubeMap.dds` with `new(P) = old(P.x, P.y, -P.z)`. The mirror is a texel permutation, so the DXT1 blocks are reordered and the rows or columns of their indices reversed, without recompression: lossless. The tool checks every texel centre against the source and checks that a second conversion gives the source back byte for byte.
- `RacingGameCasaEngine.csproj` no longer links the legacy `SkyCubeMap.dds` into the output; the converted file is copied instead.
- Only glass reflections follow the sky afterwards (the vehicle's windows and the windows of the buildings, which the original reads with the swap). The original's reflections without the swap (signs, car paint) are not reproduced: the port has no such reflection today, and whether to reproduce this accidental behaviour belongs to the PBR work.

## Consequences

- The sky is oriented as in the original on every view; the reflection of the glass of the car, of the casino and of the hotels, and of the carousel's base (reflection 0.35), is expected to read the same texels as the original's glass (not compared picture by picture: the bench shows the mean difference of the views with glass falling slightly, by 0.1 to 0.3 on `Beginner-sector-20`, `Advanced-sector-50` and `Advanced-sector-80`, relative to the diagnostic capture).
- A change to the original's `SkyCubeMap.dds` requires running the converter again; the converted file is committed with the port's other converted textures.
- The conversion accepts only DXT1 cubes with six faces and no mipmaps, which is what the game has and what the engine loader reads.
