class_name QuestDefinition
extends Resource

@export var id: String = ""
@export var title: String = ""
@export_multiline var description: String = ""
@export var start_flag: String = ""
@export var objectives: Array[QuestObjective] = []

func is_available() -> bool:
	return start_flag.is_empty() or _has_world_flag(start_flag)

func is_complete() -> bool:
	return not objectives.is_empty() and objectives.all(func(objective: QuestObjective) -> bool: return objective.is_complete())

func _has_world_flag(flag_name: String) -> bool:
	var tree := Engine.get_main_loop() as SceneTree
	var world_flags := tree.root.get_node_or_null("WorldFlags") if tree != null else null
	return world_flags != null and world_flags.has_method("has_flag") and bool(world_flags.call("has_flag", flag_name))
