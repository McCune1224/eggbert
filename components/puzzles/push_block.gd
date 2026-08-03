class_name PushBlock
extends CharacterBody2D

@export_group("PushBlock")
@export var push_speed: float = 200.0
@export var directional_mode: bool = false
@export var texture: Texture2D

var _sprite: Sprite2D
var _collision_shape: CollisionShape2D

func _ready() -> void:
	collision_layer = CollisionConfig.INTERACTABLE_LAYER
	collision_mask = CollisionConfig.WALLS_LAYER | CollisionConfig.PLAYER_LAYER
	add_to_group("pushable")
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	_collision_shape = get_node_or_null("CollisionShape2D") as CollisionShape2D
	_apply_texture()

func _apply_texture() -> void:
	if _sprite != null and texture != null:
		_sprite.texture = texture
		_sprite.region_enabled = true
		_sprite.region_rect = Rect2(0.0, 0.0, 32.0, 32.0)
	var rectangle := _collision_shape.shape as RectangleShape2D if _collision_shape != null else null
	if rectangle != null:
		rectangle.size = Vector2.ONE * 19.2

func try_push(direction: Vector2) -> bool:
	var push_direction := direction.normalized()
	if directional_mode:
		if absf(push_direction.x) > absf(push_direction.y):
			push_direction = Vector2(signf(push_direction.x), 0.0)
		else:
			push_direction = Vector2(0.0, signf(push_direction.y))
	var origin := global_position
	velocity = push_direction * push_speed
	move_and_slide()
	var moved := global_position.distance_squared_to(origin) > 0.01
	velocity = Vector2.ZERO
	return moved
