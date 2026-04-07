# RacingGame
MonoGame port (3.8.*) of the Racing Game starter kit

>Note: All content and source code downloaded from this page is bound to the Microsoft Permissive License (Ms-PL).

## Description

The Racing Game Starter Kit is a complete game. This sample comes ready to compile and run, and it's easy to customize with a little bit of C# programming. You are free to use the source code as the basis for your own XNA Game Studio projects, and to share your work with others.
Racing Game is a 3D auto racing game that features advanced graphics, audio, and input processing. Race around the track and try to beat the ghost car to achieve the best time.

## Improvements

This fork brings a series of bug fixes and improvements over the original MonoGame port.

**Bug fixes**
- Settings save/load (resolution, volumes, highscores) now working via XML.
- Best replays saved between sessions.
- Fullscreen mode properly applied from the Options screen.
- `MouseInBoxRelative` fixed (incorrect Rectangle calculation).
- `KeyToChar`: swapped `,` and `.` characters corrected.
- Per-frame memory allocations removed (`Vector3[]` for collisions, `List<Keys>` for input).
- `GetAngleBetweenVectors`: dot product clamped to prevent `NaN`.
- Update/Render separated in `Player` (win/game-over text no longer drawn inside `Update`).
- `ManualResetEvent` wrapped in `try/finally` to prevent deadlocks.
- Exception handling added on the loading thread.
- Screenshot capture implemented (F12 key → timestamped PNG).

**Architecture & code quality**
- Reduced inheritance chain: `ChaseCamera` extracted from `CarPhysics`, now used as composition in `Player`.
- `Update(GameTime)` / `Render()` separated in all GameScreens via `IGameScreen`.
- `Sound` split into `MusicManager`, `SfxManager` and `EngineSound`.
- Screen stack protected by a lock (`_screenLock`) for thread safety.
- `RacingGame.Shared` now hosts MGUI through a dedicated bridge layer instead of coupling screens directly to the renderer.
- All obsolete Xbox 360 / GamerServices / UWP preprocessor blocks removed.
- Public fields converted to properties; magic numbers extracted to named constants.
- `IDisposable` properly implemented (`ShaderEffect`, `BaseGame`, `RacingGameManager`).
- `RenderToTexture`: defaults to `SurfaceFormat.Color` instead of `Rgba64` (half the GPU memory).

**Features**
- Dynamic resolutions in Options (sourced from `GraphicsAdapter.SupportedDisplayModes`; prefers modern 16:9).
- Configurable FPS display (menu option, toggleable in-game).
- Gamepad vibration on collisions (glancing 0.35 / frontal 0.85, variable duration, On/Off option).
- Loading, menu, selection, help, highscores, and in-game HUD overlays now render through MGUI.

## UI Architecture

The UI stack in `RacingGame.Shared` is now split between legacy scene rendering and MGUI overlays.

- `BaseGame` owns the MGUI lifecycle and updates a shared `MguiUiHost`.
- `RacingGameManager` still owns the `IGameScreen` stack, but the top screen can now expose an `IMguiScreenView` through `IMguiScreen`.
- Each migrated screen has a dedicated view under `RacingGame.Shared/UI/MGUI/Views/`.
- Legacy rendering is still used for backgrounds, 3D previews, and fullscreen shader effects. Interactive controls and HUD text live in MGUI.

For new screens, keep gameplay or scene rendering inside the `GameScreen` implementation and place interactive UI composition in a corresponding MGUI view.

## Legacy Import Profile

`RacingGameCasaEngine` now keeps legacy material compatibility split between neutral CasaEngine hooks and a project-owned optional profile.
See [docs/legacy-import-profile.md](docs/legacy-import-profile.md) for the bootstrap point, isolation guarantees, and bounded verification command.

## Screenshot
![image 1](/github/XNA_Racing-Game_01_small.jpg)
![image 1](/github/XNA_Racing-Game_02_small.jpg)
![image 1](/github/XNA_Racing-Game_03_small.jpg)

