class_name FloorSwitch
extends Area2D

signal pressed
signal released

@export_group("Target")
@export var target_door_path: NodePath
@export var latching: bool = false

var _body_count: int = 0
var _target_door: Door
var _has_triggered: bool = false

var is_pressed: bool:
	get:
		return _body_count > 0 or (latching and _has_triggered)

func _ready() -> void:
	collision_layer = 0
	collision_mask = CollisionConfig.PLAYER_LAYER | CollisionConfig.INTERACTABLE_LAYER
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)
	if not target_door_path.is_empty():
		_target_door = get_node_or_null(target_door_path) as Door

func _on_body_entered(_body: Node2D) -> void:
	if _body_count == 0 and not _has_triggered:
		pressed.emit()
		if _target_door != null:
			_target_door.open()
	_body_count += 1
	_has_triggered = true

func _on_body_exited(_body: Node2D) -> void:
	_body_count = maxi(0, _body_count - 1)
	if _body_count == 0:
		released.emit()
		if not latching or not _has_triggered:
			if _target_door != null:
				_target_door.close()
		if not latching:
			_has_triggered = false

func reset() -> void:
	_has_triggered = false
	_body_count = 0
