# AGENTS.md — Eggbert

Godot 4.7 C# RPG. Undertale/EarthBound inspired, 640×360, top-down (zero gravity).

**Game design decisions live in `DESIGN.md`** — combat loop, roadmap, open questions. Read it alongside this file.

## Documentation — prefer Context7 over guessing

Use `context7_resolve-library-id` + `context7_query-docs` for Godot API questions before writing code.

| Library | Context7 ID |
|---------|------------|
| Godot 4.7 docs | `/websites/godotengine_en_4_7` |
| Godot stable | `/websites/godotengine_en_stable` |
| Godot source (C# bindings) | `/godotengine/godot` |

**Always check docs before implementing** — Godot's C# API sometimes differs from GDScript (PascalCase methods, `Godot.Collections` types, signal wiring via `+=` vs `Connect`).

## Commands

```bash
dotnet build          # compile C# (Godot.NET.Sdk/4.7.0, net8.0)
dotnet test           # no tests exist yet — skip until set up
```

Godot editor + MCP tools are available via the `godot-mcp` server (configured in `.opencode/opencode.json`). Use `godot_run_project`, `godot_launch_editor`, `godot_get_debug_output`, etc.

## Architecture

### Boot order
`boot/GameInit.tscn` (main scene) → Main menu (New Game / Continue / Settings / Quit) or debug-skip to last save → `GameController.LoadLevel(...)` → player placed at saved position.

> **Debug auto-start**: Set the `EGGBERT_SKIP_MENU=1` environment variable to skip the main menu and load the last save directly (`GameInit.BootDeferred`). Wired into the MCP `godot_run_project` environment in `.opencode/opencode.json`, so agent runs auto-skip during dev. Override with `EGGBERT_SKIP_MENU=0` or unset to test the menu. CLI: `EGGBERT_SKIP_MENU=1 godot --path .`

### Autoload singletons (defined in `project.godot`)
| Singleton | Class | Role |
|-----------|-------|------|
| `GameController` | `Node` | Level loading/unloading, tilemap bounds → camera |
| `DialogManager` | `Node2D` | NPC dialog lines + `DialogBubble` |
| `AudioManager` | `Node` | Music cross-fade (2-player pool) |
| `Player` | `CharacterBody2D` | WASD movement, dash, save/load |
| `FadeTransition` | `CanvasLayer` | Screen fade between levels |
| `SaveLoadManager` | `Node` | Persist via `ResourceSaver` → `user://savegame.tres` |
| `Inventory` | `Node` | Item stacks by id, ISavable, seeds test items on new game |
| `Equipment` | `Node` | Equip/unequip Weapon/Armor/Accessory, applies stats to HealthComponent + ParryComponent, ISavable |
| `CombatController` | `Node` | EnterCombat scene swap, saved overworld position, win/lose flow |

### Level loading
`GameController.LoadLevel(scenePath, position)` or `LoadLevel(scenePath, transitionName)`. Clears old children from `CurrentLevel` node, instantiates new packed scene, repositions player, fades in/out.

### Combat
`CombatOatmeal` fires 3 `RedBullet`s in a spread every 2s toward the player. `HealthComponent` handles HP/damage. `ParryComponent` handles proximity parry. `CombatController` manages EnterCombat/return flow. `Equipment` autoload applies stats.

### Dialog voice system
- `DialogVoiceResource` (`resources/dialog/DialogVoiceResource.cs`) — Godot `[GlobalClass]` Resource. All pitch/volume/stream params are `[Export]` → visible in Inspector. Double-click a `.tres` to tweak.
- NPCs have `[Export] public DialogVoiceResource Voice` — set in Inspector. Falls back to `DialogManager.DefaultVoice` (procedural 60ms sine blip at 440Hz) if null.
- Each character blip gets its own `AudioStreamPlayer` (one-shot, create → play → QueueFree after `BlipDuration`). Max 16 concurrent. Cleaned up in `_Process`.
- Friends send ~1–2 second .ogg recordings of vowel sounds → drop into `assets/audio/sfx/dialog/` → create a `DialogVoiceResource` .tres → assign to NPC. Only the first `BlipDuration` (default 80ms) of each clip plays per character, pitched per vowel.
`Player`, `WorldFlags`, and `Inventory` implement `ISavable`. Nodes in the `"persist"` group are iterated by `SaveLoadManager`. Save/load triggered from `OverworldMenu` (Esc/F1) + auto-save on level transition. Warps persist implicitly via WorldFlags (`warp_<id>` flags). Player health field exists in `SaveDataPlayer` but is unused until `HealthComponent` lands.

## Conventions

- **C# only** for game code. GDScript exists only in `addons/` (AsepriteWizard).
- **No tests** — no test framework, no test project. Don't try to run tests.
- **No CI** — no GitHub Actions or other CI config.
- **Physics layers**: 1=Player, 2=Walls, 3=NPCs, 4=Bullets, 5=Interactables, 6=Enemies, 7=TriggerAreas, 8=PlayerHitbox, 9=EnemyHitbox, 10=Items. Constants in `components/core/CollisionConfig.cs`.
- **Inputs**: WASD movement, E=interact/dialog advance, F1/Esc=menu, Space=dash, Shift=sprint, J=parry (combat), arrow keys+E=choice menu selection.
- Inventory and Equipment are wired up (autoload + OverworldMenu Items/Equipment panels + save).

## Design unknowns — ASK, don't assume

Most major design decisions are resolved in `DESIGN.md` (combat = bullet-hell dodge, dialog = WorldFlags branching, etc.). Remaining open questions (each tracked as a `design` issue):
- Story/narrative (who is Eggbert? what's the plot?) — **#9**
- Consumable items (what do they do?) — **#6** *(framework ready, unblocked)*
- Equipment stats (what do they affect?) — **#7** *(data fields exist, application wired but Attack/Speed unused)*
- Difficulty tuning (easy mode? HP scaling?) — *not yet filed*

**Use the `question` tool** before implementing anything that touches these areas. The user prefers being offered choices rather than having decisions made for them.

## GitHub workflow — agents read issues before coding

This project is tracked through GitHub Issues. **All non-trivial work is tied to an issue.** Read the tracker before starting; file a new issue before non-trivial work.

### Where things live
- **Issues**: https://github.com/McCune1224/eggbert/issues
- **Labels**: `bug`, `enhancement`, `design`, `documentation`, `question`; plus `priority-high|medium|low` and `phase-4|5|6`.
- **Milestones**: `Phase 4 — Combat + Dialog` (#1), `Phase 5 — Game Structure` (#2), `Phase 6 — Content & Polish` (#3).

### All work commits directly to `main`
No feature branches. No PRs. The editor-test loop is painful when work is spread across branches, so all commits land on `main`. This is intentional.

1. **Pick an issue**. Issues tagged `priority-high` should go first. Confirm with the user if several are open.
2. **Implement** against the issue's acceptance criteria. The issue body lists files, repro, and a suggested fix — treat it as the spec.
3. **Verify** with `dotnet build` (and `godot_run_project` + `godot_get_debug_output` for runtime behavior).
4. **Commit** on `main` with a message that references the issue in its body (`Closes #N`). GitHub auto-closes the issue when the commit lands on `main`.
5. **Push** `main` to origin.

### Filing a new issue
Before writing any code that isn't a one-line fix, file an issue with:
- A clear title prefixed by type where helpful ([Bug], [Enhancement], [Design]).
- **Problem/Current state** — file path + line numbers, what's wrong or missing.
- **Goal/Fix** — concrete proposal, with code sketches where useful.
- **Acceptance criteria** — a checkbox list defining "done".
- **Labels**: one of `bug`/`enhancement`/`design`/`documentation`; one `priority-*`; one `phase-*`; and the correct **milestone**.
- A `Closes #N` reference at the bottom so the issue closes on commit.

### Agent conventions for the tracker
- **Never** work without an issue number referenced in the commit — the user needs the paper trail.
- **Never** commit with `Closes #N` unless the acceptance criteria are actually met. If a criteria is deferred (e.g. playtest), leave the issue open and follow up.
- **Design issues** require the `question` tool — walk the user through options and record the decision in `STORY.md` or `DESIGN.md` before closing.
- If you discover new work while implementing, **file a fresh issue** rather than silently expanding scope. Link it ("related: #N") in the commit message.
- Keep `TODO.md` reconciled with the tracker (see #5) — it's a curated index that links to issues, not the source of truth.

### `gh` quick reference
```bash
gh issue list --state open                  # what's outstanding
gh issue view <N>                           # read the spec before coding
gh issue create --label bug --milestone "Phase 4 — Combat + Dialog"
gh issue close <N> --comment "Landed in <short-sha>"   # fallback if commit didn't auto-close
```

## Godot MCP reference

The `godot-mcp` server provides these tools. Use them instead of manual scene editing:
- `godot_run_project`, `godot_stop_project`, `godot_get_debug_output`
- `godot_launch_editor`
- `godot_create_scene`, `godot_save_scene`, `godot_add_node`, `godot_load_sprite`
- `godot_get_project_info`, `godot_get_godot_version`

GODOT_PATH: `/usr/local/bin/godot`

> **Note — 2026-07-06**: The MCP `environment` field in `.opencode/opencode.json` must be named `environment` (not `env` — the schema rejects unknown keys silently). If `godot_run_project` returns success but `godot_get_debug_output` immediately reports "No active Godot process", the env var didn't propagate — check the field name. 
