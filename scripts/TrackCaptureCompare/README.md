# TrackCaptureCompare

Compares the 3D reference captures of the original RacingGame with the CasaEngine port, view by view (ADR-0015, plan `ai-agent/tasks/rgce-3d-capture-bench-tasks.md`).

## Capture

1. Original (the reference, deterministic for a given seed):

   ```
   RacingGameOriginal\Desktop\bin\Debug\net10.0\RacingGame.exe --capture-track-audit --seed 1 --out <reference folder>
   ```

   It writes `views.json` and, for each of the 3 tracks, 5 views (`start`, `sector-20`, `sector-50`, `sector-80`, `overview`) in two variants: `<Track>-<view>-full.png` (as the game looks) and `<Track>-<view>-nopost.png` (post screen shaders off). The default size is 1280x720 (`--size WxH` changes it; it must fit on the desktop).

2. Port (replays the cameras of `views.json`, same size and projection):

   ```
   RacingGameCasaEngine\bin\Debug\net9.0-windows\RacingGameCasaEngine.exe --capture-reference-views <reference folder>\views.json --reference-capture-out <candidate folder>
   ```

   It writes `<Track>-<view>.png` and logs, per view, how far its start grid and checkpoints are from the original's positions (`alignment` lines in `racinggame-casaengine-<pid>.log`).

## Usage

```
dotnet run --project scripts/TrackCaptureCompare -- <views.json> <reference folder> <candidate folder> <output folder> [--variant full|nopost]
```

`--variant` picks the original's variant (default `nopost`, since the port has no glow). Either folder may hold original files or port files, so two runs of the same game can be compared (repeatability check).

Exit codes:

- `1` if a file is missing or two captures have different sizes;
- `2` on bad arguments;
- `0` otherwise. There is no pass threshold: the report is read, not gated.

The output folder receives:

- `report.md`: one line per view with the mean absolute difference per channel (0-255), the share of pixels whose largest channel difference exceeds 16, the mean luminance of each image, and the share of the reference's edges found within 2 pixels in the candidate (a geometry alignment hint that ignores shading);
- `compare-<Track>-<view>.png`: reference and candidate on top, difference x4 and edge overlay below (reference edges red, candidate edges green, common edges yellow), at half size.
