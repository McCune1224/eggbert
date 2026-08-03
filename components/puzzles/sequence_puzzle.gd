class_name SequencePuzzle
extends Node

@export_group("Sequence")
@export var switch_sequence: Array[NodePath] = []
@export var target_door_path: NodePath
@export var latch_on_complete: bool = true

var _switches: Array[FloorSwitch] = []
var _target_door: Door
var _current_index: int = 0
var _completed: bool = false

func _ready() -> void:
	for path in switch_sequence:
		var switch_node := get_node_or_null(path) as FloorSwitch
		_switches.append(switch_node)
		if switch_node != null:
			switch_node.pressed.connect(_on_switch_pressed.bind(switch_node))
	if not target_door_path.is_empty():
		_target_door = get_node_or_null(target_door_path) as Door

func _on_switch_pressed(switch_node: FloorSwitch) -> void:
	if _completed:
		return
	var pressed_index := _switches.find(switch_node)
	if pressed_index < 0:
		return
	if pressed_index == _current_index:
		_current_index += 1
		if _current_index >= _switches.size():
			_completed = true
			if _target_door != null:
				_target_door.open()
	else:
		_reset_all()

func _reset_all() -> void:
	_current_index = 0
	for switch_node in _switches:
		if switch_node != null:
			switch_node.reset()
	if not latch_on_complete and _target_door != null:
		_target_door.close()
