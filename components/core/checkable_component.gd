class_name CheckableComponent
extends Area2D
## Attach to any entity (NPC, object) to make it inspectable via the Check/Tattle action.
## Shows dialog with the check line when the player presses the check key while facing it.

@export var check_line: String = ""

func _ready() -> void:
	collision_layer = CollisionConfig.INTERACTABLE_LAYER
	collision_mask = CollisionConfig.PLAYER_LAYER
	if get_child_count() == 0 or get_node_or_null("CollisionShape2D") == null:
		var shape := CollisionShape2D.new()
		shape.name = "CollisionShape2D"
		var circle := CircleShape2D.new()
		circle.radius = 48.0
		shape.shape = circle
		add_child(shape)
		GameLogger.debug("CheckableComponent", "'%s': auto-created collision shape" % name)

func get_check_line() -> String:
	return check_line
