extends Area2D


func _ready():
	body_entered.connect(_on_body_entered)


func _on_body_entered(body):
	if body.is_in_group("player"):
		if CombatController.Instance != null:
			CombatController.Instance.EnterCombat(
				"res://combat/arena/GenericArena.tscn", Vector2.ZERO
			)
