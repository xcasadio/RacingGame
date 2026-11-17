# ADR-0029: RacingGameCasaEngine builds the road underside and the city ground plane of the original

- **Status**: Proposed
- **Date**: 2026-11-17
- **Source**: this chantier: `ai-agent/tasks/rgce-pbr-handover-tasks.md`, slice S1h, decisions H1 to H4 (taken in AUTO mode, to be confirmed by the author with this record), tasks T12.1 to T12.3; the other machine's commits `0ac894d` and `d0db72f` (`origin/remaster`, `RacingGameCasaEngine/Worlds/LegacyTrackSceneFactory.RoadStructures.cs`); the original: `RacingGameOriginal/Core/Tracks/Track.cs:408-486`, `TrackVertex.cs`, `Landscapes/Landscape.cs:71-77` and `:900-933`, `Graphics/PlaneRenderer.cs`

## Context

- The original draws two pieces of geometry that this port did not have:
  - the underside of the road: 4 vertices per road point (the left edge, the left edge lowered by `RoadThickness x roadWidth`, the lowered right edge, the right edge), 6 triangles per segment as the tunnel, drawn with the material `("RoadBack", "RoadBackNormal")`; `Track.GenerateShadow` ignores it and `Track.UseShadow` does not render it;
  - the city ground: one plane, made in the constructor of the `Landscape` (`new Landscape(Level.Beginner)`, `RacingGameManager.cs:402`) at the first object of `landscapeObjects` whose model name contains `hotel` or `building`, kept for the three shipped tracks (`ApplyTerrain` only makes it again for a custom terrain), drawn by `PlaneRenderer` with the material `cityMat` (`CityGround`, `CityGroundNormal`). It is not a shadow receiver in the original; the other machine makes it receive shadows because the original's screen-space shadow mask darkens what is drawn there.
- The other machine builds both in `LegacyRoadStructureBuilder`, outside its generator components, from the raw right vector of the road points, and finds the anchor of the plane by reading the Beginner file at run time.
- Here the geometry of a track is built by generator components (`RaceRoadGeneratorComponent`, `RaceTerrainGeneratorComponent`) from cooked materials (ADR-0025, ADR-0026, ADR-0028); neither piece existed. `Material.Track.Ground` was cooked and used by no scene.
- The road mesh built here takes the normalised right vector of the road points; the original, and the other machine for the underside, take the raw one (open point O11 of the plan, the author's choice pending).
- ADR-0019 states in its consequences: "Against the original, nothing is missing". This was not true while these two pieces were missing. An ADR is not rewritten.
- The scene measure of the bench (`scene.json` of the original) counts placed objects only; neither piece is in it.

## Decision

- **H1: the underside follows the road mesh built here.** `LegacyTrackSceneFactory.CreateRoadBackModel` uses the same right vector as `CreateRoadModel`, so that the upper vertices of the underside are, bit for bit, the edge vertices of the road mesh and the two meshes touch. If the author settles O11 for the raw vector, both meshes change together. Upper vertices have the normal `up` and the tangent `right`, lower ones `-up` and `-right`, as the original; V is 0, 0.135, 0.865 and 1, U is the road's `TextureU`; the indices are the pattern of `Track.cs:464-486`. The model neither casts nor receives shadows.
- **H2: two more generated models, with cooked materials held as constants.** The underside is a third model of `RaceRoadGeneratorComponent`, with `Material.Track.RoadBack` (family `Road`, the road's culling and sampler, no emissive); the city plane is a second model of `RaceTerrainGeneratorComponent`, with `Material.Track.Ground`, whose recipe now takes `CityGround.png` and `CityGroundNormal.png` (family `Terrain` kept). Like the tunnels, both ids are constants and not serialized properties, so saved worlds do not change. The terrain keeps its own material and textures.
- **H3: the anchor of the city plane is a constant, in the original's coordinates.** `LegacyCityGroundAnchor` = (994.9773, 426.16977, 28.235294), the position of the first `Building` of the Beginner level (the first model of its `CombiBuildings` combo); the half size of the plane is `min(x, y)` = 426.16977. The plane is converted for each track with the port's point conversion minus that track's origin, so the three tracks share the original's plane. The value was measured twice, independently: from this port's own placement of Beginner, and by a temporary log line in the original's `CreateCityPlane` (one line for the three levels: object count 1155, index 0, the same position). A track generator does not read another track's file.
- **H4: the city plane receives shadows and casts none**, as the other machine does, to reproduce what the original's shadow mask shows.

## Consequences

- Amends ADR-0019: against the original, the road underside and the city ground plane are now present; its statement that nothing is missing holds from this record on. The tunnels, their geometry and their material are unchanged.
- Completes what the plan took from the other machine's commit `0ac894d` (the road underside and the city ground plane; its tunnels were done earlier); `Material.Track.Ground` is now used.
- Content added by cooking: `Material.Track.RoadBack`, `Texture.RoadBack`, `Texture.RoadBackNormal`, `Texture.CityGround`, `Texture.CityGroundNormal`, and the new textures of `Material.Track.Ground`; `Material.Track.Terrain` is unchanged.
- The scene measure and the light and shadow probes of the bench do not change in their verdicts; the pictures change where the underside or the plane is visible (the bench figures are in `docs/racinggame-3d-capture-bench-local-baseline.md`, section "After the road underside and the city ground").
- The plane is lit by the light of the race world and is clearly brighter and bluer than the original's grey cobbles, as the rest of the race world is (open point O13 of the plan, not settled here).
- If O11 is settled for the raw right vector, the road, its underside and the rail change together.
- The plan's decisions H1 to H4 were taken in AUTO mode and stay `Proposed` until the author confirms them.
