extends Node

signal save_completed

func _get_save_path() -> String:
	return "user://savegame.tres"

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS

func has_save() -> bool:
	return ResourceLoader.exists(_get_save_path())

func delete_save() -> void:
	var path := _get_save_path()
	var directory := DirAccess.open(path.get_base_dir())
	if directory != null and directory.file_exists(path.get_file()):
		directory.remove(path.get_file())

func save_game(scene_path: String, position: Vector2, location_name: String) -> void:
	var save_file := SaveFile.new()
	save_file.save_point_scene_path = scene_path
	save_file.save_point_position = position
	save_file.location_name = location_name
	save_file.save_timestamp = Time.get_unix_time_from_system()
	var persist_nodes := get_tree().get_nodes_in_group("persist")
	for node: Node in persist_nodes:
		if _is_savable(node):
			save_file.component_data[node.get_save_key()] = node.serialize()
	var error := ResourceSaver.save(save_file, _get_save_path())
	if error == OK:
		save_completed.emit()
	else:
		GameLogger.error("SaveManager", "Failed to write save: %s" % error)

func load_game() -> bool:
	if not ResourceLoader.exists(_get_save_path()):
		return false
	var loaded := ResourceLoader.load(_get_save_path())
	if loaded == null or not (loaded is SaveFile):
		GameLogger.warn("SaveManager", "Legacy or corrupt save detected; deleting it.")
		delete_save()
		return false
	var save_file := loaded as SaveFile
	var savable_nodes: Array[Node] = []
	for node: Node in get_tree().get_nodes_in_group("persist"):
		if _is_savable(node):
			savable_nodes.append(node)
	savable_nodes.sort_custom(func(left: Node, right: Node) -> bool:
		return int(left.get_load_priority()) > int(right.get_load_priority())
	)
	for node: Node in savable_nodes:
		var data: Variant = save_file.component_data.get(node.get_save_key())
		if data is Dictionary:
			node.deserialize(data)
	return true

func _is_savable(node: Node) -> bool:
	return node.has_method("get_save_key") and node.has_method("serialize") and node.has_method("deserialize") and node.has_method("get_load_priority")
