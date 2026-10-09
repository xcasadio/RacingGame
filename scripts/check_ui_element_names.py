#!/usr/bin/env python3
"""Checks the element names of the MGUI screens of RacingGameCasaEngine.

Every element of RacingGameCasaEngine/Content/UI/Screens/*/*.xaml must have a Name, whose prefix is the one of its type
(Window wnd, OverlayPanel/StackPanel/Border pnl, Image img, TextBlock lbl, Button btn, followed by an upper-case letter),
and unique in its file (MGUI rejects duplicates in a window, ordinal comparison). Property elements (<X.Y>) and
RenderTransform are not elements of the MGUI tree and are skipped.

The script prints each fault with the element type and the line of its start tag, and exits with code 1 when there is any.

Usage: python scripts/check_ui_element_names.py [screens folder]
"""
import glob
import os
import sys
from xml.parsers import expat

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCREENS = os.path.join(ROOT, "RacingGameCasaEngine", "Content", "UI", "Screens")
PREFIXES = {
    "Window": "wnd",
    "OverlayPanel": "pnl",
    "StackPanel": "pnl",
    "Border": "pnl",
    "Image": "img",
    "TextBlock": "lbl",
    "Button": "btn",
}
NOT_ELEMENTS = {"RenderTransform"}


def elements(path):
    """Yields (type, name or None, line) for each MGUI element of the file, in document order."""
    found = []
    parser = expat.ParserCreate(namespace_separator="}")

    def start(tag, attrs):
        kind = tag.split("}")[-1]
        if "." not in kind and kind not in NOT_ELEMENTS:
            found.append((kind, attrs.get("Name"), parser.CurrentLineNumber))

    parser.StartElementHandler = start
    with open(path, "rb") as handle:
        parser.Parse(handle.read(), True)
    return found


def check_file(path):
    """Returns (element count, list of fault messages) for a screen file."""
    faults = []
    seen = {}
    count = 0
    try:
        shown = os.path.relpath(path, ROOT).replace(os.sep, "/")
    except ValueError:  # another drive than the repository
        shown = path
    for kind, name, line in elements(path):
        count += 1
        where = f"{shown}:{line} {kind}"
        if not name:
            faults.append(f"{where}: no Name")
            continue
        prefix = PREFIXES.get(kind)
        if prefix is None:
            faults.append(f"{where} '{name}': no prefix is defined for this element type")
        elif not (name.startswith(prefix) and len(name) > len(prefix) and name[len(prefix)].isupper()):
            faults.append(f"{where} '{name}': the prefix of {kind} must be '{prefix}' followed by an upper-case letter")
        if name in seen:
            faults.append(f"{where} '{name}': duplicate of the element at line {seen[name]}")
        else:
            seen[name] = line
    return count, faults


def main():
    folder = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else SCREENS
    files = sorted(glob.glob(os.path.join(folder, "*", "*.xaml")))
    if not files:
        print(f"SELF-CHECK FAILED: no screen found under {folder}")
        return 2
    total = 0
    faults = []
    for path in files:
        count, found = check_file(path)
        total += count
        faults.extend(found)
    for fault in faults:
        print(fault)
    print(f"{len(files)} screen(s), {total} element(s), {len(faults)} fault(s)")
    return 1 if faults else 0


if __name__ == "__main__":
    sys.exit(main())
