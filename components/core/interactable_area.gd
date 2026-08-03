class_name InteractableArea
extends Area2D
## Base class for interactable areas (signs, phones, sleeping NPCs, triggers).
## Handles player detection, prompt show/hide, and interact (E) dispatch.
## Subclasses override on_interact() for custom behavior.

@export var voice: DialogVoiceResource

var player_in_range: bool = false

func _ready() -> void:
	collision_layer = 0
	collision_mask = CollisionConfig.PLAYER_LAYER
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)

func _input(event: InputEvent) -> void:
	if not player_in_range:
		return
	if not event.is_action_pressed("interact"):
		return
	GameLogger.debug("InteractableArea", "'%s': interact triggered (type=%s)" % [name, get_class()])
	on_interact()
	get_viewport().set_input_as_handled()

func _on_body_entered(body: Node2D) -> void:
	if not body.is_in_group("player"):
		return
	player_in_range = true
	_set_prompt(true)
	GameLogger.debug("InteractableArea", "'%s': player entered range" % name)

func _on_body_exited(body: Node2D) -> void:
	if not body.is_in_group("player"):
		return
	player_in_range = false
	_set_prompt(false)
	var cutscene := get_tree().root.get_node_or_null("CutsceneController")
	if cutscene == null or not bool(cutscene.get("is_playing")):
		var dialog := get_tree().root.get_node_or_null("DialogManager")
		if dialog != null and dialog.has_method("stop_dialog"):
			dialog.call("stop_dialog")
	GameLogger.debug("InteractableArea", "'%s': player exited range" % name)

## Override to define what happens when the player interacts.
func on_interact() -> void:
	pass

func _set_prompt(available: bool) -> void:
	var player := get_tree().root.get_node_or_null("Player")
	if player == null:
		return
	var prompt: Node = player.get("interaction_prompt")
	if prompt != null and prompt.has_method("set_interactable_available"):
		prompt.call("set_interactable_available", self, available)
