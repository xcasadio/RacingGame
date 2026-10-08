"""Generate the UI image assets of RacingGameCasaEngine: one .sprite per atlas rectangle used by the screens, the
.texture wrappers they reference, and their catalogue entries in Content/AssetInfos.json.

The rectangles are those the code-built screens used (their atlas helper, removed since, and
Screens/RaceHudScreen.cs), plus the main menu's label rectangles of RacingGame's UIRenderer and its icon glyphs, which
scripts/MenuIconExtractor writes to Content/UI/Sprites/Ui.Menu.Glyphs.png. A sprite chain is png entry <- .texture
(texture_asset_id) <- .sprite (sprite_sheet_asset_id, location), as in CasaEngine's RPGDemo (Screens/MainHUD).

Catalogue contract (plan ai-agent/tasks/rgce-xaml-screens-tasks.md, P5): the script owns only the entries whose name
starts with "Ui."; it updates an owned entry in place, appends new ones at the end in table order, leaves every other
entry untouched and in place, and writes the file in its current format (2-space indent, keys id/name/file_name/
asset_type, CRLF, no final newline). Ids are uuid5 of the asset name, so a second run changes nothing.

Usage: python scripts/generate_rgce_ui_assets.py [--catalog <AssetInfos.json>] [--content <Content folder>]
"""
import argparse
import json
import pathlib
import uuid

REPO = pathlib.Path(__file__).resolve().parent.parent
NAMESPACE = uuid.uuid5(uuid.NAMESPACE_URL, "racinggame-casaengine/ui-assets")
SPRITE_FOLDER = "UI/Sprites"

# Image entries the sprites are cut from: (texture asset name, existing png catalogue name or None, png file_name).
# MGUI draws images with premultiplied blending, so the textures are the premultiplied copies that
# scripts/UiTexturePremultiplier writes under UI/Textures from the copies of RacingGame's textures (ADR-0012).
TEXTURES = [
    ("Ui.Menu.BackgroundTexture", "Ui.Menu.BackgroundImage", "UI/Textures/background.png"),
    # The menu background itself stays the plain copy: drawn at 0.85 opacity over black, its straight colours keep the
    # brightness the original reached with its 3D scene behind it, which RGCE does not draw (author's choice, ADR-0012).
    ("Ui.Menu.BackgroundPlainTexture", "MenuBackground", None),
    ("Ui.Menu.ButtonsTexture", "Ui.Menu.ButtonsImage", "UI/Textures/buttons.png"),
    ("Ui.Hud.IngameTexture", "Ui.Hud.IngameImage", "UI/Textures/ingame.png"),
    # Main-menu icon glyphs on a transparent background, written by scripts/MenuIconExtractor from buttons.png. They are
    # black, so their premultiplied form is the same image.
    ("Ui.Menu.GlyphsTexture", "Ui.Menu.GlyphsImage", "UI/Sprites/Ui.Menu.Glyphs.png"),
    # Title screen and car selection art of RacingGame (UIRenderer, CarSelection).
    ("Ui.Title.HeadersTexture", "Ui.Title.HeadersImage", "UI/Textures/headers.png"),
    ("Ui.CarSelection.ColorSelectionTexture", "Ui.CarSelection.ColorSelectionImage", "UI/Textures/ColorSelection.png"),
    ("Ui.CarSelection.OptionsWindowsTexture", "Ui.CarSelection.OptionsWindowsImage", "UI/Textures/OptionsScreenWindows.png"),
]

