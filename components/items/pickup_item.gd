class_name PickupItem
extends Area2D

@export var item_id: String = ""
@export var count: int = 1
@export var dialog_lines: Array[String] = []
@export var set_flag: String = ""

var _collected := false

func _ready() -> void:
	body_entered.connect(_on_body_entered)

func _on_body_entered(body: Node2D) -> void:
	if _collected or not body.is_in_group("player"):
		return
	_collected = true
	GameLogger.info("PickupItem", "%s collected: %dx %s (flag '%s')" % [name, count, item_id, set_flag])
	var inventory := get_tree().root.get_node_or_null("Inventory")
	if inventory != null and inventory.has_method("add_item"):
		inventory.call("add_item", item_id, count)
	if not set_flag.is_empty():
		var flags := get_tree().root.get_node_or_null("WorldFlags")
		if flags != null and flags.has_method("set_flag"):
			flags.call("set_flag", set_flag, true)
	queue_free()
