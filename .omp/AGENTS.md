# .omp/AGENTS.md — Eggbert

Godot 4.7 C# RPG. Undertale/EarthBound inspired, 640×360, top-down (zero gravity).

Read ROADMAP.md for feature objectives. Read DESIGN.md for design decisions.

## Commands

```bash
dotnet build          # compile C# (Godot.NET.Sdk/4.7.0, net8.0)
```

## Architecture

### Boot order
boot/GameInit.tscn → Main menu or debug-skip → GameController.LoadLevel → player at saved pos

Debug auto-start: EGGBERT_SKIP_MENU=1 env var skips menu, loads last save. Set in .omp/mcp.json.

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
| `SaveLoadManager` | `Node` | Persist via ResourceSaver → user://savegame.tres |
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

## Conventions
- C# only for game code. GDScript in addons/ only (AsepriteWizard).
- No tests, no CI.
- Physics layers: constants in components/core/CollisionConfig.cs. 1=Player, 2=Walls, 3=NPCs, 4=Bullets, 5=Interactables, 6=Enemies, 7=TriggerAreas, 8=PlayerHitbox, 9=EnemyHitbox, 10=Items.
- Inputs: WASD, E=interact, F1/Esc=menu, Space=dash, Shift=sprint, J=parry.
- All work commits directly to main. No branches, no PRs.

## Design unknowns — ASK, don't assume
- Story/narrative (who is Eggbert?) — #9
- Consumable items (what do they do?) — #6
- Equipment stats (what do they affect?) — #7
- Difficulty tuning (easy mode? HP scaling?) — not yet filed

## GitHub workflow
File an issue before non-trivial work. Commit with `Closes #N` on main. Push.