# (sprite asset name, texture asset name, x, y, w, h)
SPRITES = [
    # background.png (1024x1024): menu background and whole texture (plain), logo (premultiplied).
    ("Ui.Menu.Background", "Ui.Menu.BackgroundPlainTexture", 0, 0, 1024, 640),
    ("Ui.Menu.Logo", "Ui.Menu.BackgroundTexture", 0, 649, 1024, 374),
    ("Ui.Menu.SplashBackground", "Ui.Menu.BackgroundPlainTexture", 0, 0, 1024, 1024),
    # buttons.png (1024x1024): tracks, A/B buttons.
    # Main-menu icon glyphs (full 212x212 button frames, Ui.Menu.Glyphs.png) and the labels shown under the selected button
    # (buttons.png MenuText*GfxRect of RacingGame's UIRenderer).
    ("Ui.Menu.GlyphPlay", "Ui.Menu.GlyphsTexture", 0, 0, 212, 212),
    ("Ui.Menu.GlyphHighscores", "Ui.Menu.GlyphsTexture", 212, 0, 212, 212),
    ("Ui.Menu.GlyphOptions", "Ui.Menu.GlyphsTexture", 424, 0, 212, 212),
    ("Ui.Menu.GlyphHelp", "Ui.Menu.GlyphsTexture", 636, 0, 212, 212),
    ("Ui.Menu.GlyphQuit", "Ui.Menu.GlyphsTexture", 848, 0, 212, 212),
    ("Ui.Menu.LabelPlay", "Ui.Menu.ButtonsTexture", 0, 214, 212, 24),
    ("Ui.Menu.LabelHighscores", "Ui.Menu.ButtonsTexture", 212, 214, 212, 24),
    ("Ui.Menu.LabelOptions", "Ui.Menu.ButtonsTexture", 424, 214, 212, 24),
    ("Ui.Menu.LabelHelp", "Ui.Menu.ButtonsTexture", 636, 214, 212, 24),
    ("Ui.Menu.LabelQuit", "Ui.Menu.ButtonsTexture", 212, 454, 212, 24),
    ("Ui.Track.Beginner", "Ui.Menu.ButtonsTexture", 0, 480, 212, 352),
    ("Ui.Track.Advanced", "Ui.Menu.ButtonsTexture", 212, 480, 212, 352),
    ("Ui.Track.Expert", "Ui.Menu.ButtonsTexture", 424, 480, 212, 352),
    # Track selection (RacingGame's TrackSelection): the orange outline of the selected card (TrackButtonSelectionGfxRect)
    # and the name under it (TrackText*GfxRect).
    ("Ui.Track.Highlight", "Ui.Menu.ButtonsTexture", 636, 480, 212, 352),
    ("Ui.Track.LabelBeginner", "Ui.Menu.ButtonsTexture", 0, 834, 212, 24),
    ("Ui.Track.LabelAdvanced", "Ui.Menu.ButtonsTexture", 212, 834, 212, 24),
    ("Ui.Track.LabelExpert", "Ui.Menu.ButtonsTexture", 424, 834, 212, 24),
    ("Ui.Button.Select", "Ui.Menu.ButtonsTexture", 0, 872, 212, 92),
    ("Ui.Button.Back", "Ui.Menu.ButtonsTexture", 212, 872, 212, 92),
    # Orange outline drawn over a hovered A or B button (UIRenderer BottomButtonSelectionGfxRect) and the car selection's
    # arrow (SelectionArrowGfxRect, pointing right).
    ("Ui.Button.Highlight", "Ui.Menu.ButtonsTexture", 424, 240, 212, 92),
    ("Ui.CarSelection.Arrow", "Ui.Menu.ButtonsTexture", 874, 426, 53, 39),
    # Round button of the Options screen: slider handle, Show FPS and vibration toggles (SelectionRadioButtonGfxRect).
    ("Ui.Button.Radio", "Ui.Menu.ButtonsTexture", 935, 427, 39, 39),
    # headers.png (1024x512): "Press START to continue." (PressStartGfxRect), "CHOOSE YOUR CAR" (HeaderChooseCarGfxRect) and
    # "SELECT TRACK" (HeaderSelectTrackGfxRect).
    ("Ui.Title.PressStart", "Ui.Title.HeadersTexture", 2, 1, 631, 45),
    ("Ui.CarSelection.Header", "Ui.Title.HeadersTexture", 0, 212, 512, 100),
    ("Ui.TrackSelection.Header", "Ui.Title.HeadersTexture", 0, 312, 512, 100),
    # "OPTIONS", "HELP" and "HIGHSCORES" (HeaderOptionsGfxRect, HeaderHelpGfxRect, HeaderHighscoresGfxRect).
    ("Ui.Options.Header", "Ui.Title.HeadersTexture", 512, 212, 512, 100),
    ("Ui.Help.Header", "Ui.Title.HeadersTexture", 512, 312, 512, 100),
    ("Ui.Highscores.Header", "Ui.Title.HeadersTexture", 0, 412, 512, 100),
    # ColorSelection.png (64x64): the colour square, tinted per colour; OptionsScreenWindows.png: the car property bar.
    ("Ui.CarSelection.Swatch", "Ui.CarSelection.ColorSelectionTexture", 0, 0, 64, 64),
    ("Ui.CarSelection.StatBar", "Ui.CarSelection.OptionsWindowsTexture", 372, 297, 472, 6),
    # The Options panel and the areas the Options screen redraws over it: the five resolution slots and the four graphics
    # toggles (RacingGame's Options.cs, Resolution*GfxRect, FullscreenGfxRect, PostScreenEffectsGfxRect, ShadowsGfxRect,
    # HighDetailGfxRect).
    ("Ui.Options.Panel", "Ui.CarSelection.OptionsWindowsTexture", 0, 0, 1024, 512),
    ("Ui.Options.Resolution0", "Ui.CarSelection.OptionsWindowsTexture", 339, 112, 98, 32),
    ("Ui.Options.Resolution1", "Ui.CarSelection.OptionsWindowsTexture", 454, 112, 98, 32),
    ("Ui.Options.Resolution2", "Ui.CarSelection.OptionsWindowsTexture", 575, 112, 108, 32),
    ("Ui.Options.Resolution3", "Ui.CarSelection.OptionsWindowsTexture", 704, 112, 116, 32),
    ("Ui.Options.Resolution4", "Ui.CarSelection.OptionsWindowsTexture", 838, 112, 69, 32),
    ("Ui.Options.Fullscreen", "Ui.CarSelection.OptionsWindowsTexture", 339, 182, 105, 36),
    ("Ui.Options.PostScreenEffects", "Ui.CarSelection.OptionsWindowsTexture", 339, 226, 206, 36),
    ("Ui.Options.Shadows", "Ui.CarSelection.OptionsWindowsTexture", 616, 226, 90, 36),
    ("Ui.Options.HighDetail", "Ui.CarSelection.OptionsWindowsTexture", 784, 226, 120, 36),
    # ingame.png (1024x512): race HUD panels, tachometer, needle and the 10 digit glyphs.
    ("Ui.Hud.Laps", "Ui.Hud.IngameTexture", 381, 132, 222, 160),
    ("Ui.Hud.Tacho", "Ui.Hud.IngameTexture", 0, 0, 343, 341),
    ("Ui.Hud.Needle", "Ui.Hud.IngameTexture", 347, 0, 28, 186),
    ("Ui.Hud.CurrentAndBest", "Ui.Hud.IngameTexture", 381, 2, 342, 128),
    ("Ui.Hud.TrackName", "Ui.Hud.IngameTexture", 726, 2, 282, 62),
    ("Ui.Hud.Best5Row", "Ui.Hud.IngameTexture", 726, 66, 282, 62),
] + [
    (f"Ui.Hud.Digit{index}", "Ui.Hud.IngameTexture", x, 342, 78 if index == 3 else 80, 133)
    for index, x in enumerate([2, 84, 167, 247, 330, 411, 495, 578, 659, 749])
]

