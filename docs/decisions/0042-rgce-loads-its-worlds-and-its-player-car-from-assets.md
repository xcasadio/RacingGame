# ADR-0042: RacingGameCasaEngine loads its worlds and its player car from assets

- **Status**: Proposed
- **Date**: 2026-11-19
- **Source**: this chantier: `ai-agent/tasks/rgce-worlds-as-assets-tasks.md` (slice S6 of `ai-agent/tasks/rgce-editor-project-tasks.md`, re-based on the code of 2026-11-18), decisions D10 to D12 (the author's answers of 2026-11-18) and propositions P11 to P20, approved by the author on 2026-11-18; slices M1 to M7; the measurements: `artifacts/worlds-as-assets/` (`m1/` to `m7/`); ADR-0002, ADR-0023, ADR-0025, ADR-0026, ADR-0041; engine ADR-0128

## Context

Facts verified on 2026-11-18:

- The game built its worlds in code at every load: the front end, one race world per track (camera, race root, one light, the track generators, 649, 904 and 1745 scenery entities, three checkpoints, the start, the player car) and the small world of the car selection carousel. Decision D2 of the editor project plan wants the menu and the races to be `.world` and `.entity` assets the game loads and the editor can edit.
- A `.world` file persists everything the game sets at world level (shadows, ambient, image-based light intensities, the cubemap asset id, the colour pipeline), but not a cube set at run time nor the render pipeline of a view.
- The engine had no public way to write a world from entities that are not initialized; engine ADR-0128 added one.
- Two components lost state when saved: the extents of `RaceCheckpointTriggerComponent` and `RacingCarAnchorComponent.AnchorId`.

## Decision

- **The worlds and the player car are assets.** `Worlds/FrontEnd.world`, `Worlds/Race/TrackBeginner.world`, `TrackAdvanced.world`, `TrackExpert.world`, `Worlds/CarSelection.world` and `Entities/PlayerCar.entity`, catalogued as `World.FrontEnd`, `World.Race.Track<Track>`, `World.CarSelection` and `Entity.PlayerCar`.
- **The builders live in the gameplay assembly and a tool writes the files.** `RacingGame.Gameplay/Race/RaceWorldTemplates.cs` builds the entities and the environment settings; `scripts/RgceAssetCooker --worlds` writes the files from them through the engine's `EditorWorldWriter.SaveWorld(World, IReadOnlyList<Entity>)`, with deterministic ids (uuid5 of the catalogue entry, the entity name and the component path), so a second run writes nothing; `--worlds --verify` re-reads the files and compares them with the builders. The game no longer calls the builders.
- **The game loads them through the catalogue.** The first world is `ProjectSettings.FirstWorldLoaded`; a race is started with `GameManager.SetWorldToLoad(<file name of the catalogue entry>)`; a loaded world takes the name of its entry, so the front end is `World.FrontEnd` and a race world starts with `World.Race.`. The carousel loads `World.CarSelection` with `LoadCopy<World>`.
- **What a saved race world carries, and what the game sets after the load.** The file carries the shadows on, a light that casts them, the ambient and the linear colour pipeline. When a world is loaded the game applies the player's shadows option to the world and to its light, and resets the colour pipeline to its defaults in the bench's `--color-pipeline legacy` mode.
- **The sky comes from the world's cubemap asset id** (D10). The race worlds and the carousel world carry `specular_cubemap_asset_id` = `Texture.SkyCubeMap`; the game reads the id, loads the cube through the catalogue, caches cubes by asset id, sets the cube at run time and clears the id on the loaded copy. A race world without an id gets the procedural sky. The drawing of the sky background (`LegacySkyCubeViewPipeline`) stays in code: the engine does not persist the pipeline of a view. The front end keeps no cube, no pipeline and a black background.
- **The player car is spawned from its asset.** The race worlds hold no car; when a race world is loaded the binder spawns `Entity.PlayerCar`, names it `PlayerCar` and sets its state before it enters the world.
- **What stays in code**: the three cars of the carousel (painted at run time), its camera and its per-frame placement; the car's state; the sky view pipeline; the meshes of the track generators.
- **Saved fields**: the checkpoint extents and the anchor ids carry `[SerializedProperty]`.

## Consequences

- Measured on every slice from M3 to M7, each replayed twice: the 76 pictures of the capture bench (15 reference views and 3 tunnel views in `linear` and in `legacy`, 10 interface screens, 30 track audit captures) are byte-identical to the reference taken before the change; the scene export of the three tracks has no unmatched record, the player car included; the two car audit reports are identical outside the line that carries the time of the run.
- The game reads the files: an entity removed from a world file disappears from the export of that track only; a wrong file name for `World.FrontEnd` stops the start; a wrong file name for `Entity.PlayerCar` stops a race; an emptied cube id changes the views of that track only.
- The editor opens the five worlds and the car. It saves the five worlds back without loss: the only bytes it changes are the `view_distance` and the `viewport` of a camera component, which the tool leaves at zero and the editor initializes when it opens the world (999 for the distance; for the viewport, the size of its own window and the depths 1 and 1000); the engine reads them when it loads the world, and the camera's initialization then overwrites the size, the depths and the distance at every load; only the viewport's `x` and `y` are kept, and they are 0 in the tool's files and in the editor's. The editor's automation does not rewrite an entity asset it has not modified; the car, loaded and saved again through the engine's `EditorEntityWriter.SaveEntity`, gives the bytes of the tool.
- **The builders are the source of the files.** After a change to `RaceWorldTemplates`, to the track data or to the scenery placement, `RgceAssetCooker --worlds` must be run again. A world edited and saved in the editor diverges from the builders: `--worlds --verify` reports it and a later `--worlds` overwrites it. Which of the two is the source in the long run is not decided here.
- The game now switches worlds through `GameManager.SetWorldToLoad(string)`, which clears the outgoing world before the load; the worlds built in code were set through the overload that takes a world, which does not clear it.
- The scenery of a race world is frozen in its file, with the seed of ADR-0041: it is no longer drawn at run time.
- Limits: the manual check of saving `PlayerCar.entity` from the editor's own interface is left to the author; if the catalogue has no `Entity.PlayerCar` entry at all, the engine's `World.SpawnEntity` fails with an exception that names nothing.
