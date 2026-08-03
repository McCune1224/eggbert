extends SceneTree

# Headless structural verifier for the Factory expansion rooms.
# Run once per room: FACTORY_LAYOUT_SCENE=AssemblyLine or ControlRoom.
# Each process instantiates one scene to keep headless verification isolated.

const FACTORY_ROOT := "res://levels/factory/maps/"

var _failures: Array[String] = []
var _checks := 0

func _initialize() -> void:
	var selected: String = OS.get_environment("FACTORY_LAYOUT_SCENE")
	if selected.is_empty():
		selected = "AssemblyLine"
	if selected not in ["AssemblyLine", "ControlRoom"]:
		_failures.append("FACTORY_LAYOUT_SCENE must be AssemblyLine or ControlRoom")
		_finish()
		return

	var scenes: Dictionary = {}
	var path: String = FACTORY_ROOT + selected + ".tscn"
	scenes[selected] = _load_scene(path)

	var root: Node = scenes.get(selected)
	if root != null:
		_assert_level_root(root, selected)

	if selected == "AssemblyLine":
		_assert_transition(root, "SortingFloorArrival", "res://levels/factory/maps/SortingFloor.tscn", "AssemblyLineEntrance", "", scenes)
		_assert_transition(root, "AssemblyLineExit", "res://levels/factory/maps/ControlRoom.tscn", "AssemblyLineArrival", "factory_shutdown_checklist_signed", scenes)
		_verify_assembly_line(root)
	else:
		_assert_transition(root, "AssemblyLineArrival", "res://levels/factory/maps/AssemblyLine.tscn", "AssemblyLineExit", "", scenes)
		_assert_transition(root, "LoadingBayEntrance", "res://levels/factory/maps/LoadingBay.tscn", "ControlRoomReturn", "factory_shutdown_inspection_complete", scenes)
		_verify_control_room(root)

	_finish()

func _finish() -> void:
	print("[factory-layout] %d checks, %d failures" % [_checks, _failures.size()])
	if _failures.is_empty():
		print("[factory-layout] ALL CHECKS PASSED")
		quit(0)
	else:
		for failure in _failures:
			print("[factory-layout] FAIL: " + failure)
		quit(1)

func _load_scene(path: String) -> Node:
	var packed := load(path) as PackedScene
	_assert(packed != null, "load %s" % path)
	if packed == null:
		return null
	var instance := packed.instantiate()
	_assert(instance != null, "instantiate %s" % path)
	return instance

func _assert_level_root(root: Node, scene_name: String) -> void:
	_assert(root is Node2D, "%s root is Node2D" % scene_name)
	_assert(_script_path(root).ends_with("levels/base_level.gd"), "%s root uses base_level.gd" % scene_name)

	var tilemap: Node = root.get_node_or_null("CoreTilemapLayer")
	if tilemap == null and scene_name == "OpeningZone":
		tilemap = root.get_node_or_null("WarpPoint/CoreTilemapLayer")
	_assert(tilemap != null, "%s has CoreTilemapLayer" % scene_name)
	if tilemap == null:
		return
	_assert(tilemap.position == Vector2.ZERO, "%s CoreTilemapLayer is at origin" % scene_name)
	_assert(_script_path(tilemap).ends_with("components/core/level_tile_map_layer.gd"), "%s CoreTilemapLayer uses level_tile_map_layer.gd" % scene_name)
	var used_rect = tilemap.call("get_used_rect")
	_assert(used_rect is Rect2i and used_rect.size != Vector2i.ZERO, "%s CoreTilemapLayer has non-empty used rect" % scene_name)

func _assert_transition(root: Node, node_name: String, expected_level: String, expected_target: String, expected_flag: String, scenes: Dictionary) -> void:
	var transition := root.get_node_or_null(node_name)
	_assert(transition != null, "%s has transition %s" % [root.name, node_name])
	if transition == null:
		return
	_assert(_script_path(transition).ends_with("levels/level_transition.gd"), "%s/%s uses level_transition.gd" % [root.name, node_name])
	_assert(transition.get("level") == expected_level, "%s/%s targets %s" % [root.name, node_name, expected_level])
	_assert(transition.get("target_transition_name") == expected_target, "%s/%s targets node %s" % [root.name, node_name, expected_target])
	_assert(transition.get("required_flag") == expected_flag, "%s/%s requires flag '%s'" % [root.name, node_name, expected_flag])
	if expected_level.begins_with(FACTORY_ROOT):
		var destination_name: String = expected_level.get_file().get_basename()
		var destination: Node = scenes.get(destination_name)
		if destination != null:
			_assert(destination.get_node_or_null(expected_target) != null, "%s/%s target resolves in %s" % [root.name, node_name, expected_target])
		else:
			_assert(ResourceLoader.exists(expected_level), "%s/%s destination scene exists" % [root.name, node_name])

