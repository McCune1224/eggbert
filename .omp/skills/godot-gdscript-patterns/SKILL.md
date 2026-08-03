---
name: godot-gdscript-patterns
description: Typed GDScript patterns for Eggbert — autoloads, signals, exports, async flow, scene loading, and input
---

## Autoloads

Autoload names registered in `project.godot` are global nodes. Call them directly; do not create static accessors:

```gdscript
func open_level(scene_path: String, transition_name: String) -> void:
    GameController.load_level_at_transition(scene_path, transition_name)
```

## Typed exports and signals

```gdscript
class_name InteractableExample
extends Area2D

signal interacted(actor: Node2D)

@export var target_path: NodePath
@export var speed: float = 200.0
@onready var prompt: Node = get_node("Prompt")

func _on_body_entered(body: Node2D) -> void:
    if body.is_in_group("player"):
        interacted.emit(body)
```

Connect native signals with `signal.connect(_on_signal)` and await them with `await node.signal_name`. Use snake_case file names, functions, fields, and signals; PascalCase `class_name` declarations and nodes; CONSTANT_CASE constants; tabs; and typed returns/collections.

## Input and collision

Use `_unhandled_input(event: InputEvent)` for one-shot actions and `Input.get_vector()` for movement:

```gdscript
func _unhandled_input(event: InputEvent) -> void:
    if event.is_action_pressed("interact"):
        get_viewport().set_input_as_handled()
        interacted.emit(self)

func _physics_process(_delta: float) -> void:
    velocity = Input.get_vector("player_left", "player_right", "player_up", "player_down") * speed
    move_and_slide()
```

Collision layers are defined in `components/core/collision_config.gd`: Player 1, Walls 2, NPCs 3, Bullets 4, Interactables 5, Enemies 6, TriggerAreas 7, PlayerHitbox 8, EnemyHitbox 9, Items 10.

## Scene and resource loading

```gdscript
var scene: PackedScene = load("res://levels/example/example.tscn")
var instance: Node = scene.instantiate()
add_child(instance)

GameController.load_level_at_position("res://levels/example/example.tscn", Vector2.ZERO)
GameController.load_level_at_transition("res://levels/example/example.tscn", "HubArrival")
```

Use typed `NodePath` exports and `get_node_or_null()` when a link is optional. Configure nested Resources and generated UIDs in the Godot editor.

## Persistence

Nodes in `persist` implement:

```gdscript
func get_save_key() -> String: return "example"
func serialize() -> Dictionary[String, Variant]: return {}
func deserialize(data: Dictionary[String, Variant]) -> void: pass
func get_load_priority() -> int: return 0
```

`SaveManager` validates these methods before serializing and stores `user://savegame.tres`.
