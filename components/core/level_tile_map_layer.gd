extends TileMapLayer

const BORDER_THICKNESS: float = 2.0

func _ready() -> void:
	if Engine.is_editor_hint():
		return
	if not _try_get_used_local_rect():
		return
	_create_map_borders()
	var controller := get_tree().root.get_node_or_null("GameController")
	if controller == null:
		GameLogger.warn("LevelTileMapLayer", "'%s': GameController unavailable — skipping camera bounds" % name)
		return
	var bounds := _get_world_bounds()
	controller.call("change_tile_map_bounds", bounds)
	GameLogger.debug("LevelTileMapLayer", "'%s': sent bounds %s -> %s" % [name, bounds[0], bounds[1]])

var _local_rect: Rect2

func _try_get_used_local_rect() -> bool:
	if tile_set == null:
		GameLogger.warn("LevelTileMapLayer", "'%s': TileSet is null — skipping map borders and camera bounds" % name)
		return false
	var used_rect := get_used_rect()
	if used_rect.size.x <= 0 or used_rect.size.y <= 0:
		GameLogger.warn("LevelTileMapLayer", "'%s': tilemap has no used cells — skipping map borders and camera bounds" % name)
		return false
	var half_tile: Vector2 = Vector2(tile_set.tile_size) / 2.0
	var top_left := map_to_local(used_rect.position) - half_tile
	var bottom_right := map_to_local(used_rect.end - Vector2i.ONE) + half_tile
	_local_rect = Rect2(top_left, bottom_right - top_left)
	return true

func _get_world_bounds() -> Array[Vector2]:
	var top_left := to_global(_local_rect.position)
	var top_right := to_global(Vector2(_local_rect.end.x, _local_rect.position.y))
	var bottom_left := to_global(Vector2(_local_rect.position.x, _local_rect.end.y))
	var bottom_right := to_global(_local_rect.end)
	var min_x := minf(minf(top_left.x, top_right.x), minf(bottom_left.x, bottom_right.x))
	var min_y := minf(minf(top_left.y, top_right.y), minf(bottom_left.y, bottom_right.y))
	var max_x := maxf(maxf(top_left.x, top_right.x), maxf(bottom_left.x, bottom_right.x))
	var max_y := maxf(maxf(top_left.y, top_right.y), maxf(bottom_left.y, bottom_right.y))
	return [Vector2(min_x, min_y), Vector2(max_x, max_y)]

func _create_map_borders() -> void:
	var borders := Node2D.new()
	borders.name = "MapBorders"
	add_child(borders)
	var center := _local_rect.get_center()
	var horizontal_size := Vector2(_local_rect.size.x + BORDER_THICKNESS * 2.0, BORDER_THICKNESS)
	var vertical_size := Vector2(BORDER_THICKNESS, _local_rect.size.y)
	_add_border(borders, "North", horizontal_size, Vector2(center.x, _local_rect.position.y - BORDER_THICKNESS / 2.0))
	_add_border(borders, "South", horizontal_size, Vector2(center.x, _local_rect.end.y + BORDER_THICKNESS / 2.0))
	_add_border(borders, "West", vertical_size, Vector2(_local_rect.position.x - BORDER_THICKNESS / 2.0, center.y))
	_add_border(borders, "East", vertical_size, Vector2(_local_rect.end.x + BORDER_THICKNESS / 2.0, center.y))
	GameLogger.debug("LevelTileMapLayer", "'%s': created map borders" % name)

func _add_border(parent: Node2D, border_name: String, size: Vector2, position: Vector2) -> void:
	var body := StaticBody2D.new()
	body.name = border_name
	body.position = position
	body.collision_layer = CollisionConfig.WALLS_LAYER
	body.collision_mask = 0
	var shape := CollisionShape2D.new()
	shape.shape = RectangleShape2D.new()
	(shape.shape as RectangleShape2D).size = size
	body.add_child(shape)
	parent.add_child(body)
