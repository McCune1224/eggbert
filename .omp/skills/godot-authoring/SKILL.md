---
name: godot-authoring
description: Editor-first rules for Godot 4.7 GDScript scenes and Resources
---

## Editor versus code

Use the Godot editor for placing nodes, painting TileMapLayer data, configuring collision layers, setting AnimationPlayers and SpriteFrames, and saving nested Resources. Use typed GDScript for runtime logic, signals, state machines, dynamic scene construction, and deterministic verifiers.

## Resource serialization

- Create nested `CutsceneResource`, `CutsceneStep`, `DialogBranch`, and quest Resources through the Inspector, then Save As `.tres`.
- Hand-author only flat resources with scalar fields, primitive arrays, or external Resource references.
- Do not hand-edit `tile_map_data`, atlas subresources, nested Resource arrays, or generated `uid://` values.
- For typed arrays, use the field type declared by the attached GDScript and let the editor serialize it; do not invent `Array[ExtResource]` syntax.
- Exported property names are snake_case and must match the attached GDScript exactly.

## Scene conventions

Levels use a Node2D root with `levels/base_level.gd`, a direct-root `LevelTileMapLayer`, and direct-root gameplay components. Stable PascalCase node names are API surface for transitions, cutscenes, puzzles, and save points. Files and exported properties are snake_case.

```gdscript
class_name ExampleComponent
extends Area2D

signal activated
@export var required_flag: StringName
@export var target_path: NodePath
```

Use direct autoload names (`WorldFlags`, `GameController`, `Player`) and native `signal.connect`, `signal.emit`, and `await` idioms. Instantiate scenes with `PackedScene.instantiate()`.

## Verification

Import and parse after authoring:

```bash
godot --headless --path . --editor --quit
```

Then run the narrowest relevant `tests/verify_*.gd` script. Use `docs/verification.md` for the complete list and environment fixtures.
