# ADR-0014: RacingGameCasaEngine's Help and Options keep their previous layout in the menus' style; Highscores reproduces the original screen

- **Status**: Accepted
- **Date**: 2026-10-06
- **Source**: the author's feedback of 2026-10-06 on the delivered Help and Options screens, and the author's answers (plan `ai-agent/tasks/rgce-other-screens-xna-look-tasks.md`, phase 4, D7-D9, P11-P14). It supersedes ADR-0013.

## Context

- ADR-0013 recorded Highscores, Options and Help as reproductions of the original XNA screens. The delivery:
  - showed RacingGame's help image instead of RacingGameCasaEngine's text sections;
  - showed RacingGame's options panel with RacingGameCasaEngine's rows drawn in its style.
- The author then asked to keep the previous layout of Help and Options, the one before ADR-0013, but in the style of the other screens. Highscores stays as delivered.
- The previous layouts:
  - **Help**: a band, a title and six text sections (a title and two lines each) in a scrolling area;
  - **Options**: a column of labels and a column of MGUI controls: name text box, resolution and driving-mode buttons, six check boxes, three sliders with their values.
- An ADR is never rewritten, so this record restates ADR-0013 with that change.

## Decision

- **Highscores** reproduces the original screen as ADR-0013 records it. It uses RacingGameCasaEngine's highscore catalogue, opens on Advanced and has the original's inputs and sounds.
- **Help** keeps its previous arrangement in the menus' style:
  - the band, the HELP header image, the B BACK button;
  - the four sections meant for the player (Race Controls, Steering, Camera, Race Flow), texts unchanged, in RacingGame's bitmap font: titles in orange at full size, lines in white at 0.75;
  - all visible without a scroll bar.
  The two sections that were development notes are dropped. Escape, B, Back or a click close it.
- **Options** keeps its previous arrangement, a column of labels and a column of controls in the same order, drawn with the original's visual vocabulary:
  - labels in RacingGame's bitmap font;
  - the name in the original's name field with a blinking "|";
  - resolutions and driving modes as labels, amber when chosen;
  - the original's round button, orange when on and grey when off, for each option;
  - sliders made of the original's track with a round handle, and the value written next to them;
  - the original's selection arrow in front of the selected row's label.
- **Options behaviour** is the one ADR-0013 delivered:
  - a selection arrow over every row; Enter or A switch an option; Left and Right change the resolution or the driving mode, or move a slider by 10;
  - the name typed directly; the volumes applied as they change;
  - menu sounds;
  - every exit (Escape, B, Back, BACK) applies and saves the settings. ADR-0004's "only with its Back button" stays superseded.
- **Common to the three screens**:
  - layout in screen pixels every frame with RacingGame's formulas (`LegacyScreenLayout`);
  - ScreenClick when shown, Highlight when the mouse enters a tested area, ScreenBack when leaving;
  - no MGUI focus, and a first-frame input guard;
  - the gamepad stick acts past 0.5, as on the other RacingGameCasaEngine screens, where RacingGame used 0.75.
- **Assets**:
  - RacingGame's help image and the options panel's area sprites are removed;
  - the options panel's name field becomes a sprite;
  - the slider track reuses the car selection's stat bar, which is the same art.

## Consequences

- Help and Options look like the other menus without following the original's arrangement; new capture references are taken for them.
- Help no longer shows the development notes.
- Known differences with the original:
  - Help shows text sections instead of the original's image;
  - Options has RacingGameCasaEngine's rows and arrangement instead of the original's panel;
  - the stick threshold is 0.5;
  - the post-processing, the 3D scene behind the menus and the original mouse cursor are not reproduced (ADR-0009).
