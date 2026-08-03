extends SceneTree

const WORLD_FLAGS_SCRIPT := preload("res://autoload/world_flags.gd")

func _initialize() -> void:
	var flags: Node = WORLD_FLAGS_SCRIPT.new()
	flags.set_flag("combat_once", true)
	assert(flags.has_flag("combat_once"))
	print("Combat once-flag checks passed")
	quit(0)
