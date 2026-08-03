extends SceneTree

const SAVE_MANAGER_SCRIPT := preload("res://saves/save_manager.gd")

class LegacySaveManager extends SAVE_MANAGER_SCRIPT:
	func _get_save_path() -> String:
		return "user://eggbert_legacy_fixture.tres"

func _initialize() -> void:
	var path := "user://eggbert_legacy_fixture.tres"
	var file := FileAccess.open(path, FileAccess.WRITE)
	file.store_string("[gd_resource type=\"Resource\" load_steps=2 format=3]\n\n[ext_resource type=\"Script\" path=\"res://legacy_save.resource\" id=\"1\"]\n\n[resource]\nscript = ExtResource(\"1\")\n")
	file.close()
	var manager: Node = LegacySaveManager.new()
	root.add_child(manager)
	assert(manager.load_game() == false)
	assert(not FileAccess.file_exists(path))
	assert(not manager.has_save())
	manager.queue_free()
	print("SaveManager legacy-save regression passed")
	quit(0)
