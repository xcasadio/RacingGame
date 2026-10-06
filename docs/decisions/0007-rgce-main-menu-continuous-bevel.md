# ADR-0007: RacingGameCasaEngine's main menu reproduces the original XNA menu, with a continuous bevel

- **Status**: Accepted
- **Date**: 2026-10-06
- **Source**: the author's request of 2026-10-06 to fix three differences that ADR-0006 left: the four-step bevel, the dark grey glyphs of the unselected buttons and the face bottom darker than the art (plan `ai-agent/tasks/rgce-main-menu-xna-look-tasks.md`, note under T3.1). It supersedes ADR-0006.

## Context

- ADR-0006 recorded the main menu as delivered, with three known differences that the author asked to fix:
  - the bevel ramped in four steps;
  - the glyphs of the unselected buttons came out dark grey (35 to 41) instead of near black (8 to 15), because the 0.75 opacity applied to each layer;
  - the face bottom was about 8 levels darker than the art.
- MGUI still has no rounded gradient border, and `MGBandedBorderBrush` gives each band a whole number of pixels (`docs/mgui-gaps-from-rgce-xaml-screens.md`, M13 and M14).
- The art's bevel, measured at x or y = 106 in `buttons.png`, from the rim inwards: 87 to 246 at the top, 70 to 202 on the sides, 55 to 157 at the bottom. The face runs from 249 to 159.

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
  - the face is the button's background, a vertical grey gradient on the art's line (#F9F9F9 at its top, #9F9F9F at its bottom);
  - a flat grey rim;
  - an inner bevel made of one 1-pixel band per pixel of its thickness, each a docked border with the art's colour per side at that depth, darker at the bottom, because MGUI has no rounded gradient border. `RaceMainMenuButtonViewModel.BevelBrush` builds it for each thickness;
  - the black icon glyph of the atlas, extracted onto a transparent background by `scripts/MenuIconExtractor`;
  - for the selected button, the atlas's flat orange ring over the rim;
  - no hover tint.
- **Selection**, owned by the screen and always shown; the buttons do not take MGUI's focus.
  - Play is selected at the first opening. Later openings restore the last selected button, held in memory and not saved, as the original's menu did by staying under the other screens.
  - The keyboard, the D-pad, the left stick and the mouse hover move the selection.
  - Space, Enter, A or a click activate a button.
  - The selected button grows from 108 to 132 reference units in 0.5 s and wears the ring. The others are drawn at 0.75 opacity, their glyph at 0.93, so that its ink is as dark as in the original, where the whole button sprite was dimmed at once.

## Consequences

- The menu's rectangles match the original's to the pixel at every resolution tried (1920×1080, 1680×1050, 1280×720).
- At 1920×1080 the bevel, the face and the unselected glyphs match the art: the bevel follows the art's profile resampled to its thickness (87 to 246 at the top, 70 to 202 on the sides, 55 to 157 at the bottom), the face runs from 249 to 159, the glyph ink is about 10.
- Known differences with the original:
  - rounded edges are not antialiased;
  - an unselected button is dimmed layer by layer, so the face shows through its bevel, where the original showed only the band;
  - the stick threshold is 0.5 instead of 0.75;
  - a click activates on release;
  - Escape, Back and the 60-second return to the splash screen are not reproduced;
  - the original's post-processing (warm tint and bloom of `PostScreenMenu.fx`) is not reproduced.
- The other screens keep the visual-state parity of ADR-0001.
