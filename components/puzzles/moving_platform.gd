class_name MovingPlatform
extends AnimatableBody2D

@export_group("Platform")
@export var speed: float = 1.0

var _animation_player: AnimationPlayer
var _moving_forward: bool = true

func _ready() -> void:
	collision_layer = CollisionConfig.WALLS_LAYER
	_animation_player = get_node_or_null("AnimationPlayer") as AnimationPlayer
	if _animation_player != null and _animation_player.has_animation("move"):
		_animation_player.play("move")
		_animation_player.speed_scale = speed

func _process(_delta: float) -> void:
	if _animation_player == null or not _animation_player.has_animation("move"):
		return
	var length := _animation_player.current_animation_length
	if _moving_forward and _animation_player.current_animation_position >= length - 0.01:
		_moving_forward = false
		_animation_player.speed_scale = -speed
	elif not _moving_forward and _animation_player.current_animation_position <= 0.01:
		_moving_forward = true
		_animation_player.speed_scale = speed
