# AGENTS.md — Eggbert

Godot 4.7 C# RPG. Undertale/EarthBound inspired, 640×360, top-down (zero gravity).

Read MASTER_ROADMAP.md for the phased plan and goals. Read ROADMAP.md for feature objectives. Read DESIGN.md for design decisions.
Read docs/godot-editor-guide.md for editor setup, custom plugin usage, component reference, dialog/cutscene authoring, combat/quest/item systems, and architecture conventions.

## Commands

```bash
dotnet build          # compile C# (Godot.NET.Sdk/4.7.0, net8.0)
godot --headless --path . --script res://tests/<Name>.cs   # run a C# verifier
godot --headless --path . --script res://tests/VerifyAllLevels.cs   # scene gate: auto-discovers every levels/*/maps/*.tscn, instantiates each, checks transitions + warps
```

### Demo route (the shipped tutorial)
New Game → `levels/factory/maps/OpeningZone.tscn` → SortingFloor → AssemblyLine → ControlRoom → LoadingBay → arrest → `levels/eggsile/maps/EggsIsle.tscn`. Five factory rooms + Eggs Isle intake, ~10 minutes. See `docs/factory-opening.md` for the beat-by-beat contract and `tests/VerifyFactoryExpansion.cs` for the structural contract.

## Architecture

### Boot order
boot/GameInit.tscn → Main menu (New Game/Continue/Settings/Quit) or debug-skip → GameController.LoadLevel → player at saved position.

Debug auto-start: EGGBERT_SKIP_MENU=1 env var skips menu, loads last save.

### Autoload singletons (project.godot)
| Singleton | Class | Role |
|-----------|-------|------|
| GameController | Node | Level loading/unloading, tilemap bounds → camera |
| WorldFlags | Node | Dictionary<string, Variant>, dialog branching, warp/quest progression, ISavable |
| DialogManager | Node2D | NPC dialog lines + DialogBubble |
| AudioManager | Node | Music cross-fade (2-player pool) |
| Player | CharacterBody2D | WASD movement, dash, save/load |
| FadeTransition | CanvasLayer | Screen fade between levels |
| CutsceneController | Node | Resource-driven cutscene player (CutsceneResource + CutsceneStep + CutsceneCondition) |
| DebugOverlay | Node | Debug HUD overlay (FPS, state info) |
| SaveManager | Node | Persist via ResourceSaver → user://savegame.tres |
| Inventory | Node | Item stacks by id, ISavable, seeds test items on new game |
| Equipment | Node | Equip/unequip Weapon/Armor/Accessory, applies stats, ISavable |
| CombatController | Node | EnterCombat scene swap, saved overworld position, win/lose flow |

### Level loading
GameController.LoadLevel(scenePath, playerPosition|transitionName, skipAutoSave). Clears CurrentLevel children, instantiates scene, repositions player, fades.

### Combat
CombatController.EnterCombat(arenaPath, playerSpawn) → CombatArena with enemies. CombatOatmeal has 4 flavors (spread/burst/homing/aimed). State machine: idle→telegraph→attack→cooldown. Proximity parry (J key) via ParryComponent. CombatHUD with reactive HP bars. Arenas: OatmealArena, GenericArena.

### Dialog voice system
DialogVoiceResource ([GlobalClass] Resource) per NPC, procedural fallback (60ms sine blip at 440Hz). One-shot AudioStreamPlayer per blip, max 16 concurrent.

### Save system
ISavable interface. Nodes in "persist" group auto-saved. Single slot: user://savegame.tres. Saves player position/health, WorldFlags, warp unlocks, inventory, equipment.

### Dev save states (#168)
Named dev save states snapshot the full game state anywhere and load it back to test features/zones without replaying the demo. `SaveManager.SaveGame(..., slotName)` writes `user://saves/<slot>.tres`; committed read-only fixtures live in `tests/savestates/*.tres` and are resolved as a fallback (fresh clones get them for free).
- **Hotkeys** (no F-keys — they collide with editor binds): `Ctrl+S` capture quick slot, `Ctrl+L` load quick/last slot, `Ctrl+M` toggle the DevSaveStates menu (list/capture/load/delete/rename). Blocked during dialogs, cutscenes, and combat.
- **Boot**: `EGGBERT_LOAD_STATE=<slot>` (parallel to `EGGBERT_SKIP_MENU`) loads a named state at startup, even without a `savegame.tres`.
- **Regenerate fixtures**: `godot --headless --path . --script res://tests/GenerateSaveStateFixtures.cs`; verify with `res://tests/VerifySaveStates.cs`.
- Named-slot loads are non-destructive: corrupt/stale states are reported and kept, never deleted (unlike the default slot).

## Conventions
- C# only for game code and `tests/` verifiers. GDScript exists only in `addons/` (AsepriteWizard, editor plugins).
- No tests, no CI.
- Physics layers in components/core/CollisionConfig.cs: 1=Player, 2=Walls, 3=NPCs, 4=Bullets, 5=Interactables, 6=Enemies, 7=TriggerAreas, 8=PlayerHitbox, 9=EnemyHitbox, 10=Items.
- Inputs: WASD movement, E=interact/dialog advance, Esc=menu, Space=dash, Shift=sprint, J=parry (combat), arrow keys+E=choice menu selection.
- All work commits directly to main. No branches, no PRs. **Exception: when 2+ agents run concurrently, see "Concurrent agents" below — every agent works on its own branch in its own worktree.**

