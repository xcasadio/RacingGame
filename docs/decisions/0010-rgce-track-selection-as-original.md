# ADR-0010: RacingGameCasaEngine's track selection reproduces the original XNA screen

- **Status**: Accepted
- **Date**: 2026-10-06
- **Source**: this chantier: `ai-agent/tasks/rgce-track-selection-xna-look-tasks.md`, decisions D1 to D6 and proposals P2 to P7 (author answers and plan approval, 2026-10-06)

## Context

- The author asked for the track selection to match the original game, as the title screen and the car selection now do (ADR-0009), and supplied a video capture of it.
- Unlike those screens, the capture and the repository's original code (`git show 4f840a3^:RacingGame.Shared/GameScreens/TrackSelection.cs`) agree. Once the capture is mapped to the 1024×640 layout, the header, cards, outline, label, bar and A and B buttons fall on the code's formulas within one unit. The capture's other differences are post-processing and video overlays.
- The original screen:
  - draws the menu background, a black bar over (0, 220, 1024, 280), the "SELECT TRACK" header of `headers.png` and three track cards of `buttons.png`;
  - draws the selected card in full with the orange outline sprite over it and its name sprite below; the others are dimmed to 0.753 opacity;
  - grows the selected card from 108 to 132 units of width in 0.5 s, starting every opening from card 1 grown;
  - has Advanced as its default track;
  - uses Left and Right with a ButtonClick sound, mouse hover and click, A or Space to start and Escape, B or Back to go back;
  - shows no text.
- ADR-0001 keeps the visual-state look of the code-built screens; ADR-0007 and ADR-0009 replaced that rule for the main menu, the title screen and the car selection.

## Decision

- **Reference.** The track selection takes the original game as its reference, as ADR-0009 sets it out; here the capture and the code agree.
- **Layout.** A view model lays the screen out every frame in screen pixels with the original's 1024×640 formulas, rounding half to even:
  - the black bar at 0.683 opacity;
  - the header with RacingGame's 1600×1200 helper;
  - each card sized from its height and the sprite's aspect, interpolated between the inactive and the active size, and placed in a row whose start is computed from the row's resting width;
  - the outline and the label on the selected card, the label stretched to the card's width;
  - the A and B buttons as in the car selection.
  An unselected card is one image at 0.753 opacity.
- **Animation.** Each card's size moves towards 1 for the selected card and 0 for the others at 2 per second. Every opening starts from card 1 grown, as in the original. The capture automation shows the cards at rest.
- **Input and sounds.**
  - Left and Right select the previous and next card, wrapping, with RacingGame's ButtonClick sound, added as a `.sound` asset at the volume of RacingGame's XACT project (ADR-0003).
  - Hovering a card selects it once the mouse has moved since the last key; clicking a card selects it and starts the race.
  - A, Space, Enter or a click on A start the race with ScreenClick; Escape, B, Back or a click on B go back to the car selection with ScreenBack.
  - Highlight plays when the mouse enters a card or a bottom button.
  - No button takes MGUI's focus.
- **Default track.** Advanced, as in the original. The automation that relied on the old default sets Beginner explicitly.

## Consequences

- The track selection no longer matches the code-built screen; new capture references are taken for it.
- `LegacyMenuUiTheme.ApplySpriteButtonState`, used only by the old track selection, is removed.
- The ButtonClick sound is available to the other menus of the original that use it (Options, Highscores), which do not use it yet.
- Known differences with the original:
  - after a race RacingGameCasaEngine returns to the main menu, where the original returned to the track selection (author's choice);
  - the original's post-processing (warm tint, glow on the selected card), the 3D scene behind the menus, the original mouse cursor and the capture's web address pill are not reproduced, as for the other screens (ADR-0009).
- The other screens keep the visual-state parity of ADR-0001.
