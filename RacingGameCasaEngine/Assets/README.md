# RacingGameCasaEngine asset conventions

Runtime assets are resolved from `RacingGameCasaEngine/Content`.

## Rules

- `Content/AssetInfos.json` is the runtime index consumed by `AssetCatalog.Load(...)`.
- Asset names must stay stable and unique because gameplay code resolves them by logical name first.
- `file_name` values stay relative to `Content` and always use `/` or simple folder segments.
- Game-specific JSON data lives with the game project, not inside `CasaEngine`.
- Every MGUI element of `Content/UI/Screens/*/*.xaml` has a `Name`, unique in its screen, with the prefix of its type (`wnd` Window, `pnl` OverlayPanel/StackPanel/Border, `img` Image, `lbl` TextBlock, `btn` Button). `python scripts/check_ui_element_names.py` checks it.

## Suggested folders

- `Content/Textures/` for UI textures, decals, and simple 2D placeholders.
- `Content/Tracks/` for track data, checkpoints, and race metadata.
- `Content/Cars/` for car definitions, tuning, and visual assets.
- `Content/Worlds/` for the CasaEngine worlds: the ones the game loads (`FrontEnd.world`, `Race/Track<Track>.world`, `CarSelection.world`), the editor's start world and the test world.
- `Content/Entities/` for entity assets (`PlayerCar.entity`).
- `Content/Audio/` for music and sound effects.
- `Content/Shaders/` for shader assets registered in the catalog.
- `Content/UI/` for MGUI-specific assets or data files.

## Near-term migration policy

- Keep bootstrap assets minimal until the race world composition is stable.
- Prefer adding game-level loaders or data files in `RacingGameCasaEngine` instead of extending `CasaEngine` for racing-specific needs.

## Sound effects

Sound effects play through CasaEngine's audio service ([ADR-0003](../../docs/decisions/0003-rgce-sound-effects-through-casaengine-audio.md)).

- Add a sound in `Content/Audio/`: a copy of the RacingGame `.wav`, and a `.sound` asset on the `Sfx` bus that references it.
- Catalogue both in `AssetInfos.json`: `Sound.<Name>` for the `.sound` (`asset_type` `sound`), `Sound.<Name>Wave` for the `.wav` (`asset_type` `wav`).
- Play it with `AudioService.PlaySound`. Its volume follows the Sound Volume option, which sets `SoundEffect.MasterVolume`, applied by MonoGame to every sound instance CasaEngine plays.
- Current sounds: `Sound.Beep` and `Sound.Bleep`, played by the race start light (`GameFramework/RaceStartLight.cs`).
- The 22 other legacy waves are ported but not played yet ([ADR-0024](../../docs/decisions/0024-rgce-content-holds-every-game-file.md)): 19 effects and engine loops, and 3 musics on the `Music` bus, streamed. `scripts/generate_rgce_content_assets.py` copies them under their XACT wave name and writes their `.sound` with the XACT sound volume (`10^(dB/2000)`) and looping. The XACT category volumes (`Music` -3 dB, `Gears` -4 dB) are not carried: set them when the game starts playing these sounds.

## Content and catalogue

Every game file the game reads lives under `Content` and has a catalogue entry ([ADR-0024](../../docs/decisions/0024-rgce-content-holds-every-game-file.md)); the engine's own content (shaders, fonts, icons) comes from the CasaEngine project reference. The csproj copies `Content\**` to the output, minus the editor-only files, and links nothing from `RacingGame/Content`.

- Three tools write the catalogue. Ids are uuid5 of the asset name, so a second run changes nothing:
  - `scripts/generate_rgce_ui_assets.py`: the screens, sprites and UI images;
  - `scripts/generate_rgce_content_assets.py`: `Image.<name>` for the PNG images (`Texture.<name>` for the two DDS cube maps), `Data.<name>` for the height map and CombiModel files, `ModelSource.<name>` for the glTF sources, `Sound.<name>`/`Sound.<name>Wave` for the 22 ported sounds, `World.<path>` for the worlds and `Entity.<path>` for the entities;
  - `scripts/RgceAssetCooker`: the cooked models, materials and textures (see "Cooked models, materials and textures"). Run the content generator first, then the cooker. With `--worlds` it writes the worlds and the player car (see "Worlds and the player car"): there the order is the cooker, then the content generator, then `--worlds --verify`.
