# ADR-0015: RacingGame.Gameplay is the CasaEngine editor's gameplay assembly for RacingGameCasaEngine

- **Status**: Accepted
- **Date**: 2026-10-08
- **Source**: this chantier: `ai-agent/tasks/rgce-editor-project-tasks.md`, decisions D1 and D7 (author answers, 2026-10-08), proposal P4 (validated with the plan), tasks T1.1 and T1.2

## Context

- ADR-0002 made `RacingGameCasaEngine/Content` a CasaEngine editor project for the nine XAML screens:
  - its view models live in a library, `RacingGameCasaEngine.UI`, whose dll the project file names as `GameplayDllName`;
  - the project sets no `GameplayCsprojName`, so the editor never rebuilds code on Play.
- The editor loads one gameplay assembly, the one named by `GameplayDllName`, from a shadow copy of that dll alone (with its pdb and deps file). It runs the first `IPlugin` it finds there and resolves types by simple name (`AssemblyManager.cs:15-35`, `EditorScriptAssemblyService.cs:32-53`). `RacingGameCasaEngine.UI` had no `IPlugin`, so every project open logged "contains no IPlugin implementation".
- With `GameplayCsprojName` set, the editor runs `dotnet build` on that csproj before Play when its sources are newer than the loaded dll, then copies and reloads the new dll (`EditorScriptReloadCoordinator.cs`, `EditorScriptBuildService.cs`). It also writes a machine-local `CasaEngine.EnginePath.props` next to the csproj on project open (`EditorProjectAuthoringService.cs`).
- All game code (components, the car pawn, the game mode, the controller) lives in the `RacingGameCasaEngine` exe, which the editor never loads.
- The author decided that the game code moves to a gameplay DLL that the editor loads and that the exe uses to run the game (D1), and that `RacingGameCasaEngine.UI` is renamed `RacingGame.Gameplay` and becomes that DLL (D7).

## Decision

- **Rename.** `RacingGameCasaEngine.UI` becomes `RacingGame.Gameplay`: project, folder, assembly, and the view-model namespace `RacingGame.Gameplay.ViewModels`. The exe's own namespace `RacingGameCasaEngine.UI`, that of its `UI/` folder, is unrelated and unchanged.
- **Gameplay assembly.** `RacingGame.Gameplay.dll` is the project's gameplay assembly: `GameplayDllName` names it, and the game code that the editor must see moves into it in later steps of the chantier.
  - It references CasaEngine and declares `Plugin : IPlugin`, whose `Initialize()` is empty and repeatable: the engine and the editor find the game's types by name.
  - Its build still copies the dll and pdb to the project root, where they are git-ignored.
- **Rebuild on Play.** The project file sets `GameplayCsprojName` to `../../RacingGame.Gameplay/RacingGame.Gameplay.csproj`, relative to the project folder.
- **One load in the game.** The exe references the project directly and keeps its own bootstrap: it does not pass the project file to the engine, so the engine never loads the dll a second time through `Assembly.LoadFile` (P4).
- **Machine-local file.** `RacingGame.Gameplay/CasaEngine.EnginePath.props`, written by the editor, is git-ignored.

## Consequences

- Supersedes the "View models" bullet of ADR-0002 and its statement that the project sets no `GameplayCsprojName`; the rest of ADR-0002 stands. The view-model rules of ADR-0002 still apply: public, sealed, public parameterless constructor, no game dependency, `Race*ViewModel` names.
- The editor shadow-copies the gameplay dll alone. Its dependencies must be assemblies the editor has already loaded (CasaEngine, MGUI, MonoGame): a gameplay dependency of another kind would fail to load in the editor.
- Every Play that rebuilds writes a new `Content/.casaeditor/script-build/<timestamp>/` folder, and every project open a new shadow copy under `%TEMP%\casaeditor-scripts\`. The editor never deletes either; `.casaeditor/` is git-ignored.
- The editor runs `Plugin.Initialize()` on every project open and script reload, twice per automation run: anything added to it must stay repeatable.
- Game types moved into the assembly later must have names that are unique across the loaded assemblies, because types are resolved by simple name.
