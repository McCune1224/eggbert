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
	GameLogger.info("Dialog", "Started dialog (%d lines, speaker '%s'): %s" % [current_lines.size(), current_speaker, " | ".join(current_lines)])
	for line in current_lines:
		line_started.emit(line, current_speaker)
		await get_tree().process_frame
	is_dialog_active = false
	dialog_finished.emit()
	GameLogger.info("Dialog", "Dialog finished (%d lines)" % current_lines.size())

func stop_dialog() -> void:
	GameLogger.info("Dialog", "Dialog stopped early (speaker '%s')" % current_speaker)
	current_lines.clear()
	is_dialog_active = false
	dialog_finished.emit()
