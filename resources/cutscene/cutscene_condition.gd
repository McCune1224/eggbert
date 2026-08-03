class_name CutsceneCondition
extends Resource

enum ConditionType { ALWAYS, FLAG_SET, FLAG_NOT_SET, CHOICE_EQUALS }

@export var type: ConditionType = ConditionType.ALWAYS
@export var flag_key: String = ""
@export var choice_index: int = -1

func should_execute(last_choice_index: int = -1) -> bool:
	match type:
		ConditionType.FLAG_SET:
			return _has_world_flag(flag_key)
		ConditionType.FLAG_NOT_SET:
			return not _has_world_flag(flag_key)
		ConditionType.CHOICE_EQUALS:
			return last_choice_index == choice_index
		_:
			return true

func _has_world_flag(flag_name: String) -> bool:
	var tree := Engine.get_main_loop() as SceneTree
	var world_flags := tree.root.get_node_or_null("WorldFlags") if tree != null else null
	return world_flags != null and world_flags.has_method("has_flag") and bool(world_flags.call("has_flag", flag_name))
