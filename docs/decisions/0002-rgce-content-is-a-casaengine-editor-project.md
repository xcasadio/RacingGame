# ADR-0002: RacingGameCasaEngine's content folder is a CasaEngine editor project

- **Status**: Accepted
- **Date**: 2026-10-05
- **Source**: this chantier: `ai-agent/tasks/rgce-xaml-screens-tasks.md`, decisions D16, D18, D19 and D20 (author answers, 2026-10-05), and the read-only editor discovery recorded in its "État vérifié du dépôt"

## Context

- ADR-0001 requires a complete CasaEngine project so that the CasaEngine editor opens RacingGameCasaEngine's content and previews and edits its XAML screens with design-time data (D16).
- The editor opens a project from a `.json` file:
  - the file's folder becomes the project root, where `AssetInfos.json` is read;
  - five keys are required: `WindowTitle`, `ProjectName`, `FirstScreenName`, `FirstWorldLoaded` and `GameplayDllName` (`CasaEngine/CasaEngine/Framework/Configuration/Project/ProjectSettingsHelper.cs:10-55`).
- The editor loads the assembly named by `GameplayDllName`, relative to the project root, from a shadow copy. It then resolves design-time view models by simple type name, editor assemblies first (`AssemblyManager.cs:15-35`, `EditorScriptAssemblyService.cs:32-53`, `ElementFactory.cs:19-112`).
- `FirstWorldLoaded` must name a catalogued, loadable `.world`. The editor's automation command line only proceeds when that world holds at least one entity (`GameManager.cs:98-121`, `GameEditor.cs:7754-7768`).
- RacingGameCasaEngine does not use a project file at run time:
  - `Program.cs` sets the project path to the build output's `Content` and loads the catalogue itself;
  - the game builds its worlds in code.
- Six catalogued files existed only in `RacingGame/Content`, linked into the build output by the csproj: the three UI atlas PNGs and the three `.Track` files. The other linked files are not catalogued: 142 textures (199 MB, no Git LFS), the CombiModels and `LandscapeHeights.data`.
- The editor binary built in the submodule dated from 2026-04-17, before CasaEngine added design-time data (CasaEngine ADR-0038, 2026-09-24).

## Decision

- **Project root.** `RacingGameCasaEngine/Content` is the editor project root. Its project file is `RacingGameCasaEngine.json`, which holds the 13 keys the editor writes on File > Save. It sets no `GameplayCsprojName`, so the editor never rebuilds code on Play.
- **Catalogued files.** Every catalogued file lives under that root. The three UI atlas PNGs and the three `.Track` files are copied from `RacingGame/Content`, and their build links are removed. Non-catalogued linked content stays linked.
- **View models.** The view models live in a dedicated library, `RacingGameCasaEngine.UI`, referenced by the game.
  - An after-build step copies its dll and pdb to the project root. Git ignores both files, and `GameplayDllName` names the dll.
  - Every view model is public and sealed, has a public parameterless constructor and does not depend on the game. Its name starts with `Race` and ends with `ViewModel`.
- **Start world.** A start world, `Worlds/Editor.world`, has no script and holds one empty entity, which the editor's automation requires.
- **Design-time data.** Each screen has a design-time data file, `<Screen>.design.json`, next to its screen asset. The screen asset references it through `design_time_data_file`. The file is not copied to the build output.
- **Verification.** The editor is verified through its automation command line:
  - the editor is rebuilt from the submodule, which writes ignored build outputs only;
  - one capture is taken per screen;
  - the preview status line is checked for design-time data errors.

## Consequences

- **Runtime unchanged.** The game neither reads the project file nor loads the library through the editor path. Its output `Content` folder is unchanged, except that it also contains the start world.
- **Duplicated files.** Six files (1.4 MB) exist in both `RacingGame/Content` and `RacingGameCasaEngine/Content`. A change to one copy is not reflected in the other.
- **Constrained view models.** View models cannot hold game services or GPU resources. Screens fill them from game state every frame, and write two-way edits back through `PropertyChanged`.
- **Rewrites on Save.** File > Save in the editor rewrites `AssetInfos.json`, the project file and the start world. New Project must never be used on this folder, because it overwrites `AssetInfos.json`.
- **Rebuild after submodule updates.** The editor must be rebuilt from the submodule whenever it is updated. Otherwise the editor and the library it loads diverge.
- **Preview limits.** The editor preview is not run-time faithful:
  - it parses in Compatibility mode and renames elements;
  - it ignores the screen theme and uses the editor font;
  - it never runs screen code.

  Code-only behaviour (visual-state restyling, focus, code-applied values) is therefore verified at run time only. The gaps met are reported, never worked around in the submodule (ADR-0001).
