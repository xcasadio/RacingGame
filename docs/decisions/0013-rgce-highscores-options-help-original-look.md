# ADR-0013: RacingGameCasaEngine's Highscores, Options and Help screens reproduce the original XNA screens

- **Status**: Superseded by ADR-0014
- **Date**: 2026-10-06
- **Source**: the author's request of 2026-10-06 to harmonise the Highscores, Options and Help screens, and the author's answers (plan `ai-agent/tasks/rgce-other-screens-xna-look-tasks.md`, D1-D6, P2-P9). It supersedes ADR-0004.

## Context

- The main menu, the title screen, the car selection and the track selection already reproduce the original XNA screens (ADR-0007, ADR-0009, ADR-0011). Highscores, Options and Help still show an MGUI form in a stretched 1280×720 window: an orange title text, MGUI's default font and controls, a restyled "Back" button, MGUI focus, no Escape, B or Back handling and no sound (`RacingGameCasaEngine/Screens/{Highscores,Options,Help}Screen.cs`).
- The original screens (`git show 4f840a3^:RacingGame.Shared/GameScreens/{Highscores,Options,Help}.cs`) are:
  - **Highscores**:
    - a black bar;
    - the HIGHSCORES header;
    - three level tabs and ten rows of rank, name and time in RacingGame's bitmap font;
    - the BACK button.
  - **Options**:
    - the OPTIONS header;
    - a panel image with baked labels (`OptionsScreenWindows.png`) on a 1024×768 base;
    - a name typed without focus, five resolution slots and graphics toggles redrawn tinted when on, Show FPS and Gamepad Vibration as round buttons with a text, three sliders and a moving arrow;
    - the BACK button.
    - Escape, B, Back and BACK all apply and save the settings, and the volumes apply every frame.
  - **Help**: the HELP header, one image (`HelpScreenWindows.png`, keyboard and mouse, gamepad and wheel pictograms) and the BACK button. Any click, Escape, B or Back closes it.
- No capture of these original screens is available, so the code is the reference (ADR-0009).
- ADR-0004 saves the settings only when the player leaves Options with its Back button.

## Decision

- **Reference.** The three screens take the original code as their reference, as ADR-0009 sets it out.
- **Layout.** A view model lays each screen out every frame in screen pixels with the original's formulas, rounding half to even, through a shared `LegacyScreenLayout` helper:
  - the 1024×640 base for the screen;
  - the 1024×768 base for the Options and Help panels;
  - the 1600×1200 base for the headers;
  - the 1400×1050 base for the bitmap-font texts.
- **Common.**
  - Background and bouncing logo as on the other menus.
  - The header image of the screen, and the BACK button grown and outlined on hover.
  - Highlight when the mouse enters a tested area, ScreenClick when the screen is shown, ScreenBack when it is left.
  - No MGUI focus, and a first-frame input guard.
- **Highscores.**
  - The black bar at 0.683 opacity.
  - The tabs, separator lines, rank, name and time columns in the original's layout and colours.
  - The data stays RacingGameCasaEngine's current catalogue (five times per track, shared with the HUD), shown in the original's `m:ss.cc` format; no persisted score table is added.
  - The screen opens on Advanced.
  - The original's inputs: Left and Right change the level with ButtonClick; Escape, B, Back, a click below the rows or on BACK leave.
- **Options.**
  - The original panel. The baked resolution labels are covered and redrawn with RacingGameCasaEngine's resolutions (1280×720 to 3840×2160, Auto).
  - The High Detail slot, removed from RacingGameCasaEngine, shows Vertical Sync.
  - Show FPS, Gamepad Vibration and Driving Mode (a "Simulation" toggle) are round buttons with a text.
  - The keyboard and gamepad arrow visits every row, where the original reached the sliders only. Enter or A toggles; Left and Right change the resolution or a slider. The player name is typed directly, without focus.
  - Every exit (Escape, B, Back, BACK) applies and saves the settings, and the volumes apply as they change. This supersedes ADR-0004's "only with its Back button".
- **Help.** The original image under the HELP header. RacingGameCasaEngine's text sections are removed.
- **Visual states.** ADR-0001's parity of visual states gives way for these three screens, as for the four screens before.
- **Assets.**
  - Header, panel, panel-area and round-button sprites are added through `scripts/generate_rgce_ui_assets.py`.
  - `HelpScreenWindows.png` is copied from RacingGame and premultiplied like the other UI images (ADR-0012).

## Consequences

- The three screens no longer match their code-built look; new capture references are taken for them.
- The settings are also saved when the player leaves Options with Escape, B or Back. The volumes change while the player adjusts them, before the settings are saved.
- `LegacyMenuUiTheme`, `RaceMenuTextButtonViewModel` and the Help text sections become unused and are removed.
- Known differences with the original:
  - the scores are not persisted nor fed by races;
  - Options has RacingGameCasaEngine's extra rows, and its arrow reaches every row;
  - the post-processing, the 3D scene behind the menus and the original mouse cursor are not reproduced (ADR-0009). Without the bright scene, the 80 % dark Options and Help panels look darker than in the original.
