extends SceneTree

func _initialize() -> void:
	assert(ResourceLoader.exists("res://levels/factory/maps/AssemblyLine.tscn"))
	assert(ResourceLoader.exists("res://levels/factory/maps/ControlRoom.tscn"))
	print("Factory expansion checks passed")
	quit(0)
