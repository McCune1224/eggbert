#!/usr/bin/env bash
# Godot feedback-loop harness for the eggbert-gd GDScript port.
# Captures what the editor's GUI log omits: engine + .NET module + GDScript
# runtime errors that only print to stdout/stderr, plus the app userdata logs
# and the migration-integrity verifier. No MCP required.
#
# Usage:
#   .hermes/godot_feedback.sh boot       # headless boot + error extraction
#   .hermes/godot_feedback.sh logs       # tail latest app userdata logs
#   .hermes/godot_feedback.sh verify     # run verify_migration_integrity.gd
#   .hermes/godot_feedback.sh run <args> # arbitrary godot CLI, capture all
#   .hermes/godot_feedback.sh all        # boot + verify (default)

set -u
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
LOGS_DIR="$HOME/.var/app/org.godotengine.Godot/data/godot/app_userdata/Eggbert/logs"
# Fall back to the non-flatpak (system) location if the flatpak sandbox dir is absent.
if [ ! -d "$LOGS_DIR" ]; then
  LOGS_DIR="$HOME/.local/share/godot/app_userdata/Eggbert/logs"
fi
GODOT="$(command -v godot)"
OUT_DIR="$PROJECT_DIR/.hermes/out"
mkdir -p "$OUT_DIR"

# Filter out the harmless AsepriteWizard / reimport "invalid UID, using text path"
# noise so real errors stand out. Keep the backtrace headers.
filter_noise() {
  grep -viE 'invalid UID|text path instead|at: load \(scene/resources/resource_format_text'
}

boot() {
  local stamp; stamp="$(date +%Y%m%d-%H%M%S)"
  local raw="$OUT_DIR/boot-${stamp}.txt"
  echo ">> Headless boot: godot --headless --path . --verbose"
  echo ">> raw capture: $raw"
  timeout 30 "$GODOT" --headless --verbose --path "$PROJECT_DIR" >"$raw" 2>&1
  echo
  echo "=== REAL ERRORS / BACKTRACES (noise filtered) ==="
  filter_noise <"$raw" \
    | grep -nE 'ERROR|SCRIPT ERROR|Parse Error|Cannot|Failed to load|backtrace|GDScript|_ready|_boot|null|leaked|still in use' \
    | head -60
  echo
  echo "=== .NET / assembly status ==="
  grep -E '\.NET:|Failed to load project assembly' "$raw" | head -5 || echo "(no .NET messages)"
  echo
  echo "=== last 15 boot log lines ==="
  filter_noise <"$raw" | tail -15
}

logs() {
  echo ">> App userdata logs: $LOGS_DIR"
  echo "=== latest engine log (godot*.log) ==="
  local latest; latest="$(ls -t "$LOGS_DIR"/godot*.log 2>/dev/null | head -1)"
  if [ -n "$latest" ]; then echo "( $latest )"; tail -30 "$latest" | filter_noise; fi
  echo
  echo "=== latest game log (eggbert_*.log) ==="
  local game; game="$(ls -t "$LOGS_DIR"/eggbert_*.log 2>/dev/null | head -1)"
  if [ -n "$game" ]; then echo "( $game )"; tail -20 "$game"; fi
}

verify() {
  local out; out="$OUT_DIR/verify-$(date +%Y%m%d-%H%M%S).txt"
  echo ">> Migration integrity verifier"
  "$GODOT" --headless --path "$PROJECT_DIR" --script res://tests/verify_migration_integrity.gd >"$out" 2>&1
  local rc=$?
  echo "exit code: $rc"
  if [ "$rc" -eq 0 ]; then
    grep -iE 'passed|loaded$' "$out" | tail -3
  else
    echo "FAILURES:"
    grep -iE 'still|integrity|failed' "$out" | sort -u
  fi
  echo "(raw: $out)"
  return "$rc"
}

run() {
  local stamp; stamp="$(date +%Y%m%d-%H%M%S)"
  local raw="$OUT_DIR/run-${stamp}.txt"
  echo ">> godot $*"
  "$GODOT" "$@" >"$raw" 2>&1
  echo "exit: $?"
  echo "=== errors (noise filtered) ==="
  filter_noise <"$raw" | grep -iE 'ERROR|SCRIPT ERROR|backtrace|Cannot|Failed' | head -40
  echo "(raw: $raw)"
}

case "${1:-all}" in
  boot)   boot ;;
  logs)   logs ;;
  verify) verify ;;
  run)    shift; run "$@" ;;
  all)    boot; echo; verify; exit $? ;;
  *)      echo "usage: godot_feedback.sh [boot|logs|verify|run <args>|all]"; exit 1 ;;
esac
