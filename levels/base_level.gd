class_name BaseLevel
extends Node2D

signal level_started
signal level_ended

@export var level_ambience: AudioStream
@export var level_music: AudioStream
@export var level_name: String = ""

func _ready() -> void:
	if level_name.is_empty():
		level_name = name
	var audio := get_tree().root.get_node_or_null("AudioManager")
	if audio != null:
		if level_music != null and audio.has_method("play_music"):
			audio.call("play_music", level_music)
		if level_ambience != null and audio.has_method("play_ambience"):
			audio.call("play_ambience", level_ambience)
	level_started.emit()

func _exit_tree() -> void:
	var audio := get_tree().root.get_node_or_null("AudioManager")
	if audio != null and level_ambience != null and audio.has_method("stop_ambience"):
		audio.call("stop_ambience")
	level_ended.emit()
