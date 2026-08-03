# Eggbert contributor guide

Godot 4.7, statically typed GDScript, 640×360 pixel-art top-down RPG inspired by Undertale and EarthBound. Game scripts use snake_case `.gd` paths and PascalCase `class_name` declarations; the game runs directly in Godot.

Read `ROADMAP.md` for objectives, `DESIGN.md` for settled design, `LOGGING.md` for diagnostics, and `docs/godot-editor-guide.md` before authoring scenes or resources.

## Commands

From the repository root:

```bash
# Import resources and parse the project without opening a window
godot --headless --path . --editor --quit
# Run a targeted verifier
godot --headless --path . --script res://tests/verify_migration_integrity.gd
# Run the game
godot --path .
```

Targeted structural verifiers live in `tests/`: `verify_factory_layout.gd`, `verify_factory_expansion.gd`, `verify_quest_auto_pin.gd`, `verify_dialog_branch.gd`, `verify_combat_once_flag.gd`, `verify_warp_fix.gd`, and `verify_save_manager.gd`. The UI verifier is `verify_ui_colors.gd` at the project root. Factory layout checks select a map with `FACTORY_LAYOUT_SCENE=AssemblyLine` or `ControlRoom`.

## Architecture

### Boot order

`boot/GameInit.tscn` opens the main menu (or skips it when `EGGBERT_SKIP_MENU=1`), then `GameController` loads the current level and restores Player's saved position.

### Autoloads

| Name | Path | Role |
|---|---|---|
| `WorldFlags` | `autoload/world_flags.tscn` | Persistent story, quest, and warp flags |
| `QuestManager` | `autoload/quest_manager.tscn` | Ordered quest objectives and pinning |
| `GameController` | `autoload/game_controller.tscn` | Level replacement, pause/fade ordering, camera bounds |
| `DialogManager` | `autoload/dialog_manager.tscn` | Dialog sessions and `DialogBubble` |
| `AudioManager` | `autoload/audio_manager.tscn` | Music, ambience, and SFX |
| `Player` | `autoload/player/player.tscn` | Movement, interaction, combat return, persistence |
| `FadeTransition` | `ui/fade_transition.tscn` | Screen fades and location banners |
| `CutsceneController` | `autoload/cutscene_controller.gd` | Resource-driven cutscenes |
| `DebugOverlay` | `autoload/debug_overlay.gd` | Runtime diagnostics |
| `SaveManager` | `saves/save_manager.tscn` | `user://savegame.tres` persistence |
| `Inventory` | `autoload/inventory.gd` | Item counts |
| `Equipment` | `autoload/equipment.gd` | Equipment and stat bonuses |
| `CombatController` | `autoload/combat_controller.gd` | Arena entry and overworld return |
| `KeybindManager` | `autoload/keybind_manager.gd` | Input bindings |
| `FactoryOpeningFlow` | `levels/factory/factory_opening_flow.gd` | Opening route state |

Use these names directly. Do not add singleton wrapper accessors.

### Level loading

`GameController` exposes two non-overloaded methods:

```gdscript
GameController.load_level_at_position(scene_path, player_position)
GameController.load_level_at_transition(scene_path, target_transition_name)
```

Both clear the current level, pause and fade, instantiate the requested scene, place `Player`, emit `level_loaded`, and unpause. Transition loading applies the existing directional offset. Position callers use the first method; named-transition callers use the second.

### Save contract

Nodes in the `persist` group implement `get_save_key() -> String`, `serialize() -> Dictionary[String, Variant]`, `deserialize(data: Dictionary[String, Variant]) -> void`, and `get_load_priority() -> int`. `SaveManager` keeps `player`, `inventory`, `equipment`, and `world_flags` keys, loads in priority order (Player 10, Equipment 5, other persistent nodes 0), and deletes invalid save resources before starting a fresh run.

### Combat and dialog

Combat is an arena handoff: `CombatController` saves the overworld position, instantiates an arena, and returns through `GameController.load_level_at_position`. Enemies follow idle → telegraph → attack → cooldown. Proximity parry uses J. Cutscenes are `CutsceneResource` `.tres` files containing typed `CutsceneStep` resources and optional `CutsceneCondition`/`DialogBranch` resources.

## Conventions

- Tabs for indentation; typed variables, parameters, returns, and collections.
- `class_name` and scene node names are PascalCase. Files, functions, variables, signals, and exported fields are snake_case; constants are `CONSTANT_CASE`.
- Prefer `@export`, `@onready`, native signals (`signal.connect`, `signal.emit`, `await node.signal`), `PackedScene.instantiate()`, and typed `NodePath` lookups.
- Physics layers remain 1 Player, 2 Walls, 3 NPCs, 4 Bullets, 5 Interactables, 6 Enemies, 7 TriggerAreas, 8 PlayerHitbox, 9 EnemyHitbox, 10 Items. Definitions are in `util/collision_config.gd`.
- Inputs: WASD movement, E interact/advance, Esc pause, Space dash, Shift sprint, J parry, F check, Tab dialog log, and Backtick debug overlay.
- Author scenes and nested resources in the Godot editor. Do not hand-edit tilemap data, atlas subresources, or generated UIDs.
- Supported editor addons are AsepriteWizard, `nklbdev.aseprite_importers`, `level_assembly`, and `cutscene_inspector`; the latter is GDScript-based.

## Workflow

Work on a feature branch (the migration branch is `port/gdscript-migration`), keep commits focused, and open a review when a change is ready. Keep `main` integration-only. Report changed paths and the exact headless verifier commands exercised. Read the latest `user://logs/eggbert_YYYY-MM-DD.log` before repeating a debugging loop.

## Design unknowns

Ask before inventing unresolved story, consumable, equipment, or difficulty decisions. `FEATURE_IDEAS.md` is an unprioritized idea bucket.
