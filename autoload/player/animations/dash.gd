extends Node2D

@export var dash_scale: float = 3.0
@export var dash_duration: float = 0.2
@export var dash_cooldown: float = 0.4

var dash_direction: Vector2 = Vector2.ZERO
var _can_dash: bool = true
var _duration_timer: Timer
var _ghost_timer: Timer
var _ghost_scene: PackedScene

func _ready() -> void:
	_duration_timer = get_node_or_null("DurationTimer") as Timer
	_ghost_timer = get_node_or_null("GhostTimer") as Timer
	if _duration_timer != null:
		_duration_timer.timeout.connect(_stop_dash)
	if _ghost_timer != null:
		_ghost_timer.timeout.connect(_spawn_dash_ghost)
	_ghost_scene = load("res://autoload/player/animations/DashGhost.tscn") as PackedScene

func start_dash(direction: Vector2) -> Vector2:
	if not _can_dash:
		return Vector2.ZERO
	dash_direction = direction.normalized()
	if dash_direction == Vector2.ZERO:
		dash_direction = Vector2.DOWN
	_can_dash = false
	_spawn_dash_ghost()
	if _ghost_timer != null:
		_ghost_timer.start()
	if _duration_timer != null:
		_duration_timer.start(dash_duration)
	var sprite := get_parent().get_node_or_null("Sprite2D") as Sprite2D
	if sprite != null:
		var shader := load("res://autoload/player/animations/DashGhost.gdshader") as Shader
		if shader != null:
			var material := ShaderMaterial.new()
			material.shader = shader
			sprite.material = material
	return dash_direction

func is_dashing() -> bool:
	return _duration_timer != null and not _duration_timer.is_stopped()

func _stop_dash() -> void:
	if _ghost_timer != null:
		_ghost_timer.stop()
	var sprite := get_parent().get_node_or_null("Sprite2D") as Sprite2D
	if sprite != null:
		sprite.material = null
	dash_direction = Vector2.ZERO
	var cooldown_timer := get_tree().create_timer(dash_cooldown)
	cooldown_timer.timeout.connect(_enable_dash)

func _enable_dash() -> void:
	_can_dash = true

func _spawn_dash_ghost() -> void:
	if _ghost_scene == null:
		return
	var player := get_parent() as Node2D
	var sprite := player.get_node_or_null("Sprite2D") as Sprite2D
	if sprite == null:
		return
	var ghost := _ghost_scene.instantiate() as Sprite2D
	ghost.global_position = player.global_position
	ghost.texture = sprite.texture
	ghost.hframes = sprite.hframes
	ghost.vframes = sprite.vframes
	ghost.frame = sprite.frame
	ghost.flip_h = sprite.flip_h
	ghost.modulate = Color(1.0, 1.0, 1.0, 0.5)
	get_tree().root.add_child(ghost)
