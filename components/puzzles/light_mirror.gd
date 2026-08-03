class_name LightMirror
extends StaticBody2D

@export var mirror_texture: Texture2D

var _sprite: Sprite2D

func _ready() -> void:
	collision_layer = CollisionConfig.WALLS_LAYER
	collision_mask = CollisionConfig.PLAYER_LAYER
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	if _sprite != null and mirror_texture != null:
		_sprite.texture = mirror_texture
	add_to_group("pushable")

func rotate_mirror() -> void:
	rotation += deg_to_rad(45.0)

func get_surface_normal(incoming_dir: Vector2) -> Vector2:
	var normal := Vector2(cos(rotation), sin(rotation))
	if normal.dot(incoming_dir) > 0.0:
		normal = -normal
	return normal
