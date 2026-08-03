class_name EggrollerArena
extends CombatArena

const ENEMY_SCENE: PackedScene = preload("res://combat/enemies/RollingEgg.tscn")

func _ready() -> void:
	player_spawn_position = Vector2(0, 80)
	super._ready()
	_spawn_enemy(Vector2(-100, -80))
	_spawn_enemy(Vector2(100, -80))
	if combat_hud != null:
		combat_hud.set_player_health_component(_player_health)

func _spawn_enemy(spawn_position: Vector2) -> void:
	var enemy: Node = ENEMY_SCENE.instantiate()
	enemy.position = spawn_position
	add_child(enemy)
	register_enemy(enemy, "Eggroller")