- `python scripts/generate_rgce_content_assets.py --check` lists the content files that no entry names. The project files are not catalogued: the project file, the catalogue, the gameplay dll and pdb, `.casaeditor/`, the `.design.json` and `.xaml` of the screens, and the glTF `.bin` buffers.
- Textures are PNG, apart from the two cube-map DDS. The 129 legacy `.tga` textures were converted to PNG files of the same name by `scripts/LegacyModelGltfConverter`: 106 by the model conversion (the 100 textures the models name, and the six normal maps of the meshes the game generates, given with `--normal-map`) and 23 by its `--textures` mode. 74 are colour copies with the same RGBA pixels; the 55 `*Normal*` files are normal maps converted to the standard channel layout (red from the legacy green, green from the legacy alpha, blue unchanged, opaque alpha; [ADR-0016](../../docs/decisions/0016-normal-maps-are-converted-to-the-standard-channel-layout.md)). `--verify-textures` checks every pair, first as a colour copy, then as a converted normal map, lists both sets with their counts, and fails only for a pair that is neither. `SkyCubeMap.dds` is not a copy of its source: the converter rewrites it in the axes of the converted world ([ADR-0017](../../docs/decisions/0017-sky-cube-is-converted-to-the-engine-axes.md)); the other cube map is. The game compares texture names without extension, and resolves a `.tga` named by a model's legacy metadata to the `.png` of the same name.
- `.gitattributes` marks the binaries (`png`, `wav`, `dds`, `data`, `tga`, `bin`); there is no LFS.

## CasaEngine editor project

`Content/` is also a CasaEngine editor project ([ADR-0002](../../docs/decisions/0002-rgce-content-is-a-casaengine-editor-project.md), [ADR-0023](../../docs/decisions/0023-racinggame-gameplay-is-the-editor-gameplay-assembly.md)). The game itself does not read the project file.

- Files:
  - project file `Content/RacingGameCasaEngine.json`;
  - start world `Content/Worlds/Editor.world`, with one empty entity, `EditorAutomationAnchor`, required by the editor automation;
  - gameplay assembly `RacingGame.Gameplay` (view models, `Plugin`, and since ADR-0025 the game code: components, the car, the race session, the track layout and generators). Its build copies `RacingGame.Gameplay.dll` and `.pdb` into `Content/`; both files are git-ignored and named by `GameplayDllName`;
  - `GameplayCsprojName` names `RacingGame.Gameplay.csproj`: before Play, the editor rebuilds and reloads the dll when its sources are newer. On project open it writes `RacingGame.Gameplay/CasaEngine.EnginePath.props`, machine-local and git-ignored.
- Before opening:
  - build the editor from the submodule, `dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln -c Debug`, again after every submodule update;
  - build `RacingGame.slnx`, so that the gameplay dll exists.
- Open: `CasaEngine/CasaEngine.Editor/bin/Debug/net9.0-windows/CasaEngine.Editor.exe --project <absolute path>/RacingGameCasaEngine/Content/RacingGameCasaEngine.json`, or Browse in the editor launcher.
- Capture one screen without interaction: `scripts/capture_editor_screen.ps1 -Screen UI/Screens/<Name>/<Name>.uiscreen`.
  - Options: `-ProjectRoot` to work on a copy, `-SetScreenProperty <Node>:<Property>=<Value>`, `-SaveProject`, `-TimeoutSeconds` (120 by default).
  - Exit codes: 0 success, 1 failure, 2 timeout (the editor process it started is stopped).
  - It prints the paths of the capture (`<Name>-final.png`) and of the diagnostics.
- Screen authoring rules for the preview:
  - a full-screen window keeps `ScreenHorizontalAlignment="Stretch" ScreenVerticalAlignment="Stretch"` for the game and also declares the design size `Width="1280" Height="720"`. The preview takes a `Window` root as is and would otherwise size the window to its content. In the game, the Stretch placement replaces that size on every update (`MGWindow.ApplyScreenPlacement`);
  - design-time data is `<Name>.design.json` next to the screen, `{"view_model_type": "<simple type name>", "values": {...}}`, referenced by `design_time_data_file` in the `.uiscreen`. It is not copied to the build output.
- Preview limits, in CasaEngine and not worked around here:
  - Compatibility parsing: a screen that previews can still fail the Strict load of the game;
  - elements renamed `_cse_<id>`;
  - `theme_name` and `preview_resolution` ignored;
  - editor font (JetBrainsMono);
  - screen code never runs: visual-state restyling, focus and code-applied values are not shown.
