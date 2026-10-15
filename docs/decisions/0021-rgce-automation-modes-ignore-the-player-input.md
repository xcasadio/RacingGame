# ADR-0021: RacingGameCasaEngine's automation modes ignore the player input

- **Status**: Accepted; completed by ADR-0022
- **Date**: 2026-10-15
- **Source**: this chantier: `ai-agent/tasks/rgce-automation-input-tasks.md`, decisions D1 to D4 (author approval, 2026-10-15), from open point O3 of `ai-agent/tasks/rgce-race-finished-capture-tasks.md`

## Context

- RacingGameCasaEngine has six automation modes that create the game: `--smoke-frontend`, `--capture-ui-screens`, `--capture-track-audit`, `--capture-car-profile-audit`, `--capture-car-top-speed-audit` and `--export-track-runtime-scene` (`RacingGameCasaEngine/Bootstrap/RaceLaunchOptions.cs`, `RacingGameCasaEngine/Program.cs:22-29`). Their validators drive the screens through the `…ForAutomation` methods of `RaceFrontEndFlow`, and the two car audits drive the car through `ForcedControlInput`. None of them reads the player input.
- Some `--capture-ui-screens` runs timed out at the race-finished step: 1 of 7 on 2026-10-09, 2 of 9 on 2026-10-15. A probe in a scratch copy of the game showed the cause, in two runs on 2026-10-15. A real left click in another application (Discord, Firefox), outside the game window, closed the race-finished panel. One of these clicks came while the game window was inactive.
  - The race-finished panel closes on any left click, with no position or focus test (`RacingGameCasaEngine/Screens/RaceHudScreen.cs:98-116`).
  - CasaEngine's `MonoGameWindowInputSource.GetMouseState` has no focus test (`CasaEngine/CasaEngine/Framework/Input/MonoGameWindowInputSource.cs:67-71`). MonoGame DesktopGL 3.8.5.1 reads the mouse buttons with `Sdl.Mouse.GetGlobalState`, that is, those of the whole desktop (decompiled `Mouse.PlatformGetState`).
  - `Game.IsActive` stays true while the window has never had the focus, which was the case in 18 of the 19 probe runs (decompiled `GamePlatform.BeforeInitialize` and `SdlGamePlatform`).
- Other paths also read the player directly:
  - every front-end screen, the race driving, the pause key and the debug keys F1 to F5 read the input component;
  - MGUI reads the gamepad from MonoGame itself, for its focus navigation (`CasaEngine/MGUI/MGUI.Shared/Input/GamePad/GamePadTracker.cs:100`);
  - the Options screen subscribed to `Window.TextInput` for the player name (`RacingGameCasaEngine/Screens/OptionsScreen.cs`).

## Decision

- While an automation mode runs, no real input reaches the game: mouse, keyboard, gamepad and typed text. `RaceLaunchOptions.IsAutomation` covers the six modes. `--verify-legacy-import-profile` exits before the game is created and is not concerned.
- This is done once for the whole process, in RacingGameCasaEngine only, without changing CasaEngine (`RacingGameCasaEngine/Bootstrap/AutomationPlayerInput.cs`):
  - before the game is created, `Program` sets `EngineRuntimeContext.WindowInputSource` to a source that always returns `WindowInputSnapshot.Empty`: position (0, 0) and no button (`Program.cs:81-84`). The input component and MGUI's mouse and keyboard read this source;
  - it also wraps `EngineRuntimeContext.UIViewRuntimeFactory`, so that every `UIRoot` created has `Desktop.InputTracker.GamePad.Enabled` set to false;
  - after `base.Initialize()`, the game gives the input component empty keyboard, mouse and gamepad providers (`RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs:202-208`);
  - the Options screen does not subscribe to `Window.TextInput` when `RacingGameCasaEngineGame.IgnoresPlayerInput` is true (`RacingGameCasaEngine/Screens/OptionsScreen.cs:106`).
- Normal play, with no automation option, is unchanged.

## Consequences

- A click or a gamepad button from the author no longer disturbs a validation run, whether or not the game window has the focus. The race-finished timeouts caused by real clicks are gone. 14 probe runs on the fixed code held the race-finished panel for 20 s: in 5 of them, 19 real clicks came during the panel, 12 of them in 3 runs on the final code, and none closed it. In one run, the author's gamepad sent Start twice and B during the panel, and it stayed open.
- The keyboard, the typed text and the events addressed to the game window are covered by reading the code, without runtime evidence: in the runs after the fix, the game window was never in the foreground.
- The captures do not change. One `--capture-ui-screens` run on the fixed code is pixel-identical to the references. The others stay within the threshold, with the known cadence variants (help, car-selection, track-selection, race-hud, options, main-menu). These variants remain with no input at all, so the cursor does not cause them.
- An automation run cannot be steered by hand any more: no debug camera (F1), no manual screenshot (F2), no pause on Escape. Closing the window still ends the process.
- A new screen or component that reads the input component, or MGUI's mouse and keyboard, is covered automatically. Code that reads MonoGame or the window directly (`Mouse.GetState`, `Keyboard.GetState`, `GamePad.GetState`, window events) is not: it must check `IgnoresPlayerInput` itself, as the Options screen does.
- `RaceLaunchOptions.IsAutomation` is a hand-written list: a new automation mode that creates the game must be added to it, or it keeps the player input.
- Only the input is cut. The gamepad vibration (`RaceGamepadVibration`) still drives a connected pad, and the window state still acts on a run: MonoGame sleeps 20 ms per frame while the window is inactive (`InactiveSleepTime`), which changes the cadence, and the window can be resized. These are outside this decision (open point O5 of the plan).
- In normal play, CasaEngine still sees the mouse buttons of the whole desktop: a click in another application can close the race-finished panel, the Splash or the Help screen. That fix belongs in CasaEngine (open point O2 of the plan) and is not part of this decision.
