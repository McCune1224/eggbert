# .omp/AGENTS.md — Eggbert

> **Harness note (2026-08-03):** The Oh My Pi harness is retired. `.omp/` remains as
> the repo's authoring contract, but the executable skills now live in Hermes:
> `eggbert-level-authoring`, `eggbert-godot-authoring`, `eggbert-godot-csharp-patterns`.
> The godot-mcp server/addon was removed 2026-08-05 (#169) — no MCP server for Godot.

Godot 4.7 C# RPG. Undertale/EarthBound inspired, 640×360, top-down (zero gravity).

Read ROADMAP.md for feature objectives. Read DESIGN.md for design decisions. Read LOGGING.md for the logging system and AI debugging recipes.
Read docs/godot-editor-guide.md for the editor setup, plugin usage, component reference, dialog/cutscene authoring, combat/quest/item systems, and architecture conventions.

## Commands

```bash
dotnet build          # compile C# (Godot.NET.Sdk/4.7.0, net8.0)
```

## Architecture

### Boot order
boot/GameInit.tscn → Main menu or debug-skip → GameController.LoadLevel → player at saved pos

Debug auto-start: EGGBERT_SKIP_MENU=1 env var skips menu, loads last save.

### Autoload singletons
| Singleton | Class | Role |
|-----------|-------|------|
| `GameController` | `Node` | Level loading/unloading, tilemap bounds → camera |
| `WorldFlags` | `Node` | Dictionary<string, Variant>, dialog branching, warp/quest progression, ISavable |
| `DialogManager` | `Node2D` | NPC dialog lines + DialogBubble |
| `AudioManager` | `Node` | Music cross-fade (2-player pool) |
| `Player` | `CharacterBody2D` | WASD movement, dash, save/load |
| `FadeTransition` | `CanvasLayer` | Screen fade between levels |
| `CutsceneController` | `Node` | Resource-driven cutscene player |
| `DebugOverlay` | `Node` | Debug HUD overlay |
| `SaveManager` | `Node` | Persist via ResourceSaver → user://savegame.tres |
| `Inventory` | `Node` | Item stacks by id, ISavable |
| `Equipment` | `Node` | Equip/unequip Weapon/Armor/Accessory, stat application |
| `CombatController` | `Node` | EnterCombat scene swap, win/lose flow |

### Level loading
GameController.LoadLevel(scenePath, playerPosition|transitionName, skipAutoSave). Clears CurrentLevel, instantiates scene, repositions player, fades.

### Combat
CombatController.EnterCombat(arenaPath, playerSpawn). State machine on enemies (idle→telegraph→attack→cooldown). Proximity parry (J key). Win/lose returns to overworld.

### Dialog voice system
DialogVoiceResource ([GlobalClass] Resource) per NPC, procedural fallback (60ms sine blip). One-shot AudioStreamPlayer per blip, max 16 concurrent.

### Save system
ISavable interface. Nodes in "persist" group auto-saved. Single slot: user://savegame.tres.

### Logging system
See LOGGING.md for the full logging reference. Quick facts:
- **File:** `user://logs/eggbert_YYYY-MM-DD.log` (auto-rotates)
- **Env:** `EGGBERT_LOG_LEVEL=debug` for verbose traces
- **Bridge:** `GameLogBridge` captures engine errors to file
- **Init:** `GameLogger.InitializeFromEnv()` in `boot/GameInit.cs`

## Verification workflow
- C# only — no GDScript outside `addons/`. Write verifiers as C# `SceneTree` scripts in `tests/` (e.g. `tests/VerifyFactoryExpansion.cs`), run with `godot --headless --path . --script res://tests/<Name>.cs`.
- Prefer test-driven changes for behavior. Write the smallest targeted verification script before or alongside the code change when practical.
- For scene/layout changes, use a C# verifier that instantiates the scene and checks node names, positions, exported properties, and resource references. Keep it headless when `_Ready()` side effects are irrelevant; add the scene to the tree only when `_Ready()` behavior is what you're proving.
- Run `dotnet build`, then the relevant `tests/*.cs` verifiers, before commit.


## Conventions
- C# only for game code and `tests/` verifiers. GDScript exists only in `addons/` (AsepriteWizard, editor plugins).
- No tests, no CI.
- Physics layers: constants in components/core/CollisionConfig.cs. 1=Player, 2=Walls, 3=NPCs, 4=Bullets, 5=Interactables, 6=Enemies, 7=TriggerAreas, 8=PlayerHitbox, 9=EnemyHitbox, 10=Items.
- Inputs: WASD, E=interact, Esc=menu, Space=dash, Shift=sprint, J=parry.
- All work commits directly to main. No branches, no PRs.

## Design unknowns — ASK, don't assume
- Story/narrative (who is Eggbert?) — #9
- Consumable items (what do they do?) — #6
- Equipment stats (what do they affect?) — #7
- Difficulty tuning (easy mode? HP scaling?) — not yet filed

## GitHub workflow
File an issue before non-trivial work. Commit with `Closes #N` on main. Push.
