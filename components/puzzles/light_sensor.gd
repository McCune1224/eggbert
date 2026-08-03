class_name LightSensor
extends Area2D

signal beam_received

@export_group("Target")
@export var target_door_path: NodePath
@export_group("Visuals")
@export var active_color: Color = Color(0.0, 1.0, 0.0, 0.5)
@export var inactive_color: Color = Color(1.0, 0.0, 0.0, 0.3)

var _active: bool = false
var _sprite: Sprite2D
var _target_door: Door

func _ready() -> void:
	collision_layer = CollisionConfig.TRIGGER_AREA_LAYER
	collision_mask = 0
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	if _sprite != null:
		_sprite.modulate = inactive_color
	if not target_door_path.is_empty():
		_target_door = get_node_or_null(target_door_path) as Door

func activate() -> void:
	if _active:
		return
	_active = true
	if _sprite != null:
		_sprite.modulate = active_color
	if _target_door != null:
		_target_door.open()
	beam_received.emit()

func deactivate() -> void:
	if not _active:
		return
	_active = false
	if _sprite != null:
		_sprite.modulate = inactive_color
	if _target_door != null:
		_target_door.close()
