# MenuIconExtractor

Extracts the black icon glyphs of RacingGame's main-menu buttons from `buttons.png` onto a transparent background. RacingGameCasaEngine draws its main-menu buttons with MGUI brushes and lays these glyphs over them (plan `ai-agent/tasks/rgce-main-menu-xna-look-tasks.md`, P3; ADR-0007).

## Method

In each 212×212 button cell of `buttons.png` (Play, Highscores, Options, Help, Quit, in that order), the icon is pure black, antialiased over a face that is a vertical grey gradient, flat along each row.

- The face grey of a row is the lightest pixel of that row inside the face core (x and y 22 to 189, clear of the rim and the inner bevel).
- A pixel of luminance L over a face grey F is black at coverage (F − L) / F.
- Only the glyph's box is kept: its pixels at least half covered, widened by 3 px of antialiasing. The face also darkens at the rounded inner corners, which are not ink.

The output is a 1060×212 PNG: the five cells side by side, each keeping its full 212×212 frame, transparent outside the glyph. A glyph stretched over a button of the cell's size therefore lands where the original icon was.

## Usage

```
dotnet run --project scripts/MenuIconExtractor -- RacingGameCasaEngine/Content/Textures/buttons.png RacingGameCasaEngine/Content/UI/Sprites/Ui.Menu.Glyphs.png
```

Then `python scripts/generate_rgce_ui_assets.py` catalogues the image and its `Ui.Menu.Glyph*` sprites.

Exit codes:
- 0: image written;
- 2: bad arguments or input, or a cell without a glyph.

A second run writes the same file, bit for bit.
