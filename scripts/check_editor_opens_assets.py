#!/usr/bin/env python3
"""Open every catalogued asset of a game project in the CasaEngine editor and give a verdict per asset.

One editor process is launched per asset (a launch takes some seconds), always under a machine-wide lock file so that
two sessions never run the editor or the game at the same time:

    CasaEngine.Editor.exe --project <project.json> --open-asset <file_name> --diagnostics-out <file> --automation-timeout <n>

The editor writes its diagnostics and exits by itself (exit code 0 success, 1 failure, 2 timed out).

Pass rule of one asset (plan P23), implemented by judge_asset() alone; the first condition that fails is recorded:
  timeout              the process was killed by this script, or the editor exited with code 2 (its own timeout);
  exit_code            the exit code is not 0;
  opened_line          the diagnostics have no line  [Automation] Opened asset='<file_name>'  for this asset;
  error                the diagnostics have an [Error] line;
  warning_names_file   the diagnostics have a [Warning] line that names the asset's file (relative path with either
                       slash, or bare file name). Any other [Warning] does not fail the asset: it is collected.

Run it on a COPY of the project content, never on the repository's own content: the editor rewrites .casaeditor/ when it
closes. The script itself never writes inside the project.

Usage:
    python -I scripts/check_editor_opens_assets.py --editor <CasaEngine.Editor.exe> --project <copy>/<Project>.json \\
        --out <output folder> [--timeout 120] [--only <file_name>]... [--type <ext>]... [--resume] [--lock <path>]

    python -I scripts/check_editor_opens_assets.py --judge <diagnostics file> --asset <file_name> \\
        [--exit-code 0] [--strip-opened-line]       # verdict function on a recorded diagnostics file, no editor

Exit code: 1 when at least one asset failed (or, with --judge, when the verdict is fail), 0 otherwise; 2 on a usage
error; 3 when the lock stayed held by someone else for more than 45 minutes (the lock is never stolen).
"""

import argparse
import collections
import datetime
import json
import os
import re
import subprocess
import sys
import time

DEFAULT_LOCK = "D:/development/rgce-run.lock"
LOCK_OWNER = "editor-closure"
LOCK_POLL_SECONDS = 5
LOCK_REPORT_SECONDS = 60
LOCK_GIVE_UP_SECONDS = 45 * 60
KILL_GRACE_SECONDS = 30

EXIT_OK = 0
EXIT_FAILED = 1
EXIT_USAGE = 2
EXIT_LOCK = 3

# Conditions, in the order they are checked.
COND_TIMEOUT = "timeout"
COND_EXIT_CODE = "exit_code"
COND_OPENED_LINE = "opened_line"
COND_ERROR = "error"
COND_WARNING = "warning_names_file"

# One log line of the diagnostics file: "  - 23:00:31.710 [Error] text" (section "Logs:") or
# "2026-11-18 23:00:33.886 [Info] text" (section "Entries:"). Only [Error] and [Warning] lines matter.
_LOG_LINE = re.compile(
    r"^\s*(?:-\s+)?(?:\d{4}-\d{2}-\d{2}\s+)?(?:\d{2}:\d{2}:\d{2}(?:\.\d+)?\s+)?\[(Error|Warning)\]\s?(.*)$")
_OPENED_LINE = re.compile(r"\[Automation\] Opened asset='(.*)'\s*$")
_PREVIEW_LINE = re.compile(r"\[Editor\] Opened asset preview file='(.*?)', kind=(\S+?), panel='(.*?)'")
_DOCUMENT_LINE = re.compile(r"\[Editor\] Opened (.+?) asset='(.*?)', (?:panel|viewport)='(.*?)'")
_ACTIVE_PANEL = re.compile(r"^Active document panel:\s*(.*)$")


def _slashes(text):
    return text.replace("\\", "/")


