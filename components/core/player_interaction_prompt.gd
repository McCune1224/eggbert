extends Sprite2D
## Shows an interaction prompt (E key icon) when the player is within range
## of an interactable object (InteractableArea). Visibility is toggled by the
## parent InteractableArea via set_interactable_available().

var _available_source_ids: Dictionary[int, bool] = {}

func _ready() -> void:
	texture = load("res://assets/ui/NPCPrompt.png") as Texture2D
	position = Vector2(0.0, -32.0)
	z_index = 1
	visible = false

func set_interactable_available(source: Node, is_available: bool) -> void:
	if source == null:
		return
	var source_id := source.get_instance_id()
	if is_available:
		_available_source_ids[source_id] = true
	else:
		_available_source_ids.erase(source_id)
	_update_visibility()

func _update_visibility() -> void:
	visible = not _available_source_ids.is_empty() and Settings.show_interaction_prompt