- File > Save rewrites:
  - the edited screen's XAML: only the edited element's start tag is rewritten, on one line;
  - the start world, the project file and `AssetInfos.json`. The committed versions are in the form the editor writes, so a Save leaves them unchanged.
- Never use File > New Project on this folder: it overwrites `AssetInfos.json`.
- Editor state (`Content/.casaeditor/`) is git-ignored. Every project open leaves a shadow copy of the gameplay assembly under `%TEMP%\casaeditor-scripts\`, and every rebuild before Play a folder under `Content/.casaeditor/script-build/`; the editor never deletes either.

## Cooked models, materials and textures

The game and the editor load every model, material and texture of the race by asset id ([ADR-0026](../../docs/decisions/0026-rgce-models-and-materials-are-cooked-assets.md)). `scripts/RgceAssetCooker` cooks them from the glTF sources:

- `Models/<Name>.staticModel` (`StaticModel.<Name>`): the geometry of `Models/<Name>.gltf` as the legacy reader reads it. The scenery, posts, column segments and start lights carry the legacy root rotated by pi/2 about X; the car and the selection plate do not.
- `Materials/Models/<Name>/<Slot>.material` (`Material.<Name>.<Slot>`) and `Materials/Track/<Role>.material` (`Material.Track.<Role>`): `lit-diffuse` materials with the values the game used to compute from texture names (specular tuning, alpha cutout, back-face culling, reflection). Render states the game left unset are written as the renderer's defaults.
- `Textures/<Name>.texture` (`Texture.<Name>`): one per image a cooked material uses, pointing to `Image.<Name>`.
- After a change to a glTF, a recipe or a texture, run the cooker again; `--check` lists what is missing and `--verify` compares every cooked file with the glTF and the recipes. A second run changes nothing.

Run-time state that is not cooked:

- **Car paint.** Each car index takes a private copy of `StaticModel.Car`. The slots that sample the car texture (base texture `Texture.RacerCar` in the cooked material) get, in that copy only, a copy of their material with a new id and the painted or unpainted texture of `CarPaint`, which re-colours the `RacerCar*` image for the chosen colour. The painted-or-not rule is the one of the legacy game, applied to the slot name; the cooker's `--verify` checks it against the recipes.
- **Start light.** `RaceStartLight` switches its component between `StaticModel.StartLight`, `StartLight2` and `StartLight3` during the countdown.
- **Track generators.** The road, tunnel, terrain, rail and column meshes are still built in code; their material comes from `material_asset_id`, a cooked `Material.Track.<Role>` by default. The tunnels always use `Material.Track.RoadTunnel`, which the road generator holds as a constant, not as a serialized property.

## Worlds and the player car

The game loads its worlds and its player car from assets ([ADR-0042](../../docs/decisions/0042-rgce-loads-its-worlds-and-its-player-car-from-assets.md)); it builds none of them in code.

- Files and catalogue entries: `Worlds/FrontEnd.world` (`World.FrontEnd`), `Worlds/Race/TrackBeginner.world`, `TrackAdvanced.world`, `TrackExpert.world` (`World.Race.Track<Track>`), `Worlds/CarSelection.world` (`World.CarSelection`), `Entities/PlayerCar.entity` (`Entity.PlayerCar`).
- A race world holds the camera, `RaceRoot`, the light `Light.Key`, the five track generators, the scenery (649, 904 and 1745 entities), three checkpoints and `PlayerStart`, plus its environment: shadows, ambient, the linear colour pipeline and `specular_cubemap_asset_id` = `Texture.SkyCubeMap`. It holds no car. The scenery is the one of the seed of [ADR-0041](../../docs/decisions/0041-rgce-draws-the-scenery-of-the-game-with-the-seed-of-the-reference-capture.md), frozen in the file.
- **The builders are the source.** `RacingGame.Gameplay/Race/RaceWorldTemplates.cs` and `RacingGame.Gameplay/Entities/RaceCarEntityFactory.cs` build the entities; the tool writes the files from them:
  1. `scripts/RgceAssetCooker/bin/Debug/net9.0-windows/RgceAssetCooker.exe --worlds` writes the six files (a second run writes nothing);
  2. `python scripts/generate_rgce_content_assets.py` catalogues them;
  3. `RgceAssetCooker.exe --worlds --verify` re-reads them and compares them with the builders.
  Run them again after a change to the builders or to what they read: a `.Track` file, the CombiModel files, the scenery placement. A world edited and saved in the editor no longer matches the builders: `--worlds --verify` reports it and `--worlds` overwrites it.
- What the game does when a world is loaded: for a race world, the player's shadows option on the world and on `Light.Key`, the colour pipeline reset in `--color-pipeline legacy`, then the player car spawned from `Entity.PlayerCar` and given its state, then the sky cube loaded from the world's cubemap asset id (a race world without an id gets the procedural sky). The front end has no cube and a black background.
- What stays in code: the three cars of the carousel, its camera and its placement; the sky background pipeline; the meshes of the track generators.
- The editor opens the five worlds and the car. It saves the five worlds back without loss: it changes only the `view_distance` and the `viewport` of a camera, which the tool leaves at zero and the editor initializes when it opens the world (the viewport with the size of its window); the game never reads them from the file. Saving the car from the editor's own interface has not been checked yet; loaded and saved again through the engine's `EditorEntityWriter.SaveEntity`, it gives the bytes of the tool.

## Track generator components

The race track is not stored as meshes. Four components of `RacingGame.Gameplay` rebuild it from the data assets, in the game and in the editor. Each one is the root component of its own entity:

- `RaceTerrainGeneratorComponent`: the ground, from the heightmap;
- `RaceRoadGeneratorComponent`: the road, and its tunnels (the `Tunnel` spans of the road helpers);
- `RaceGuardRailGeneratorComponent`: one side of the guard rail, with its posts;
- `RaceColumnGeneratorComponent`: the columns and their segments.

`RaceTrackPhysicsComponent` is not a generator: it computes the track profile that the car physics reads, from the same two ids.

The `.Track` and `.CombiModel` files are read as the original game reads them ([ADR-0019](../../docs/decisions/0019-rgce-reads-the-legacy-track-helpers.md)): the width helpers set the road widths, the road helpers place the palms, laterns and tunnels, and the CombiModel groups place their objects.

- Serialized settings:
  - `track_asset_id`: the `.Track` asset, a `track` entry of `AssetInfos.json` (`TrackBeginnerData`, `TrackAdvancedData`, `TrackExpertData`);
  - `heightmap_asset_id`: the heightmap asset (`Data.LandscapeHeights`);
  - guard rail: `side` (`Left` or `Right`) and `holder_gap` (distance between posts, default 15);
  - columns: `columns_distance` (distance between columns, default 33).
- A generator rebuilds its geometry when a setting changes. An absent or unknown id writes one warning naming the entity, and the generator draws nothing.
- The posts, the column segments and the meshes are generated children (`RaceGeneratedModelComponent`). They are shown in the inspector and are marked `[Browsable(false)]`, so the editor does not save them: a saved world holds only the settings above.
- Test world: `Worlds/Test/TrackBeginnerGenerators.world` (catalogue entry `World.Test.TrackBeginnerGenerators`). It holds the five generators of the Beginner track under the names the game uses (`Track.Road.Beginner`, `Track.GuardRail.Left.Beginner`, `Track.GuardRail.Right.Beginner`, `Track.Columns.Beginner`, `Track.Ground.Beginner`), a directional light, and one entity with `RaceTrackPhysicsComponent`. The file is in the form the editor writes: a Save leaves it unchanged.
- Opening it, on a scratch copy of `Content` (never on the real folder):
  - `CasaEngine.Editor.exe --project <copy>\RacingGameCasaEngine.json --open-asset Worlds/Test/TrackBeginnerGenerators.world --entity-index 1 --capture-delay 3 --diagnostics-out <file> --screenshot-out <png>`;
  - from the editor, open the world from the content browser, then change a setting in the inspector (for example `holder_gap` of the left rail) and File > Save;
  - there is no automation option that sets a component property: edit the copied `.world` instead.
- Camera tip: the track is hundreds of units wide, and the editor viewport starts at distance 10 from the origin, so at scale 1 the camera sits inside the terrain. Nothing sets that camera from a file or a command line option (`.casaeditor/viewport.editor.json` only holds the 2D view state). Double-click an entity in the hierarchy to frame it, or, for automated captures, scale the roots of the copied world to 0.02 and move them to y = -2. Keep the committed world at scale 1.
- `generate_rgce_content_assets.py` catalogues every `Worlds/**/*.world` that no entry names, as `World.<path without extension, "/" replaced by ".">`, and checks that the `id` written in the world is the id of its entry.