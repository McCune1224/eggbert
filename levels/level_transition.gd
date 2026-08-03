@tool
extends Area2D

signal transition_requested(level: String, target_transition_name: String)

enum TransitionSide {
	UP,
	DOWN,
	LEFT,
	RIGHT,
}

@export_file("*.tscn") var level: String = ""
@export var required_flag: String = ""
@export_enum("Up", "Down", "Left", "Right") var side: int = TransitionSide.UP:
	set(value):
		side = value
		_update_area()
@export_range(1, 12, 1) var size: int = 1:
	set(value):
		size = maxi(1, value)
		_update_area()
@export var snap_to_grid: bool = false:
	set(value):
		snap_to_grid = value
		if value:
			position = Vector2(roundf(position.x / 16.0) * 16.0, roundf(position.y / 16.0) * 16.0)
@export var target_transition_name: String = ""

@onready var collision_shape: CollisionShape2D = get_node_or_null("CollisionShape2D") as CollisionShape2D
var _transition_started: bool = false

func _ready() -> void:
	_update_area()
	if Engine.is_editor_hint():
		return
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)

func _process(_delta: float) -> void:
	if Engine.is_editor_hint():
		_update_area()

func _on_body_entered(body: Node2D) -> void:
	if _transition_started or not body.is_in_group("player"):
		return
	var flags := get_tree().root.get_node_or_null("WorldFlags")
	if not required_flag.is_empty() and (flags == null or not bool(flags.call("has_flag", required_flag))):
		return
	if level.is_empty():
		return
	_transition_started = true
	transition_requested.emit(level, target_transition_name)
	var controller := get_tree().root.get_node_or_null("GameController")
	if controller != null:
		if target_transition_name.is_empty() and controller.has_method("load_level_at_position"):
			controller.call("load_level_at_position", level, Vector2.ZERO)
		elif controller.has_method("load_level_at_transition"):
			controller.call("load_level_at_transition", level, target_transition_name)

func _on_body_exited(body: Node2D) -> void:
	if body.is_in_group("player"):
		_transition_started = false

func _update_area() -> void:
	if collision_shape == null:
		collision_shape = get_node_or_null("CollisionShape2D") as CollisionShape2D
	if collision_shape == null:
		return
	var rectangle := RectangleShape2D.new()
	var dimensions := Vector2(16.0, float(size) * 16.0)
	var offset := Vector2.ZERO
	match side:
		TransitionSide.LEFT:
			offset.x = -8.0
		TransitionSide.RIGHT:
			offset.x = 8.0
		TransitionSide.UP:
			dimensions = Vector2(float(size) * 16.0, 16.0)
			offset.y = -8.0
		TransitionSide.DOWN:
			dimensions = Vector2(float(size) * 16.0, 16.0)
			offset.y = 8.0
	rectangle.size = dimensions
	collision_shape.position = offset
	collision_shape.shape = rectangle
