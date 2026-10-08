# ADR-0016: RacingGameCasaEngine's content folder holds every game file

- **Status**: Accepted
- **Date**: 2026-10-08
- **Source**: this chantier: `ai-agent/tasks/rgce-editor-project-tasks.md`, decisions D4 and D8 (author answers, 2026-10-08), proposal P10 (validated with the plan), tasks T3.1 to T3.5

## Context

- ADR-0002 made `RacingGameCasaEngine/Content` a CasaEngine editor project and kept "non-catalogued linked content" linked. The csproj linked from the legacy game `..\RacingGame\Content\Textures\*.*` (129 TGA, 2 DDS, 5 PNG), `LandscapeHeights.data` and the 10 `*.CombiModel`. Those files existed only in the build output, so the editor could not open them.
- Of the 28 legacy waves, 6 were ported as `.sound` assets. The other 22 (19 effects and engine loops, 3 musics) were not.
- The game compared texture file names with their `.tga` extension (material tuning, car paint, import-profile check) and loaded the road, terrain, rail and column textures by `.tga` name.
- The author decided that all the legacy content is ported into `Content` and catalogued, sounds included, even those the game does not play yet (D4). The new binaries go into git normally, with a `.gitattributes` that marks them binary (D8).

## Decision

- **One content folder.** Every game file the game reads lives under `RacingGameCasaEngine/Content` and has a catalogue entry. The engine's own content (shaders, fonts, icons), which the CasaEngine project reference copies to the output, stays in the engine. The only exceptions are the project files: the project file, the catalogue itself, the gameplay dll and pdb, `.casaeditor/`, the `.design.json` and `.xaml` of the screens, and the `.bin` buffers of the glTF models, which their `.gltf` references.
- **Textures.** The 29 legacy TGA that no model referenced join `Content/Textures` as PNG files of the same name with the same RGBA pixels. They are converted by `scripts/LegacyModelGltfConverter --textures`, and `--verify-textures` checks all 129 TGA/PNG pairs. The game loads the PNG files, and compares texture names without extension. A model whose legacy metadata names a `.tga` resolves it to the `.png` of the same name.
- **Other files.** The 2 cube-map DDS, `LandscapeHeights.data` and the 10 CombiModel files are copied unchanged.
- **Sounds.** The 22 remaining waves are copied under their XACT wave name, without spaces (`Content/Audio/<Name>.wav`). Each has a `.sound`:
  - volume `10^(dB/2000)` of the XACT sound volume;
  - bus `Sfx`, or `Music` with streaming for the three musics (P10; the XACT wave bank has no streaming setting);
  - looping where XACT loops the wave: the gear loops and the musics.
  The XACT category volumes are not carried: `Music` is -3 dB and `Gears` -4 dB in `RacingGame.xap`. The game does not play these sounds yet; whoever adds their playback sets those levels.
- **Catalogue.** `scripts/generate_rgce_content_assets.py` writes the content entries, as `scripts/generate_rgce_ui_assets.py` writes the UI entries:
  - ids are uuid5 of the asset name;
  - names are `Texture.<name>`, `Data.<name>`, `ModelSource.<name>`, `Sound.<name>` and `Sound.<name>Wave`;
  - the catalogue format is kept, and a second run changes nothing;
  - `--check` lists the content files that no entry names.
- **Build.** One `Content\**` rule copies the content to the output, minus the editor-only files. The csproj links nothing from `RacingGame/Content`. Only the engine font `tahoma.ttf` stays linked from the CasaEngine submodule.
- **Git.** `.gitattributes` marks `*.png`, `*.wav`, `*.dds`, `*.data`, `*.tga` and `*.bin` binary. There is no LFS.

## Consequences

- Supersedes the rule of ADR-0002 that non-catalogued linked content stays linked. The rest of ADR-0002 stands, as amended by ADR-0015.
- The editor can open every content file: the catalogue grows from 94 to 345 entries.
- The repository grows by 91 files and 94.4 MiB (66.1 MiB of WAV, 27.2 MiB of PNG). `RacingGame/Content` keeps its own copies for the legacy XNA game, which is unchanged.
- A new content file needs a catalogue entry, written by one of the two generators or by the editor. `generate_rgce_content_assets.py --check` finds the ones that are missing.
- The legacy texture names now live in the code without extension. A texture that is added later must be a PNG.
