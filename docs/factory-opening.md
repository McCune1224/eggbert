# Factory opening — canonical level reference

The opening route is the three-room sequence `OpeningZone → SortingFloor → LoadingBay` under `levels/factory/maps/`. It teaches movement, interaction, one puzzle, saving, and the first story transition without changing the narrative goals in `STORY.md`.

## Route graph

| Room | Arrival | Required beat | Gate/output | Exit |
|---|---|---|---|---|
| OpeningZone | Player at `Vector2.ZERO` | Talk to `TimeClock` with E | Sets `tutorial_clocked_out` | Transition to SortingFloor |
| SortingFloor | Named transition | Talk to `FactoryJamitor` | Sets `met_jamitor`; introduces crate puzzle | Transition to LoadingBay |
| LoadingBay | Named transition | Push `FactoryCrate` onto `FactoryPressurePlate` | Opens `CrateGate`; crossing triggers one-shot arrest | Sets `arrested`, `cutscene_arrest`, `warp_eggsile_area1` |
| Eggsile area1 | `HubArrival` | Continue prison route | Existing story flags | Normal level graph |

Stable PascalCase node names are referenced by transitions and verifiers. Keep the tilemap and gameplay components direct children of the level root. Use the Inspector for tilemap data, nested Resources, and generated UIDs.

## Authoring conventions

Runtime scripts are snake_case GDScript paths. Attach `levels/base_level.gd` to each level root and configure typed exported fields in the Inspector. Use `CutsceneResource` `.tres` files for arrest and NPC sequences; use `CutsceneTrigger.dialog_lines` only for simple one-off lines. Call `GameController.load_level_at_transition(scene_path, target_transition_name)` for named arrivals and `load_level_at_position(scene_path, player_position)` for explicit positions.

## Verification

Import from the repository root:

```bash
godot --headless --path . --editor --quit
FACTORY_LAYOUT_SCENE=AssemblyLine godot --headless --path . --script res://tests/verify_factory_layout.gd
FACTORY_LAYOUT_SCENE=ControlRoom godot --headless --path . --script res://tests/verify_factory_layout.gd
godot --headless --path . --script res://tests/verify_factory_expansion.gd
```

The layout verifier checks direct-root tilemaps, named transitions and flags, save points, puzzle configuration, pickup IDs, and route producers. Exercise the route interactively with `godot --path .` when validating movement, pushing, cutscene sequencing, and the one-shot arrest. Read the latest `user://logs/eggbert_YYYY-MM-DD.log` after a failed traversal.
