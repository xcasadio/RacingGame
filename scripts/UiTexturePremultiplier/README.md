# UiTexturePremultiplier

Writes premultiplied-alpha copies of RacingGameCasaEngine's UI images (ADR-0012). MGUI draws its images with premultiplied blending, CasaEngine loads PNG files with straight alpha, and RacingGame premultiplied its textures when it built them. Without this step, the semi-transparent edge pixels of the UI images add their full colour and show as light fringes.

## Images

| Source | Output |
|---|---|
| `RacingGameCasaEngine/Content/Textures/background.png` | `RacingGameCasaEngine/Content/UI/Textures/background.png` |
| `RacingGameCasaEngine/Content/Textures/buttons.png` | `RacingGameCasaEngine/Content/UI/Textures/buttons.png` |
| `RacingGameCasaEngine/Content/Textures/ingame.png` | `RacingGameCasaEngine/Content/UI/Textures/ingame.png` |
| `RacingGameCasaEngine/Content/Textures/headers.png` | `RacingGameCasaEngine/Content/UI/Textures/headers.png` |
| `RacingGameCasaEngine/Content/Textures/ColorSelection.png` | `RacingGameCasaEngine/Content/UI/Textures/ColorSelection.png` |
| `RacingGameCasaEngine/Content/Textures/OptionsScreenWindows.png` | `RacingGameCasaEngine/Content/UI/Textures/OptionsScreenWindows.png` |
| `RacingGame/Content/Textures/GameFont.png` | `RacingGameCasaEngine/Content/UI/Fonts/GameFont.png` |

Each texel becomes (round(r·a/255), round(g·a/255), round(b·a/255), a): opaque texels keep their colour. The sources are left as they are.

## Usage

```
dotnet run --project scripts/UiTexturePremultiplier -- <repository root>
```

Then `python scripts/generate_rgce_ui_assets.py` and `python scripts/generate_rgce_gamefont.py` catalogue the images.

Exit codes:
- 0: images written;
- 2: bad arguments or a missing source.

A second run writes the same files, bit for bit.
