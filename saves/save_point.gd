class_name SavePoint
extends Area2D

@export var location_name: String = "Save Point"
@export var save_sfx: AudioStream

var _player_nearby := false
var _saving := false
@onready var _animation_player: AnimationPlayer = get_node_or_null("AnimationPlayer")
@onready var _save_label: Label = get_node_or_null("SaveLabel")

func _ready() -> void:
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)
	if _save_label != null:
		_save_label.visible = false
	if _animation_player != null and _animation_player.has_animation("idle"):
		_animation_player.play("idle")
	if save_sfx == null:
		save_sfx = load("res://assets/audio/sfx/meep.ogg") as AudioStream

func _unhandled_input(event: InputEvent) -> void:
	if _player_nearby and event.is_action_pressed("interact"):
		_save_game()
		get_viewport().set_input_as_handled()

func _on_body_entered(body: Node2D) -> void:
	if body.is_in_group("player"):
		_player_nearby = true

func _on_body_exited(body: Node2D) -> void:
	if body.is_in_group("player"):
		_player_nearby = false

func _save_game() -> void:
	if _saving:
		return
	_saving = true
	if _animation_player != null and _animation_player.has_animation("save_burst"):
		_animation_player.play("save_burst")
	if save_sfx != null and AudioManager.has_method("play_sfx"):
		AudioManager.play_sfx(save_sfx)
	var player := get_tree().root.get_node_or_null("Player")
	if player != null:
		var health := player.get_node_or_null("HealthComponent")
		if health != null and health.has_method("heal"):
			health.heal(100000)
	var level := GameController.current_level if GameController != null else null
	var scene_path := level.scene_file_path if level != null else ""
	SaveManager.save_game(scene_path, global_position, location_name)
	_show_saved_label()
	get_tree().create_timer(0.5).timeout.connect(func() -> void: _saving = false)

func _show_saved_label() -> void:
	if _save_label == null:
		return
	_save_label.visible = true
	_save_label.modulate = Color.WHITE
	_save_label.position = Vector2(0, -16)
	var tween := create_tween().set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tween.tween_property(_save_label, "position", Vector2(0, -48), 1.0)
	tween.parallel().tween_property(_save_label, "modulate", Color(1, 1, 1, 0), 1.0)
	tween.tween_callback(func() -> void: _save_label.visible = false)
