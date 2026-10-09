# ADR-0017: RacingGame.Gameplay holds the game code, and generator components rebuild the track

- **Status**: Accepted; superseded in part by ADR-0019 (reading of the legacy helper and CombiModel items)
- **Date**: 2026-10-08
- **Source**: this chantier: `ai-agent/tasks/rgce-editor-project-tasks.md`, decisions D1, D2 and D5 (author answers, 2026-10-08), proposals P5, P6 and P9 (validated with the plan), tasks T4.1 to T4.6

## Context

- ADR-0015 made `RacingGame.Gameplay` the editor's gameplay assembly, but the game code still lived in the `RacingGameCasaEngine` exe, which the editor never loads:
  - 12 types derived from `EntityComponent`, `SceneComponent` or `Entity`, with `RaceGameMode` and `RacingPlayerController`;
  - 5 of them reached the game through `World.Game as RacingGameCasaEngineGame`;
  - `RacingCarPawn`, an `Entity` subclass, built its children in its constructor.
- The race track (terrain, road, guard rails with posts, columns with segments) was built as meshes by a code factory, `LegacyTrackSceneFactory`, which mixed placement computation and mesh building.
- The CasaEngine editor saves the serialized properties of game components (ADR-0071 of CasaEngine) and skips every component whose type has `[Browsable(false)]`.
- The author decided that the game code moves to the gameplay DLL (D1), that worlds become editable assets (D2), and that road, rails, columns and terrain are generator components rebuilt from data assets and tuned in the inspector (D5).
- Two runs of the track audit gave different images. The debug free camera rewrote the poses from the mouse, frames could be stale, and the chase rig left a varying roll. So pixel parity could not be measured.

## Decision

- **Game code in the DLL.**
  - What moves into `RacingGame.Gameplay`, under `RacingGame.Gameplay.<Folder>` namespaces:
    - components, entities and the game framework;
    - car profiles (`Cars`);
    - the legacy track factory and the physics profile (`Track`);
    - the race session, the catalog and the options state (`Race`);
    - the legacy model import (`Import`).
  - The exe keeps the bootstrap, the screens, the UI, persistence, the validators and the world assembly (`RaceWorldFactory`).
- **Access to the game.** Components reach the game through `IRaceGameHost`, an internal interface of the DLL with `RaceSession`, `RaceVibration`, `IsActive`, `Window` and `InputComponent`.
  - `RacingGameCasaEngineGame` implements `RaceSession` and `RaceVibration` explicitly, and the other three through inherited members. The DLL grants `InternalsVisibleTo("RacingGameCasaEngine")`.
  - In the editor, there is no such host, and the components do nothing that needs one.
  - Entity names the components read live in `RaceWorldNames`.
- **The car is an entity.** `RacingCarPawn` is removed:
  - `RaceCarEntityFactory` builds the same anchors and components on a plain `Entity`;
  - `RaceCarStateComponent` carries the race state;
  - the former pawn helpers are extension methods on `Entity`.
- **Placements apart from meshes.** `RaceTrackLayout.Compute` computes, from a track and the height map, with no GPU object:
  - the road spline, the rail points and posts, the columns and segments;
  - the scenery, the checkpoints, the start pose and the physics profile.
  `LegacyTrackSceneFactory` only builds meshes, materials and entities from it.
- **Generator components.**
  - `RaceTerrainGeneratorComponent`, `RaceRoadGeneratorComponent`, `RaceGuardRailGeneratorComponent` and `RaceColumnGeneratorComponent` are `SceneComponent`s used as the root of their entity.
  - They serialize `track_asset_id` and `heightmap_asset_id` (catalogue ids) and their settings: `side`, `holder_gap`, `columns_distance`.
  - They build their geometry at `InitializeWithWorld`, and rebuild it when a settings signature changes. They put it in `RaceGeneratedModelComponent` children, marked `[Browsable(false)]`, so that a saved world holds only the settings.
  - They free only the GPU buffers of the meshes they create, never the shared cached models and textures.
  - `RaceTrackPhysicsComponent` computes its profile from the same two ids.
- **Legacy files.** The `.Track` and `.CombiModel` files are read with `XDocument`. `XmlSerializer` cannot build serializers for types of the gameplay DLL when the editor loads it in a collectible context. The reader returns what `XmlSerializer` returned.
- **Parity tooling.** The track audit is repeatable:
  - the free camera leaves automation poses alone;
  - steps wait for drawn frames;
  - poses reset the camera orientation;
  - the chase view waits until the rig converges.
  The scene export gains one record per drawn mesh: matrix, material values, texture pixel hashes, vertex and index hashes. `scripts/compare_race_scene_exports.py` compares two exports independently of entity names, and `scripts/compare_track_audit.py` compares audits. `scripts/check_gameplay_type_names.py` guards simple-name collisions with the engine and the editor.

## Consequences

- Complements ADR-0015, which stands: the gameplay assembly now holds the game code too.
- The editor resolves the race components, so worlds can reference them. `Worlds/Test/TrackBeginnerGenerators.world` opens, saves to the same bytes and keeps its settings.
- The posts and column segments are no longer entities but generated children of their rails and columns. Entity-level comparisons must use the per-mesh export. Culling and shadow casting are decided per generator entity.
- A type added to the DLL must keep a simple name unique across the engine, MGUI and the editor, because the engine resolves types by simple name. `check_gameplay_type_names.py` checks it.
- The editor's automation cannot frame its 3D camera, so automated captures of the test world scale a copy of it. This is a CasaEngine limit.
- A legacy parsing defect is kept for parity and recorded as open point O7 of the plan: the helper and combi elements of the legacy files were never read, so palms, lanterns, tunnels and combi scenery are missing.