## Design unknowns — ASK, don't assume
- Story/narrative (who is Eggbert?) — #9
- Consumable items (what do they do?) — #6
- Equipment stats (what do they affect?) — #7
- Difficulty tuning (easy mode? HP scaling?) — not yet filed

## GitHub workflow

File an issue before non-trivial work. Commit with `Closes #N` on main. Push.

### Issue lifecycle (agents must follow)
1. **File an issue** before starting non-trivial work (bug, feature, design, content).
2. **Label it** at creation: `bug` / `enhancement` / `design` / `content` + `priority-*` + area label (`dialog`, `combat`, `puzzles`, `audio`, `inventory`, `story`, `demo`, …).
3. **Assign the phase milestone** — the milestone set mirrors MASTER_ROADMAP.md:
   - `Phase 1 — Story playable end-to-end` (demo content chain #75–#90, scene stability)
   - `Phase 2 — Design lockdown` (design decisions #6/#7/#9)
   - `Phase 3 — Content depth & world feel` (QoL, secrets, NPC behaviors)
   - `Phase 4 — Polish & release` (art replacement, juice, QA)
   - `Phase 5 — Post-release backlog` (FEATURE_IDEAS.md pulls)
4. **Implement**, then verify: `dotnet build` (0 warnings) + `VerifyAllLevels.cs` when scenes touched.
5. **Commit with `Closes #N`** — the issue auto-closes as `completed`.

### Close-reason discipline
- `completed` — ONLY when the work is verifiably done (built, tested, committed). Never mark an issue `completed` because the idea was dropped or parked.
- `not planned` — the idea is dropped or parked (e.g. moved to FEATURE_IDEAS.md). Reopen + reclose with `--reason "not planned"` to fix a wrong reason.
- Design issues (#7 lesson): never close as `completed` without recording the actual decision in the issue. If no decision was made, leave it open.
- Audit trail matters: a future agent searching "was X built?" reads close reasons and labels. A false `completed` hides missing work.

### Issue audit conventions
- **Verify before closing bug reports**: run `VerifyAllLevels.cs` / the relevant verifier; cite the commit that fixed it in the close comment.
- **Never bulk-close**: the 2026-07-17 sweep marked 18 unbuilt FEATURE_IDEAS items as `completed`; they were re-marked `not planned` in the 2026-08-05 audit. Close one issue at a time with an accurate reason and comment.

**When another agent is committing to main**: do feature work in a separate git worktree
(`git worktree add <path> -b feature/<name> main`), and keep it synced with
`git rebase main` — never merge. Fast-forward main to the feature tip when done.
See the `eggbert-git-rebase-workflow` skill for the full procedure.

## Concurrent agents (2–3 agents at once)

**Goal: linear main, zero stepping on each other.** Main is the only long-lived branch.
Every agent works in its own worktree on a short-lived `feature/<area>` branch, rebases
onto main, and lands via fast-forward. Never merge main into a feature branch; never
commit to main directly while other agents are active.

### File ownership — this is the conflict-avoider

Two agents editing the **same `.tscn` scene** produces an unmergeable mess (scene files
and `tile_map_data` cannot be hand-merged). Assign areas so files don't overlap:

| Agent | Owns | Avoid touching |
|-------|------|----------------|
| Levels agent | `levels/**/*.tscn`, `levels/**/*.cs`, `docs/factory-opening.md`, `docs/level-authoring.md` | components/, autoload/ |
| Systems agent | `components/**/*.cs`, `autoload/**/*.cs`, `resources/dialog/**`, `resources/cutscene/**` | levels/ scenes |
| Content agent | `ItemDatabase.cs`, `resources/quests/**`, `.tres` resources, `docs/` design docs | scene files, component logic |

**Hot files (single-owner, additive-only):** `AGENTS.md`, `ItemDatabase.cs`,
`CollisionConfig.cs`, `project.godot`. Only one agent edits these per session; when they
must change, **append** (new flags/items/keys) instead of rewriting, so rebases don't collide.

### Cadence

- **Before starting, before committing, and before landing: `git rebase main`.**
- **Land small and often** — prefer 1-commit daily landings over week-long branches.
  Long-lived branches accumulate conflicts; short ones rebase trivially.
- If an agent's work must live >2 days, rebase onto main at least daily.
- Land in order; after each fast-forward, the next agent rebases again.

### Landing (from the main worktree)

```bash
git merge --ff-only feature/<area>   # only after that agent's rebase + verify
git push origin main
```

### What agents must NOT do

- No `git checkout -b` in a dirty worktree — create a new worktree instead.
- No `git merge main` into a feature branch (breaks linear history).
- No `git add -A` / `git add .` — stray `.import` sidecars and `.hermes/` exist.
- No touching another agent's worktree or committing on their behalf.
- No rebasing a branch that has already been pushed/shared (rewrites history).

## Feature ideas
`FEATURE_IDEAS.md` is a loose bucket of feature ideas — dialog, puzzles, NPC behaviors,
atmosphere, items, secrets. No priority, no phases. Pull from it when you want something to build.
