# AI Authoring Documentation — Evaluation

**Date:** 2026-08-03
**Scope:** All AI-facing authoring docs and prompts in the repo: `AGENTS.md`, `.omp/AGENTS.md`, `.omp/RULES.md`, `.omp/skills/*`, `.omp/agent/*`, `.omp/commands/*`, `autonomous-ai-prompt.md`, `docs/godot-editor-guide.md`, `docs/level-authoring.md`, `docs/factory-opening.md`, `docs/gossip-pattern.md`, `ROADMAP.md`, `DESIGN.md`, `STORY.md`.
**Method:** Read every doc, then verified every claim against the actual codebase (C# sources, `.tscn` files, `project.godot`) with the Godot 4.7 Mono runtime headless, plus `dotnet build`.

---

## Summary

The authoring stack is **strong and worth keeping**. The layered structure — canonical reference docs (`godot-editor-guide.md`, `level-authoring.md`, `factory-opening.md`), a mandatory execution-contract skill (`factory-level-authoring`), subagent prompts, commands, and a verification culture — is the right shape for AI-driven content production. The Factory tutorial is the best exemplar: a five-room route with flag-gated transitions, one-shot cutscene triggers, save points, and a C# structural verifier (`tests/VerifyFactoryExpansion.cs`, 87 assertions, passing).

The problems are **drift, not architecture**:

1. **Language policy drift.** Docs and repo contradict the C#-only rule: the docs' verification workflow instructed agents to write *GDScript* verifiers, and game-level verifier scripts existed in GDScript (`tests/VerifyFactoryLayout.gd`, `verify_ui_colors.gd`). Per the project owner's directive (C# only), verification guidance now mandates C# `SceneTree` verifiers, and a C# port of the UI verifier ships in `tests/VerifyUiTheme.cs`.
2. **Stale naming.** `AGENTS.md` / `.omp/AGENTS.md` documented the save singleton as `SaveLoadManager`; the actual autoload is `SaveManager` (`saves/SaveManager.cs`). Any agent following the docs would fail to find the class.
3. **Stale demo reference.** `docs/factory-opening.md` documented a three-room route (OpeningZone → SortingFloor → LoadingBay). The shipped demo is five rooms (AssemblyLine + ControlRoom added). The canonical reference was wrong about the shipped content.
4. **Stale autonomous prompt.** `autonomous-ai-prompt.md` referenced tool names that don't exist in the current environment (`issue://N`, `github search_issues`), an outdated singleton/items/arenas inventory, and a stale issue status (#74 and #84 are closed).
5. **Tool-name leakage.** Several `.omp` files reference Oh My Pi–specific tool names (`glob`, `grep`, `read`, `lsp`, `task`, `hub`, `inspect_image`) as if universal. They are now annotated with Hermes equivalents (`search_files`, `read_file`, `delegate_task`, `clarify`, `vision_analyze`).

---

## Findings by document

### 1. AGENTS.md (root) — **fixed**
| Issue | Detail | Fix |
|-------|--------|-----|
| Wrong singleton name | `SaveLoadManager` in the autoload table; actual class is `SaveManager` | Renamed |
| No demo route documented | The shipped tutorial route wasn't described anywhere in the top-level contract | Added "Demo route" block (5 rooms, ~10 min, verifier pointer) |
| Verifier command missing | Only `dotnet build` documented | Added `godot --headless --path . --script res://tests/<Name>.cs` |
| Language rule loose | "GDScript exists only in addons/" — didn't cover `tests/` verifiers | Tightened: game code *and* `tests/` verifiers are C#; GDScript only in `addons/` |

### 2. .omp/AGENTS.md — **fixed**
Same singleton-name fix; verification workflow rewritten from "use a GDScript verifier" to "C# only — no GDScript outside `addons/`. Write verifiers as C# `SceneTree` scripts in `tests/` (e.g. `VerifyFactoryExpansion.cs`)".

### 3. .omp/RULES.md — **no change needed**
Five crisp rules; all still accurate and useful.

### 4. .omp/skills/factory-level-authoring/SKILL.md — **fixed**
- Prerequisite #3 said "canonical three-room Factory reference" → updated to five rooms with the ~10-min route.
- "Use Oh My Pi deliberately" section → "Use tools deliberately"; every OMP-only tool name annotated with the Hermes equivalent.
- Static verification step now names the C# verifier pattern and run command.

### 5. .omp/skills/godot-authoring + godot-csharp-patterns — **verified accurate**
Both match current code (`[Export]` fields, singleton pattern, `.tres` serialization rules). No drift found.

### 6. .omp/agent/* — **fixed**
- `level-author.md`: added the C#-only + C#-verifier constraint to Key constraints.
- `scene-builder.md`: fixed godot-mcp tool names (`godot_create_scene` etc. → documented `create_scene`/`add_node`/`save_scene`/`load_sprite` surface), added C#-only rule, removed implicit GDScript-verifier assumption.
- `gameplay-dev.md`: fixed "12 autoloads" → 15, corrected save-system singleton to `SaveManager`, added C#-only rule.
- `design-partner.md`: accurate; no change.

### 7. .omp/commands/* — **fixed**
- `new-level.md`: OMP tool names → search/read equivalents; verification step names the C# verifier pattern.
- `build.md`, `run.md`, `debug.md`: accurate (`dotnet build`, `godot_run_project`, `godot_get_debug_output`).
- `new-scene.md`, `design.md`: accurate.

### 8. autonomous-ai-prompt.md — **rewritten**
- Removed `issue://N` / `github search_issues` (Oh My Pi–only syntax) → `gh issue view/list`.
- Singleton table corrected and completed (`SaveManager`, `QuestManager`, `FactoryOpeningFlow`, `KeybindManager`).
- Items list synchronized with `ItemDatabase.cs` (adds `warden_key`; full consumables/equipment lists).
- Arenas list updated (adds Cereal, Yogurt, SunnysideLeader).
- Demo route added (five rooms) with a "do not regress" warning.
- Issue list refreshed: #74/#84 CLOSED; remaining open set enumerated; instruction to verify acceptance criteria before starting.
- Workflow loop: added C# verifier step (write/extend `tests/*.cs`, run headless) between build and commit.

### 9. docs/godot-editor-guide.md — **fixed**
- Headless verification section: run command corrected to `res://tests/<Name>.cs`; script list updated to include `VerifyFactoryExpansion.cs`; new levels required to ship a `Verify<Level>.cs`.
- Everything else (plugins, resource rules, component tables, combat, save system) verified against code — accurate.

### 10. docs/level-authoring.md — **fixed**
- Tools-by-task table: OMP tool names annotated with Hermes equivalents.
- Static checks: added item 7 — write a C# verifier (Factory route as the reference), with run command.

### 11. docs/factory-opening.md — **rewritten as the five-room canonical reference**
- Reference scene graph now covers OpeningZone → SortingFloor → **AssemblyLine** → **ControlRoom** → LoadingBay → Eggs Isle handoff, with per-room beat tables for the two new rooms (conveyor lesson + shutdown checklist; sequence puzzle + inspection + soup reward).
- Progression keys table extended: `factory_shutdown_checklist_signed`, `factory_shutdown_inspection_complete`; noted `met_jamitor` is no longer a transition gate (re-keyed to `tutorial_crate_gate_open`).
- Verification assertions extended from 8 to 10 (conveyors, pads, sequence puzzle, both-direction transitions).

### 12. docs/gossip-pattern.md — **spot-checked, accurate**

### 13. ROADMAP.md / DESIGN.md / STORY.md — **verified against code, no drift**
ROADMAP status marks match the current codebase (dialog/combat/overworld/puzzles/equipment/save all implemented; open items match the open issues).

---

## What was verified with the runtime (not just read)

| Check | Result |
|-------|--------|
| `dotnet build` (Godot.NET.Sdk/4.7.0) | 0 errors (3 pre-existing warnings) |
| `VerifyFactoryExpansion.cs` (C# structural contract, 87 assertions) | **ALL CHECKS PASSED** on Godot 4.7 Mono |
| All 12 `LevelTransition` pairs across the 5 factory rooms resolve both directions | Passed |
| All 5 rooms load headless as `BaseLevel` + `LevelTileMapLayer` with non-empty used rects | Passed |
| Flag wiring: TimeClock→`tutorial_clocked_out`, Jamitor→`met_jamitor` (via cutscene), plate→`tutorial_crate_gate_open`, checklist→`factory_shutdown_checklist_signed`, inspection→`factory_shutdown_inspection_complete`, arrest→`arrested` | Passed |
| Save points: all 5 rooms have `SavePoint` with correct `LocationName`s | Passed |
| Runtime boot smoke test: New Game path loads OpeningZone, player spawns at (0,0), HP 100, no errors | Passed (`tests/SmokeFactoryDemo.cs`) |
| UI theme verifier C# port | Passed (`tests/VerifyUiTheme.cs`, 13 checks) |
| Earlier "verifier failures" on the factory rooms | **False alarm** — caused by running the project on the installed Godot **4.3** flatpak instead of the project's **4.7** runtime; on 4.7 all checks pass |

## Environment note (affects every agent session)

- `/usr/local/bin/godot` is a wrapper script that runs `flatpak run org.godotengine.Godot` — a flatpak that is **not installed** (only `org.godotengine.GodotSharp` 4.3 is). The correct runtime for this project is Godot **4.7 Mono**; a copy lives at `~/.local/opt/Godot_v4.7-stable_mono_linux_x86_64/Godot_v4.7-stable_mono_linux.x86_64`. Running verifiers on 4.3 produces bogus C#-assembly failures (see the false-alarm row above).
- `.omp/mcp.json` referenced by `.omp/AGENTS.md` doesn't exist; only `.omp/mcp.json.bak` does. Restore it before relying on `EGGBERT_SKIP_MENU=1` / godot-mcp.

## Pending items (need owner decision)

1. **Remove GDScript verifiers.** `tests/VerifyFactoryLayout.gd` (superseded byte-for-byte by `tests/VerifyFactoryExpansion.cs`) and `verify_ui_colors.gd` (now ported to `tests/VerifyUiTheme.cs`) are tracked in git. Deleting them enforces the C#-only rule; both have passing C# replacements. **Deferred per owner AFK instruction — no removals while unattended.** The `.omp/verify_*.gd` scripts verify *addon* internals (cutscene inspector) and can stay — addons are permitted GDScript.

## Completed after the evaluation (same session)

- **Logging overhaul (feedback loop).** `util/GameLogger.cs` now writes a machine-readable JSONL mirror (`eggbert_YYYY-MM-DD.jsonl`) alongside the text log, with ISO timestamps, level, tag, message, source ref, and a per-boot **session id** (`EGGBERT_SESSION_ID` env override). `LOGGING.md` documents the format, agent query recipes (grep/jq/python), and `EGGBERT_SESSION_ID`.
- **Diagnostics bundle.** `tools/collect-diagnostics.sh [godot-binary]` packages logs + `dotnet build` result + every `tests/Verify*.cs` headless result (exit codes + pass/fail) + git state into `diagnostics/<stamp>/` with `summary.json` and a `PASTE_ME.md` block for handing a bug to a future session. Verified end-to-end (build 0/0, all verifiers exit 0).
- **Hermes skill migration.** The three Oh My Pi skills migrated to Hermes skills (`~/.hermes/skills/game-dev/`): `eggbert-level-authoring` (execution contract + C# verifier pattern + 4.7-runtime pitfall), `eggbert-godot-authoring` (serialization rules), `eggbert-godot-csharp-patterns` (C# patterns). `.omp/AGENTS.md` updated with a harness-retirement note.
- **`.omp/mcp.json` restored** from `.bak`, `GODOT_PATH` pointed at the 4.7 Mono binary (the `.bak` pointed at the broken flatpak wrapper).
- **New C# verifier** `tests/SmokeFactoryDemo.cs` — boots OpeningZone via the real New-Game path headless, asserts level load + player spawn (passes).
- **UI theme verifier ported to C#** — `tests/VerifyUiTheme.cs` (13 checks, passes), replacing `verify_ui_colors.gd` coverage.

## Recommendations for future authoring sessions

1. **Verify on 4.7, always.** Use the extracted 4.7 Mono binary; never trust verifier output from the 4.3 flatpak.
2. **Ship a `Verify<Level>.cs` with every level.** The Factory route proves the pattern: instantiate headless, assert root/type, direct-root transitions (both directions), NodePaths, flags, save points, item IDs.
3. **Keep `docs/factory-opening.md` as the canonical five-room reference** when authoring new levels — copy the beat-table shape, never the narrative.
4. **When in doubt about a design fact, ask (question gate)** — the docs' rule stands: never invent narrative, item, or tuning facts.
