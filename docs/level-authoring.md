# Level authoring guide

This guide describes the editor-first workflow for adding a complete level to Eggbert's Godot 4.7 GDScript project. Read `docs/factory-opening.md` for the canonical three-room example and `LOGGING.md` before debugging a traversal.

## Before editing

Inspect nearby scenes and typed scripts with `glob`, `grep`, and `read`. Confirm narrative, rewards, progression flags, puzzle outcomes, combat outcomes, and difficulty from `STORY.md` and `DESIGN.md`; ask focused questions for anything unresolved. Write a compact graph table containing arrival, required interactions, gate flag/item, puzzle input/output, save point, optional content, outgoing source transition, target scene and transition node, and return route.

## Editor-first authoring

Use the Godot editor and retained `level_assembly` addon for nodes, TileMapLayer painting, collision shapes, animations, and nested Resources. Create `CutsceneResource`, `CutsceneStep`, and `DialogBranch` `.tres` resources with the Inspector and Cutscene Inspector. Use typed snake_case GDScript for reusable runtime behavior.

Never hand-edit `tile_map_data`, atlas subresources, nested Resource arrays, or generated UIDs. Save the scene through the editor after configuring exports. Runtime files, exported properties, functions, and signals are snake_case; PascalCase node names are stable references for transitions and NodePaths.

## Required level structure

```text
Node2D (levels/base_level.gd)
├── LevelTileMapLayer (direct child at 0,0)
├── LevelTransition / SavePoint / WarpPoint
├── NPC and CutsceneTrigger components
├── Doors, switches, and puzzles
└── PickupItem / ConditionalItem
```

Keep gameplay components direct children of the level root rather than nesting them below the tilemap. Configure `BaseLevel` metadata and ambient audio. A `LevelTransition` references its scene and `target_transition_name`; a `SavePoint` has a `location_name`; a `WarpPoint` uses a `WarpDatabase` id and documented unlock flag. Configure NodePaths only after both endpoints exist. Do not add mandatory combat without a known save/checkpoint and return route.

## Runtime APIs

Use direct autoload names. Position-based loading:

```gdscript
GameController.load_level_at_position(scene_path, player_position)
```

Named transition loading:

```gdscript
GameController.load_level_at_transition(scene_path, target_transition_name)
```

Persistent nodes implement `get_save_key`, `serialize`, `deserialize`, and `get_load_priority`; save data remains at `user://savegame.tres`.

## Verification

From the project root:

```bash
godot --headless --path . --editor --quit
godot --headless --path . --script res://tests/verify_migration_integrity.gd
```

Run the relevant targeted scripts from `docs/verification.md`, including both factory layout variants. Exercise both directions of each transition, interact with every required NPC/puzzle, test save/reload and combat return, and inspect the newest log with `EGGBERT_LOG_LEVEL=debug`. Report exact changed files, flags, transition pairs, and observed commands. A scene skeleton is not a finished level.
