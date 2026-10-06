# ADR-0012: RacingGameCasaEngine's UI images are premultiplied, except the menu background

- **Status**: Accepted
- **Date**: 2026-10-06
- **Source**: the author's feedback of 2026-10-06 on light pixels at the rounded corners of the selected track card, and the author's answers on the fix (plan `ai-agent/tasks/rgce-track-selection-xna-look-tasks.md`, phase 4, P10)

## Context

- MGUI draws its images with premultiplied blending (`BlendState.AlphaBlend`: `CasaEngine/MGUI/MGUI.Shared/Rendering/DrawSettings.cs:82`, `CasaEngine/CasaEngine/Framework/UI/Backend/MonoGame/CasaMonoGameRenderInterop.cs:19-20`).
- CasaEngine loads PNG files as they are, with straight alpha (`Texture2D.FromStream`, `CasaEngine/CasaEngine/Framework/Assets/Loaders/Texture2DLoader.cs:12`). RacingGame premultiplied its textures when it built them (`RacingGame/Content/Content.mgcb:1607`, `PremultiplyAlpha=True`).
- Drawn as they were, the semi-transparent edge pixels of the UI images added their full colour. At the selected track card's corner, the card's grey rim and the outline's orange edge added up to (255, 255, ~162), lighter than the outline.
- Every UI atlas had the defect on its antialiased edges: logo, headers, cards, labels, buttons, arrows, colour squares, HUD and the GameFont page. The main menu's glyphs are black, so they did not.
- The menu background (`background.png`, 0,0,1024,640) is mostly semi-transparent (mean alpha 220, 20 % opaque). Premultiplied, it made every menu much darker: RacingGame drew it over a bright 3D scene, which RacingGameCasaEngine does not draw. The author chose to keep its current brightness.

## Decision

- `scripts/UiTexturePremultiplier` writes premultiplied copies of the UI images under `RacingGameCasaEngine/Content/UI/Textures/`: `background.png`, `buttons.png`, `ingame.png`, `headers.png`, `ColorSelection.png` and `OptionsScreenWindows.png`. It also writes the GameFont page, `UI/Fonts/GameFont.png`.
  - Each texel becomes (round(r·a/255), round(g·a/255), round(b·a/255), a); opaque texels keep their colour.
  - The tool is deterministic.
- The UI `.texture` assets use these copies (`scripts/generate_rgce_ui_assets.py`), the logo included.
- The menu background sprites (`Ui.Menu.Background`, `Ui.Menu.SplashBackground`) keep the plain copy, `Textures/background.png`. Drawn at 0.85 opacity over black, its straight colours keep the brightness the original reached with its 3D scene.
- The plain copies under `Content/Textures/` stay as they are; `scripts/MenuIconExtractor` reads `buttons.png` there.

## Consequences

- The light fringes are gone from every UI image's edges; opaque areas do not change. Captures change by less than 0.3 % of their pixels, all darker, at the edges.
- Any new UI image copied from RacingGame goes through `scripts/UiTexturePremultiplier` before being catalogued.
- The menu background's own semi-transparent texels are still drawn brighter than their premultiplied value, by the author's choice.
- The catalogue entries `MenuBackground` (still used by the background) and `MenuButtons` (no longer referenced) are not owned by the generator and stay in place.
