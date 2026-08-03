extends SceneTree

const GAME_CONTROLLER_SCRIPT := preload("res://autoload/game_controller.gd")

func _initialize() -> void:
	var controller: Node = GAME_CONTROLLER_SCRIPT.new()
	assert(controller.has_method("load_level_at_position"))
	assert(controller.has_method("load_level_at_transition"))
	print("Warp loading API checks passed")
	quit(0)
