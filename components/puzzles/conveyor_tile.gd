class_name ConveyorTile
extends Area2D

@export_group("Conveyor")
@export var conveyor_direction: Vector2 = Vector2.RIGHT
@export var conveyor_speed: float = 80.0

func _ready() -> void:
	collision_layer = 0
	collision_mask = CollisionConfig.PLAYER_LAYER | CollisionConfig.INTERACTABLE_LAYER
	body_entered.connect(_on_body_entered)

func _on_body_entered(body: Node2D) -> void:
	if body is PushBlock:
		(body as PushBlock).try_push(conveyor_direction)

func get_conveyor_velocity(body: Node2D) -> Vector2:
	if body.is_in_group("player") and Input.is_action_pressed("player_sprint"):
		return Vector2.ZERO
	return conveyor_direction.normalized() * conveyor_speed
