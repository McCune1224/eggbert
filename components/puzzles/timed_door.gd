class_name TimedDoor
extends Door

@export_group("TimedDoor")
@export var open_duration: float = 3.0
@export var blink_before_close: bool = true

var _close_timer: Timer
var _warning_timer: Timer
var _blink_tween: Tween
var _open_generation: int = 0
const BLINK_DURATION: float = 1.0

func _ready() -> void:
	_close_timer = Timer.new()
	_close_timer.one_shot = true
	_close_timer.timeout.connect(_on_close_timer_timeout)
	add_child(_close_timer)
	_warning_timer = Timer.new()
	_warning_timer.one_shot = true
	_warning_timer.timeout.connect(_start_blinking)
	add_child(_warning_timer)
	super._ready()

func open() -> void:
	_open_generation += 1
	_stop_blinking()
	_close_timer.stop()
	_warning_timer.stop()
	super.open()
	if open_duration <= 0.0:
		_close_deferred(_open_generation)
		return
	_close_timer.start(open_duration)
	if not blink_before_close:
		return
	if open_duration <= BLINK_DURATION:
		_start_blinking()
	else:
		_warning_timer.start(open_duration - BLINK_DURATION)

func close() -> void:
	if _close_timer != null and not _close_timer.is_stopped():
		return
	_warning_timer.stop()
	_stop_blinking()
	super.close()

func _on_close_timer_timeout() -> void:
	_warning_timer.stop()
	_stop_blinking()
	super.close()

func _close_deferred(generation: int) -> void:
	if generation != _open_generation:
		return
	_warning_timer.stop()
	_stop_blinking()
	super.close()

func _start_blinking() -> void:
	_stop_blinking()
	_blink_tween = create_tween().set_loops(6)
	_blink_tween.tween_property(self, "modulate", Color(1.0, 1.0, 1.0, 0.15), BLINK_DURATION / 12.0)
	_blink_tween.tween_property(self, "modulate", Color(1.0, 1.0, 1.0, 0.45), BLINK_DURATION / 12.0)

func _stop_blinking() -> void:
	if _blink_tween != null:
		_blink_tween.kill()
		_blink_tween = null
