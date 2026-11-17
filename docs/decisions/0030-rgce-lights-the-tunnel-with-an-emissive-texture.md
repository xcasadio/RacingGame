# ADR-0030: RacingGameCasaEngine lights the tunnel with the original's material ambient as an emissive texture

- **Status**: Proposed
- **Date**: 2026-11-17
- **Source**: this chantier: `ai-agent/tasks/rgce-pbr-handover-tasks.md`, slice S3 (phase 15), decisions T1 to T3 (taken in AUTO mode, to be confirmed by the author with this record), open point O1, tasks T15.1 and T15.2; the original: `RacingGameOriginal/Core/Tracks/Track.cs:84-89`, `Track.cs:1309-1343`, `Content/Shaders/ShadowMap.fx:36` and `:240`; the engine: `CasaEngine/CasaEngine/Content/Shaders/LitPbr.fx:521-557`

## Context

- The original's tunnel material is `roadTunnelMaterial`: ambient 182, diffuse 80, specular 64, with the comment "Use mainly ambient color (tunnel uses lightmaps)". The original lights a pixel as `texture x (A + D x N.L)`; the road and the tunnels cast and receive shadows (`Track.GenerateShadow`, `Track.UseShadow`), and its shadow mask multiplies the image by (0.25, 0.26, 0.27).
- Here the tunnel material is a cooked `lit-pbr` material with a base colour of 80 and no emissive. `lit-pbr` has no ambient colour per material, and the ambient of the race world is the sky cube's irradiance times 0.05. The inside of the tunnel is almost black: 57.1 % of the pixels of the Beginner tunnel audit capture have a luminance under 10 (verification of phase 13; 1.4 % on Advanced and 0.4 % on Expert, 19.7 % on average), as it was before PBR (ADR-0019, Consequences, open point O1 of the plan).
- The engine can carry the term `texture x A`: in `LitPbr.fx` the emissive is `EmissiveColor x EmissiveTexture`, both display colours, decoded together and added to the pixel, and the shadow tint does not touch it.
- The recipes had an emissive colour only, and the cooker wrote no emissive texture.
- None of the 15 reference views of the original is taken in a tunnel; the usual views never enter one.

## Decision

- **T1: the original exports a reference view inside the tunnel.** `--capture-track-audit` of `RacingGameOriginal` gains the opt-in `--tunnel-views`: only one view per track is exported, taken in the longest `Tunnel` range of the road helpers (camera at the road point a quarter of the way into the range, 2 m above it; target the first road point at least 15 m further along, 1.5 m above it; the car is placed at the camera point and not drawn). Without the option the capture is unchanged: the 15 views, their images, `views.json` and `scene.json` are identical to the earlier export, and `artifacts/pbr-handover/s1c/ref` is untouched. The views of the tunnels go to their own folder, `artifacts/pbr-handover/s3/ref-tunnel`; the port replays it with its usual `--capture-reference-views`.
- **T2: the tunnel gets an emissive texture.** `RaceMaterialRecipe` gains `EmissiveTextureFilePath`; `ForRoadTunnel` sets it to the tunnel's diffuse texture and gives `EmissiveColor` the factor below, so that the emissive is the original's `texture x A`. The cooker writes the key `emissive_texture` only for a recipe that names an emissive texture, and its verifier reads an absent key as the null id, so the 98 other materials are cooked as before. No light is added in the tunnel.
- **T3: the factor was chosen by measurement, between two candidates.** (a) 182/255, the original's ambient; (b) 182/255 times the shadow colour (0.25, 0.26, 0.27), because the engine's emissive escapes the shadow tint while the original's ambient takes it. Each candidate was cooked in a copy of the game's build folder, outside the repository, and replayed from that copy; the one whose mean per-channel difference with the original over the three tunnel views (`nopost`, `linear`) was smaller was kept. No other value was tried. The result and the figures are in the section "After the tunnel emissive" of `docs/racinggame-3d-capture-bench-local-baseline.md`; the kept candidate is the one in `RaceMaterialRecipes.LegacyTunnelEmissiveFactor`.

## Consequences

- Amends ADR-0019: its consequence about the tunnel being much darker than the original's, left open as O1, is answered by this record; the tunnel's geometry and the decisions about the helpers are unchanged.
- The cooked `Material.Track.RoadTunnel` has `emissive_color` and `emissive_texture`, and nothing else of the content changes.
- Editing the emissive of `RoadTunnel.material` in the editor changes the game, and running the cooker again reverts it; the factor lives in `RaceMaterialRecipes`.
- The emissive is not attenuated by shadows, so the tunnel does not darken further where a shadow falls on it; the shadow tint of the original's ambient is carried by the factor, in one value.
- The outsides of the tunnels, seen from the road, take the same material and change a little; the bench lists the views of the 15 that change.
- Not settled here: the lens flare of the original, which brightens some tunnel views and which the port does not draw; the colour of the lamps of the tunnel; the decisions T1 to T3, taken in AUTO mode, stay `Proposed` until the author confirms them.