SAMPLER_STATE = {
    "texture_filter": "Linear",
    "address_u": "Clamp",
    "address_v": "Clamp",
    "address_w": "Clamp",
    "border_color": {"r": 255, "g": 255, "b": 255, "a": 255},
    "max_anisotropy": 4,
    "max_mip_level": 0,
    "mip_map_level_of_detail_bias": 0.0,
    "comparison_function": "Never",
    "filter_mode": "Default",
}


def asset_id(name):
    return str(uuid.uuid5(NAMESPACE, name))


def write_json(path, value):
    text = json.dumps(value, indent=2, ensure_ascii=False).replace("\n", "\r\n")
    path.parent.mkdir(parents=True, exist_ok=True)
    data = text.encode("utf-8")
    if not path.exists() or path.read_bytes() != data:
        path.write_bytes(data)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--catalog", default=str(REPO / "RacingGameCasaEngine" / "Content" / "AssetInfos.json"))
    parser.add_argument("--content", default=str(REPO / "RacingGameCasaEngine" / "Content"))
    arguments = parser.parse_args()

    catalog_path = pathlib.Path(arguments.catalog)
    content = pathlib.Path(arguments.content)
    catalog = json.loads(catalog_path.read_bytes().decode("utf-8-sig"))
    entries = catalog["asset_infos"]
    by_name = {entry.get("name"): entry for entry in entries}

    owned = []
    image_ids = {}
    for texture_name, image_name, image_file in TEXTURES:
        if image_file is not None:
            owned.append({"id": asset_id(image_name), "name": image_name, "file_name": image_file, "asset_type": "png"})
            image_ids[texture_name] = asset_id(image_name)
        elif image_name in by_name:
            image_ids[texture_name] = by_name[image_name]["id"]
        else:
            raise SystemExit(f"catalogue entry '{image_name}' not found in {catalog_path}")

    for texture_name, _, _ in TEXTURES:
        file_name = f"{SPRITE_FOLDER}/{texture_name}.texture"
        write_json(content / file_name, {"id": asset_id(texture_name), "name": texture_name,
                                         "texture_asset_id": image_ids[texture_name], "sampler_state": SAMPLER_STATE})
        owned.append({"id": asset_id(texture_name), "name": texture_name, "file_name": file_name, "asset_type": "texture"})

    for sprite_name, texture_name, x, y, w, h in SPRITES:
        file_name = f"{SPRITE_FOLDER}/{sprite_name}.sprite"
        write_json(content / file_name, {"id": asset_id(sprite_name), "name": sprite_name,
                                         "sprite_sheet_asset_id": asset_id(texture_name),
                                         "location": {"x": x, "y": y, "w": w, "h": h}, "hotspot": {"x": 0, "y": 0},
                                         "collisions": [], "sockets": []})
        owned.append({"id": asset_id(sprite_name), "name": sprite_name, "file_name": file_name, "asset_type": "sprite"})

    for entry in owned:
        existing = next((index for index, current in enumerate(entries) if current.get("name") == entry["name"]), None)
        if existing is None:
            entries.append(entry)
        else:
            entries[existing] = entry

    write_json(catalog_path, catalog)
    print(f"{len(TEXTURES)} textures, {len(SPRITES)} sprites, {len(owned)} owned catalogue entries")


if __name__ == "__main__":
    main()
