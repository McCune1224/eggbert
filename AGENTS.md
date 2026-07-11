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
`boot/GameInit.tscn` (main scene) → `GameController.LoadLevel("overworld/Overworld.tscn")` → player placed at (0,0). No main menu exists yet.

### Autoload singletons (defined in `project.godot`)
| Singleton | Class | Role |
|-----------|-------|------|
| `GameController` | `Node` | Level loading/unloading, tilemap bounds → camera |
| `DialogManager` | `Node2D` | NPC dialog lines + typewriter `TextBox` |
| `AudioManager` | `Node` | Music cross-fade (2-player pool) |
| `Player` | `CharacterBody2D` | WASD movement, dash, save/load |
| `FadeTransition` | `CanvasLayer` | Screen fade between levels |
| `SaveLoadManager` | `Node` | Persist via `ResourceSaver` → `user://savegame.tres` |
| `Inventory` | `Node` | Item stacks by id, ISavable, seeds test items on new game |
| `Equipment` | `Node` | Equip/unequip Weapon/Armor/Accessory, applies stats to HealthComponent + ParryComponent, ISavable |
| `CombatController` | `Node` | EnterCombat scene swap, saved overworld position, win/lose flow |

`components/core/SoundConfig.cs` exists but is not wired into anything yet.

### Level loading
`GameController.LoadLevel(scenePath, position)` or `LoadLevel(scenePath, transitionName)`. Clears old children from `CurrentLevel` node, instantiates new packed scene, repositions player, fades in/out.

### Combat
Phase 4 underway. `CombatOatmeal` fires 3 `RedBullet`s in a spread every 2s toward the player. `HealthComponent` (HP/damage Node) being built — unblocks consumables, equipment stats, combat HP, death/respawn. `ParryComponent` replaces graze meter. `CombatController` handles EnterCombat/return flow. `Equipment` autoload manages equip/unequip.

### Saving
`Player`, `WorldFlags`, and `Inventory` implement `ISavable`. Nodes in the `"persist"` group are iterated by `SaveLoadManager`. Save/load triggered from `OverworldMenu` (Esc/F1) + auto-save on level transition. Warps persist implicitly via WorldFlags (`warp_<id>` flags). Player health field exists in `SaveDataPlayer` but is unused until `HealthComponent` lands.

## Conventions

- **C# only** for game code. GDScript exists only in `addons/` (AsepriteWizard).
- **No tests** — no test framework, no test project. Don't try to run tests.
- **No CI** — no GitHub Actions or other CI config.
- **Physics layers**: 1=Player, 2=Walls, 3=NPCs, 4=Bullets, 5=Interactables, 6=Enemies, 7=TriggerAreas, 8=PlayerHitbox, 9=EnemyHitbox, 10=Items. Constants in `components/core/CollisionConfig.cs`.
- **Inputs**: WASD movement, E=interact, F1/Esc=menu, Space=advance dialog / dash, Shift=sprint, J=parry (combat), arrow keys+E=choice menu selection.
- `components/core/SoundConfig.cs` exists but is not wired into anything yet. Inventory is wired up (autoload + OverworldMenu Items panel + save).

## Design unknowns — ASK, don't assume

