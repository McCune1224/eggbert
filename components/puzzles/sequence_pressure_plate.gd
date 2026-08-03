class_name SequencePressurePlate
extends Area2D

@export_group("Sequence")
@export var sequence_index: int = 0

var controller: SequencePuzzleController
var _sprite: Sprite2D

func _ready() -> void:
	collision_layer = 0
	collision_mask = CollisionConfig.PLAYER_LAYER | CollisionConfig.INTERACTABLE_LAYER
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	body_entered.connect(_on_body_entered)

func _on_body_entered(body: Node2D) -> void:
	if not body.is_in_group("player") and not body.is_in_group("pushable"):
		return
	if controller == null:
		controller = get_parent().get_node_or_null("SequenceController") as SequencePuzzleController
	if controller != null:
		controller.step_pressed(sequence_index)

func flash(correct: bool) -> void:
	if _sprite == null:
		return
	_sprite.modulate = Color.GREEN if correct else Color.RED
	create_tween().tween_property(_sprite, "modulate", Color.WHITE, 0.5)
