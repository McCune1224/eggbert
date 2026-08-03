class_name YogurtArena
extends CombatArena

const ENEMY_SCENE: PackedScene = preload("res://combat/enemies/CombatYogurt.tscn")
const WALL_THICKNESS: float = 16.0
const WALLS_LAYER: int = 2

@export var arena_size: Vector2 = Vector2(480, 320)
@export var enemy_spawn: Vector2 = Vector2(0, -120)

func _ready() -> void:
	player_spawn_position = Vector2(0, 100)
	super._ready()
	_build_bounds(arena_size)
	var enemy: Node = ENEMY_SCENE.instantiate()
	enemy.position = enemy_spawn
	add_child(enemy)
	register_enemy(enemy, "Yogurt")
	if combat_hud != null:
		combat_hud.set_player_health_component(_player_health)

func _build_bounds(size: Vector2) -> void:
	var bounds := StaticBody2D.new()
	bounds.name = "ArenaBounds"
	bounds.collision_layer = WALLS_LAYER
	bounds.collision_mask = 0
	add_child(bounds)
	var half_size := size * 0.5
	_add_wall(bounds, Vector2(0, -half_size.y), Vector2(size.x, WALL_THICKNESS))
	_add_wall(bounds, Vector2(0, half_size.y), Vector2(size.x, WALL_THICKNESS))
	_add_wall(bounds, Vector2(-half_size.x, 0), Vector2(WALL_THICKNESS, size.y))
	_add_wall(bounds, Vector2(half_size.x, 0), Vector2(WALL_THICKNESS, size.y))

func _add_wall(parent: StaticBody2D, position: Vector2, size: Vector2) -> void:
	var shape := CollisionShape2D.new()
	shape.position = position
	var rectangle := RectangleShape2D.new()
	rectangle.size = size
	shape.shape = rectangle
	parent.add_child(shape)
