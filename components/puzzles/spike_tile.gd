class_name SpikeTile
extends Area2D

@export_group("Damage")
@export var damage: int = 1
@export var one_shot: bool = false

var _has_triggered: bool = false

func _ready() -> void:
	collision_layer = 0
	collision_mask = CollisionConfig.PLAYER_LAYER
	body_entered.connect(_on_body_entered)

func _on_body_entered(body: Node2D) -> void:
	if one_shot and _has_triggered:
		return
	if not body.is_in_group("player"):
		return
	var health := body.get("health_component") as Node
	if health != null and health.has_method("take_damage"):
		health.call("take_damage", damage, self)
	_has_triggered = true
