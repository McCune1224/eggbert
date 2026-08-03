extends Node

var items: Dictionary[String, int] = {}

func add_item(item_id: String, count: int = 1) -> void:
	items[item_id] = int(items.get(item_id, 0)) + count

func has_item(item_id: String, count: int = 1) -> bool:
	return int(items.get(item_id, 0)) >= count

func remove_item(item_id: String, count: int = 1) -> bool:
	if not has_item(item_id, count):
		return false
	items[item_id] -= count
	if items[item_id] <= 0:
		items.erase(item_id)
	return true

func get_save_key() -> String:
	return "inventory"

func serialize() -> Dictionary[String, Variant]:
	return {"items": items.duplicate()}

func deserialize(data: Dictionary[String, Variant]) -> void:
	items = data.get("items", {}).duplicate()

func get_load_priority() -> int:
	return 0
