# ADR-0015: The original game exports the 3D reference views; the port replays them

- **Status**: Accepted
- **Date**: 2026-11-14
- **Source**: this chantier: `ai-agent/tasks/rgce-3d-capture-bench-tasks.md` (D2)

## Context

The port to CasaEngine is checked against the original MonoGame RacingGame, copied unmodified into `RacingGameOriginal/` (commit `7686c0a`). Comparing the two renderings needs the same camera, projection and image size on both sides.

Facts verified in the code on 2026-11-14:

- The original had no capture facility: `Desktop/Program.cs` ignored its arguments and `Core/GameLogic/ScreenshotCapturer.cs` saved nothing.
- Its rendering was not reproducible: `Core/Helpers/RandomHelper.cs` seeded the generator from the clock (random scenery along the track), `Core/Graphics/BaseGame.cs` lowered `Model.MaxViewDistance` with the frame rate, and the windmill wings turned with the total time.
- The port's `--capture-track-audit` computed its views from the converted entities, so its cameras could not be matched with the original.
- The port converts the original's coordinates with (x, y, z) -> (x, z, -y) at scale 1 (`RacingGameCasaEngine/Worlds/LegacyTrackSceneFactory.cs`).

## Decision

- The original owns the views. `RacingGame.exe --capture-track-audit [--seed N] [--out <dir>] [--size WxH]` skips the menus, renders 5 views of each built-in track (start, 20 %, 50 %, 80 % of the track, overview) and writes them as PNG files with a `views.json` holding every camera (position, target, up), the car pose, the projection, the image size and the light direction, in the original's own coordinates (Z up).
- Each view is saved twice: `full` (as the game looks) and `nopost` (post screen shaders off), because the port has no glow yet.
- The port replays `views.json` instead of computing views of its own.
- In capture mode the original uses the given seed, fixed settings (it neither reads nor writes the player's settings file), no view distance limit and frozen windmills. Without the argument it behaves as before.

## Consequences

- Two runs of the original with the same seed give byte-identical files (checked on 30 PNG files and `views.json`), so any difference in a pair comes from the port.
- The default size is 1280x720: the original shrinks a window that does not fit on the desktop, and a 1920x1080 window does not fit on a 1920x1080 desktop.
- The reference shows every object whatever its distance, which the game itself does not do on slow machines; the frame-rate driven culling is not part of what is compared.
- The car is moved to the point each view looks at, because the original's shadow map follows the car. It is drawn (and casts its shadow) only in the start and overview views, where it stands on the start grid like the port's car; `views.json` says so per view (`car.visible`).
- A change of the view set or of `views.json` must be made in the original first, then followed by the port.
