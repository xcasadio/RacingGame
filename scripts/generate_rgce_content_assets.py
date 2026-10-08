"""Catalogue the content of RacingGameCasaEngine that the UI generator does not own, and port the legacy sounds.

Owned entries (plan ai-agent/tasks/rgce-editor-project-tasks.md, T3.3 and T3.4):
- Sound.<Wave> and Sound.<Wave>Wave for the 22 RacingGame waves not ported before: the wave is copied from
  RacingGame/Content/Audio/Waves under its XACT wave name, without spaces (Content/Audio/<Wave>.wav), and its .sound is
  written with the XACT volume (10^(dB/2000), as the six sounds ported before), bus Sfx (Music for the three musics,
  streamed), looped when XACT loops it (the gear loops and the musics);
- Texture.<stem> for every .png and .dds of Content/Textures that no catalogue entry names yet;
- Data.LandscapeHeights for Content/LandscapeHeights.data, Data.<stem> for Content/Models/*.CombiModel;
- ModelSource.<stem> for every Content/Models/*.gltf (its .bin buffer is a sidecar of the .gltf, not catalogued);
- World.<path> for every Content/Worlds/**/*.world that no entry names yet (T4.6): the path is relative to Worlds, without
  the extension, with "/" replaced by "." (Worlds/Test/TrackBeginnerGenerators.world is World.Test.TrackBeginnerGenerators).
  The "id" written in the .world must be the id of its entry: the script reports a world whose id differs.

Catalogue contract, as scripts/generate_rgce_ui_assets.py: the script updates its own entries in place, appends new
ones at the end in a stable order, leaves every other entry untouched, and keeps the file format (2-space indent,
CRLF, no final newline; the .sound files keep the LF of the six sounds ported before). Ids are uuid5 of the asset name in the same namespace as the UI generator, so a second run
changes nothing.

Usage: python scripts/generate_rgce_content_assets.py [--check]
--check lists the files of Content that no catalogue entry names, outside the sidecar and project files, and exits 1 if
there is any.
"""
import argparse
import json
import pathlib
import shutil
import sys
import uuid

REPO = pathlib.Path(__file__).resolve().parent.parent
CONTENT = REPO / "RacingGameCasaEngine" / "Content"
LEGACY_WAVES = REPO / "RacingGame" / "Content" / "Audio" / "Waves"
CATALOG = CONTENT / "AssetInfos.json"
NAMESPACE = uuid.uuid5(uuid.NAMESPACE_URL, "racinggame-casaengine/ui-assets")

# (wave name, legacy file, XACT sound volume in 1/100 dB, looped, music)
# Source: the XACT project RacingGame/Content/Audio/RacingGame.xap (cue and sound tables).
SOUNDS = [
    ("BrakeCurveMajor", "BrakeCurveMajor.wav", 0, False, False),
    ("BrakeCurveMinor", "BrakeCurveMinor.wav", -100, False, False),
    ("BrakeMajor", "BrakeMajor.wav", 0, False, False),
    ("BrakeMinor", "BrakeMinor.wav", -100, False, False),
    ("CarCrashMinor", "CarCrashMinor.wav", -100, False, False),
    ("CarCrashTotal", "CarCrashTotal.wav", -100, False, False),
    ("CarLose", "CarLose.wav", -100, False, False),
    ("CheckpointBetter", "CheckpointBetter.wav", -800, False, False),
    ("CheckpointWorse", "CheckpointWorse.wav", -800, False, False),
    ("Gear1", "Gear1.wav", 0, True, False),
    ("Gear1ToGear2", "Gear1ToGear2.wav", 0, False, False),
    ("Gear2", "Gear2.wav", 0, True, False),
    ("Gear2ToGear3", "Gear2ToGear3.wav", 0, False, False),
    ("Gear3", "Gear3.wav", 0, True, False),
    ("Gear3ToGear4", "Gear3ToGear4.wav", 0, False, False),
    ("Gear4", "Gear4.wav", 0, True, False),
    ("Gear4ToGear5", "Gear4ToGear5.wav", 0, False, False),
    ("Gear5", "Gear5.wav", 0, True, False),
    ("Victory", "Victory.wav", -100, False, False),
    ("RacingGameGameMusic1", "RacingGame Game Music 1.wav", 0, True, True),
    ("RacingGameGameMusic2", "RacingGame Game Music 2.wav", 0, True, True),
    ("RacingGameMenuMusic2", "RacingGame Menu Music 2.wav", 0, True, True),
]

# Files of Content that are not assets of their own (checked by --check).
PROJECT_FILES = {"RacingGameCasaEngine.json", "AssetInfos.json", "RacingGame.Gameplay.dll", "RacingGame.Gameplay.pdb"}
SIDECAR_SUFFIXES = (".design.json", ".xaml", ".bin")


def asset_id(name):
    return str(uuid.uuid5(NAMESPACE, name))


