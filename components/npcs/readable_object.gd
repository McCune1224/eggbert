class_name ReadableObject
extends InteractableArea
## Interact-triggered dialog for signs, posters, books, scribbled notes.

@export var dialog_lines: PackedStringArray = []
@export var alternate_lines: PackedStringArray = []
## If set, the WorldFlag to check before showing dialog; when true, AlternateLines are shown.
@export var gate_flag: String = ""
## If true, this readable can only be read once.
@export var once: bool = false

var _has_been_read: bool = false

func _ready() -> void:
	super._ready()
	if Engine.is_editor_hint():
		return
	if once and not gate_flag.is_empty():
		var flags := get_tree().root.get_node_or_null("WorldFlags")
		if flags != null and flags.has_method("has_flag") and bool(flags.call("has_flag", "read_" + gate_flag)):
			_has_been_read = true
			queue_free()
			GameLogger.debug("ReadableObject", "'%s': already read (flag='read_%s') — removed" % [name, gate_flag])

func on_interact() -> void:
	if _has_been_read:
		return
	if once:
		var flag: String = "read_" + (gate_flag if not gate_flag.is_empty() else str(name))
		var flags := get_tree().root.get_node_or_null("WorldFlags")
		if flags != null and flags.has_method("set_flag"):
			flags.call("set_flag", flag, true)
		_has_been_read = true
	var lines: PackedStringArray
	if not gate_flag.is_empty():
		var flags := get_tree().root.get_node_or_null("WorldFlags")
		var gated := flags != null and flags.has_method("has_flag") and bool(flags.call("has_flag", gate_flag))
		if gated and not alternate_lines.is_empty():
			lines = alternate_lines
			GameLogger.debug("ReadableObject", "'%s': showing alternate lines (gate='%s'=true)" % [name, gate_flag])
		else:
			lines = dialog_lines
			GameLogger.debug("ReadableObject", "'%s': showing default lines (%d)" % [name, dialog_lines.size()])
	else:
		lines = dialog_lines
		GameLogger.debug("ReadableObject", "'%s': showing default lines (%d)" % [name, dialog_lines.size()])
	_show_dialog(lines)

func _show_dialog(lines: PackedStringArray) -> void:
	if lines.is_empty():
		return
	var cutscene := get_tree().root.get_node_or_null("CutsceneController")
	if cutscene != null and cutscene.has_method("start_dialog"):
		cutscene.call("start_dialog", lines, voice)
	else:
		var dialog := get_tree().root.get_node_or_null("DialogManager")
		if dialog != null and dialog.has_method("start_dialog"):
			dialog.call("start_dialog", lines, voice)
