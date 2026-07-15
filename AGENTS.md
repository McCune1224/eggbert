# AGENTS.md — Eggbert

Godot 4.7 C# RPG. Undertale/EarthBound inspired, 640×360, top-down (zero gravity).

Read ROADMAP.md for feature objectives. Read DESIGN.md for design decisions.

## Commands

```bash
dotnet build          # compile C# (Godot.NET.Sdk/4.7.0, net8.0)
```

Godot MCP tools available via `godot-mcp` server (godot_run_project, godot_launch_editor, godot_get_debug_output, etc.).

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
| SaveLoadManager | Node | Persist via ResourceSaver → user://savegame.tres |
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

## Conventions
- C# only for game code. GDScript exists only in addons/ (AsepriteWizard).
- No tests, no CI.
- Physics layers in components/core/CollisionConfig.cs: 1=Player, 2=Walls, 3=NPCs, 4=Bullets, 5=Interactables, 6=Enemies, 7=TriggerAreas, 8=PlayerHitbox, 9=EnemyHitbox, 10=Items.
- Inputs: WASD movement, E=interact/dialog advance, Esc=menu, Space=dash, Shift=sprint, J=parry (combat), arrow keys+E=choice menu selection.
- All work commits directly to main. No branches, no PRs.

## Design unknowns — ASK, don't assume
- Story/narrative (who is Eggbert?) — #9
- Consumable items (what do they do?) — #6
- Equipment stats (what do they affect?) — #7
- Difficulty tuning (easy mode? HP scaling?) — not yet filed

## GitHub workflow
File an issue before non-trivial work. Commit with `Closes #N` on main. Push.

## Feature ideas
`FEATURE_IDEAS.md` is a loose bucket of feature ideas — dialog, puzzles, NPC behaviors,
atmosphere, items, secrets. No priority, no phases. Pull from it when you want something to build.
