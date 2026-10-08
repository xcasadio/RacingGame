"""Compares two folders of --capture-track-audit captures (audit-<Track>-<view>-<timestamp>.png).

Captures are paired by name without the timestamp. For each pair it prints the mean absolute difference per channel
R, G and B (same metric as scripts/UiCaptureCompare).

usage: compare_track_audit.py <reference folder> <after folder> [--threshold X]

Without --threshold the exit code is 0. With --threshold it is 1 when a capture's worst channel exceeds the
threshold, when a reference capture is missing from the after folder, or when sizes differ.
Requires numpy and Pillow.
"""
import argparse
import os
import re
import sys

import numpy as np
from PIL import Image

PATTERN = re.compile(r"^(audit-.+)-\d{8}-\d{6,9}\.png$")


def index(folder):
    found = {}
    for name in os.listdir(folder):
        match = PATTERN.match(name)
        if match:
            found[match.group(1)] = os.path.join(folder, name)
    return found


def channel_means(path_a, path_b):
    a = np.asarray(Image.open(path_a).convert("RGB")).astype(np.int32)
    b = np.asarray(Image.open(path_b).convert("RGB")).astype(np.int32)
    if a.shape != b.shape:
        return None
    diff = np.abs(a - b)
    return [float(diff[..., c].mean()) for c in range(3)]


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("reference")
    parser.add_argument("after")
    parser.add_argument("--threshold", type=float, default=None)
    args = parser.parse_args()

    reference = index(args.reference)
    after = index(args.after)
    failed = 0
    print("capture\tR\tG\tB\tworst\tverdict")
    for key in sorted(reference):
        if key not in after:
            print(f"{key}\tMISSING in after")
            failed += 1
            continue
        means = channel_means(reference[key], after[key])
        if means is None:
            print(f"{key}\tSIZE MISMATCH")
            failed += 1
            continue
        worst = max(means)
        verdict = "-"
        if args.threshold is not None:
            verdict = "within" if worst <= args.threshold else "ABOVE"
            failed += verdict == "ABOVE"
        print(f"{key}\t{means[0]:.3f}\t{means[1]:.3f}\t{means[2]:.3f}\t{worst:.3f}\t{verdict}")
    for key in sorted(set(after) - set(reference)):
        print(f"{key}\tEXTRA in after (not in reference)")
    print(f"{len(reference)} reference captures, {len(after)} after captures, {failed} above threshold or missing")
    if args.threshold is not None and failed:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
