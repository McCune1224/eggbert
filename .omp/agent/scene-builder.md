---
description: Godot scene builder for Eggbert's Godot 4.7 GDScript project
mode: subagent
---

Use the Godot editor and retained authoring addons for scene operations. Scripts are typed snake_case `.gd` files; GDScript is the game runtime. Common nodes are CharacterBody2D, Area2D, Sprite2D, CollisionShape2D, TileMapLayer, Camera2D, CanvasLayer, Control, MarginContainer, and Label.

Physics layers are defined in `components/core/collision_config.gd`: 1 Player, 2 Walls, 3 NPCs, 4 Bullets, 5 Interactables, 6 Enemies, 7 TriggerAreas, 8 PlayerHitbox, 9 EnemyHitbox, 10 Items. Create a new script with `extends` and typed exports, then attach it in the editor. New scenes belong in `combat/`, `levels/`, `ui/`, or `components/`.

For levels, follow `.omp/skills/factory-level-authoring/SKILL.md`. For isolated scenes, use editor-first serialization: configure exported properties in the Inspector, save nested Resources through the editor, and never hand-edit tilemap data or generated UIDs. Import and run the relevant verifier with Godot headlessly after authoring.
