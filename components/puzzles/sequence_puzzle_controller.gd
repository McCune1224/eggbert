class_name SequencePuzzleController
extends Node

@export_group("Targets")
@export var plate_paths: Array[NodePath] = []
@export var target_door_path: NodePath
@export_group("Timing")
@export var time_window: float = 5.0

var _next_expected_index: int = 0
var _timer: Timer
var _target_door: Door
var _plates: Array[SequencePressurePlate] = []

func _ready() -> void:
	_timer = Timer.new()
	_timer.one_shot = true
	_timer.timeout.connect(_reset_puzzle)
	add_child(_timer)
	if not target_door_path.is_empty():
		_target_door = get_node_or_null(target_door_path) as Door
	for path in plate_paths:
		var plate := get_node_or_null(path) as SequencePressurePlate
		_plates.append(plate)
		if plate != null:
			plate.controller = self

func step_pressed(index: int) -> void:
	if index < 0 or index >= _plates.size():
		return
	if index == _next_expected_index:
		_plates[index].flash(true)
		_next_expected_index += 1
		_timer.start(time_window)
		if _next_expected_index >= _plates.size():
			_timer.stop()
			if _target_door != null:
				_target_door.open()
	else:
		_plates[index].flash(false)
		_reset_puzzle()

func _reset_puzzle() -> void:
	_next_expected_index = 0
	_timer.stop()
	for plate in _plates:
		if plate != null:
			plate.flash(false)
