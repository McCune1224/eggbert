class_name CombatEnemy
extends Area2D

const PLAYER_LAYER: int = 1
const BULLET_LAYER: int = 8
const ENEMY_LAYER: int = 32

@export var max_hp: int = 40
@export var contact_damage: int = 8

var health: HealthComponent

func _ready() -> void:
	add_to_group("enemy")
	collision_layer = ENEMY_LAYER
	collision_mask = PLAYER_LAYER | BULLET_LAYER
	body_entered.connect(_on_body_entered)
	area_entered.connect(_on_area_entered)
	health = get_node_or_null("HealthComponent") as HealthComponent
	if health == null:
		health = HealthComponent.new()
		health.name = "HealthComponent"
		health.max_hp = max_hp
		health.current_hp = max_hp
		add_child(health)
	health.died.connect(_on_died)

func _on_died() -> void:
	if health != null and health.died.is_connected(_on_died):
		health.died.disconnect(_on_died)
	var arena := get_parent() as CombatArena
	if arena != null:
		arena.on_enemy_defeated()
	queue_free()

func _on_body_entered(body: Node2D) -> void:
	if body.is_in_group("player") and contact_damage > 0:
		var player_health: HealthComponent = body.get("health_component") as HealthComponent
		if player_health == null:
			player_health = body.get_node_or_null("HealthComponent") as HealthComponent
		if player_health != null:
			player_health.take_damage(contact_damage, self)

func _on_area_entered(area: Area2D) -> void:
	var bullet := area as RedBullet
	if bullet != null and bullet.reflected:
		health.take_damage(10 + _attack_bonus(), bullet)

func on_parried(knockback: Vector2) -> void:
	global_position += knockback * 0.05

func _attack_bonus() -> int:
	var equipment: Node = get_tree().root.get_node_or_null("Equipment")
	return int(equipment.get("total_attack_boost")) if equipment != null else 0
