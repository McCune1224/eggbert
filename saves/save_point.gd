class_name SavePoint
extends InteractableArea
## Undertale-style save point placed in levels. Player interacts (E key) to:
## 1. Be fully healed
## 2. Save the game (writes SaveFile to disk)
## 3. See/hear save feedback ("Game saved." popup)

@export var location_name: String = "Save Point"
@export var save_sfx: AudioStream

var _star_sprite: Sprite2D
var _animation_player: AnimationPlayer
var _save_label: Label
var _saving: bool = false

func _ready() -> void:
	super._ready()
	if Engine.is_editor_hint():
		return
	_star_sprite = get_node_or_null("StarSprite") as Sprite2D
	_animation_player = get_node_or_null("AnimationPlayer") as AnimationPlayer
	_save_label = get_node_or_null("SaveLabel") as Label
	if _save_label != null:
		_save_label.visible = false
	if _animation_player != null and _animation_player.has_animation("idle"):
		_animation_player.play("idle")
	if save_sfx == null:
		save_sfx = load("res://assets/audio/sfx/meep.ogg") as AudioStream
	GameLogger.debug("SavePoint", "'%s': _Ready — location='%s'" % [name, location_name])

func on_interact() -> void:
	if _saving:
		return
	_saving = true
	if _animation_player != null and _animation_player.has_animation("save_burst"):
		_animation_player.play("save_burst")
	if save_sfx != null:
		var audio := get_tree().root.get_node_or_null("AudioManager")
		if audio != null and audio.has_method("play_sfx"):
			audio.call("play_sfx", save_sfx)
	else:
		GameLogger.warn("SavePoint", "'%s': save_sfx is null" % name)
	var player := get_tree().root.get_node_or_null("Player")
	if player != null:
		var health: Node = player.get("health_component")
		if health != null and health.has_method("set_max_hp"):
			health.call("set_max_hp", int(health.get("max_hp")), true)
	var controller := get_tree().root.get_node_or_null("GameController")
	var scene_path := ""
	if controller != null and controller.get("current_level") != null:
		var level: Node = controller.get("current_level")
		scene_path = str(level.get("scene_file_path"))
		if scene_path.is_empty():
			scene_path = str(level.get("scene_path"))
	var save_manager := get_tree().root.get_node_or_null("SaveManager")
	if save_manager != null and save_manager.has_method("save_game"):
		save_manager.call("save_game", scene_path, global_position, location_name)
	GameLogger.info("SavePoint", "'%s': saved at '%s' — scene='%s', pos=%s, player healed" % [name, location_name, scene_path, global_position])
	if _save_label != null:
		_save_label.visible = true
		_save_label.modulate = Color(1.0, 1.0, 1.0, 1.0)
		_save_label.position = Vector2(0.0, -16.0)
		var tween := create_tween().set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
		tween.tween_property(_save_label, "position", Vector2(0.0, -48.0), 1.0)
		tween.parallel().tween_property(_save_label, "modulate", Color(1.0, 1.0, 1.0, 0.0), 1.0)
		tween.chain().tween_callback(func() -> void: _save_label.visible = false)
	await get_tree().create_timer(0.5).timeout
	_saving = false
