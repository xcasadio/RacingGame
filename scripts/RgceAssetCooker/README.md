# RgceAssetCooker

Cooks the 56 legacy glTF models of `RacingGameCasaEngine/Content/Models` into catalogued engine assets, so the editor and the game
can load models, materials and textures by asset id instead of building them in code (plan
`ai-agent/tasks/rgce-editor-project-tasks.md`, T5.4).

Like the other tools of `scripts/`, it is not part of `RacingGame.slnx`. Build and run it on its own:

```
dotnet build scripts/RgceAssetCooker/RgceAssetCooker.csproj
scripts/RgceAssetCooker/bin/Debug/net9.0-windows/RgceAssetCooker.exe [--content <Content folder>] [--check | --verify] [--worlds]
scripts/RgceAssetCooker/bin/Debug/net9.0-windows/RgceAssetCooker.exe --content <copy of Content> (--roundtrip | --roundtrip-controls) [--out <folder>] [--type <extension>] [--file <file name>]
```

The default `--content` is `RacingGameCasaEngine/Content` of the repository that holds the executable. No graphics device is needed.

## What it writes

| Asset | File | Catalogue name | Type |
|---|---|---|---|
| One per glTF | `Models/<Name>.staticModel` | `StaticModel.<Name>` | `staticmodel` |
| One per model material | `Materials/Models/<Name>/<Slot>.material` | `Material.<Name>.<Slot>` | `material` |
| One per track material (`Road`, `Ground`, `Terrain`, `GuardRail`, `RoadColumn`, `RoadTunnel`, `RoadBack`) | `Materials/Track/<Role>.material` | `Material.Track.<Role>` | `material` |
| One per image a cooked material uses | `Textures/<Name>.texture` | `Texture.<Name>` | `texture` |

- Files are written with the editor serializers (`EditorAssetWriterService`), so the editor re-saves them unchanged.
- Ids are `uuid5` of the asset name in the namespace of `generate_rgce_ui_assets.py` (`racinggame-casaengine/ui-assets`).
  A `.texture` points to the png entry `Image.<Name>` (`uuid5("Image.<Name>")`).
- **Models**: vertices, indices, nodes and tangents are exactly what `LegacyGltfModelReader` reads, with the RacingGame import profile. A mesh whose glTF primitive has a `TANGENT` accessor gets a `tangents` array (one entry per vertex, written by the engine serializer, ADR-0123 of CasaEngine); the converter writes `TANGENT` for the primitives whose material has a normal texture.
  Every model except `Car` and `CarSelectionPlate` gets the root node rotated by pi/2 about X that the game used to add when it loaded
  the glTF (`LegacyTrackSceneFactory.ApplyLegacyModelRootCorrection`, removed in T5.5 once the game loaded the cooked models), named
  `<Name>.LegacyRootCorrection`. Each mesh points to its material by `material_asset_id`.
