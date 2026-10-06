"""Generate RacingGame's GameFont as a CasaEngine bitmap font: a BMFont text file (Content/UI/Fonts/GameFont.fnt) next to
a copy of RacingGame/Content/Textures/GameFont.png, and their catalogue entries in Content/AssetInfos.json.

The glyphs are RacingGame's TextureFont.CharRects (git show 4f840a3^:RacingGame.Shared/Graphics/TextureFont.cs:41-144),
characters 32 to 126. TextureFont draws each glyph from (x, y + 1, width, 36) of the 256x256 texture and advances by
the rectangle's height field, which holds the used width. Its 5-unit raise of every glyph (SubRenderHeight) is left to
the screens' layout: the .fnt keeps yoffset 0.

The page is catalogued with the '\\'-separated file name CasaEngine's font loader resolves it by (ADR-0036 of
CasaEngine, BitmapFontDescriptor.CatalogFileNameNextTo). Catalogue contract: as scripts/generate_rgce_ui_assets.py,
for the entries whose name starts with "Font.GameFont"; ids are uuid5 of the asset name, so a second run changes nothing.

Usage: python scripts/generate_rgce_gamefont.py [--catalog <AssetInfos.json>] [--content <Content folder>]
"""
import argparse
import json
import pathlib
import shutil
import uuid

REPO = pathlib.Path(__file__).resolve().parent.parent
NAMESPACE = uuid.uuid5(uuid.NAMESPACE_URL, "racinggame-casaengine/ui-assets")
SOURCE_TEXTURE = REPO / "RacingGame" / "Content" / "Textures" / "GameFont.png"
FONT_FOLDER = "UI/Fonts"
FACE = "GameFont"
FONT_HEIGHT = 36
TEXTURE_SIZE = 256

# TextureFont.CharRects: (x, y, width, advance) for characters 32 (space) to 126 (~).
CHAR_RECTS = [
    (0, 0, 1, 8), (1, 0, 11, 10), (12, 0, 14, 13), (26, 0, 20, 18), (46, 0, 20, 18), (66, 0, 24, 22), (90, 0, 25, 23),
    (115, 0, 8, 7), (124, 0, 10, 9), (136, 0, 10, 9), (146, 0, 20, 18), (166, 0, 20, 18), (186, 0, 10, 8),
    (196, 0, 10, 9), (207, 0, 10, 8), (217, 0, 18, 16), (235, 0, 20, 19),
    (0, 36, 20, 18), (20, 36, 20, 18), (40, 36, 20, 18), (60, 36, 21, 19), (81, 36, 20, 18), (101, 36, 20, 18),
    (121, 36, 20, 18), (141, 36, 20, 18), (161, 36, 20, 18), (181, 36, 10, 8), (191, 36, 10, 8), (201, 36, 20, 18),
    (221, 36, 20, 18),
    (0, 72, 20, 18), (20, 72, 19, 17), (39, 72, 26, 24), (65, 72, 22, 20), (87, 72, 22, 20), (109, 72, 22, 20),
    (131, 72, 23, 21), (154, 72, 20, 18), (174, 72, 19, 17), (193, 72, 23, 21), (216, 72, 23, 21), (239, 72, 11, 10),
    (0, 108, 15, 13), (15, 108, 22, 20), (37, 108, 19, 17), (56, 108, 29, 26), (85, 108, 23, 21), (108, 108, 24, 22),
    (132, 108, 22, 20), (154, 108, 24, 22), (178, 108, 24, 22), (202, 108, 21, 19), (223, 108, 17, 15),
    (0, 144, 22, 20), (22, 144, 22, 20), (44, 144, 30, 28), (74, 144, 22, 20), (96, 144, 20, 18), (116, 144, 20, 18),
    (136, 144, 10, 9), (146, 144, 18, 16), (167, 144, 10, 9), (177, 144, 17, 16), (194, 144, 17, 16),
    (211, 144, 17, 16), (228, 144, 20, 18),
    (0, 180, 20, 18), (20, 180, 18, 16), (38, 180, 20, 18), (58, 180, 20, 18), (79, 180, 14, 12), (93, 180, 20, 18),
    (114, 180, 19, 18), (133, 180, 11, 10), (145, 180, 11, 10), (156, 180, 20, 18), (176, 180, 11, 9),
    (187, 180, 29, 27), (216, 180, 20, 18), (236, 180, 20, 19),
    (0, 216, 20, 18), (20, 216, 20, 18), (40, 216, 13, 12), (53, 216, 17, 16), (70, 216, 14, 11), (84, 216, 19, 18),
    (104, 216, 17, 16), (122, 216, 25, 23), (148, 216, 19, 17), (168, 216, 18, 16), (186, 216, 16, 15),
    (203, 216, 10, 9), (214, 216, 12, 11), (227, 216, 10, 9), (237, 216, 18, 17),
]


