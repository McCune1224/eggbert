# Eggbert development checkpoint

**Date:** 2026-08-02  
**Project:** Godot 4.7 statically typed GDScript RPG  
**Working tree:** Migration work is in progress on `port/gdscript-migration`; preserve unrelated changes.

## Current stable state

The port retains the 640×360 pixel-art presentation, overworld-to-arena loop, authored scenes/resources, input map, and progression goals. Runtime scripts use snake_case `.gd` paths and direct autoload names. The repository root is the only Godot project root.

### Scene and resource migration

- Patrol, sleeping-NPC, and cutscene resources have typed GDScript counterparts.
- Authored cutscene resources retain typed `CutsceneStep` subresources and their action enum order.
- Level scenes keep direct-root tilemap layers and stable transition, save-point, puzzle, NPC, and flag node names.
- Character sprites and placeholder visuals remain part of the sparse map presentation.

### Runtime contracts

- `GameController.load_level_at_position(scene_path, player_position)` handles position-based placement.
- `GameController.load_level_at_transition(scene_path, target_transition_name)` resolves named transitions and directional offsets.
- Persistent nodes implement `get_save_key`, `serialize`, `deserialize`, and `get_load_priority`.
- `SaveManager` uses `user://savegame.tres`; invalid old resources are removed and produce a fresh-run path.

## Logging

`GameLogger` writes tagged game-originated messages to `user://logs/eggbert_YYYY-MM-DD.log`. `EGGBERT_LOG_LEVEL=debug|info|warn|error|off` controls filtering, `EGGBERT_LOG_ECHO=0` selects file-only output, and the five newest daily files are retained. Godot engine errors remain in stdout/stderr because engine-level logger interception is not part of the GDScript port. Read the latest log before diagnosing a new issue.

## Verification workflow

From the repository root, import and parse the project with:

```bash
godot --headless --path . --editor --quit
godot --headless --path . --script res://tests/verify_migration_integrity.gd
```

Then run the targeted scripts listed in `docs/verification.md`, including both `FACTORY_LAYOUT_SCENE=AssemblyLine` and `FACTORY_LAYOUT_SCENE=ControlRoom`. Do not treat a scene skeleton as complete: verify transitions, interactions, flags, save/reload, and arena return.

## Working-tree guidance

Do not reset or discard unrelated modifications. Keep commits focused on the migration branch and review before integration. Preserve authored scenes and resources; use the Godot editor for nested resources and generated UIDs rather than hand-editing serialized internals.

## Next work

Continue visual traversal of the remaining maps, inspect fresh logs after transitions, expand deterministic verifier coverage, and fill unresolved narrative, consumable, and difficulty decisions through the design process before implementing them.
