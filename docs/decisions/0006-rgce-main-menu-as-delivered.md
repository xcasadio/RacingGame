# ADR-0006: RacingGameCasaEngine's main menu reproduces the original XNA menu, as delivered

- **Status**: Accepted
- **Date**: 2026-10-05
- **Source**: this chantier: `ai-agent/tasks/rgce-main-menu-xna-look-tasks.md`, decisions D1 to D5, proposals P2 to P5, validation notes T2.1 and T2.2, and the independent verification of T3.1. It supersedes ADR-0005.

## Context

- ADR-0005 recorded the decision before the work. One of its statements was wrong: the code-built menu's fixed values were the original's proportions at 1280×720, so it was right at that resolution only, not at 1920×1080. The delivered menu also differs from ADR-0005 on three points:
  - the bevel keeps one tone per side, darker at the bottom, where ADR-0005 announced a uniform one;
  - the menu reopens on the last selected button, where ADR-0005 said Play is selected when the menu opens;
  - the layout is computed by the view model in screen pixels, which ADR-0005 did not state.
- An ADR is never rewritten, so this record restates the decision as delivered.
- ADR-0001 keeps the visual-state look of the code-built screens (`docs/decisions/0001-rgce-screens-as-casaengine-screen-assets.md:36`). This record replaces that rule for the main menu only.

## Decision

- **Reference.** The main menu takes RacingGame's XNA menu as its reference (`git show 4f840a3^:RacingGame.Shared/GameScreens/MainMenu.cs`), not the code-built menu.
- **Content.**
  - Five buttons (Play, Highscores, Options, Help, Quit), without the Credits button of the button atlas.
  - A single label under the selected button, cut from the atlas (« START RACE », « HIGHSCORES », « OPTIONS », « HELP », « QUIT »).
- **Layout.** `RaceMainMenuViewModel` lays the band and the buttons out every frame, in screen pixels, with the original's formulas.
  - It uses a 1024×640 reference layout: the band and the button sizes follow the height, the gaps follow the width.
  - Rounding is half to even, as in the original.
  - The result matches the original's rectangles at any resolution.
- **Drawing**, with MGUI brushes, not with the atlas button images:
  - the face is the button's background, a vertical grey gradient;
  - a flat grey rim;
  - an inner bevel made of four concentric bands, each a docked border with one solid colour per side, darker at the bottom, because MGUI has no rounded gradient border;
  - the black icon glyph of the atlas, extracted onto a transparent background by `scripts/MenuIconExtractor`;
  - for the selected button, the atlas's flat orange ring over the rim;
  - no hover tint.
- **Selection**, owned by the screen and always shown; the buttons do not take MGUI's focus.
  - Play is selected at the first opening. Later openings restore the last selected button, held in memory and not saved, as the original's menu did by staying under the other screens.
  - The keyboard, the D-pad, the left stick and the mouse hover move the selection.
  - Space, Enter, A or a click activate a button.
  - The selected button grows from 108 to 132 reference units in 0.5 s and wears the ring. The others are drawn at 0.75 opacity.

## Consequences

- The main menu no longer matches the code-built menu pixel for pixel; new capture references are taken for it.
- The menu's rectangles match the original's to the pixel at every resolution tried (1920×1080, 1680×1050, 1280×720).
- Known differences with the original, reported in the plan's end report:
  - the bevel ramps in four steps (M13 and M14 of `docs/mgui-gaps-from-rgce-xaml-screens.md`);
  - rounded edges are not antialiased;
  - unselected glyphs come out dark grey rather than near black, because the opacity applies to each layer and not to the button as a whole;
  - the stick threshold is 0.5 instead of 0.75;
  - a click activates on release;
  - Escape, Back and the 60-second return to the splash screen are not reproduced;
  - the original's post-processing (warm tint and bloom of `PostScreenMenu.fx`) is not reproduced.
- The other screens keep the visual-state parity of ADR-0001.
