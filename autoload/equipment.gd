extends Node

var equipped: Dictionary[String, String] = {}
var total_speed_boost: int = 0

func equip(item_id: String, slot: String) -> void:
	equipped[slot] = item_id
	GameLogger.info("Equipment", "Equipped %s in slot '%s'" % [item_id, slot])

func get_save_key() -> String:
	return "equipment"

func serialize() -> Dictionary[String, Variant]:
	return {"equipped": equipped.duplicate()}

func deserialize(data: Dictionary[String, Variant]) -> void:
	equipped = data.get("equipped", {}).duplicate()

func get_load_priority() -> int:
	return 5
