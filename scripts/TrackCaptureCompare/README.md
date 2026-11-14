# TrackCaptureCompare

Compares the 3D reference captures of the original RacingGame with the CasaEngine port, view by view (ADR-0015, plan `ai-agent/tasks/rgce-3d-capture-bench-tasks.md`).

## Capture

1. Original (the reference, deterministic for a given seed):

   ```
   RacingGameOriginal\Desktop\bin\Debug\net10.0\RacingGame.exe --capture-track-audit --seed 1 --out <reference folder>
   ```

   It writes `views.json`, `scene.json` (the generated track points and every placed object of each track) and, for each of the 3 tracks, 5 views (`start`, `sector-20`, `sector-50`, `sector-80`, `overview`) in two variants: `<Track>-<view>-full.png` (as the game looks) and `<Track>-<view>-nopost.png` (post screen shaders off). The default size is 1280x720 (`--size WxH` changes it; it must fit on the desktop).

2. Port (replays the cameras of `views.json`, same size and projection):

   ```
   RacingGameCasaEngine\bin\Debug\net9.0-windows\RacingGameCasaEngine.exe --capture-reference-views <reference folder>\views.json --reference-capture-out <candidate folder>
   ```

   When the original's `scene.json` lies next to `views.json`, it also writes `scene-measure-<Track>.txt`: the distance of each generated road point from the original's (position, right and up vectors, road width), and for each object placed by the original the nearest object of the same model in the port (distance, rotation, scale), plus the objects without a match on either side.

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

## Probes (light and shadow levels)

```
dotnet run --project scripts/TrackCaptureCompare -- <views.json> <reference folder> <candidate folder> <output folder> --probes scripts/TrackCaptureCompare/probes.json --terrain-detail RacingGame/Content/Textures/LandscapeDetail.tga
```

`--probes <probes.json>` adds a probe evaluation after the usual comparison (`report.md` and `compare-*.png` are unchanged for the same inputs) and writes `probes.md` in the output folder (also printed on the console). The exit code does not depend on the probe verdicts: the report is read, not gated. A missing probes file, or `--probes` without `--terrain-detail` or `--no-terrain-detail-correction`, exits with `2`.

### probes.json

```json
{ "views": { "<Track>-<view>": [ { "name": "road-lit-1", "x": 480, "y": 600, "class": "lit", "surface": "road", "note": "why this point has this class" } ] } }
```

- View names are the `<Track>-<view>` names of views.json. `x`, `y` are full-resolution pixels (1280x720) of the capture, the centre of a 9x9 square; the same pixel is the same surface point in both pictures (geometry is aligned, see `docs/racinggame-3d-capture-bench-findings.md`).
- `class`: `lit` (in the sun), `shadow` (shadowed by the original), `away` (faces away from the sun and the original does NOT shadow it: terrain slope with no caster, building wall farther than 129 m from the car of that view, where the original's shadow map stops), `away-shadowed` (faces away and the original shadows it too, e.g. guard rails and near buildings: reported, not judged; for a later slice). `note` says why, and for `away` why the original does not shadow it.
- `surface` (optional): free label (`road`, `terrain`, `city`, `building`). `shadow` and `lit` probes are paired by surface for the ratio. `terrain` triggers the detail correction below.
- Points sit on patches that are uniform at the 9x9 scale and at least 8 px from any shadow edge, object edge or lane marking in both pictures (checked by sampling: the report prints the luminance standard deviation of each square).

### Report and criteria

`probes.md` gives, per view, each probe's reference and candidate mean colour (RGB, with the luminance standard deviation of the square), their per-channel difference (candidate minus reference) and the verdicts, then the shadow/lit luminance ratio of each side, and a summary table.

1. `lit` and `away`: candidate within +-12 (of 255) of the reference per channel.
2. `shadow`: the candidate's shadow/lit ratio is within +-0.05 of the reference's. The ratio is the mean luminance (0.299 R + 0.587 G + 0.114 B) of the `shadow` probes over the mean luminance of the `lit` probes, per view and per `surface` (only when both exist).
3. Class agreement: a probe counts as shadowed in the candidate when its luminance is below 0.6 x the mean luminance of the candidate's own `lit` probes of the same surface in that view (all `lit` probes of the view when that surface has none). Each `shadow` probe must be shadowed and each `lit` probe must not be. The original's shadow colour multiplies the image by about 0.26, so 0.6 sits between lit and shadowed.

### Terrain detail correction

The original multiplies the terrain colour by `2 x LandscapeDetail` (tiled x24); the port does not yet. `--terrain-detail <tga|png>` (use `RacingGame/Content/Textures/LandscapeDetail.tga`; the tool reads uncompressed and RLE 24/32-bit TGA itself) gives the per-channel mean of `2 x detail / 255`; the REFERENCE colour of `surface: terrain` probes is divided by it (clamped to 255) before the criteria, and the factor is printed in the report. The candidate is never corrected. When both folders hold the same game (original against itself), pass `--no-terrain-detail-correction` instead (factor 1), otherwise the reference alone would be altered.
