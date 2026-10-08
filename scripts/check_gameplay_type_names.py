#!/usr/bin/env python3
"""Checks that no top-level type of RacingGame.Gameplay has the simple name of a type of CasaEngine, MGUI or the editor.

The editor loads the gameplay assembly next to the engine assemblies and resolves types by name, so a gameplay type that
shares its simple name with an engine type is ambiguous. The script collects the simple names of the top-level types
(class, struct, enum, interface, record, delegate) declared in RacingGame.Gameplay/**/*.cs and in the CasaEngine projects,
skipping bin and obj, prints the collisions and exits with code 1 when there is any.

Usage: python scripts/check_gameplay_type_names.py
"""
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GAMEPLAY = ["RacingGame.Gameplay"]
ENGINE = [
    "CasaEngine/CasaEngine",
    "CasaEngine/MGUI",
    "CasaEngine/CasaEngine.Editor",
    "CasaEngine/CasaEngine.EditorServices",
]
SKIPPED_DIRS = {"bin", "obj"}

MODIFIERS = r"(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|unsafe|file|ref|new)\s+)*"
FILE_SCOPED_NAMESPACE = re.compile(r"^namespace\s+[\w.]+\s*;", re.M)
TYPE_PATTERN = r"{indent}" + MODIFIERS + r"(?:class|struct|enum|interface|record(?:\s+(?:class|struct))?|delegate\s+[\w<>\[\]?,.\s]+?)\s+(\w+)"


def declared_types(path):
    """Simple names of the top-level types of a source file."""
    with open(path, encoding="utf-8-sig", errors="replace") as handle:
        text = handle.read()
    # Top-level types are at column 0 under a file-scoped namespace, and one level in under a block namespace.
    indent = "" if FILE_SCOPED_NAMESPACE.search(text) else r"[ ]{4}"
    names = set()
    for match in re.finditer(TYPE_PATTERN.replace("{indent}", "^" + indent), text, re.M):
        names.add(match.group(1))
    return names


def collect(folders):
    """Maps each simple type name to the files that declare it."""
    found = {}
    for folder in folders:
        for current, dirs, files in os.walk(os.path.join(ROOT, folder)):
            dirs[:] = [d for d in dirs if d not in SKIPPED_DIRS]
            for name in files:
                if not name.endswith(".cs"):
                    continue
                path = os.path.join(current, name)
                for type_name in declared_types(path):
                    found.setdefault(type_name, []).append(os.path.relpath(path, ROOT).replace(os.sep, "/"))
    return found


def main():
    gameplay = collect(GAMEPLAY)
    engine = collect(ENGINE)

    # Self-check: the engine set must hold the types the gameplay code is most likely to clash with.
    for expected in ("Entity", "World"):
        if expected not in engine:
            print(f"SELF-CHECK FAILED: '{expected}' not found in the engine types; the scan is broken.")
            return 2

    collisions = sorted(set(gameplay) & set(engine))
    print(f"Gameplay types: {len(gameplay)}, engine/editor types: {len(engine)}, self-check: Entity and World found")
    for name in collisions:
        print(f"COLLISION {name}")
        for path in gameplay[name]:
            print(f"  gameplay: {path}")
        for path in engine[name][:5]:
            print(f"  engine:   {path}")
    print(f"{len(collisions)} collision(s)")
    return 1 if collisions else 0


if __name__ == "__main__":
    sys.exit(main())
