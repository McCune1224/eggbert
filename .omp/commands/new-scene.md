---
name: new-scene
description: Create a new Godot scene with proper structure. Usage: /new-scene <type> <path> (e.g., /new-scene enemy combat/enemies/Slime)
---
Create a new Godot scene for Eggbert using $ARGUMENTS. The first argument is the scene type (level, enemy, npc, bullet, ui, item), the second is the path relative to res://.

Choose root node and structure based on type:
- level: Node2D (BaseLevel.cs). Add LevelTileMapLayer.
- enemy: CharacterBody2D or Area2D. Add CollisionShape2D, Sprite2D. Set layer per CollisionConfig.
- npc: CharacterBody2D. Add PromptArea2D, CollisionShape2D, Sprite2D.
- bullet: Area2D (reference RedBullet.tscn). Add CollisionShape2D, Sprite2D.
- ui: CanvasLayer or Control. Follow patterns in ui/.
- item: Area2D. Set collision layer 10 (Items).

New scenes go in appropriate subdirectories. Always build after creating C# scripts.
