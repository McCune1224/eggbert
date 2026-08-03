---
name: new-scene
description: Create a Godot scene with the project's typed GDScript structure. Usage: /new-scene <type> <path>
---

Create a scene for the type and path in `$ARGUMENTS`, relative to `res://`:

- `level`: Node2D with `levels/base_level.gd` and a direct-root `LevelTileMapLayer`.
- `enemy`: CharacterBody2D or Area2D with CollisionShape2D and Sprite2D; choose a layer from `components/core/collision_config.gd`.
- `npc`: CharacterBody2D with interaction area, collision, and sprite.
- `bullet`: Area2D following `combat/components/red_bullet.tscn`.
- `ui`: CanvasLayer or Control following existing `ui/` scenes.
- `item`: Area2D on Items layer 10.

Use snake_case filenames and typed `@export` fields. Configure nodes and nested Resources in the editor, save the scene, then import it with `godot --headless --path . --editor --quit` and run a relevant verifier.
