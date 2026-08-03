class_name Settings
extends RefCounted

const CONFIG_PATH: String = "user://settings.cfg"

static var show_interaction_prompt: bool = true
static var _loaded: bool = false

static func load_settings() -> void:
	if _loaded:
		return
	_loaded = true
	var config := ConfigFile.new()
	if config.load(CONFIG_PATH) != OK:
		return
	show_interaction_prompt = bool(config.get_value("general", "show_interaction_prompt", true))

static func save_settings() -> void:
	var config := ConfigFile.new()
	config.set_value("general", "show_interaction_prompt", show_interaction_prompt)
	config.save(CONFIG_PATH)

static func set_show_interaction_prompt(enabled: bool) -> void:
	load_settings()
	if show_interaction_prompt == enabled:
		return
	show_interaction_prompt = enabled
	save_settings()
