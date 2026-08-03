class_name CombatTargeter
extends RefCounted

static func get_player_position(fallback: Vector2 = Vector2.ZERO) -> Vector2:
	var loop: MainLoop = Engine.get_main_loop()
	var tree: SceneTree = loop as SceneTree
	var root: Node = tree.root if tree != null else null
	var player: Node = root.get_node_or_null("Player") if root != null else null
	if player is Node2D:
		return (player as Node2D).global_position
	return fallback

static func get_nearest_enemy_position(from_position: Vector2) -> Vector2:
	var tree: SceneTree = Engine.get_main_loop() as SceneTree
	if tree == null:
		return from_position
	var nearest: Vector2 = from_position
	var nearest_distance: float = INF
	for node: Node in tree.get_nodes_in_group("enemy"):
		if node is Node2D and not bool(node.get("is_dead")):
			var distance: float = from_position.distance_squared_to((node as Node2D).global_position)
			if distance < nearest_distance:
				nearest_distance = distance
				nearest = (node as Node2D).global_position
	return nearest
