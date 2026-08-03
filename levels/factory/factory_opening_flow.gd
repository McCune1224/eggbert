extends Node

func start_opening() -> void:
	if GameController != null:
		GameController.load_level_at_position("res://levels/factory/maps/OpeningZone.tscn", Vector2.ZERO)
