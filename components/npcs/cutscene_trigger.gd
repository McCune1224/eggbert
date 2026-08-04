class_name CutsceneTrigger
extends InteractableArea
## Cutscene/dialog trigger for NPCs and world objects.
## TriggerMode.OnInteract requires the interact key; OnEnter fires when the
## player steps into the area. Supports Once/CutsceneId lifecycle and dispatch
## to CutsceneResource, DialogLines, or raw signal.

enum TriggerMode { ON_INTERACT, ON_ENTER }

@export_group("Trigger")
## When the trigger fires. OnInteract requires the interact key; OnEnter fires on entry.
@export var mode: TriggerMode = TriggerMode.ON_INTERACT
## If true, the trigger fires only once (sets flag "cutscene_" + cutscene_id for dedup).
@export var once: bool = false
## Identifier used with Once for flag-based dedup ("cutscene_" + cutscene_id).
@export var cutscene_id: String = ""
## Reference to a CutsceneResource defining the sequence of steps. Takes priority over dialog_lines.
@export var cutscene: CutsceneResource
## Fallback inline dialog lines shown when cutscene is not assigned.
@export var dialog_lines: PackedStringArray = []
## World flags set to true when this trigger fires (e.g. "met_jamitor").
@export var set_flags_on_fire: PackedStringArray = []

signal triggered

var _has_fired: bool = false

func _ready() -> void:
	super._ready()
	if Engine.is_editor_hint():
		return
	if once and not cutscene_id.is_empty():
		var flags := get_tree().root.get_node_or_null("WorldFlags")
		if flags != null and flags.has_method("has_flag") and bool(flags.call("has_flag", "cutscene_" + cutscene_id)):
			_has_fired = true
			queue_free()
			GameLogger.debug("CutsceneTrigger", "'%s': already seen (id='%s') — removed" % [name, cutscene_id])

func _input(event: InputEvent) -> void:
	if mode != TriggerMode.ON_INTERACT:
		return
	if _has_fired or not player_in_range or not event.is_action_pressed("interact"):
		return
	fire()
	get_viewport().set_input_as_handled()

func _on_body_entered(body: Node2D) -> void:
	if not body.is_in_group("player"):
		return
	if mode == TriggerMode.ON_ENTER:
		player_in_range = true
		GameLogger.debug("CutsceneTrigger", "'%s': player entered — OnEnter trigger mode" % name)
		fire()
		return
	super._on_body_entered(body)

func _on_body_exited(body: Node2D) -> void:
	if not body.is_in_group("player"):
		return
	if mode == TriggerMode.ON_ENTER:
		player_in_range = false
		var cutscene_controller := get_tree().root.get_node_or_null("CutsceneController")
		if cutscene_controller == null or not bool(cutscene_controller.get("is_playing")):
			var dialog := get_tree().root.get_node_or_null("DialogManager")
			if dialog != null and dialog.has_method("stop_dialog"):
				dialog.call("stop_dialog")
		return
	super._on_body_exited(body)

func on_interact() -> void:
	fire()

func fire() -> void:
	if _has_fired:
		GameLogger.debug("CutsceneTrigger", "'%s': Fire skipped — already fired (Once=%s)" % [name, once])
		return
	var cutscene_controller := get_tree().root.get_node_or_null("CutsceneController")
	if cutscene_controller != null and bool(cutscene_controller.get("is_playing")):
		GameLogger.debug("CutsceneTrigger", "'%s': Fire skipped — cutscene already playing" % name)
		return
	var flags := get_tree().root.get_node_or_null("WorldFlags")
	if once:
		_has_fired = true
		if not cutscene_id.is_empty():
			if flags != null and flags.has_method("set_flag"):
				flags.call("set_flag", "cutscene_" + cutscene_id, true)
	for flag in set_flags_on_fire:
		if flag.is_empty():
			continue
		if flags != null and flags.has_method("set_flag"):
			flags.call("set_flag", flag, true)
		GameLogger.info("CutsceneTrigger", "'%s': set flag '%s'=true" % [name, flag])
	if cutscene != null:
		GameLogger.info("CutsceneTrigger", "'%s': firing cutscene '%s', Once=%s" % [name, cutscene.resource_path, once])
		cutscene_controller.call("play_cutscene", cutscene)
	elif not dialog_lines.is_empty():
		GameLogger.info("CutsceneTrigger", "'%s': firing dialog (%d lines), Once=%s" % [name, dialog_lines.size(), once])
		if cutscene_controller != null and cutscene_controller.has_method("start_dialog"):
			cutscene_controller.call("start_dialog", dialog_lines, voice)
		else:
			var dialog := get_tree().root.get_node_or_null("DialogManager")
			if dialog != null and dialog.has_method("start_dialog"):
				dialog.call("start_dialog", dialog_lines, voice)
	else:
		GameLogger.debug("CutsceneTrigger", "'%s': firing raw signal, Once=%s" % [name, once])
		triggered.emit()
