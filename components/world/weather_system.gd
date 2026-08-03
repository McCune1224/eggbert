extends Node

@export var min_interval: float = 60.0
@export var max_interval: float = 180.0
@export var rain_duration: float = 30.0

var _timer: Timer
var _is_raining: bool = false
var _rain_particles: GPUParticles2D
var _dark_overlay: ColorRect
var _tween: Tween

func _ready() -> void:
	_rain_particles = get_node_or_null("RainParticles") as GPUParticles2D
	if _rain_particles != null:
		_rain_particles.emitting = false
	_dark_overlay = ColorRect.new()
	_dark_overlay.color = Color(0.0, 0.0, 0.0, 0.0)
	_dark_overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_dark_overlay.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_timer = Timer.new()
	_timer.one_shot = true
	_timer.timeout.connect(_start_rain)
	add_child(_timer)
	_timer.start(randf_range(min_interval, max_interval))

func _start_rain() -> void:
	if _is_raining:
		return
	_is_raining = true
	if _rain_particles != null:
		_rain_particles.amount = 100
		_rain_particles.emitting = true
	var root := get_tree().root
	if _dark_overlay.get_parent() == null:
		root.add_child(_dark_overlay)
	_dark_overlay.visible = true
	_tween = create_tween()
	_tween.tween_property(_dark_overlay, "color", Color(0.0, 0.0, 0.0, 0.15), 2.0)
	_timer.timeout.disconnect(_start_rain)
	_timer.timeout.connect(_stop_rain)
	_timer.start(rain_duration)

func _stop_rain() -> void:
	_is_raining = false
	if _rain_particles != null:
		_rain_particles.emitting = false
	_tween = create_tween()
	_tween.tween_property(_dark_overlay, "color", Color(0.0, 0.0, 0.0, 0.0), 2.0)
	_tween.tween_callback(_hide_overlay)
	_timer.timeout.disconnect(_stop_rain)
	_timer.timeout.connect(_start_rain)
	_timer.start(randf_range(min_interval, max_interval))

func _hide_overlay() -> void:
	_dark_overlay.visible = false
	if _dark_overlay.get_parent() != null:
		_dark_overlay.get_parent().remove_child(_dark_overlay)
