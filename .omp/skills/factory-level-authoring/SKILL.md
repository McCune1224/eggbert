---
name: factory-level-authoring
description: Mandatory editor-first contract for authoring a complete Eggbert level
---

Read `docs/godot-editor-guide.md`, `docs/level-authoring.md`, `docs/factory-opening.md`, `LOGGING.md`, and `../godot-authoring/SKILL.md` before editing.

## 1. Resolve design facts

Confirm rewards, progression flags, puzzle outcomes, combat/mercy outcomes, difficulty, and return routes. Ask focused questions instead of inventing a load-bearing narrative or balance value.

## 2. Plan a graph

Record arrival, mandatory interactions, gate flag/item, puzzle input and success output, save point, optional content, outgoing source transition, target scene and direct-root node, and return route.

## 3. Author in the editor

Use the Godot editor and retained `level_assembly` addon for scene nodes, TileMapLayer painting, collision, animations, and nested Resources. Create `CutsceneResource` and `DialogBranch` `.tres` files through the Inspector. Use typed snake_case GDScript for missing reusable behavior. Never hand-edit tilemap data, atlas subresources, nested Resources, or generated UIDs.

## 4. Compose

Use a Node2D root with `levels/base_level.gd`, a direct-root tilemap at `(0, 0)`, and direct-root gameplay components. Keep stable PascalCase transition, trigger, save-point, and puzzle node names. Configure NodePaths only after both referenced nodes exist. A WarpPoint requires a matching WarpDatabase entry and unlock path. Use only approved ItemDatabase IDs and provide a checkpoint/return path for mandatory encounters.

## 5. Verify

Import with:

```bash
godot --headless --path . --editor --quit
```

Run `verify_factory_layout.gd` twice with `FACTORY_LAYOUT_SCENE=AssemblyLine` and `ControlRoom`, then the relevant expansion, quest, dialog, combat, warp, save, and UI verifiers. Exercise transitions and save/reload when in-editor behavior matters. Run with `EGGBERT_LOG_LEVEL=debug` and inspect `user://logs/eggbert_YYYY-MM-DD.log` for `LevelTransition`, `CutsceneTrigger`, `WorldFlags`, `SavePoint`, and puzzle tags.

Report exact changed paths, flags, transitions, and observed commands/results. A scene skeleton is not a complete level.