def write_bytes_if_changed(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists() or path.read_bytes() != data:
        path.write_bytes(data)


def write_json(path, value, newline="\r\n"):
    write_bytes_if_changed(path, json.dumps(value, indent=2, ensure_ascii=False).replace("\n", newline).encode("utf-8"))


def catalogued_files(entries):
    return {entry.get("file_name", "").replace("\\", "/").lower() for entry in entries}


def sound_entries():
    owned = []
    for wave, legacy_file, decibels, looped, music in SOUNDS:
        wave_file = f"Audio/{wave}.wav"
        write_bytes_if_changed(CONTENT / wave_file, (LEGACY_WAVES / legacy_file).read_bytes())
        wave_name = f"Sound.{wave}Wave"
        sound_name = f"Sound.{wave}"
        sound_file = f"Audio/{wave}.sound"
        write_json(CONTENT / sound_file, {
            "id": asset_id(sound_name),
            "name": sound_name,
            "audio_file_asset_id": asset_id(wave_name),
            "volume": round(10 ** (decibels / 2000.0), 3),
            "pitch": 0.0,
            "is_looped": looped,
            "bus_name": "Music" if music else "Sfx",
            "is_streaming": music,
        }, newline="\n")
        owned.append({"id": asset_id(wave_name), "name": wave_name, "file_name": wave_file, "asset_type": "wav"})
        owned.append({"id": asset_id(sound_name), "name": sound_name, "file_name": sound_file, "asset_type": "sound"})
    return owned


def file_entries(already_catalogued, owned_names):
    owned = []

    def add(name, file_name, asset_type):
        if file_name.lower() in already_catalogued and name not in owned_names:
            return
        owned.append({"id": asset_id(name), "name": name, "file_name": file_name, "asset_type": asset_type})

    for path in sorted((CONTENT / "Textures").iterdir(), key=lambda p: p.name.lower()):
        if path.suffix.lower() in (".png", ".dds"):
            add(f"Texture.{path.stem}", f"Textures/{path.name}", path.suffix.lower()[1:])
    add("Data.LandscapeHeights", "LandscapeHeights.data", "data")
    for path in sorted((CONTENT / "Models").glob("*.CombiModel"), key=lambda p: p.name.lower()):
        add(f"Data.{path.stem}", f"Models/{path.name}", "combimodel")
    for path in sorted((CONTENT / "Models").glob("*.gltf"), key=lambda p: p.name.lower()):
        add(f"ModelSource.{path.stem}", f"Models/{path.name}", "gltf")
    for path in sorted((CONTENT / "Worlds").rglob("*.world"), key=lambda p: p.as_posix().lower()):
        relative = path.relative_to(CONTENT / "Worlds").with_suffix("").as_posix()
        add("World." + relative.replace("/", "."), path.relative_to(CONTENT).as_posix(), "world")
    return owned


def check(entries):
    known = catalogued_files(entries)
    missing = []
    for path in sorted(CONTENT.rglob("*")):
        if not path.is_file():
            continue
        relative = path.relative_to(CONTENT).as_posix()
        if relative.startswith(".casaeditor/") or relative in PROJECT_FILES or relative.lower().endswith(SIDECAR_SUFFIXES):
            continue
        if relative.lower() not in known:
            missing.append(relative)
    for relative in missing:
        print(f"not catalogued: {relative}")
    print(f"{len(missing)} files of Content are not catalogued")
    return 1 if missing else 0


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    arguments = parser.parse_args()

    catalog = json.loads(CATALOG.read_bytes().decode("utf-8-sig"))
    entries = catalog["asset_infos"]
    if arguments.check:
        return check(entries)

    owned = sound_entries()
    owned_names = {entry["name"] for entry in owned}
    # A file is owned when its entry is ours from a previous run, or when no entry names it yet.
    previously_owned = {entry["name"] for entry in entries if entry.get("name", "").startswith(("Texture.", "Data.", "ModelSource.", "World."))}
    already_catalogued = catalogued_files(entry for entry in entries if entry.get("name") not in previously_owned)
    owned += file_entries(already_catalogued, owned_names)

    for entry in owned:
        existing = next((index for index, current in enumerate(entries) if current.get("name") == entry["name"]), None)
        if existing is None:
            entries.append(entry)
        else:
            entries[existing] = entry

    ids = [entry["id"] for entry in entries]
    names = [entry.get("name") for entry in entries]
    if len(set(ids)) != len(ids) or len(set(names)) != len(names):
        raise SystemExit("duplicate catalogue id or name")

    for entry in owned:
        if entry["asset_type"] == "world":
            world_id = json.loads((CONTENT / entry["file_name"]).read_bytes().decode("utf-8-sig")).get("id")
            if world_id != entry["id"]:
                raise SystemExit(f"{entry['file_name']} has id {world_id}, its catalogue entry {entry['name']} has id {entry['id']}")

    write_json(CATALOG, catalog)
    print(f"{len(SOUNDS)} sounds, {len(owned)} owned catalogue entries, {len(entries)} entries in the catalogue")
    return 0


if __name__ == "__main__":
    sys.exit(main())
