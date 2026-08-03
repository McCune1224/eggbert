class_name FakeWall
extends StaticBody2D

@export_group("Behavior")
@export var require_interact: bool = false
@export_group("Dialog")
@export var reveal_dialog_lines: Array[String] = []
@export var reveal_voice: Resource

var _collision: CollisionShape2D
var _sprite: Sprite2D
var _trigger_area: Area2D
var _player_near: bool = false
var _revealed: bool = false

func _ready() -> void:
	collision_layer = CollisionConfig.WALLS_LAYER
	_collision = get_node_or_null("CollisionShape2D") as CollisionShape2D
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	_trigger_area = get_node_or_null("TriggerArea") as Area2D
	if _trigger_area != null:
		if require_interact:
			_trigger_area.body_entered.connect(_on_proximity_entered)
			_trigger_area.body_exited.connect(_on_proximity_exited)
		else:
			_trigger_area.body_entered.connect(_on_body_entered)
		_update_prompt()

func _input(event: InputEvent) -> void:
	if require_interact and _player_near and not _revealed and event.is_action_pressed("interact"):
		_reveal()

func _on_body_entered(body: Node2D) -> void:
	if body.is_in_group("player"):
		_reveal()

func _on_proximity_entered(body: Node2D) -> void:
	if body.is_in_group("player"):
		_player_near = true
		_update_prompt()

func _on_proximity_exited(body: Node2D) -> void:
	if body.is_in_group("player"):
		_player_near = false
		_update_prompt()

func _reveal() -> void:
	if _revealed:
		return
	_revealed = true
	if _collision != null:
		_collision.set_deferred("disabled", true)
	if _sprite != null:
		_sprite.modulate = Color(1.0, 1.0, 1.0, 0.3)
	if not reveal_dialog_lines.is_empty():
		var cutscene := get_tree().root.get_node_or_null("CutsceneController")
		if cutscene != null and cutscene.has_method("start_dialog"):
			cutscene.call("start_dialog", reveal_dialog_lines, reveal_voice)
	_update_prompt()

func _update_prompt() -> void:
	var player := get_tree().root.get_node_or_null("Player")
	var prompt: Node = player.get("interaction_prompt") if player != null else null
	if prompt != null and prompt.has_method("set_interactable_available"):
		prompt.call("set_interactable_available", self, require_interact and _player_near and not _revealed)
