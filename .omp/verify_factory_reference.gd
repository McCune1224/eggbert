extends SceneTree

# Verifies the Factory tutorial reference scene graph by loading and inspecting scenes.
# Uses _initialize() (not _ready) for standalone script execution.
# Scenes are instantiated but NOT added to the tree — avoids _Ready() side effects.

var _errors: Array = []
var _passed: int = 0
var _total: int = 0

func _assert(condition: bool, msg: String) -> void:
	_total += 1
	if condition:
		_passed += 1
		print("[factory-ref] PASS: " + msg)
	else:
		_errors.append(msg)
		print("[factory-ref] FAIL: " + msg)

func _load_scene(path: String) -> Node:
	var scene = load(path) as PackedScene
	_assert(scene != null, "Load %s" % path)
	if scene == null:
		return null
	var instance = scene.instantiate()
	_assert(instance != null, "Instantiate %s" % path)
	return instance

func _get_node(node: Node, path: String) -> Node:
	var result = node.get_node_or_null(path)
	_assert(result != null, "Node %s exists on %s" % [path, node.name])
	return result

func _assert_flag(node: Node, path: String, flag: String) -> void:
	var trigger = _get_node(node, path)
	if trigger == null:
		return
	var flags = trigger.get("SetFlagsOnFire")
	if flags is PackedStringArray:
		_assert(flag in flags, "%s SetFlagsOnFire contains %s" % [path, flag])
	elif flags is Array:
		_assert(flag in flags, "%s SetFlagsOnFire contains %s" % [path, flag])
	else:
		_assert(false, "%s SetFlagsOnFire is not a string array (got %s)" % [path, typeof(flags)])

func _assert_once(node: Node, path: String, expected: bool) -> void:
	var trigger = _get_node(node, path)
	if trigger == null:
		return
	var once = trigger.get("Once")
	_assert(once == expected, "%s Once = %s (expected %s)" % [path, once, expected])

func _assert_cutscene_id(node: Node, path: String, expected_id: String) -> void:
	var trigger = _get_node(node, path)
	if trigger == null:
		return
	var id = trigger.get("CutsceneId")
	_assert(id == expected_id, "%s CutsceneId = %s (expected %s)" % [path, id, expected_id])

func _assert_required_flag(node: Node, path: String, expected_flag: String) -> void:
	var transition = _get_node(node, path)
	if transition == null:
		return
	var flag = transition.get("RequiredFlag")
	_assert(flag == expected_flag, "%s RequiredFlag = %s (expected %s)" % [path, flag, expected_flag])

func _assert_target(node: Node, path: String, expected_target: String) -> void:
	var transition = _get_node(node, path)
	if transition == null:
		return
	var target = transition.get("TargetTransitionName")
	_assert(target == expected_target, "%s TargetTransitionName = %s (expected %s)" % [path, target, expected_target])

func _assert_level(node: Node, path: String, expected_level: String) -> void:
	var transition = _get_node(node, path)
	if transition == null:
		return
	var level = transition.get("Level")
	_assert(level == expected_level, "%s Level = %s (expected %s)" % [path, level, expected_level])

func _assert_target_door(node: Node, path: String, expected_door: String) -> void:
	var plate = _get_node(node, path)
	if plate == null:
		return
	var target = plate.get("TargetDoorPath")
	if target is NodePath:
		var resolved = plate.get_node_or_null(target)
		_assert(resolved != null and resolved.name == expected_door,
			"%s TargetDoorPath resolves to %s (expected %s)" % [path, resolved.name if resolved else "null", expected_door])

func _assert_eggsile_transition(node: Node, path: String, expected_scene: String, expected_target: String) -> void:
	var transition = _get_node(node, path)
	if transition == null:
		return
	var level = transition.get("Level")
	var target = transition.get("TargetTransitionName")
	_assert(level == expected_scene, "%s Level = %s (expected %s)" % [path, level, expected_scene])
	_assert(target == expected_target, "%s TargetTransitionName = %s (expected %s)" % [path, target, expected_target])

func _assert_base_level(node: Node, scene_name: String) -> void:
	var script = node.get_script()
	_assert(script != null, "Root %s has a script" % scene_name)

func _assert_resource_used(node: Node, path: String, expected_resource: String) -> void:
	var target = _get_node(node, path)
	if target == null:
		return
	var res = target.get("Cutscene")
	if res != null:
		var res_path = res.resource_path
		_assert(res_path.ends_with(expected_resource),
			"%s uses %s (got %s)" % [path, expected_resource, res_path])

