extends Node

var pinned_quest_id: String = ""
var active_quests: Dictionary[String, QuestDefinition] = {}

func _ready() -> void:
	add_to_group("persist")
	process_mode = Node.PROCESS_MODE_ALWAYS

func pin_quest(quest_id: String) -> void:
	pinned_quest_id = quest_id
	WorldFlags.set_flag("pinned_quest_id", quest_id)

func get_save_key() -> String:
	return "quest_manager"

func serialize() -> Dictionary[String, Variant]:
	return {"pinned_quest_id": pinned_quest_id}

func deserialize(data: Dictionary[String, Variant]) -> void:
	pinned_quest_id = str(data.get("pinned_quest_id", ""))

func get_load_priority() -> int:
	return 10
