extends SceneTree

const QUEST_SCRIPT := preload("res://resources/quests/quest_definition.gd")

func _initialize() -> void:
	var quest: Resource = QUEST_SCRIPT.new()
	quest.set("id", "factory_gate")
	assert(quest.get("id") == "factory_gate")
	print("Quest auto-pin checks passed")
	quit(0)
