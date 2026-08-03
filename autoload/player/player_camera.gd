extends Camera2D

var _shake_intensity: float = 0.0
var _shake_duration: float = 0.0
var _shake_elapsed: float = 0.0

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	var controller := get_tree().root.get_node_or_null("GameController")
	if controller != null and controller.has_signal("tile_map_bounds_changed"):
		controller.tile_map_bounds_changed.connect(update_limits)
		var bounds: Array[Vector2] = controller.get("current_tile_map_bounds")
		if not bounds.is_empty():
			update_limits(bounds)

func update_limits(bounds: Array[Vector2]) -> void:
	if bounds.size() < 2:
		return
	limit_left = int(bounds[0].x)
	limit_top = int(bounds[0].y)
	limit_right = int(bounds[1].x)
	limit_bottom = int(bounds[1].y)

func shake(intensity: float, duration: float) -> void:
	_shake_intensity = maxf(_shake_intensity, intensity)
	_shake_duration = maxf(_shake_duration, duration)
	_shake_elapsed = 0.0

func _process(delta: float) -> void:
	if _shake_elapsed < _shake_duration:
		_shake_elapsed += delta
		var decay := 1.0 - clampf(_shake_elapsed / _shake_duration, 0.0, 1.0)
		offset = Vector2(
			randf_range(-1.0, 1.0) * _shake_intensity * decay,
			randf_range(-1.0, 1.0) * _shake_intensity * decay,
		)
	elif offset != Vector2.ZERO:
		offset = Vector2.ZERO