- **Materials**: definition `lit-pbr` (ADR-0028), with the values of `RaceMaterialRecipes` (`ForScenery`, `ForCar` with car index 0, `ForPlate`,
  and `ForRoad`, `ForGround`, `ForTerrain`, `ForGuardRail`, `ForRoadColumn`, `ForRoadTunnel`, `ForRoadBack` with the first texture candidate that exists):
  `base_color_texture`, `normal_texture`, `detail_texture` and `detail_tiling` (the terrain's `LandscapeDetail`, tiled 24 times), `base_color`
  (the diffuse colour, alpha 255; 210 on every car part but the glass), `metallic`, `roughness`, `clear_coat` and `clear_coat_roughness` from the PBR
  family of the surface (`RacingGamePbrMaterialMapping`: terrain, road, road surface, guard rail, lamp post, masonry, signage, vegetation, matte industrial, building
  glass, car paint, chrome, glass and tyre, carousel plate, default), `emissive_color`, `emissive_texture` (only for a recipe that names an emissive texture, today the road tunnel's and the signs': the key is absent from every other file, and the verifier reads an absent key as the null id), `emissive_shadow_factor` (only for a recipe that asks for a non-default value, today the road tunnel's 1: the key is absent from every other file, and the verifier reads an absent key as the engine default 0), `occlusion_texture` and `occlusion_strength` (only for a recipe that names an occlusion texture, today the road's black `NoEnvironmentLight.png` at strength 1, ADR-0034: the engine's occlusion, 1 + strength x (red - 1), scales the image-based light only; both keys are absent from every other file, and the verifier reads an absent texture key as the null id and an absent strength as none) and `alpha_cutoff`. The ambient, specular and reflection settings of the
  former `lit-diffuse` materials are not cooked. A render state the
  game leaves `null` is written as the state the renderer applies: blend `Opaque`, depth `Default`, rasterizer `CullCounterClockwise`,
  sampler `AnisotropicClamp`. The slots of the car that sample the car texture (painted or not) use `Texture.RacerCar`; the game replaces it
  at run time (T5.6), and paints the slots whose cooked material has a clear coat (the car paint family is the only one with one).
  `<Slot>` is the display name of the glTF material, with a numeric suffix if two materials of a model share it. `receive_shadows` of the file comes from the recipe: true unless the recipe says otherwise (today the window glass of Hotel01 and Hotel02, ADR-0038).
- **Textures**: one per image a cooked material names as base, normal, detail, emissive or occlusion texture (`Texture.LandscapeDetail` for the terrain's detail, `Texture.NoEnvironmentLight` for the road's occlusion). The sampler is the one of the recipes (`AnisotropicWrap`). `generate_mipmaps` is true on every `.texture`: the engine then draws the texture with a full mipmap chain, built at load time from the image (ADR-0124 of CasaEngine); the png of the image entry is not changed.

No fallback material is cooked: no glTF has a mesh without a material (the fallback recipes were removed in T5.6).

## Catalogue

The cooker updates its own entries of `Content/AssetInfos.json` in place and appends the new ones at the end (models, then materials,
then textures, each in the order of the plan). The file keeps its format (2-space indent, CRLF, no final newline); the cooker refuses a
catalogue it could not write back byte for byte. Files whose bytes do not change are not touched, so a second run changes nothing.

Run `python scripts/generate_rgce_content_assets.py` first on a catalogue that still names the png images `Texture.<Name>`: the generator renames them
`Image.<Name>` in place. The cooker then takes the `Texture.<Name>` name for the `.texture` assets. In the other order the result is the
same set of entries.

## Worlds and the player car (`--worlds`)

`--worlds` writes the game's worlds and its car as CasaEngine assets, built by the game's own constructors
(`RacingGame.Gameplay/Race/RaceWorldTemplates.cs`, `Entities/RaceCarEntityFactory.cs`), through the engine's public writers
(`EditorWorldWriter.SaveWorld(World, IReadOnlyList<Entity>)`, `EditorEntityWriter.SaveEntity`; CasaEngine ADR-0128). The game does not load
these files yet (plan `ai-agent/tasks/rgce-worlds-as-assets-tasks.md`, M5).

| Asset | File | Catalogue name | Type | Entities |
|---|---|---|---|---|
| Front end | `Worlds/FrontEnd.world` | `World.FrontEnd` | `world` | 2 (camera, player start) |
| Races | `Worlds/Race/Track<Beginner\|Advanced\|Expert>.world` | `World.Race.Track<Track>` | `world` | 661, 916, 1757 (5 generators, the scenery, camera, `RaceRoot`, `Light.Key`, 3 checkpoints, player start) |
| Car selection | `Worlds/CarSelection.world` | `World.CarSelection` | `world` | 4 (`Carousel.Light`, three plates) |
| Player car | `Entities/PlayerCar.entity` | `Entity.PlayerCar` | `entity` | the entity of `RaceCarEntityFactory.Create()`, renamed `Entity.PlayerCar` |

- The entities are built with `EngineEnvironment.ProjectPath` set to the Content folder (the track scene reads the catalogue, that path and the
  track files), then written into a scratch folder; only the files whose bytes change are copied, so a second run writes nothing. No entity is
  initialized or added to a world. A race world has the shadows enabled, a key light that casts them, the ambient and the `linear` colour
  pipeline of `RaceWorldTemplates.ApplyRaceEnvironment`; the races and the carousel carry the id of `Texture.SkyCubeMap` as their
  `specular_cubemap_asset_id`.
- **Ids** are deterministic, set by the same reflection as `CookPlanner.SetIdentity` without renaming (`CookPlanner.SetId`): the world and the car are
  `uuid5` of their catalogue name (`World.FrontEnd`, `Entity.PlayerCar`; the generator checks that a file's id is its entry's); an entity of a
  world is `uuid5("<entry>/<entity name>")`; a component is `uuid5("<entry>/<entity name>/<component path>")`, the path being the names of the
  components from the root of the entity down, joined by `/` (the name alone for a component outside the root's tree). Among siblings, a name that occurs
  more than once becomes `Name#rank` (rank counted from 0 among the siblings of that name). For the car the prefix `<entry>/<entity name>` is
  `Entity.PlayerCar`. The tool fails if two entities of a world share a name, if two component paths of an entity are equal, if two ids of a world collide, or if an
  entity has child entities (the writer does not save them).
- The catalogue is not written by the tool: run `python scripts/generate_rgce_content_assets.py` after `--worlds` (it catalogues `Worlds/**/*.world`
  as `World.<path>` and `Entities/**/*.entity` as `Entity.<path>`, with the asset type the engine infers from the extension, and checks that each file's `id` is its entry's).
- A camera component is written in its state before initialization: `view_distance` and `viewport` are zeros. The editor initializes the camera when it opens the
  world and saves the size of its window, 1, 1000 and 999; the game never reads those values from the file.
- `--worlds --verify` re-reads the six files with public engine APIs only (`World.Load` on the file, `Entity.Load` on each inline entity node and on the car file) and
  compares them with the constructors run again: the world's id and name, its keys, its environment key by key (against what `WorldEnvironmentSettings.Save` writes for the expected environment),
  the sky cube id (empty for the front end), the number of entities, each entity in order (name, id, policies, the tree of components with type, name and id, local transforms to 1e-6,
  the model id and shadow flags of the static models, the light, the camera, the player start, and every `[SerializedProperty]`: checkpoint half extents, track and height map ids, generator
  sides, car anchor ids), and the catalogue entry of each file (id, file, type). Exit code 1 on any mismatch, each one listed. Run it after the generator.

## Options

- no option: cook, then verify.
- `--check`: lists the glTF models and the images of cooked materials with no cooked file or catalogue entry, and the cooked files the plan does not
  contain. Writes nothing; exit code 1 if there is any.
- `--verify`: re-reads every cooked file with the engine loaders (`StaticModel.Load`, `MaterialAsset.Load`, `Texture.Load`) and compares it with
  a fresh read of the glTF files and the recipes: vertices, tangents (the number of entries and every value), indices, nodes, material ids, every material value under the effective-state rule,
  the render state the engine resolves for the asset, textures (image, sampler and `generate_mipmaps`), the catalogue (no duplicate id or name, every entry's file exists), and the car
  paint rule the game applies at run time (a car-textured slot is painted when its cooked material has a clear coat) against the paint role of each `Car.gltf` material in the recipes. Exit code 1 on any mismatch. It does not compile the materials (that needs a graphics device); the editor catalogue walk opens them.
- `--worlds`: write the five worlds and `Entities/PlayerCar.entity` (see above). It touches neither the models, materials and textures nor the catalogue. Not combinable with `--check`.
- `--worlds --verify`: re-read those six files and compare them with the constructors. Writes nothing; exit code 1 on any mismatch.

## Save round trip and preview headers (`--roundtrip`)

`--roundtrip` measures whether the engine writers rewrite every editable asset without loss, outside the editor and with no graphics device (plan
`ai-agent/tasks/rgce-editor-project-tasks.md`, T7.3, P25 and P30). It fixes nothing: a failing asset is reported. It must be given a **copy** of the
project's `Content` folder (`--content`, required: it is read, never written; `EngineEnvironment.ProjectPath` is set to it) and writes only into
`--out` (default `artifacts/editor-closure/roundtrip` of the repository, git-ignored; an output folder inside a Content folder is refused). The tool
creates the output folders; the engine writer does not. It walks `AssetInfos.json` and picks the check by file extension.

```
RgceAssetCooker.exe --roundtrip --content D:/development/rgce-s7/content --out artifacts/editor-closure/roundtrip
RgceAssetCooker.exe --roundtrip-controls --content D:/development/rgce-s7/content --out artifacts/editor-closure/roundtrip
```

- **Editable assets** (`.world`, `.entity`, `.staticmodel`, `.material`, `.texture`, `.sound`, `.uiscreen`, `.sprite`): loaded with the engine loader of the
  type (`Entity`, `StaticModel`, `MaterialAsset`, `Texture`, `SoundAsset`, `UIScreenAsset`, `SpriteData`: `Load(JObject)`), written with
  `EditorAssetWriterService.SaveAsset` to `<out>/first/<file name>`, the first write loaded again and written to `<out>/second/<file name>`. A `.world` creates its
  entities only in a running game, so it is loaded with `entity_references` emptied, each inline entity node is loaded by `Entity.Load`, and the world is written by
  `EditorWorldWriter.SaveWorld(World, IReadOnlyList<Entity>)` (the method of `--worlds --verify`); a reference whose `asset_id` is not empty fails as
  "asset reference not supported". No internal type of the engine is called.
- **Checks**, each reported under its name: `unstable` (the second write has not the bytes of the first); `key lost` (a key of the source JSON is missing at the same place
  in the first write); `value changed` (a value differs: numbers to 1e-6, arrays element by element in order, strings and booleans exactly); `unresolved type` (a component
  of a world or of `Entities/PlayerCar.entity` is loaded as `UnresolvedComponent`, which would write its node back unchanged and pass the round trip for nothing);
  `logged error` (the engine logged an error through `Logs` while the asset was processed); `component count` (the components loaded under their real type, walked from the
  root component, the component list and the children, are not as many as the component nodes of the source: root component, `components`, `children_component` trees);
  `load failed` / `write failed` (an exception, with its type, message and first stack frames); `asset reference not supported`; `file missing`. A failed asset is
  reported under the first of these names in the order of `RoundTripCheck.Order` (the order above, `file missing` first, `unreadable preview` last); all its failed checks are in the report.
  The tool loads the `RacingGame.Gameplay` assembly before the first read, because the engine looks component types up in the loaded assemblies only.
  The keys the writer adds are not failures: they are listed per type. The written file is not compared with the source byte for byte (a source may differ by its line endings
  or its final newline).
- **Preview assets** (`.png`, `.dds`, `.wav`, `.fnt`, `.gltf`, `.track`, `.combimodel`, `.data`): the header is read by `AssetPreviewReader.Read`, which is what the editor's
  preview reads; `AssetPreviewInfo.Problem` not null fails the asset as `unreadable preview`, with the kind and the text. A glTF whose header is sound but which gives no mesh is not seen.
- **Reports** in `--out`: `roundtrip.json` (`summary` per type, then one record per asset: verdict, failed checks and details, lost keys, changed values with the source
  and the written value (the first 30 per asset, with the total), added keys, component counts, logged errors and warnings, preview kind and problem), `summary.txt` (what
  is printed) and the files of the two writes. Exit code 1 if any asset fails, 2 on a wrong command line.
- `--type <extension without its dot>` and `--file <file name of the catalogue>` restrict the run to one type or one file.
- **`--roundtrip-controls`** runs the five negative controls on altered copies under `<out>/controls`, never in the content copy: a material whose first write loses a key
  (`key lost`), has a value changed (`value changed`) or whose second write has one byte altered (`unstable`); a copy of a world in which a component type is renamed to a name no
  assembly declares (`unresolved type`; the lookup ignores case, so the name is not a change of case); a copy of a `.wav` overwritten with text (`unreadable preview`). Each
  subject is first run unaltered and must pass. The run exits 1 if a control is not reported under its own name first.
