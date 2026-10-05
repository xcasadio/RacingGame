# UiCaptureCompare

Compares two runs of the RacingGameCasaEngine UI capture (`RacingGameCasaEngine.exe --capture-ui-screens`), used to check that the move of the screens to XAML keeps their look (plan `ai-agent/tasks/rgce-xaml-screens-tasks.md`, P10).

## Capture

`--capture-ui-screens` walks 10 states through the front-end automation entry points:

1. `splash`
2. `main-menu`
3. `highscores`
4. `options`
5. `help`
6. `car-selection`
7. `track-selection`
8. `race-hud`
9. `pause`
10. `race-finished`

For each state, it captures the back buffer once the state has held for one second. It writes the captures to `%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine\Screenshots\ui-run-<yyyyMMdd-HHmmss>\ui-<state>.png`, one file per state, and logs that folder.

## Usage

```
dotnet run --project scripts/UiCaptureCompare -- <reference folder> <after folder> <output folder>
```

Each folder must contain exactly one `ui-<state>.png` per state.

Exit codes:

- `1` if a state is missing, a state has more than one `ui-<state>*.png`, or two captures have different sizes;
- `2` on bad arguments;
- `0` otherwise.

The output folder receives:

- `report.txt`: one line per state with the R, G and B differences and `within` or `ABOVE` the 2.0 threshold;
- `compare-<state>.png`: reference and after side by side at half size.

## Metric

Mean absolute difference per R, G, B channel, on 8-bit sRGB values (0-255), over the whole image. One "level" is 1 on that scale.

Animated areas differ between two runs of the same build:

- logo bounce;
- rotating 3D car preview;
- race timer.

Judge those areas on the sheets.

## Reference captures

References come from a capture run of the plan's T0.2 commit, before any screen change. Regenerate them in any session with:

1. `git worktree add <folder> <sha>`
2. `git submodule update --init --recursive` inside the worktree
3. build
4. `--capture-ui-screens`

Both runs must use the same display settings (same back buffer size).
