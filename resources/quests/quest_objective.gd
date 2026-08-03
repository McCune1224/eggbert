class_name QuestObjective
extends Resource

@export var id: String = ""
@export_multiline var description: String = ""
@export var completion_flag: String = ""

func is_complete() -> bool:
	if completion_flag.is_empty():
		return false
	var flags := Engine.get_main_loop() as SceneTree
	var world_flags := flags.root.get_node_or_null("WorldFlags") if flags != null else null
	return world_flags != null and world_flags.has_method("has_flag") and bool(world_flags.call("has_flag", completion_flag))
