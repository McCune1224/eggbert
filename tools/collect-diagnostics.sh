#!/usr/bin/env bash
# collect-diagnostics.sh — package Eggbert state for sharing with a future
# agent session (debug / assessment feedback loop).
#
# Produces a timestamped folder under diagnostics/ containing:
#   - game logs (text + JSONL) for today and the previous day
#   - dotnet build result
#   - every C# verifier result (headless, Godot 4.7 Mono)
#   - git HEAD + dirty-file list
#   - a summary.json manifest (machine-readable)
#
# Usage:  tools/collect-diagnostics.sh [godot-binary]
# Default binary: the Godot 4.7 Mono copy under ~/.local/opt, else `godot`.

set -u

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
GODOT="${1:-}"
if [ -z "$GODOT" ]; then
  CANDIDATE="$HOME/.local/opt/Godot_v4.7-stable_mono_linux_x86_64/Godot_v4.7-stable_mono_linux.x86_64"
  if [ -x "$CANDIDATE" ]; then GODOT="$CANDIDATE"; else GODOT="$(command -v godot || true)"; fi
fi
if [ -z "$GODOT" ] || [ ! -x "$GODOT" ]; then
  echo "ERROR: no runnable Godot. Pass the 4.7 Mono binary as arg 1." >&2
  exit 2
fi

STAMP="$(date +%Y%m%d-%H%M%S)"
OUT="$REPO/diagnostics/$STAMP"
mkdir -p "$OUT"
echo "==> diagnostics bundle: $OUT"

# --- logs ---------------------------------------------------------------
LOG_DIR="$HOME/.local/share/godot/app_userdata/Eggbert/logs"
mkdir -p "$OUT/logs"
for f in "$LOG_DIR"/eggbert_*.log "$LOG_DIR"/eggbert_*.jsonl; do
  [ -e "$f" ] && cp "$f" "$OUT/logs/" 2>/dev/null
done
echo "==> logs copied from $LOG_DIR"

# --- build --------------------------------------------------------------
( cd "$REPO" && dotnet build > "$OUT/build.txt" 2>&1 )
echo "==> build: $(grep -cE 'error' "$OUT/build.txt" || true) error(s), $(grep -cE 'warning' "$OUT/build.txt" || true) warning(s)"

# --- verifiers ----------------------------------------------------------
mkdir -p "$OUT/verifiers"
for v in "$REPO"/tests/Verify*.cs; do
  name="$(basename "$v" .cs)"
  timeout 180 "$GODOT" --headless --path "$REPO" --script "res://tests/$name.cs" > "$OUT/verifiers/$name.txt" 2>&1
  code=$?
  echo "$code" > "$OUT/verifiers/$name.exit"
  summary="$(grep -E 'PASS|FAIL|check' "$OUT/verifiers/$name.txt" | tail -2 | tr '\n' ' ')"
  echo "==> $name: exit=$code $summary"
done

# --- git ----------------------------------------------------------------
( cd "$REPO" && git rev-parse HEAD > "$OUT/git-head.txt" 2>&1; git status --porcelain > "$OUT/git-status.txt" 2>&1 )
echo "==> git HEAD: $(cat "$OUT/git-head.txt" 2>/dev/null | head -1)"

# --- manifest -----------------------------------------------------------
python3 - "$OUT" "$STAMP" "$GODOT" "$LOG_DIR" <<'PY'
import json, os, re, sys
out, stamp, godot, log_dir = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]

def first(path, default=""):
    try:
        with open(path) as f:
            return f.read().strip().splitlines()[0]
    except Exception:
        return default

verifiers = {}
for name in sorted(os.listdir(os.path.join(out, "verifiers"))):
    if not name.endswith(".txt"):
        continue
    stem = name[:-4]
    path = os.path.join(out, "verifiers", name)
    txt = open(path, errors="replace").read()
    try:
        exit_code = int(open(os.path.join(out, "verifiers", stem + ".exit")).read().strip())
    except Exception:
        exit_code = -1
    text_fail = ("FAILED" in txt or "FAILURE" in txt or "FAIL:" in txt or "SCRIPT ERROR" in txt)
    text_pass = ("ALL CHECKS PASSED" in txt or "passed." in txt or "passed" in txt and "FAIL" not in txt)
    passed = (exit_code == 0) and not text_fail
    if exit_code == 0 and text_pass and not text_fail:
        passed = True
    verifiers[stem] = {"exit": exit_code, "passed": passed, "failed": not passed}

# Godot version from the first verifier header line ("Godot Engine vX.Y...")
godot_version = "n/a"
for name in sorted(os.listdir(os.path.join(out, "verifiers"))):
    if name.endswith(".txt"):
        for line in open(os.path.join(out, "verifiers", name), errors="replace"):
            if line.startswith("Godot Engine v"):
                godot_version = line.strip().split(" - ")[0]
                break
        if godot_version != "n/a":
            break

manifest = {
    "stamp": stamp,
    "godot": godot,
    "godot_version": godot_version,
    "git_head": first(os.path.join(out, "git-head.txt")),
    "dirty_files": sum(1 for _ in open(os.path.join(out, "git-status.txt"), errors="replace")) if os.path.exists(os.path.join(out, "git-status.txt")) else 0,
    "build": {"errors": 0, "warnings": 0},
    "verifiers": verifiers,
}
bt = first(os.path.join(out, "build.txt"))
if bt:
    m = re.search(r"(\d+) Error", bt)
    if m: manifest["build"]["errors"] = int(m.group(1))
    m = re.search(r"(\d+) Warning", bt)
    if m: manifest["build"]["warnings"] = int(m.group(1))

with open(os.path.join(out, "summary.json"), "w") as f:
    json.dump(manifest, f, indent=2)
print("==> summary.json written")
PY

# --- shareable pointer --------------------------------------------------
cat > "$OUT/PASTE_ME.md" <<EOF
# Eggbert diagnostics bundle — $STAMP

Copy the block below into a fresh agent session to give it full context.

\`\`\`
EGGBERT DIAGNOSTICS — $STAMP
- git HEAD: $(cat "$OUT/git-head.txt" 2>/dev/null | head -1)
- Build: $(grep -E "[0-9]+ Error" "$OUT/build.txt" | tail -1)
- Verifier results: see diagnostics/$STAMP/verifiers/*.txt
- Game logs (text): diagnostics/$STAMP/logs/eggbert_*.log
- Game logs (JSONL, agent-parseable): diagnostics/$STAMP/logs/eggbert_*.jsonl
- Read LOGGING.md for the tag map, then grep the JSONL for the failing tag.
\`\`\`
EOF
echo "==> done. Share: $OUT/PASTE_ME.md  (or the whole $OUT folder)"
