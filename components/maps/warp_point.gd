class_name WarpPoint
extends Area2D
## An area in a level the player can unlock as a warp destination.
## Tracks unlock state via WorldFlags ("warp_<id>") and shows the interaction
## prompt while locked.

@export var warp_id: String = ""

var _prompt_area: Area2D
var _player_near: bool = false
var _unlocked: bool = false

func _ready() -> void:
	_unlocked = WarpDatabase.is_unlocked(warp_id)
	_prompt_area = get_node_or_null("PromptArea2D") as Area2D
	if _prompt_area != null:
		_prompt_area.body_entered.connect(_on_body_entered)
		_prompt_area.body_exited.connect(_on_body_exited)
	_update_interaction_prompt()
	var crystal := get_node_or_null("WarpCrystal") as Control
	if crystal != null:
		var float_tween := create_tween().set_loops()
		float_tween.tween_property(crystal, "position:y", -4.0, 0.75).as_relative().set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)
		float_tween.tween_property(crystal, "position:y", 4.0, 0.75).as_relative().set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)
	GameLogger.debug("WarpPoint", "'%s': _Ready — id='%s', unlocked=%s" % [name, warp_id, _unlocked])

func _on_body_entered(body: Node2D) -> void:
	if not body.is_in_group("player"):
		return
	_player_near = true
	_update_interaction_prompt()

func _on_body_exited(body: Node2D) -> void:
	if not body.is_in_group("player"):
		return
	_player_near = false
	_update_interaction_prompt()

func _process(_delta: float) -> void:
	if _unlocked or not _player_near or not Input.is_action_just_pressed("interact"):
		return
	_unlocked = true
	WarpDatabase.unlock(warp_id)
	_update_interaction_prompt()
	GameLogger.info("WarpPoint", "'%s': unlocked (id='%s')" % [name, warp_id])
	var destination := WarpDatabase.get_warp(warp_id)
	if not destination.is_empty():
		var dialog := get_tree().root.get_node_or_null("DialogManager")
		if dialog != null and dialog.has_method("start_dialog"):
			dialog.call("start_dialog", ["Warp unlocked: %s" % destination.get("name")])

func _update_interaction_prompt() -> void:
	var player := get_tree().root.get_node_or_null("Player")
	if player == null:
		return
	var prompt: Node = player.get("interaction_prompt")
	if prompt != null and prompt.has_method("set_interactable_available"):
		prompt.call("set_interactable_available", self, _player_near and not _unlocked)