func _initialize() -> void:
	var opening = _load_scene("res://levels/factory/maps/OpeningZone.tscn")
	var sorting = _load_scene("res://levels/factory/maps/SortingFloor.tscn")
	var loading = _load_scene("res://levels/factory/maps/LoadingBay.tscn")

	if opening == null or sorting == null or loading == null:
		print("[factory-ref] ABORT: could not load one or more scenes")
		quit(1)
		return

	# --- OpeningZone ---
	print("[factory-ref] === OpeningZone ===")
	_assert_base_level(opening, "OpeningZone")

	var time_clock = _get_node(opening, "TimeClock")
	_assert(time_clock != null, "TimeClock exists")
	if time_clock != null:
		_assert_flag(opening, "TimeClock", "tutorial_clocked_out")

	_assert_required_flag(opening, "SortingFloorEntrance", "tutorial_clocked_out")

	# --- SortingFloor ---
	print("[factory-ref] === SortingFloor ===")
	_assert_base_level(sorting, "SortingFloor")

	# FactoryJamitor runs FactoryJamitorTutorial.tres
	_assert_resource_used(sorting, "FactoryJamitor", "FactoryJamitorTutorial.tres")

	# FactoryJamitor sets met_jamitor via SetFlagKey in DialogBranch
	var jamitor = _get_node(sorting, "FactoryJamitor")
	if jamitor != null:
		var trigger = jamitor.get_node_or_null("CutsceneTrigger")
		if trigger != null:
			var cutscene = trigger.get("Cutscene")
			if cutscene != null:
				var cutscene_path = cutscene.resource_path
				_assert(cutscene_path.ends_with("FactoryJamitorTutorial.tres"),
					"FactoryJamitor Cutscene is FactoryJamitorTutorial.tres")
				# Check the DialogBranch resource for SetFlagKey = met_jamitor
				var tutorial_res = load(cutscene_path)
				if tutorial_res != null:
					var nodes = tutorial_res.get("Nodes")
					if nodes is Array:
						for node in nodes:
							if node is Dictionary and node.has("SetFlagKey") and node["SetFlagKey"] == "met_jamitor":
								break

	# FactoryCrate on FactoryPressurePlate targets CrateGate
	var pressure_plate = _get_node(sorting, "FactoryPressurePlate")
	_assert(pressure_plate != null, "FactoryPressurePlate exists")
	if pressure_plate != null:
		_assert_target_door(sorting, "FactoryPressurePlate", "CrateGate")
		var pp_flag = pressure_plate.get("PushablePressedFlag")
		_assert(pp_flag == "tutorial_crate_gate_open",
			"FactoryPressurePlate PushablePressedFlag = tutorial_crate_gate_open (got %s)" % pp_flag)

	_assert_required_flag(sorting, "LoadingBayEntrance", "met_jamitor")

	# --- LoadingBay ---
	print("[factory-ref] === LoadingBay ===")
	_assert_base_level(loading, "LoadingBay")

	_assert_target_door(loading, "LoadingTimedSwitchWest", "LoadingTimedGate")
	_assert_target_door(loading, "LoadingTimedSwitchEast", "LoadingTimedGate")

	_assert_once(loading, "ArrestCutscene", true)
	_assert_cutscene_id(loading, "ArrestCutscene", "arrest")
	_assert_flag(loading, "ArrestCutscene", "arrested")

	_assert_resource_used(loading, "ArrestCutscene", "OfficerBaconArrest.tres")

	_assert_eggsile_transition(loading, "EggsileTransition", "res://levels/eggsile/maps/area1.tscn", "HubArrival")
	_assert_required_flag(loading, "EggsileTransition", "arrested")

	_assert_target(loading, "SortingFloorReturn", "LoadingBayEntrance")
	_assert_level(loading, "SortingFloorReturn", "res://levels/factory/maps/SortingFloor.tscn")

	# --- Summary ---
	print("[factory-ref] === Summary ===")
	print("[factory-ref] %d/%d passed" % [_passed, _total])
	if _errors.size() > 0:
		for err in _errors:
			print("[factory-ref] ERROR: " + err)
		quit(1)
	else:
		print("[factory-ref] ALL OK")
		quit(0)