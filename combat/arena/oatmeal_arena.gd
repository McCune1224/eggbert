class_name OatmealArena
extends CombatArena

const ENEMY_SCENE: PackedScene = preload("res://combat/enemies/CombatOatmeal.tscn")

@export var spawn_positions: Array[Vector2] = [
	Vector2(1, -130),
	Vector2(140, -70),
	Vector2(-140, -70),
	Vector2(0, -190),
]

func _ready() -> void:
	player_spawn_position = Vector2(0, 50)
	var placed := get_node_or_null("Oatmeal")
	if placed != null:
		placed.queue_free()
	super._ready()
	var flavors: Array[int] = [0, 1, 2, 3]
	var names: Array[String] = ["Vanilla", "Strawberry", "Chocolate", "Mint"]
	for index in mini(spawn_positions.size(), flavors.size()):
		var enemy: Node = ENEMY_SCENE.instantiate()
		enemy.position = spawn_positions[index]
		enemy.set("flavor", flavors[index])
		add_child(enemy)
		if enemy.has_method("apply_flavor"):
			enemy.call("apply_flavor")
		register_enemy(enemy, "%s Oatmeal" % names[index])
	if combat_hud != null:
		combat_hud.set_player_health_component(_player_health)
