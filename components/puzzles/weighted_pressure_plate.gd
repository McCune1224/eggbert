class_name WeightedPressurePlate
extends Area2D

signal plate_pressed
signal plate_released

@export_group("Target")
@export var target_door_path: NodePath
@export_group("Progression")
@export var pushable_pressed_flag: String = ""

var _body_count: int = 0
var _target_door: Door
var _sprite: Sprite2D

func _ready() -> void:
	collision_layer = 0
	collision_mask = CollisionConfig.PLAYER_LAYER | CollisionConfig.INTERACTABLE_LAYER
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	if not target_door_path.is_empty():
		_target_door = get_node_or_null(target_door_path) as Door
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)

func _on_body_entered(body: Node2D) -> void:
	if not body.is_in_group("player") and not body.is_in_group("pushable"):
		return
	_body_count += 1
	if body.is_in_group("pushable") and not pushable_pressed_flag.is_empty():
		_set_world_flag(pushable_pressed_flag)
	if _body_count == 1:
		_press()

func _on_body_exited(body: Node2D) -> void:
	if not body.is_in_group("player") and not body.is_in_group("pushable"):
		return
	_body_count = maxi(0, _body_count - 1)
	if _body_count == 0:
		_release()

func _press() -> void:
	if _sprite != null:
		_sprite.position = Vector2(0.0, 4.0)
	if _target_door != null:
		_target_door.open()
	plate_pressed.emit()

func _release() -> void:
	if _sprite != null:
		_sprite.position = Vector2.ZERO
	if _target_door != null:
		_target_door.close()
	plate_released.emit()

func _set_world_flag(flag: String) -> void:
	var world_flags := get_tree().root.get_node_or_null("WorldFlags")
	if world_flags != null:
		world_flags.call("set_flag", flag, true)
