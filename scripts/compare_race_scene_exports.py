"""Compares the DrawnMeshes of two --export-track-runtime-scene files, track by track.

The records of a track are compared as multisets, independently of entity names and component paths.
A record's key is: mesh name, vertex hash, index hash and material signature (material type, material parameters,
texture pixel hashes), plus what the exporter records about how the mesh is drawn (primitive type, counts, entity
visibility, shadow flags). Within a key, records are matched by world matrix, every element within --tolerance.

usage: compare_race_scene_exports.py <a.json> <b.json> [--tolerance 1e-4] [--renames] [--max-list N]

--renames also reports the entity-name changes between matched records (pairs of different names).
Exit code 1 when any record is unmatched on either side, or a track is missing from one file; 0 otherwise.
Requires numpy.
"""
import argparse
import json
import sys
from collections import Counter, defaultdict

import numpy as np


def load_tracks(path):
    with open(path, encoding="utf-8-sig") as handle:
        data = json.load(handle)
    return {track["TrackName"]: track.get("DrawnMeshes") for track in data["Tracks"]}


def record_key(record):
    return json.dumps(
        [
            record.get("MeshName"),
            record.get("VertexHash"),
            record.get("IndexHash"),
            record.get("MaterialType"),
            record.get("MaterialParameters"),
            record.get("TextureHashes"),
            record.get("PrimitiveType"),
            record.get("VertexCount"),
            record.get("IndexCount"),
            record.get("PrimitiveCount"),
            record.get("EntityIsVisible"),
            record.get("CastShadows"),
            record.get("ReceiveShadows"),
        ],
        sort_keys=True,
    )


def matrix_of(record):
    return np.asarray(record["WorldMatrix"], dtype=np.float64).reshape(16)


def match_group(records_a, records_b, tolerance):
    """Greedy match of two lists of records by world matrix. Returns (pairs, unmatched_a, unmatched_b) as indices."""
    if not records_a or not records_b:
        return [], list(range(len(records_a))), list(range(len(records_b)))
    matrices_b = np.stack([matrix_of(r) for r in records_b])
    free_b = np.ones(len(records_b), dtype=bool)
    pairs = []
    unmatched_a = []
    for index_a, record_a in enumerate(records_a):
        distances = np.abs(matrices_b - matrix_of(record_a)).max(axis=1)
        distances[~free_b] = np.inf
        best = int(np.argmin(distances))
        if distances[best] <= tolerance:
            free_b[best] = False
            pairs.append((index_a, best))
        else:
            unmatched_a.append(index_a)
    return pairs, unmatched_a, [i for i in range(len(records_b)) if free_b[i]]


def describe(record):
    translation = record["WorldMatrix"][3][:3]
    return (
        f"entity={record['EntityName']} path={record['ComponentPath']} mesh={record['MeshName']} "
        f"material={record.get('MaterialType')} position=({translation[0]}, {translation[1]}, {translation[2]})"
    )


def compare_track(name, records_a, records_b, args):
    groups_a = defaultdict(list)
    groups_b = defaultdict(list)
    for record in records_a:
        groups_a[record_key(record)].append(record)
    for record in records_b:
        groups_b[record_key(record)].append(record)

    unmatched_a = []
    unmatched_b = []
    matched = 0
    renames = Counter()
    for key in sorted(set(groups_a) | set(groups_b)):
        group_a = groups_a.get(key, [])
        group_b = groups_b.get(key, [])
        pairs, free_a, free_b = match_group(group_a, group_b, args.tolerance)
        matched += len(pairs)
        unmatched_a.extend(group_a[i] for i in free_a)
        unmatched_b.extend(group_b[i] for i in free_b)
        for index_a, index_b in pairs:
            name_a = group_a[index_a]["EntityName"]
            name_b = group_b[index_b]["EntityName"]
            if name_a != name_b:
                renames[(name_a, name_b)] += 1

    print(f"== {name}: A={len(records_a)} records, B={len(records_b)} records, matched={matched}, "
          f"only in A={len(unmatched_a)}, only in B={len(unmatched_b)}")
    for label, records in (("only in A", unmatched_a), ("only in B", unmatched_b)):
        for record in records[: args.max_list]:
            print(f"   {label}: {describe(record)}")
        if len(records) > args.max_list:
            print(f"   {label}: ... {len(records) - args.max_list} more")
    if args.renames:
        records_renamed = sum(renames.values())
        print(f"   renamed entities: {len(renames)} distinct name changes, {records_renamed} matched records")
        for (name_a, name_b), count in sorted(renames.items())[: args.max_list]:
            print(f"   renamed: {name_a} -> {name_b} ({count} records)")
        if len(renames) > args.max_list:
            print(f"   renamed: ... {len(renames) - args.max_list} more")
    return len(unmatched_a) + len(unmatched_b), len(renames)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("a")
    parser.add_argument("b")
    parser.add_argument("--tolerance", type=float, default=1e-4)
    parser.add_argument("--renames", action="store_true")
    parser.add_argument("--max-list", type=int, default=20)
    args = parser.parse_args()

    tracks_a = load_tracks(args.a)
    tracks_b = load_tracks(args.b)
    total_unmatched = 0
    total_renames = 0
    problems = 0
    for name in sorted(set(tracks_a) | set(tracks_b)):
        if name not in tracks_a or name not in tracks_b:
            print(f"== {name}: present only in {'A' if name in tracks_a else 'B'}")
            problems += 1
            continue
        if tracks_a[name] is None or tracks_b[name] is None:
            print(f"== {name}: no DrawnMeshes section in {'A' if tracks_a[name] is None else 'B'}")
            problems += 1
            continue
        unmatched, renamed = compare_track(name, tracks_a[name], tracks_b[name], args)
        total_unmatched += unmatched
        total_renames += renamed

    summary = f"summary: {total_unmatched} unmatched records, {problems} track problems"
    if args.renames:
        summary += f", {total_renames} entity-name changes"
    print(summary)
    return 1 if total_unmatched or problems else 0


if __name__ == "__main__":
    sys.exit(main())
