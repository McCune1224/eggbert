extends Sprite2D

@export var swing_speed: float = 2.0
@export var swing_angle: float = 5.0

var _time: float = 0.0

func _process(delta: float) -> void:
	_time += delta * swing_speed
	rotation = deg_to_rad(sin(_time) * swing_angle)
