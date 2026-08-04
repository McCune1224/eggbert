class_name CombatArena
extends Node2D

signal battle_won
signal battle_lost

@export var player_spawn_position: Vector2 = Vector2.ZERO

var enemies_remaining: int = 0
var combat_hud: CombatHUD
var _battle_finished: bool = false
var _player_health: HealthComponent

func _ready() -> void:
	var camera := get_node_or_null("Camera2D") as Camera2D
	if camera != null:
		camera.make_current()
		camera.position = Vector2.ZERO
	combat_hud = CombatHUD.new()
	add_child(combat_hud)
	var player: Node = get_tree().root.get_node_or_null("Player")
	if player is Node2D:
		(player as Node2D).position = player_spawn_position
	_player_health = _get_player_health(player)
	if _player_health != null:
		_player_health.died.connect(_on_player_died)

func _exit_tree() -> void:
	if _player_health != null and _player_health.died.is_connected(_on_player_died):
		_player_health.died.disconnect(_on_player_died)

func on_enemy_defeated() -> void:
	if _battle_finished:
		return
	enemies_remaining = maxi(0, enemies_remaining - 1)
	GameLogger.debug("Combat", "Enemy defeated; %d remaining" % enemies_remaining)
	if enemies_remaining == 0:
		_battle_finished = true
		GameLogger.info("Combat", "All enemies defeated; battle won")
		if is_instance_valid(combat_hud):
			combat_hud.queue_free()
		battle_won.emit()

func _on_player_died() -> void:
	if _battle_finished:
		return
	_battle_finished = true
	GameLogger.info("Combat", "Player died in arena; battle lost")
	battle_lost.emit()

func _get_player_health(player: Node) -> HealthComponent:
	if player == null:
		return null
	var health: HealthComponent = player.get("health_component") as HealthComponent
	if health == null:
		health = player.get_node_or_null("HealthComponent") as HealthComponent
	return health

func register_enemy(enemy: Node, display_name: String = "") -> void:
	if enemy == null:
		return
	enemies_remaining += 1
	var health: HealthComponent = enemy.get("health") as HealthComponent
	if health == null:
		health = enemy.get_node_or_null("HealthComponent") as HealthComponent
	if combat_hud != null and health != null:
		combat_hud.add_enemy(display_name if not display_name.is_empty() else str(enemy.name), health)
