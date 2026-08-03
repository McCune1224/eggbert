extends Node

signal tile_map_bounds_changed(bounds: Array[Vector2])
signal level_load_started
signal level_loaded

var current_tile_map_bounds: Array[Vector2] = []
var current_level: Node

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	current_level = get_node_or_null("CurrentLevel")
	call_deferred("_ensure_objective_tracker")

func _ensure_objective_tracker() -> void:
	var existing := get_tree().root.get_node_or_null("ObjectiveTracker")
	if existing != null:
		return
	var tracker_scene := load("res://ui/ObjectiveTracker.tscn") as PackedScene
	if tracker_scene == null:
		GameLogger.error("GameController", "Failed to load ObjectiveTracker.tscn.")
		return
	var tracker := tracker_scene.instantiate()
	tracker.name = "ObjectiveTracker"
	get_tree().root.add_child(tracker)
	GameLogger.info("GameController", "ObjectiveTracker instantiated")

func change_tile_map_bounds(bounds: Array[Vector2]) -> void:
	current_tile_map_bounds = bounds
	tile_map_bounds_changed.emit(bounds)

func load_level_at_position(scene_path: String, player_position: Vector2) -> void:
	await _load_level(scene_path)
	var player := get_tree().root.get_node_or_null("Player")
	if player != null:
		player.position = player_position
	await _finish_level_load()

func load_level_at_transition(scene_path: String, target_transition_name: String) -> void:
	await _load_level(scene_path)
	var transition := current_level.get_node_or_null(target_transition_name) if current_level != null else null
	var player := get_tree().root.get_node_or_null("Player")
	if transition != null and player != null:
		var side: String = str(transition.get("side"))
		match side.to_lower():
			"left": player.position = transition.global_position + Vector2(30.0, 0.0)
			"right": player.position = transition.global_position - Vector2(30.0, 0.0)
			"up": player.position = transition.global_position + Vector2(0.0, 50.0)
			"down": player.position = transition.global_position - Vector2(0.0, 50.0)
	await _finish_level_load()

func _load_level(scene_path: String) -> void:
	get_tree().paused = true
	level_load_started.emit()
	GameLogger.info("GameController", "Loading level %s" % scene_path)
	var root := get_node_or_null("CurrentLevel")
	if root == null:
		root = Node.new()
		root.name = "CurrentLevel"
		add_child(root)
	for child in root.get_children():
		child.queue_free()
	await get_tree().process_frame
	var packed := ResourceLoader.load(scene_path) as PackedScene
	if packed == null:
		GameLogger.error("GameController", "Unable to load level %s" % scene_path)
		return
	current_level = packed.instantiate()
	root.add_child(current_level)
	GameLogger.info("GameController", "Level %s loaded" % scene_path)

func _finish_level_load() -> void:
	level_loaded.emit()
	get_tree().paused = false
