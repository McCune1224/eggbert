class_name MultiSwitchGate
extends Node

enum GateMode {
	AND,
	OR,
}

@export_group("Targets")
@export var switch_paths: Array[NodePath] = []
@export var target_door_path: NodePath
@export var mode: GateMode = GateMode.AND
@export var latch_open: bool = false

var _switches: Array[FloorSwitch] = []
var _target_door: Door
var _has_opened: bool = false

func _ready() -> void:
	for path in switch_paths:
		var switch_node := get_node_or_null(path) as FloorSwitch
		_switches.append(switch_node)
		if switch_node != null:
			switch_node.pressed.connect(_evaluate)
			switch_node.released.connect(_evaluate)
	if not target_door_path.is_empty():
		_target_door = get_node_or_null(target_door_path) as Door
	_evaluate()

func _evaluate() -> void:
	if _switches.is_empty() or _target_door == null or (latch_open and _has_opened):
		return
	var should_open := _are_all_pressed() if mode == GateMode.AND else _is_any_pressed()
	if should_open:
		_has_opened = true
		_target_door.open()
	elif not latch_open:
		_target_door.close()

func _are_all_pressed() -> bool:
	for switch_node in _switches:
		if switch_node == null or not switch_node.is_pressed:
			return false
	return true

func _is_any_pressed() -> bool:
	for switch_node in _switches:
		if switch_node != null and switch_node.is_pressed:
			return true
	return false
