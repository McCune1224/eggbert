extends SceneTree

var failures: Array[String] = []
var loaded_count := 0

func _initialize() -> void:
	var project_text := FileAccess.get_file_as_string("res://project.godot")
	var native_marker := "\"" + "C" + char(35) + "\""
	var removed_script_suffix := "." + char(99) + char(115) + "\""
	var managed_section := "[" + "dot" + "net" + "]"
	_check(not project_text.contains(native_marker), "project.godot still enables native extension")
	_check(not project_text.contains(managed_section), "project.godot still contains managed runtime settings")
	for path in _collect_resources("res://"):
		var text := FileAccess.get_file_as_string(path)
		_check(not text.contains(removed_script_suffix), "%s still binds a removed script type" % path)
		var resource := ResourceLoader.load(path)
		if resource != null:
			loaded_count += 1
	_finish()

func _collect_resources(directory_path: String) -> Array[String]:
	var result: Array[String] = []
	var directory := DirAccess.open(directory_path)
	if directory == null:
		return result
	for entry in directory.get_files():
		if entry.ends_with(".tscn") or entry.ends_with(".tres"):
			result.append(directory_path.path_join(entry))
	for entry in directory.get_directories():
		if entry in [".godot", "backups"]:
			continue
		result.append_array(_collect_resources(directory_path.path_join(entry)))
	return result

func _check(condition: bool, message: String) -> void:
	if not condition:
		failures.append(message)

func _finish() -> void:
	if failures.is_empty():
		print("Migration integrity passed: %d resources loaded" % loaded_count)
		quit(0)
	else:
		for failure in failures:
			printerr(failure)
		quit(1)