func _verify_assembly_line(root: Node) -> void:
	_assert_script(root, "AssemblyLineSavePoint", "saves/save_point.gd")
	var assembly_save := root.get_node_or_null("AssemblyLineSavePoint")
	_assert(assembly_save != null and assembly_save.get("location_name") == "Factory — Assembly Line", "AssemblyLine save point location")
	_assert_script(root, "ShutdownChecklist", "components/npcs/cutscene_trigger.gd")
	_assert_string_array_contains(root, "ShutdownChecklist", "set_flags_on_fire", "factory_shutdown_checklist_signed")
	_assert_one_shot(root, "ShutdownChecklist", "shutdown_checklist")
	_assert_script(root, "ConveyorInstruction", "components/npcs/readable_object.gd")
	_assert_script(root, "MaintenanceTransferInstruction", "components/npcs/readable_object.gd")

	for index in range(1, 13):
		var conveyor_name := "Conveyor%02d" % index
		_assert_script(root, conveyor_name, "components/puzzles/conveyor_tile.gd")
		var conveyor := root.get_node_or_null(conveyor_name)
		if conveyor != null:
			_assert(conveyor.get("conveyor_direction") == Vector2(-1, 0), "%s pushes west" % conveyor_name)
			_assert(is_equal_approx(conveyor.get("conveyor_speed"), 80.0), "%s speed is 80" % conveyor_name)

	_assert_script(root, "MaintenancePadWest", "components/puzzles/teleport_pad.gd")
	_assert_script(root, "MaintenancePadEast", "components/puzzles/teleport_pad.gd")
	var west := root.get_node_or_null("MaintenancePadWest")
	var east := root.get_node_or_null("MaintenancePadEast")
	if west != null and east != null:
		_assert(west.get_node_or_null(west.get("target_pad_path")) == east, "MaintenancePadWest pairs with MaintenancePadEast")
		_assert(east.get_node_or_null(east.get("target_pad_path")) == west, "MaintenancePadEast pairs with MaintenancePadWest")

func _verify_control_room(root: Node) -> void:
	_assert_script(root, "ControlRoomSavePoint", "saves/save_point.gd")
	var control_save := root.get_node_or_null("ControlRoomSavePoint")
	_assert(control_save != null and control_save.get("location_name") == "Factory — Control Room", "ControlRoom save point location")
	_assert_script(root, "InspectionDoor", "components/puzzles/door.gd")
	_assert_script(root, "InspectionApproved", "components/npcs/cutscene_trigger.gd")
	_assert_string_array_contains(root, "InspectionApproved", "set_flags_on_fire", "factory_shutdown_inspection_complete")
	_assert_one_shot(root, "InspectionApproved", "shutdown_inspection_complete")
	_assert_script(root, "SequenceController", "components/puzzles/sequence_puzzle_controller.gd")
	_assert_script(root, "SequenceInstruction", "components/npcs/readable_object.gd")

	for index in range(3):
		var plate_name := "SequencePlate%c" % char(65 + index)
		_assert_script(root, plate_name, "components/puzzles/sequence_pressure_plate.gd")
		var plate := root.get_node_or_null(plate_name)
		if plate != null:
			_assert(plate.get("sequence_index") == index, "%s SequenceIndex is %d" % [plate_name, index])

	var controller := root.get_node_or_null("SequenceController")
	if controller != null:
		var plate_paths = controller.get("plate_paths")
		_assert(plate_paths is Array and plate_paths.size() == 3, "SequenceController has three plate paths")
		if plate_paths is Array and plate_paths.size() == 3:
			for index in range(3):
				var resolved := controller.get_node_or_null(plate_paths[index])
				_assert(resolved != null and resolved.name == "SequencePlate%c" % char(65 + index), "SequenceController plate path %d resolves in order" % index)
		var door_path = controller.get("target_door_path")
		_assert(controller.get_node_or_null(door_path) == root.get_node_or_null("InspectionDoor"), "SequenceController target door resolves")
		_assert(is_equal_approx(controller.get("time_window"), 5.0), "SequenceController time window is 5 seconds")

	_assert_script(root, "EggdropSoupPickup", "components/items/pickup_item.gd")
	var pickup := root.get_node_or_null("EggdropSoupPickup")
	if pickup != null:
		_assert(pickup.get("item_id") == "eggdrop_soup", "EggdropSoupPickup item id is eggdrop_soup")
		_assert(pickup.get("count") == 1, "EggdropSoupPickup count is 1")

func _assert_script(root: Node, node_path: String, expected_suffix: String) -> void:
	var node := root.get_node_or_null(node_path)
	_assert(node != null, "%s has %s" % [root.name, node_path])
	if node != null:
		_assert(_script_path(node).ends_with(expected_suffix), "%s/%s uses %s" % [root.name, node_path, expected_suffix])

func _assert_one_shot(root: Node, node_path: String, expected_id: String) -> void:
	var node := root.get_node_or_null(node_path)
	if node == null:
		return
	_assert(node.get("once") == true, "%s/%s is one-shot" % [root.name, node_path])
	_assert(node.get("cutscene_id") == expected_id, "%s/%s CutsceneId is %s" % [root.name, node_path, expected_id])

func _assert_string_array_contains(root: Node, node_path: String, property_name: String, expected: String) -> void:
	var node := root.get_node_or_null(node_path)
	if node == null:
		return
	var values = node.get(property_name)
	_assert(values is Array or values is PackedStringArray, "%s/%s.%s is a string array" % [root.name, node_path, property_name])
	if values is Array or values is PackedStringArray:
		_assert(expected in values, "%s/%s.%s contains %s" % [root.name, node_path, property_name, expected])

func _script_path(node: Node) -> String:
	var script = node.get_script()
	return script.resource_path if script != null else ""

func _assert(condition: bool, message: String) -> void:
	_checks += 1
	if not condition:
		_failures.append(message)