def _names_file(text, file_name):
    """True when text mentions the relative path of the asset (either slash) or its bare file name."""
    haystack = _slashes(text).lower()
    relative = _slashes(file_name).lower()
    bare = relative.rsplit("/", 1)[-1]
    for needle in (relative, bare):
        if not needle:
            continue
        pattern = r"(?<![\w.\-])" + re.escape(needle) + r"(?![\w])"
        if re.search(pattern, haystack):
            return True
    return False


def parse_diagnostics(text):
    """Extract the facts the verdict and the report need from the text of a diagnostics file."""
    errors = []
    warnings = []
    opened = []
    panel = None
    active_panel = None
    for line in (text or "").splitlines():
        stripped = line.strip()
        log = _LOG_LINE.match(line)
        if log:
            (errors if log.group(1) == "Error" else warnings).append(log.group(2).strip())
            continue
        match = _OPENED_LINE.search(stripped)
        if match:
            opened.append(match.group(1))
            continue
        match = _PREVIEW_LINE.search(stripped)
        if match:
            panel = {"kind": "preview/" + match.group(2), "panel_id": match.group(3)}
            continue
        match = _DOCUMENT_LINE.search(stripped)
        if match:
            panel = {"kind": match.group(1), "panel_id": match.group(3)}
            continue
        match = _ACTIVE_PANEL.match(stripped)
        if match and active_panel is None:
            active_panel = match.group(1).strip()
    if panel is None and active_panel and active_panel != "<none>":
        panel = {"kind": None, "panel_id": active_panel}
    return {"errors": errors, "warnings": warnings, "opened": opened, "panel": panel}


def judge_asset(exit_code, diagnostics_text, file_name):
    """Pass rule of one asset (P23). Pure: no file, no process.

    exit_code is None when the process was killed by the caller. Returns (verdict, failed_condition, details) where verdict
    is "pass" or "fail", failed_condition is None for a pass, and details holds what was found.
    """
    facts = parse_diagnostics(diagnostics_text)
    wanted = _slashes(file_name)
    opened_line = any(_slashes(path) == wanted for path in facts["opened"])
    file_warnings = [w for w in facts["warnings"] if _names_file(w, file_name)]
    details = {
        "exit_code": exit_code,
        "opened_line": opened_line,
        "errors": facts["errors"],
        "warnings": facts["warnings"],
        "warnings_naming_file": file_warnings,
        "panel": facts["panel"],
    }

    if exit_code is None:
        return "fail", COND_TIMEOUT, details
    if exit_code == 2:
        return "fail", COND_TIMEOUT, details
    if exit_code != 0:
        return "fail", COND_EXIT_CODE, details
    if not opened_line:
        return "fail", COND_OPENED_LINE, details
    if facts["errors"]:
        return "fail", COND_ERROR, details
    if file_warnings:
        return "fail", COND_WARNING, details
    return "pass", None, details


# ---------------------------------------------------------------------------------------------------------- lock (P28)

class LockTimeout(Exception):
    pass