def asset_id(name):
    return str(uuid.uuid5(NAMESPACE, name))


def write_bytes(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists() or path.read_bytes() != data:
        path.write_bytes(data)


def font_text():
    assert len(CHAR_RECTS) == 126 - 32 + 1
    lines = [
        f'info face="{FACE}" size={FONT_HEIGHT} bold=0 italic=0 charset="" unicode=1 stretchH=100 smooth=1 aa=1 '
        f'padding=0,0,0,0 spacing=0,0 outline=0',
        f'common lineHeight={FONT_HEIGHT} base={FONT_HEIGHT} scaleW={TEXTURE_SIZE} scaleH={TEXTURE_SIZE} pages=1 '
        f'packed=0 alphaChnl=0 redChnl=0 greenChnl=0 blueChnl=0',
        f'page id=0 file="{FACE}.png"',
        f"chars count={len(CHAR_RECTS)}",
    ]
    for index, (x, y, width, advance) in enumerate(CHAR_RECTS):
        # TextureFont reads FontHeight rows from y + 1.
        assert y + 1 + FONT_HEIGHT <= TEXTURE_SIZE
        lines.append(f"char id={32 + index} x={x} y={y + 1} width={width} height={FONT_HEIGHT} xoffset=0 yoffset=0 "
                     f"xadvance={advance} page=0 chnl=15")
    return ("\r\n".join(lines) + "\r\n").encode("ascii")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--catalog", default=str(REPO / "RacingGameCasaEngine" / "Content" / "AssetInfos.json"))
    parser.add_argument("--content", default=str(REPO / "RacingGameCasaEngine" / "Content"))
    arguments = parser.parse_args()

    content = pathlib.Path(arguments.content)
    page = content / FONT_FOLDER / f"{FACE}.png"
    page.parent.mkdir(parents=True, exist_ok=True)
    if not page.exists() or page.read_bytes() != SOURCE_TEXTURE.read_bytes():
        shutil.copyfile(SOURCE_TEXTURE, page)
    write_bytes(content / FONT_FOLDER / f"{FACE}.fnt", font_text())

    folder = FONT_FOLDER.replace("/", "\\")
    owned = [
        {"id": asset_id("Font.GameFontImage"), "name": "Font.GameFontImage", "file_name": f"{folder}\\{FACE}.png",
         "asset_type": "png"},
        {"id": asset_id("Font.GameFont"), "name": "Font.GameFont", "file_name": f"{folder}\\{FACE}.fnt",
         "asset_type": "fnt"},
    ]

    catalog_path = pathlib.Path(arguments.catalog)
    catalog = json.loads(catalog_path.read_bytes().decode("utf-8-sig"))
    entries = catalog["asset_infos"]
    for entry in owned:
        existing = next((index for index, current in enumerate(entries) if current.get("name") == entry["name"]), None)
        if existing is None:
            entries.append(entry)
        else:
            entries[existing] = entry

    text = json.dumps(catalog, indent=2, ensure_ascii=False).replace("\n", "\r\n")
    write_bytes(catalog_path, text.encode("utf-8"))
    print(f"{len(CHAR_RECTS)} glyphs, {len(owned)} owned catalogue entries")


if __name__ == "__main__":
    main()
