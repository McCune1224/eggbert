class_name RedBullet
extends Area2D

const PLAYER_LAYER: int = 1
const BULLET_LAYER: int = 8
const ENEMY_LAYER: int = 32
const PLAYER_BULLET_MASK: int = PLAYER_LAYER | 128
const HOMING_STRENGTH: float = 2.5

@export var speed: float = 200.0
@export var lifetime: float = 3.0
@export var direction: Vector2 = Vector2.RIGHT

var reflected: bool = false
var is_homing: bool = false
var fired_by: Node2D
var _alive_time: float = 0.0

func _ready() -> void:
	add_to_group("bullet")
	area_entered.connect(_on_area_entered)
	body_entered.connect(_on_body_entered)

func _process(delta: float) -> void:
	if is_homing and not reflected:
		var to_player: Vector2 = CombatTargeter.get_player_position(global_position) - global_position
		if to_player.length_squared() > 0.01:
			direction = direction.lerp(to_player.normalized(), clampf(HOMING_STRENGTH * delta, 0.0, 1.0)).normalized()
	global_position += direction.normalized() * speed * delta
	rotation = direction.angle()
	_alive_time += delta
	if _alive_time >= lifetime:
		queue_free()

func set_direction(new_direction: Vector2, new_speed: float = -1.0) -> void:
	if new_direction.length_squared() > 0.001:
		direction = new_direction.normalized()
	if new_speed >= 0.0:
		speed = new_speed

func reset_lifetime() -> void:
	_alive_time = 0.0

func reflect(from_position: Vector2 = global_position) -> void:
	var target: Vector2 = CombatTargeter.get_nearest_enemy_position(from_position)
	set_direction(from_position.direction_to(target), 400.0)
	reflected = true
	is_homing = false
	collision_mask = ENEMY_LAYER
	modulate = Color(0.0, 1.0, 1.0)
	reset_lifetime()

func _on_area_entered(area: Area2D) -> void:
	if reflected and (area.collision_layer & ENEMY_LAYER) != 0:
		var health: HealthComponent = area.get("health") as HealthComponent
		if health == null:
			health = area.get_node_or_null("HealthComponent") as HealthComponent
		if health != null:
			health.take_damage(10 + _attack_bonus(), self)
	queue_free()

func _on_body_entered(body: Node2D) -> void:
	if not reflected and body.is_in_group("player"):
		var health: HealthComponent = body.get("health_component") as HealthComponent
		if health == null:
			health = body.get_node_or_null("HealthComponent") as HealthComponent
		if health != null:
			health.take_damage(10, self)
	queue_free()

func _attack_bonus() -> int:
	var equipment: Node = get_tree().root.get_node_or_null("Equipment")
	return int(equipment.get("total_attack_boost")) if equipment != null else 0
