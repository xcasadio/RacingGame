# RacingGameCasaEngine asset conventions

Runtime assets are resolved from `RacingGameCasaEngine/Content`.

## Rules

- `Content/AssetInfos.json` is the runtime index consumed by `AssetCatalog.Load(...)`.
- Asset names must stay stable and unique because gameplay code resolves them by logical name first.
- `file_name` values stay relative to `Content` and always use `/` or simple folder segments.
- Game-specific JSON data lives with the game project, not inside `CasaEngine`.

## Suggested folders

- `Content/Textures/` for UI textures, decals, and simple 2D placeholders.
- `Content/Tracks/` for track data, checkpoints, and race metadata.
- `Content/Cars/` for car definitions, tuning, and visual assets.
- `Content/Worlds/` for serialized CasaEngine worlds when the bootstrap stops building them in code.
- `Content/Audio/` for music and sound effects.
- `Content/Shaders/` for shader assets registered in the catalog.
- `Content/UI/` for MGUI-specific assets or data files.

## Near-term migration policy

- Keep bootstrap assets minimal until the race world composition is stable.
- Prefer adding game-level loaders or data files in `RacingGameCasaEngine` instead of extending `CasaEngine` for racing-specific needs.

## CasaEngine editor project

`Content/` is also a CasaEngine editor project ([ADR-0002](../../docs/decisions/0002-rgce-content-is-a-casaengine-editor-project.md)). The game itself does not read the project file.

- Files:
  - project file `Content/RacingGameCasaEngine.json`;
  - start world `Content/Worlds/Editor.world`, with one empty entity, `EditorAutomationAnchor`, required by the editor automation;
  - view models in `RacingGameCasaEngine.UI`. Its build copies `RacingGameCasaEngine.UI.dll` and `.pdb` into `Content/`; both files are git-ignored and named by `GameplayDllName`.
- Before opening:
  - build the editor from the submodule, `dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln -c Debug`, again after every submodule update;
  - build `RacingGame.slnx`, so that the view-model dll exists.
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
- Editor state (`Content/.casaeditor/`) is git-ignored. Every project open leaves a shadow copy of the view-model assembly under `%TEMP%\casaeditor-scripts\`, which the editor never deletes.