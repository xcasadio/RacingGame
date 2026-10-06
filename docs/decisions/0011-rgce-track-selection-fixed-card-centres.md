# ADR-0011: RacingGameCasaEngine's track selection reproduces the original XNA screen, with cards growing around fixed centres

- **Status**: Accepted
- **Date**: 2026-10-06
- **Source**: the author's feedback of 2026-10-06 on the delivered track selection (plan `ai-agent/tasks/rgce-track-selection-xna-look-tasks.md`, phase 4, P9). It supersedes ADR-0010.

## Context

- ADR-0010 recorded the track selection as a reproduction of the original screen, cards included: the original places each card after the previous one's current width (`git show 4f840a3^:RacingGame.Shared/GameScreens/TrackSelection.cs:79, 217`), so a card that grows pushes the next ones.
- The author asked for the cards to keep their position and grow around their centre without disturbing the others.
- An ADR is never rewritten, so this record restates ADR-0010 with that change.

## Decision

- **Reference.** The track selection takes the original game as its reference, as ADR-0009 sets it out; here the capture and the code agree. One exception, at the author's request: the cards do not slide (see Layout).
- **Layout.** A view model lays the screen out every frame in screen pixels with the original's 1024×640 formulas, rounding half to even:
  - the black bar at 0.683 opacity;
  - the header with RacingGame's 1600×1200 helper;
  - each card sized from its height and the sprite's aspect, interpolated between the inactive and the active size, and grown around a fixed centre: the centre it has in the original's resting row with the middle card selected (x 698, 960.5 and 1223 at 1920×1080), vertically as the original already did;
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
  - the cards keep fixed centres and grow around them, where the original slid them along the row as the selected card grew (`TrackSelection.cs:79`), at the author's request;
  - after a race RacingGameCasaEngine returns to the main menu, where the original returned to the track selection (author's choice);
  - the original's post-processing (warm tint, glow on the selected card), the 3D scene behind the menus, the original mouse cursor and the capture's web address pill are not reproduced, as for the other screens (ADR-0009).
- The other screens keep the visual-state parity of ADR-0001.
