class_name TeleportPad
extends Area2D

@export_group("Teleport")
@export var target_pad_path: NodePath
@export var cooldown_seconds: float = 0.5

var _target_pad: TeleportPad
var _cooldown_timer: float = 0.0

func _ready() -> void:
	collision_layer = 0
	collision_mask = CollisionConfig.PLAYER_LAYER | CollisionConfig.INTERACTABLE_LAYER
	if not target_pad_path.is_empty():
		_target_pad = get_node_or_null(target_pad_path) as TeleportPad
	body_entered.connect(_on_body_entered)

func _process(delta: float) -> void:
	_cooldown_timer = maxf(0.0, _cooldown_timer - delta)

func _on_body_entered(body: Node2D) -> void:
	if _cooldown_timer > 0.0 or _target_pad == null:
		return
	if not body.is_in_group("player") and not body.is_in_group("pushable"):
		return
	_cooldown_timer = cooldown_seconds
	_target_pad._cooldown_timer = cooldown_seconds
	_teleport(body)

func _teleport(body: Node2D) -> void:
	var fade := get_tree().root.get_node_or_null("FadeTransition")
	if fade != null and fade.has_method("play_fade_out"):
		await fade.call("play_fade_out")
	body.global_position = _target_pad.global_position
	if fade != null and fade.has_method("play_fade_in"):
		await fade.call("play_fade_in")
