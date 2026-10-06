# ADR-0008: RacingGameCasaEngine's title screen and car selection reproduce the original XNA screens as captured

- **Status**: Accepted
- **Date**: 2026-10-06
- **Source**: this chantier: `ai-agent/tasks/rgce-title-car-selection-xna-look-tasks.md`, decisions D1 to D7 and proposals P2 to P10 (author answers and plan approval, 2026-10-06)

## Context

- The author asked for the title screen and the car selection to match the original game. The author supplied two video captures of the original.
- The captures and the original code kept in the repository (`git show 4f840a3^:RacingGame.Shared/…`, the MonoGame port) show two different builds:
  - The capture's main menu has six buttons; the code's has five.
  - Title screen: the code draws a 3D fly-over, a black bar at y 518 of 640 and no logo (`GameScreens/SplashScreen.cs:40-56`). The capture shows the menu background, the bouncing logo and the bar at y 350, 61 high.
  - Car selection: the code centres the carousel on (1.5, 0, 1) (`GameScreens/CarSelection.cs:162-168`), draws the arrows at x 35 and 636 (`:279-294`) and uses a palette of named colours (`RacingGameManager.cs:77-91`). The capture shows a centred carousel, arrows against the front platform and a hue-ring palette from orange to gold.
  - Everything else (bars, header, stat bars, colour squares, A and B buttons) matches within about 3 px once the capture is mapped to the 1024×640 layout.
- The colour chosen in the car selection is not rendered by CasaEngine's pipeline. `LegacyCarVisualFactory` sets `LitDiffuseMaterial.TintColor` and `TintStrength`, which no engine code or shader reads (`CasaEngine/CasaEngine/Framework/Materials/Runtime/LitDiffuseMaterial.cs:29-31`). Only the old car preview approximated it.
- ADR-0001 keeps the visual-state look of the code-built screens. ADR-0007 already replaced that rule for the main menu.

## Decision

- **Reference.** The title screen and the car selection take the original game as their reference. Where the author's captures and the repository's original code differ, the captures win. Positions that differ are measured on the captures in 1024×640 units; everything else keeps the code's formulas.
- **Layout.** As for the main menu (ADR-0007), a view model lays each screen out every frame in screen pixels with the original's 1024×640 formulas, rounding half to even.
- **Title screen.**
  - It shows the menu background at 0.85 opacity and the bouncing logo.
  - A black bar at 0.683 opacity spans (0, 350, 1024, 61).
  - The "Press START to continue." sprite of `headers.png` is centred at (512, 380), 26 units high, and blinks as in the original: hidden for 0.375 s, then shown for 0.75 s.
  - A click anywhere, Space, Escape or gamepad Start leave it, as in the original, and so do Enter and A, as in the main menu. Leaving plays ScreenBack.
- **Car selection.**
  - It keeps the original's bar, header, six stat bars with their formulas, tinted colour squares with the selected one enlarged, animated arrows and A and B buttons with their hover outline.
  - Its texts use the original GameFont, converted to a CasaEngine bitmap font and scaled at draw time like the original's text.
  - Its palette is the capture's hue ring, also used for the race car.
  - Input follows the original, with Enter also confirming. Left and Right turn the carousel, Up and Down change the colour, and the mouse uses the original's click zones and colour squares. The Highlight, ScreenClick and ScreenBack sounds of the original play through CasaEngine's audio (ADR-0003).
- **Carousel.**
  - Three cars on three `CarSelectionPlate` platforms are rendered by CasaEngine's pipeline, in a preview world drawn into a window-sized render target. An MGUI image shows that target over the bar and the sprites, under the texts.
  - Each car spins at 1/3.9 rad/s, and the carousel turns at 5 rad/s. The front car is the selected one.
  - The geometry and the camera are fitted to the capture at 4:3. The vertical field of view is kept at other aspects, and the arrows follow the front platform's edge.
- **Car paint.** `LegacyCarVisualFactory` paints the car's diffuse texture on the CPU with the original formula, `rgb = lerp(rgb, colour, texture alpha)`, alpha set to 255. The race and the carousel share this texture.
- **Background.** Worlds other than the race clear to black, as the original did, instead of cornflower blue.
- **Automation.** Under `--capture-ui-screens`, the blink, the spin and the arrow swing are pinned, so that two runs give the same image.

## Consequences

- The title screen and the car selection no longer match the code-built screens; new capture references are taken for them. The other menus change only by their black background.
- The chosen colour now shows on the race car, which it never did in RacingGameCasaEngine.
- The car selection shows the original's speeds (288, 275 and 242 mph), not the catalogue's 240 mph for the third car. The race keeps the catalogue's car profiles.
- Known differences with the captures:
  - the original's post-processing (warm tint, glow and streaks of `PostScreenMenu.fx`) is not reproduced, so "Press START" stays golden instead of cream;
  - the three cars wear the same colour, as in the repository's code; the capture's differently coloured rear cars cannot be made from the repository's textures;
  - the original's soft contact shadow is replaced by the engine's shadow map;
  - the 3D fly-over behind the menus, the web address pill, the 60-second return to the title and the original mouse cursor are not reproduced.
- The other screens keep the visual-state parity of ADR-0001.
