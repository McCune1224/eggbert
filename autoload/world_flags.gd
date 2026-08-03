extends Node

signal state_changed

var _flags: Dictionary[String, Variant] = {}

func _ready() -> void:
	add_to_group("persist")
	process_mode = Node.PROCESS_MODE_ALWAYS

func set_flag(key: String, value: Variant) -> void:
	_flags[key] = value
	GameLogger.debug("WorldFlags", "set %s = %s" % [key, str(value)])
	state_changed.emit()

func get_flag(key: String, default_value: Variant = null) -> Variant:
	return _flags.get(key, default_value)

func has_flag(key: String) -> bool:
	return bool(_flags.get(key, false))

func clear_flag(key: String) -> void:
	if _flags.erase(key):
		GameLogger.debug("WorldFlags", "cleared %s" % key)
		state_changed.emit()

func clear_all() -> void:
	_flags.clear()
	GameLogger.debug("WorldFlags", "cleared all flags")
	state_changed.emit()

func get_all_flags() -> Dictionary[String, Variant]:
	return _flags.duplicate(true)

func get_save_key() -> String:
	return "world_flags"

func serialize() -> Dictionary[String, Variant]:
	return {"flags": _flags.duplicate(true)}

func deserialize(data: Dictionary[String, Variant]) -> void:
	_flags = data.get("flags", {}).duplicate(true)
	state_changed.emit()

func get_load_priority() -> int:
	return 0
