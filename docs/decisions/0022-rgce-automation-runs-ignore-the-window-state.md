# ADR-0022: RacingGameCasaEngine's automation runs ignore the window state

- **Status**: Accepted
- **Date**: 2026-10-15
- **Source**: this chantier: `ai-agent/tasks/rgce-automation-input-tasks.md`, decision D5 (author answer to open point O5, 2026-10-15). Completes ADR-0021.

## Context

- ADR-0021 keeps the player input out of the six automation modes. The window state still acted on a run.
- MonoGame DesktopGL 3.8.5.1 sleeps `Game.InactiveSleepTime` (0.02 s by default) at every tick while the window is inactive (decompiled `Game.Tick`), and no code of the repository or of CasaEngine changed it. The game window loses the focus during runs, by itself or when the author works elsewhere. The cadence then changes. With the window forced inactive during a `--capture-ui-screens` run, a probe measured 38 to 39 draws per second for about 60 updates per second in the race. A capture reads an older draw, and the update that draw shows depends on that ratio (`ai-agent/tasks/rgce-race-finished-capture-tasks.md`, established cause of O2). The small capture variants of its open point O4 probably come from the same mechanism; that was not established.
- RacingGameCasaEngine let the user resize the window (`GameSettings.ProjectSettings.AllowUserResizing = true` in `RacingGameCasaEngineGame.Initialize`). CasaEngine applies that setting to the window once, at initialization (`CasaEngine/CasaEngine/Framework/Application/CasaEngineGame.cs:482`).

## Decision

While an automation mode runs (`RaceLaunchOptions.IsAutomation`), in `RacingGameCasaEngineGame.Initialize`:

- `AllowUserResizing` is false, so the user can neither resize nor maximize the window;
- `InactiveSleepTime` is zero, so an inactive window keeps the cadence of an active one.

Normal play is unchanged: the window stays resizable and MonoGame keeps its default inactive sleep.

## Consequences

- A lost focus no longer slows a run down. With the window forced inactive, the probe measured 60 draws per second for 60 updates per second, against 38 to 39 before.
- The capture size set by a validator still applies. Changing the resolution by code still works: `--smoke-frontend` changes it and checks it.
- An inactive window during a run uses as much CPU and GPU as an active one.
- Minimizing or moving the window is not blocked, and its effect on a run was not measured. Closing the window ends the run.
- The display rate of the machine still sets the cadence, so the small capture variants of O4 may remain.