Most major design decisions are resolved in `DESIGN.md` (combat = bullet-hell dodge, dialog = WorldFlags branching, etc.). Remaining open questions (each tracked as a `design` issue):
- Story/narrative (who is Eggbert? what's the plot?) — **#9**
- Consumable items (what do they do?) — **#6** *(framework ready, unblocked)*
- Equipment stats (what do they affect?) — **#7** *(data fields exist, application wired but Attack/Speed unused)*
- Difficulty tuning (easy mode? HP scaling?) — *not yet filed*

**Use the `question` tool** before implementing anything that touches these areas. The user prefers being offered choices rather than having decisions made for them.

## GitHub workflow — agents read issues before coding

This project is tracked through GitHub Issues + a Project Board. **All non-trivial work is tied to an issue.** Read the tracker before starting; file a new issue before opening a branch.

### Where things live
- **Issues**: https://github.com/McCune1224/eggbert/issues
- **Project Board**: Kanban (Backlog → Up Next → In Progress → Done). *(Board creation pending `gh auth refresh -s project` — see note below.)*
- **Labels**: `bug`, `enhancement`, `design`, `documentation`, `question`; plus `priority-high|medium|low` and `phase-4|5|6`.
- **Milestones**: `Phase 4 — Combat + Dialog` (#1), `Phase 5 — Game Structure` (#2), `Phase 6 — Content & Polish` (#3).

### Working an issue (standard flow)
1. **Pick an issue**. Issues tagged `priority-high` should go first. Confirm with the user if several are open.
2. **Create a branch** named after the work — no worktrees, just branches:
   - `fix/<short-slug>` for bugs (e.g. `fix/combat-controller-lambda`)
   - `feat/<short-slug>` for features (e.g. `feat/enemy-state-machine`)
   - `design/<short-slug>` for design decisions
   - `docs/<short-slug>` for documentation
3. **Implement** against the issue's acceptance criteria. The issue body lists files, repro, and a suggested fix — treat it as the spec.
4. **Verify** with `dotnet build` (and `godot_run_project` + `godot_get_debug_output` for runtime behavior).
5. **Open a PR** against `main`. Title/description reference the issue: `Closes #N`. Link the PR to the milestone if applicable.
6. **Move the issue** on the project board to `In Progress` when starting, `Done` when the PR merges.

### Filing a new issue
Before writing any code that isn't a one-line fix, file an issue with:
- A clear title prefixed by type where helpful ([Bug], [Enhancement], [Design]).
- **Problem/Current state** — file path + line numbers, what's wrong or missing.
- **Goal/Fix** — concrete proposal, with code sketches where useful.
- **Acceptance criteria** — a checkbox list defining "done".
- **Labels**: one of `bug`/`enhancement`/`design`/`documentation`; one `priority-*`; one `phase-*`; and the correct **milestone**.
- A suggested branch name at the bottom (`→ PR closes #N`).

### Agent conventions for the tracker
- **Never** work without an issue number referenced in the branch/PR — the user needs the paper trail.
- **Never** close an issue manually with a commit message unless the acceptance criteria are actually met. PR merge auto-closes via `Closes #N`.
- **Design issues** require the `question` tool — walk the user through options and record the decision in `STORY.md` or `DESIGN.md` before closing.
- If you discover new work while implementing, **file a fresh issue** rather than silently expanding scope. Link it ("related: #N") in the current PR.
- Keep `TODO.md` reconciled with the tracker (see #5) — it's a curated index that links to issues, not the source of truth.

### `gh` quick reference
```bash
gh issue list --state open                  # what's outstanding
gh issue view <N>                           # read the spec before coding
gh issue create --label bug --milestone "Phase 4 — Combat + Dialog"
gh pr create --title "..." --body "Closes #N"
gh pr merge --merge                          # after review
```

## Godot MCP reference

The `godot-mcp` server provides these tools. Use them instead of manual scene editing:
- `godot_run_project`, `godot_stop_project`, `godot_get_debug_output`
- `godot_launch_editor`
- `godot_create_scene`, `godot_save_scene`, `godot_add_node`, `godot_load_sprite`
- `godot_get_project_info`, `godot_get_godot_version`

GODOT_PATH: `/usr/lib/godot-mono/godot.linuxbsd.editor.x86_64.mono`

> **Note — 2026-07-06**: The MCP `environment` field in `.opencode/opencode.json` must be named `environment` (not `env` — the schema rejects unknown keys silently). If `godot_run_project` returns success but `godot_get_debug_output` immediately reports "No active Godot process", the env var didn't propagate — check the field name.

> **Note — 2026-07-10**: Project Board creation is blocked on the `project` OAuth scope. Run `gh auth refresh -s project` (interactive — opens a browser/device-code flow) to enable `gh project` commands. Once done, create a Board named `Eggbert Development` linked to the repo with columns `Backlog`, `Up Next`, `In Progress`, `Done`, then add all open issues to it. Issues #1–#9 already exist; the Board just isn't wired up yet.
