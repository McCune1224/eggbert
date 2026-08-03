extends Sprite2D

@export var trigger_radius: float = 80.0
@export var scuttle_distance: float = 48.0
@export var scuttle_speed: float = 60.0
@export var pause_min: float = 1.0
@export var pause_max: float = 3.0

var _start_position: Vector2
var _target_position: Vector2
var _moving: bool = false
var _returning: bool = false
var _pause_timer: float = 0.0

func _ready() -> void:
	_start_position = position
	_target_position = _start_position + Vector2(scuttle_distance, 0.0)
	flip_h = false
	var trigger_area := Area2D.new()
	var shape := CollisionShape2D.new()
	var circle := CircleShape2D.new()
	circle.radius = trigger_radius
	shape.shape = circle
	trigger_area.add_child(shape)
	trigger_area.collision_layer = 0
	trigger_area.collision_mask = CollisionConfig.PLAYER_LAYER
	trigger_area.body_entered.connect(_on_trigger_entered)
	add_child(trigger_area)
	_pause_timer = randf_range(pause_min, pause_max)

func _process(delta: float) -> void:
	if not _moving and _pause_timer > 0.0:
		_pause_timer -= delta
		if _pause_timer <= 0.0:
			_moving = true
			_returning = false
			flip_h = true
		return
	if not _moving:
		return
	var target := _start_position if _returning else _target_position
	var direction := target - position
	if direction.length_squared() < 4.0:
		position = target
		_moving = false
		_pause_timer = randf_range(pause_min, pause_max)
		_returning = not _returning
		flip_h = not _returning
		return
	position += direction.normalized() * scuttle_speed * delta

func _on_trigger_entered(body: Node2D) -> void:
	if body.is_in_group("player") and not _moving and _pause_timer > 0.0:
		_pause_timer = 0.1
