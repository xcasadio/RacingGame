# ADR-0005: RacingGameCasaEngine's main menu reproduces the original XNA menu with MGUI brushes

- **Status**: Superseded by ADR-0006
- **Date**: 2026-10-05
- **Source**: this chantier: `ai-agent/tasks/rgce-main-menu-xna-look-tasks.md`, decisions D1 to D5 and proposals P2 to P5 (author answers and plan approval, 2026-10-05)

## Context

- ADR-0001 keeps the hover, focus and selection looks of the screens exactly as the code-built screens drew them (`docs/decisions/0001-rgce-screens-as-casaengine-screen-assets.md:36`).
- The code-built main menu, ported from RacingGame's MGUI view, differs from the original XNA menu (`git show 4f840a3^:RacingGame.Shared/GameScreens/MainMenu.cs`).
  - Its band is higher and smaller.
  - Its buttons are flat-coloured nested plates, with a label under every button.
  - It shows no selection until a key is pressed.
- The author asked for the original's layout and look: a lower, taller band; buttons with a vertical gradient face and gradient borders drawn with MGUI's gradients; an outline around the selected button.
- MGUI fills rounded shapes with four-corner gradients (`MGUI/MGUI.Core/UI/Brushes/FillBrushes/MGGradientFillBrush.cs:63-91`). It has no rounded gradient border: a non-solid fill on a rounded border falls back to rectangular strips (`MGUI/MGUI.Core/UI/Brushes/BorderBrushes/MGUniformBorderBrush.cs:103-107`).

## Decision

- **Reference.** The main menu takes the original XNA menu as its reference, not the code-built menu: this replaces, for the main menu only, the visual-state parity of ADR-0001.
- **Content.** Five buttons (Play, Highscores, Options, Help, Quit), without the Credits button of the button atlas, and a single label under the selected button, as in the original.
- **Layout.** The original's proportions at every resolution: a black band at 0.683 opacity from 43.75 % to 73.75 % of the height, square buttons whose sizes are 108 and 132 units of a 640-unit height, centred in a row.
- **Drawing.** The buttons are drawn with MGUI brushes, not with the atlas button images:
  - a vertical gradient face;
  - a flat grey rim;
  - an inner bevel made of concentric solid bands, since MGUI has no rounded gradient border;
  - black icon glyphs extracted from the atlas on a transparent background.
- **Selection.** The screen owns the selected button, always shown:
  - Play is selected when the menu opens;
  - keyboard, gamepad and mouse hover move the selection;
  - the selected button grows from 108 to 132 units in 0.5 s and wears the original's orange ring;
  - the other buttons are drawn at 0.75 opacity.

## Consequences

- The main menu no longer matches the code-built menu pixel for pixel; new capture references are taken for it.
- The menu follows the screen resolution instead of being correct only at 1920×1080.
- The bevel is uniform around the button, where the original's is darker at the bottom; the gap is reported in `docs/mgui-gaps-from-rgce-xaml-screens.md`.
- The original's post-processing (warm tint and bloom of `PostScreenMenu.fx`) is not reproduced.
- The other screens keep the visual-state parity of ADR-0001.