def acquire_lock(lock_path):
    """Create the lock file exclusively, waiting while it exists; give up (never steal) after 45 minutes."""
    started = time.monotonic()
    last_report = started
    while True:
        try:
            fd = os.open(lock_path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
        except FileExistsError:
            now = time.monotonic()
            if now - started > LOCK_GIVE_UP_SECONDS:
                raise LockTimeout(
                    "lock '%s' held by someone else for more than %d minutes: stopping, the lock is not stolen"
                    % (lock_path, LOCK_GIVE_UP_SECONDS // 60))
            if now - last_report >= LOCK_REPORT_SECONDS:
                print("  waiting for lock '%s' (%d s)" % (lock_path, int(now - started)), flush=True)
                last_report = now
            time.sleep(LOCK_POLL_SECONDS)
            continue
        with os.fdopen(fd, "w", encoding="utf-8") as handle:
            handle.write("%s %s\n" % (LOCK_OWNER, datetime.datetime.now().astimezone().isoformat(timespec="seconds")))
        return


def release_lock(lock_path):
    try:
        os.remove(lock_path)
    except FileNotFoundError:
        pass


# ------------------------------------------------------------------------------------------------------ editor launch

def _kill_tree(process):
    try:
        subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"], stdout=subprocess.DEVNULL,
                       stderr=subprocess.DEVNULL, check=False)
    except OSError:
        pass
    try:
        process.kill()
    except OSError:
        pass


def launch_editor(editor, project, file_name, diagnostics_path, stdout_path, timeout_seconds):
    """One editor launch. Returns (exit_code or None when killed, duration in seconds)."""
    command = [editor, "--project", project, "--open-asset", file_name, "--diagnostics-out", diagnostics_path,
               "--automation-timeout", str(timeout_seconds)]
    started = time.monotonic()
    with open(stdout_path, "wb") as sink:
        process = subprocess.Popen(command, cwd=os.path.dirname(editor), stdout=sink, stderr=subprocess.STDOUT,
                                   stdin=subprocess.DEVNULL)
        try:
            exit_code = process.wait(timeout=timeout_seconds + KILL_GRACE_SECONDS)
        except subprocess.TimeoutExpired:
            _kill_tree(process)
            process.wait()
            exit_code = None
        except BaseException:
            _kill_tree(process)
            process.wait()
            raise
    return exit_code, time.monotonic() - started


def run_one(args, entry, lock_path):
    file_name = entry["file_name"]
    safe = _slashes(file_name).replace("/", "__")
    diagnostics_path = os.path.join(args.out, safe + ".diagnostics.txt")
    stdout_path = os.path.join(args.out, safe + ".stdout.txt")
    if os.path.exists(diagnostics_path):
        os.remove(diagnostics_path)  # a stale file of an earlier run must not be judged

    acquire_lock(lock_path)
    try:
        exit_code, duration = launch_editor(args.editor, args.project, file_name, diagnostics_path, stdout_path,
                                            args.timeout)
    finally:
        release_lock(lock_path)

    text = None
    if os.path.exists(diagnostics_path):
        with open(diagnostics_path, "r", encoding="utf-8-sig", errors="replace") as handle:
            text = handle.read()
    verdict, condition, details = judge_asset(exit_code, text, file_name)
    panel = details["panel"] or {}
    return {
        "name": entry.get("name"),
        "file_name": file_name,
        "extension": _extension(file_name),
        "exit_code": exit_code,
        "diagnostics_file": os.path.basename(diagnostics_path),
        "diagnostics_found": text is not None,
        "opened_line_found": details["opened_line"],
        "panel_kind": panel.get("kind"),
        "panel_id": panel.get("panel_id"),
        "error_count": len(details["errors"]),
        "warning_count": len(details["warnings"]),
        "errors": details["errors"],
        "warnings": details["warnings"],
        "warnings_naming_file": details["warnings_naming_file"],
        "duration_seconds": round(duration, 2),
        "verdict": verdict,
        "failed_condition": condition,
    }


# -------------------------------------------------------------------------------------------------------- reporting

def _extension(file_name):
    base = _slashes(file_name).rsplit("/", 1)[-1]
    return base.rsplit(".", 1)[-1].lower() if "." in base else ""


_GUID = re.compile(r"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")
_DRIVE_PATH = re.compile(r"[A-Za-z]:[\\/][^'\"<>|*?]*?(?=['\"]|\s\(|\)|$)")
_QUOTED_PATH = re.compile(r"'[^']*[\\/][^']*'")
_NUMBER = re.compile(r"\d+(?:\.\d+)?")


def normalise_warning(text, file_name=None):
    """Group key of a warning text: file paths, the asset name, GUIDs and numbers are replaced."""
    result = text
    if file_name:
        bare = _slashes(file_name).rsplit("/", 1)[-1]
        for needle in (file_name, _slashes(file_name), file_name.replace("/", "\\"), bare):
            if needle:
                result = re.sub(re.escape(needle), "<asset>", result, flags=re.IGNORECASE)
    result = _GUID.sub("<guid>", result)
    result = _DRIVE_PATH.sub("<path>", result)
    result = _QUOTED_PATH.sub("'<path>'", result)
    result = _NUMBER.sub("N", result)
    return result


def build_summary(results, total_selected):
    by_extension = collections.OrderedDict()
    failed_by_condition = collections.OrderedDict()
    warning_groups = collections.OrderedDict()
    for file_name in sorted(results):
        item = results[file_name]
        row = by_extension.setdefault(item["extension"], {"passed": 0, "failed": 0})
        row["passed" if item["verdict"] == "pass" else "failed"] += 1
        if item["verdict"] != "pass":
            failed_by_condition.setdefault(item["failed_condition"], []).append(file_name)
        for text in item.get("warnings", []):
            key = normalise_warning(text, file_name)
            group = warning_groups.setdefault(key, {"text": key, "count": 0, "assets": [], "example": text})
            group["count"] += 1
            if file_name not in group["assets"]:
                group["assets"].append(file_name)
    passed = sum(1 for item in results.values() if item["verdict"] == "pass")
    durations = [item["duration_seconds"] for item in results.values()]
    return {
        "selected": total_selected,
        "judged": len(results),
        "passed": passed,
        "failed": len(results) - passed,
        "total_duration_seconds": round(sum(durations), 1),
        "by_extension": by_extension,
        "failed_by_condition": failed_by_condition,
        "distinct_warnings": sorted(warning_groups.values(), key=lambda g: (-g["count"], g["text"])),
    }


def print_summary(summary):
    print()
    print("Summary: %d judged of %d selected, %d passed, %d failed, %.0f s of editor time"
          % (summary["judged"], summary["selected"], summary["passed"], summary["failed"],
             summary["total_duration_seconds"]))
    print("Per extension:")
    for extension, row in summary["by_extension"].items():
        print("  .%-10s passed %4d  failed %4d" % (extension, row["passed"], row["failed"]))
    if summary["failed_by_condition"]:
        print("Failures per condition:")
        for condition, assets in summary["failed_by_condition"].items():
            print("  %s (%d)" % (condition, len(assets)))
            for file_name in assets:
                print("    " + file_name)
    if summary["distinct_warnings"]:
        print("Distinct [Warning] texts:")
        for group in summary["distinct_warnings"]:
            print("  %4d x %s" % (group["count"], group["text"]))


def write_json(path, data):
    temporary = path + ".tmp"
    with open(temporary, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(data, handle, indent=2, ensure_ascii=False)
        handle.write("\n")
    os.replace(temporary, path)


# ------------------------------------------------------------------------------------------------------------ main

def load_catalogue(project):
    path = os.path.join(os.path.dirname(os.path.abspath(project)), "AssetInfos.json")
    with open(path, "r", encoding="utf-8-sig") as handle:
        data = json.load(handle)
    entries = data["asset_infos"] if isinstance(data, dict) else data
    return [entry for entry in entries if entry.get("file_name")]


def select_entries(entries, only, types):
    wanted = {_slashes(name).lower() for name in only}
    extensions = {t.lower().lstrip(".") for t in types}
    selected = []
    for entry in entries:
        if wanted and _slashes(entry["file_name"]).lower() not in wanted:
            continue
        if extensions and _extension(entry["file_name"]) not in extensions:
            continue
        selected.append(entry)
    missing = wanted - {_slashes(e["file_name"]).lower() for e in selected}
    return selected, sorted(missing)


def run_judge(args):
    with open(args.judge, "r", encoding="utf-8-sig", errors="replace") as handle:
        text = handle.read()
    if args.strip_opened_line:
        text = "\n".join(line for line in text.splitlines() if "[Automation] Opened asset=" not in line)
    verdict, condition, details = judge_asset(args.exit_code, text, args.asset)
    print(json.dumps({"verdict": verdict, "failed_condition": condition, "details": details}, indent=2,
                     ensure_ascii=False))
    return EXIT_FAILED if verdict != "pass" else EXIT_OK


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--editor", help="path to CasaEngine.Editor.exe")
    parser.add_argument("--project", help="path to the project .json of the content copy")
    parser.add_argument("--out", help="output folder")
    parser.add_argument("--timeout", type=float, default=120.0,
                        help="seconds, given to the editor as --automation-timeout (default 120); the process is killed "
                             "%d s later" % KILL_GRACE_SECONDS)
    parser.add_argument("--only", action="append", default=[], help="catalogue file_name to check (repeatable)")
    parser.add_argument("--type", action="append", default=[], help="extension to check, e.g. png (repeatable)")
    parser.add_argument("--resume", action="store_true", help="do not relaunch an asset that has a verdict in results.json")
    parser.add_argument("--lock", default=DEFAULT_LOCK, help="lock file shared with the other sessions (default %(default)s)")
    parser.add_argument("--judge", help="verdict function on this recorded diagnostics file, no editor launch")
    parser.add_argument("--asset", help="with --judge: catalogue file_name of the asset")
    parser.add_argument("--exit-code", type=int, default=0, help="with --judge: exit code of the recorded run (default 0)")
    parser.add_argument("--strip-opened-line", action="store_true",
                        help="with --judge: remove the '[Automation] Opened asset=' line before judging")
    args = parser.parse_args(argv)

    if args.judge:
        if not args.asset:
            parser.error("--judge needs --asset")
        return run_judge(args)
    for name in ("editor", "project", "out"):
        if not getattr(args, name):
            parser.error("--%s is required" % name)

    args.editor = os.path.abspath(args.editor)
    args.project = os.path.abspath(args.project)
    args.out = os.path.abspath(args.out)
    if not os.path.isfile(args.editor):
        print("editor not found: " + args.editor, file=sys.stderr)
        return EXIT_USAGE
    if not os.path.isfile(args.project):
        print("project not found: " + args.project, file=sys.stderr)
        return EXIT_USAGE

    entries = load_catalogue(args.project)
    selected, missing = select_entries(entries, args.only, args.type)
    if missing:
        print("--only entries not in the catalogue: " + ", ".join(missing), file=sys.stderr)
        return EXIT_USAGE
    if not selected:
        print("no asset selected", file=sys.stderr)
        return EXIT_USAGE

    os.makedirs(args.out, exist_ok=True)
    results_path = os.path.join(args.out, "results.json")
    results = {}
    if args.resume and os.path.exists(results_path):
        with open(results_path, "r", encoding="utf-8") as handle:
            results = {item["file_name"]: item for item in json.load(handle)["assets"]}

    def save():
        ordered = [results[e["file_name"]] for e in selected if e["file_name"] in results]
        write_json(results_path, {
            "project": args.project,
            "selected": len(selected),
            "assets": ordered,
        })
        write_json(os.path.join(args.out, "summary.json"), build_summary(
            {e["file_name"]: results[e["file_name"]] for e in selected if e["file_name"] in results}, len(selected)))

    print("%d asset(s) selected, %d already judged" % (len(selected), sum(1 for e in selected if e["file_name"] in results)))
    try:
        for index, entry in enumerate(selected, 1):
            if entry["file_name"] in results:
                continue
            item = run_one(args, entry, args.lock)
            results[entry["file_name"]] = item
            save()
            print("[%d/%d] %s  %s%s  (%.1f s)" % (
                index, len(selected), "PASS" if item["verdict"] == "pass" else "FAIL", entry["file_name"],
                "" if item["verdict"] == "pass" else "  <- " + item["failed_condition"], item["duration_seconds"]),
                flush=True)
    except LockTimeout as problem:
        save()
        print(str(problem), file=sys.stderr)
        return EXIT_LOCK
    except KeyboardInterrupt:
        save()
        print("interrupted: results so far are in " + results_path, file=sys.stderr)
        return EXIT_USAGE

    save()
    summary = build_summary({e["file_name"]: results[e["file_name"]] for e in selected if e["file_name"] in results},
                            len(selected))
    print_summary(summary)
    return EXIT_FAILED if summary["failed"] else EXIT_OK


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(errors="replace")
        sys.stderr.reconfigure(errors="replace")
    except (AttributeError, ValueError):
        pass
    sys.exit(main())
