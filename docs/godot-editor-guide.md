# Godot Editor Guide — Eggbert

Reference for Godot 4.7 editor setup, retained addons, typed GDScript conventions, and Resource authoring.

## Project setup

- Viewport: 640×360, `canvas_items` stretch, theme `res://assets/themes/eggbert_theme.tres`.
- Main scene: `boot/GameInit.tscn`.
- Inputs: WASD movement, E interact/advance, Esc pause, Space dash, Shift sprint, F check, J parry, Tab dialog log, Backtick debug overlay.
- Skip menu for a debug run with `EGGBERT_SKIP_MENU=1`.
- Import and parse from the root with `godot --headless --path . --editor --quit`.

## Retained addons

| Addon | Use | Status |
|---|---|---|
| `addons/AsepriteWizard/` | Spritesheet, tileset, and texture imports | Enabled |
| `addons/cutscene_inspector/` | Typed cutscene and dialog-resource editing | Enabled |
| `addons/level_assembly/` | Preconfigured scene instances | Opt-in |
| `addons/nklbdev.aseprite_importers/` | Low-level Aseprite support | Available, opt-in |

Enable Level Assembly from Project > Project Settings > Plugins. The Cutscene Inspector edits `CutsceneResource.steps` and `DialogBranch.nodes` through UndoRedo and preserves typed subresources.

## Editor-first Resources

Use the Inspector to create and save nested `CutsceneResource`, `CutsceneStep`, `DialogBranch`, `DialogNode`, quest, and item Resources. Hand-author only flat `.tres` files with scalar values or external Resource references. Never hand-edit tilemap data, atlas subresources, nested arrays, or generated UIDs. Exported property names are snake_case and must exactly match the attached GDScript.

## Scene hierarchy

```text
Node2D (levels/base_level.gd)
├── LevelTileMapLayer (direct child; do not hand-edit tilemap data)
├── LevelTransition / SavePoint / WarpPoint
├── NPC and trigger components
├── Doors, switches, and puzzles
└── PickupItem / ConditionalItem
```

Direct-root tilemaps and gameplay components are required. PascalCase node names referenced by NodePaths are stable API. Runtime files and fields are snake_case. Use typed `@export var target_path: NodePath`, direct autoload names, native signals, and `PackedScene.instantiate()`.

## Component reference

| Component | Scene | Script | Important fields |
|---|---|---|---|
| Level transition | `levels/level_transition.tscn` | `levels/level_transition.gd` | `level`, `target_transition_name`, `side`, `size`, `required_flag` |
| Save point | `components/core/save_point.tscn` | `components/core/save_point.gd` | `location_name` |
| Door | `components/puzzles/door.tscn` | `components/puzzles/door.gd` | `start_open` |
| Key door | `components/puzzles/key_door.tscn` | `components/puzzles/key_door.gd` | `required_flag`, `locked_message` |
| Timed door | `components/puzzles/timed_door.tscn` | `components/puzzles/timed_door.gd` | `open_duration` |
| Floor switch | `components/puzzles/floor_switch.tscn` | `components/puzzles/floor_switch.gd` | `target_door_path` |
| Push block | `components/puzzles/push_block.tscn` | `components/puzzles/push_block.gd` | `directional_mode` |
| Teleport pad | `components/puzzles/teleport_pad.tscn` | `components/puzzles/teleport_pad.gd` | `target_pad_path` |
| Conveyor | `components/puzzles/conveyor_tile.tscn` | `components/puzzles/conveyor_tile.gd` | `direction`, `speed` |
| Moving platform | `components/puzzles/moving_platform.tscn` | `components/puzzles/moving_platform.gd` | AnimationPlayer endpoints |
| Timed spikes | `components/puzzles/timed_spikes.tscn` | `components/puzzles/timed_spikes.gd` | `telegraph_duration`, `active_duration` |
| Spike tile | `components/puzzles/spike_tile.tscn` | `components/puzzles/spike_tile.gd` | `damage` |
| Weighted plate | `components/puzzles/weighted_pressure_plate.tscn` | `components/puzzles/weighted_pressure_plate.gd` | body weight |

Configure NodePaths only after both nodes exist. Keep arrival points clear, reserve escape routes around conveyors, and avoid mandatory pre-combat encounters without a checkpoint.

## Dialog and cutscenes

For one-off dialog, configure `CutsceneTrigger.dialog_lines`, `trigger_mode`, `once`, and `cutscene_id`. For branching interactions, create a `DialogBranch` `.tres`; each `DialogNode` has an id, speaker, lines, responses, condition, and enter flags. Each response has text, next node id, selection flag, and condition.

`CutsceneStep` retains the serialized action order from `SAY_DIALOG` through `DIALOG_BRANCH`; use the Cutscene Inspector rather than editing the array by hand. Conditions are Always, FlagSet, FlagNotSet, and ChoiceEquals. Voice resources support optional `.ogg` clips and an 80 ms procedural fallback.

## Combat authoring

A `CombatArena` scene contains a camera, `CombatHUD`, and enemy nodes. `CombatController` enters with an arena path and spawn position, then returns through `GameController.load_level_at_position`. Enemies use idle → telegraph → attack → cooldown; J triggers proximity parry. No pre-combat save is used.

## Verification

Run the narrowest relevant script under `tests/` after importing. See `docs/verification.md` for the complete command matrix and fixtures.
