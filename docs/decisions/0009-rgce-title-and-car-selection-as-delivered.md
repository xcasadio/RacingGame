# ADR-0009: RacingGameCasaEngine's title screen and car selection reproduce the original XNA screens as captured, as delivered

- **Status**: Accepted
- **Date**: 2026-10-06
- **Source**: this chantier: `ai-agent/tasks/rgce-title-car-selection-xna-look-tasks.md`, decisions D1 to D7, proposals P2 to P10, validation notes T1.1 to T4.3 and open point O5. It supersedes ADR-0008.

## Context

- ADR-0008 recorded the decision before the work. The delivery differs from it on four points:
  - **Car paint.** The painted car texture keeps its alpha, where ADR-0008 set it to 255. The alpha is RacingGame's paint mask, and both RacingGame's shader (`NormalMapping.fx`, `bump * spec * specularColor * diffusePixel.a`) and CasaEngine's (`Lighting.fxh`, `AddSpecular`) scale the specular by it. With alpha 255 the whole car body turned glossy in the race.
  - **Carousel geometry.** Only the carousel's centre and radius are fitted to the capture; RacingGame's camera is kept.
  - **Carousel view.** A screen cannot add or remove a view: CasaEngine's screen changes walk the views and push each screen into every view's UI (`docs/mgui-gaps-from-rgce-xaml-screens.md`, C1).
  - **Carousel transparency.** The render target needs a pass that makes the drawn geometry opaque, since the lit shaders write the texture's alpha (C3).
- An ADR is never rewritten, so this record restates the decision as delivered.

## Decision

- **Reference.** The title screen and the car selection take the original game as their reference. Where the author's captures and the repository's original code differ, the captures win. Positions that differ are measured on the captures in 1024×640 units; everything else keeps the code's formulas.
- **Layout.** As for the main menu (ADR-0007), a view model lays each screen out every frame in screen pixels with the original's 1024×640 formulas, rounding half to even.
- **Title screen.**
  - It shows the menu background at 0.85 opacity and the bouncing logo.
  - A black bar at 0.683 opacity spans (0, 350, 1024, 61).
  - The "Press START to continue." sprite of `headers.png` is centred at (512, 380), 26 units high, and blinks as in the original: hidden for 0.375 s, then shown for 0.75 s.
  - A click anywhere, Space, Escape or gamepad Start leave it, as in the original, and so do Enter and A, as in the main menu. Leaving plays ScreenBack.
- **Car selection.**
  - It keeps the original's bar, header, six stat bars with their formulas, tinted colour squares with the selected one enlarged, swinging arrows and A and B buttons with their hover outline.
  - Its palette is the capture's hue ring, also used for the race car.
  - Its texts use the original GameFont, as a CasaEngine bitmap font. MGUI draws a bitmap font at its own size, so each text is scaled at draw time to RacingGame's (width / 1400, height / 1050), and does not wrap or clip.
  - Input follows the original, with Enter also confirming. The Highlight, ScreenClick and ScreenBack sounds of the original play through CasaEngine's audio (ADR-0003), at the volumes of RacingGame's XACT project.
- **Carousel.**
  - Three cars on three `CarSelectionPlate` platforms are rendered by CasaEngine's pipeline, in a world of their own, into a render target as large as the screen. An MGUI image shows it over the bar and the sprites, under the texts.
  - It uses RacingGame's camera, field of view and light, converted from RacingGame's Z-up world by the rotation (x, y, z) → (x, z, −y). The carousel's centre is (−0.2, −0.3, 2.78) and its radius 6.74, fitted to the capture at 4:3; RacingGame's code had (1.5, 1, 0) and 5.
  - Each car spins at 1/3.9 rad/s. The carousel turns at 5 rad/s from car 1 to the selected car, which stands in front.
  - The vertical field of view is kept at other aspects. The arrows are centred on the front plate's projected edges.
  - Shadows follow the Shadows option, cast by the cars only. Only the plate's material reflects the sky.
  - The game owns the carousel. The car selection asks for it every update, and the game adds or enables its view after its own update, adds it again after a world load, and removes its UI runtime.
  - A view pipeline writes alpha 1 wherever geometry was drawn, so the cars are opaque over the screen.
- **Car paint.** `LegacyCarVisualFactory` builds one model per car and paints the selected colour into the car texture on the CPU, with RacingGame's `rgb = lerp(rgb, colour, texture alpha)`, alpha kept. The race and the carousel share it.
- **Background.** Worlds other than the race clear to black, as the original did.
- **Automation.** Under `--capture-ui-screens`, the blink, the spin, the carousel's turn and the arrow swing are pinned, so that two runs give the same image.

## Consequences

- The title screen and the car selection no longer match the code-built screens; new capture references are taken for them. The other menus change only by their black background.
- The chosen colour now shows on the race car, which it never did in RacingGameCasaEngine; only the paint mask changes, the body's shading stays.
- The car selection shows the original's speeds (288, 275 and 242 mph), not the catalogue's 240 mph for the third car. The race keeps the catalogue's car profiles.
- A colour change repaints three 2048×2048 textures, about 10 to 16 ms each. The carousel keeps its world, render target and painted textures for the game's life once opened.
- Known differences with the captures:
  - the original's post-processing (warm tint, glow and streaks of `PostScreenMenu.fx`) is not reproduced, so "Press START" stays golden instead of cream, and the background is not the capture's blurred sepia;
  - the three cars wear the same colour, as in the repository's code; the capture's differently coloured rear cars cannot be made from the repository's textures;
  - the engine's shadow map gives harder shadow edges than the original's blurred one;
  - at 16:9 the carousel is narrower than in the 4:3 capture;
  - the 3D fly-over behind the menus, the web address pill, the 60-second return to the title and the original mouse cursor are not reproduced.
- The other screens keep the visual-state parity of ADR-0001.
