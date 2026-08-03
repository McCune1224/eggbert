class_name CrackpotPuddle
extends Area2D

const PLAYER_LAYER: int = 1

@export var damage: int = 5
@export var lifetime: float = 4.0
@export var damage_interval: float = 0.5

var _alive_time: float = 0.0
var _damage_timer: float = 0.0
var _puddle: Polygon2D

func _ready() -> void:
	var points := PackedVector2Array()
	for index in 24:
		var angle := float(index) * TAU / 24.0
		points.append(Vector2(cos(angle), sin(angle)) * 24.0)
	_puddle = Polygon2D.new()
	_puddle.polygon = points
	_puddle.color = Color(0.8, 0.4, 0.1, 0.6)
	add_child(_puddle)
	var collision := CollisionShape2D.new()
	var rectangle := RectangleShape2D.new()
	rectangle.size = Vector2(48, 48)
	collision.shape = rectangle
	add_child(collision)
	collision_layer = 0
	collision_mask = PLAYER_LAYER

func _process(delta: float) -> void:
	_alive_time += delta
	_damage_timer += delta
	var growth := 1.0 + (_alive_time / maxf(lifetime, 0.01)) * 0.3
	_puddle.scale = Vector2.ONE * growth
	if _alive_time > lifetime - 1.0:
		_puddle.color.a = lerpf(0.6, 0.0, clampf(_alive_time - (lifetime - 1.0), 0.0, 1.0))
	if _damage_timer >= damage_interval:
		_damage_timer = 0.0
		for body: Node2D in get_overlapping_bodies():
			if body.is_in_group("player"):
				var health: HealthComponent = body.get("health_component") as HealthComponent
				if health == null:
					health = body.get_node_or_null("HealthComponent") as HealthComponent
				if health != null:
					health.take_damage(damage, self)
	if _alive_time >= lifetime:
		queue_free()
