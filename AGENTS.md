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

Most major design decisions are resolved in `DESIGN.md` (combat = bullet-hell dodge, dialog = WorldFlags branching, etc.). Remaining open questions:
- Story/narrative (who is Eggbert? what's the plot?)
- Consumable items (what do they do?) — *framework ready, effects blocked on HealthComponent*
- Equipment stats (what do they affect?) — *data fields exist, application blocked on stat system*
- Difficulty tuning (easy mode? HP scaling?)

**Use the `question` tool** before implementing anything that touches these areas. The user prefers being offered choices rather than having decisions made for them.

## Godot MCP reference

The `godot-mcp` server provides these tools. Use them instead of manual scene editing:
- `godot_run_project`, `godot_stop_project`, `godot_get_debug_output`
- `godot_launch_editor`
- `godot_create_scene`, `godot_save_scene`, `godot_add_node`, `godot_load_sprite`
- `godot_get_project_info`, `godot_get_godot_version`

GODOT_PATH: `/usr/lib/godot-mono/godot.linuxbsd.editor.x86_64.mono`

> **Note — 2026-07-06**: The MCP `environment` field in `.opencode/opencode.json` must be named `environment` (not `env` — the schema rejects unknown keys silently). If `godot_run_project` returns success but `godot_get_debug_output` immediately reports "No active Godot process", the env var didn't propagate — check the field name.
