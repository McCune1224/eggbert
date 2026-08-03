class_name KeyDoor
extends Door

@export_group("KeyDoor")
@export var required_flag: String = ""
@export var locked_message: String = "It's locked."
@export var unlock_jingle: AudioStream

var _permanently_unlocked: bool = false

func open() -> void:
	if _permanently_unlocked or required_flag.is_empty():
		super.open()
		return
	var flags := get_tree().root.get_node_or_null("WorldFlags")
	if flags != null and bool(flags.call("has_flag", required_flag)):
		_permanently_unlocked = true
		_play_sfx(unlock_jingle)
		super.open()
		return
	_start_dialog([locked_message])

func close() -> void:
	if not _permanently_unlocked:
		super.close()

func _start_dialog(lines: Array[String]) -> void:
	var dialog := get_tree().root.get_node_or_null("DialogManager")
	if dialog != null and dialog.has_method("start_dialog"):
		dialog.call("start_dialog", lines)
