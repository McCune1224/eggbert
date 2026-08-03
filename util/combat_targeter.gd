extends RefCounted

static func get_player_position() -> Vector2:
	var main_loop := Engine.get_main_loop()
	if not main_loop is SceneTree:
		return Vector2.ZERO
	var player := (main_loop as SceneTree).root.get_node_or_null("Player")
	return player.global_position if player is Node2D else Vector2.ZERO
