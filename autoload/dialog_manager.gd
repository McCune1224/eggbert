extends Node

signal dialog_finished
signal line_started(text: String, speaker: String)

var is_dialog_active := false
var current_lines: Array[String] = []
var current_speaker := ""
var current_voice: DialogVoiceResource

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS

func start_dialog(lines: Array[String], voice: DialogVoiceResource = null, speaker: String = "") -> void:
	current_lines = lines.duplicate()
	current_voice = voice
	current_speaker = speaker
	is_dialog_active = true
	for line in current_lines:
		line_started.emit(line, current_speaker)
		await get_tree().process_frame
	is_dialog_active = false
	dialog_finished.emit()

func stop_dialog() -> void:
	current_lines.clear()
	is_dialog_active = false
	dialog_finished.emit()
