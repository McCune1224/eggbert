class_name Door
extends StaticBody2D

@export_group("Door")
@export var start_open: bool = false
@export var open_sfx: AudioStream
@export var close_sfx: AudioStream
@export var texture: Texture2D
@export var _texture: Texture2D

var _collision: CollisionShape2D
var _sprite: Sprite2D

var is_open: bool:
	get:
		return _collision != null and _collision.disabled

func _ready() -> void:
	collision_layer = CollisionConfig.WALLS_LAYER
	_collision = get_node_or_null("CollisionShape2D") as CollisionShape2D
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	var tex := texture if texture != null else _texture
	if tex != null and _sprite != null:
		_sprite.texture = tex
	if start_open:
		open()

func open() -> void:
	_play_sfx(open_sfx)
	if _collision != null:
		_collision.set_deferred("disabled", true)
	modulate = Color(1.0, 1.0, 1.0, 0.3)

func close() -> void:
	_play_sfx(close_sfx)
	if _collision != null:
		_collision.set_deferred("disabled", false)
	modulate = Color.WHITE

func toggle() -> void:
	if is_open:
		close()
	else:
		open()

func _play_sfx(stream: AudioStream) -> void:
	if stream == null:
		return
	var audio := get_tree().root.get_node_or_null("AudioManager")
	if audio != null and audio.has_method("play_sfx"):
		audio.call("play_sfx", stream)
