class_name ParryComponent
extends Node2D

signal parried

@export var parry_radius: float = 110.0
@export var parry_damage: int = 10
@export var cooldown: float = 0.5

const BULLET_LAYER: int = 8
var _cooldown_timer: float = 0.0
var _ring_flash: float = 0.0
var _can_parry: bool = true

func _process(delta: float) -> void:
	if not _can_parry:
		_cooldown_timer = maxf(0.0, _cooldown_timer - delta)
		if _cooldown_timer <= 0.0:
			_can_parry = true
	if _can_parry and Input.is_action_just_pressed("combat_parry") and _in_combat():
		try_parry()
	_ring_flash = move_toward(_ring_flash, 0.0, delta * 4.0)
	queue_redraw()

func try_parry() -> void:
	if not _can_parry:
		return
	_can_parry = false
	_cooldown_timer = cooldown
	var successful: bool = false
	for node: Node in get_tree().get_nodes_in_group("bullet"):
		var bullet: RedBullet = node as RedBullet
		if bullet == null or bullet.reflected:
			continue
		if global_position.distance_to(bullet.global_position) <= parry_radius:
			bullet.reflect(global_position)
			successful = true
	for node: Node in get_tree().get_nodes_in_group("enemy"):
		if not node is Node2D:
			continue
		var enemy: Node2D = node as Node2D
		if global_position.distance_to(enemy.global_position) > parry_radius:
			continue
		var health: HealthComponent = enemy.get("health") as HealthComponent
		if health == null or health.is_dead:
			continue
		if enemy.has_method("on_parried"):
			enemy.call("on_parried", global_position.direction_to(enemy.global_position) * 300.0)
		health.take_damage(parry_damage + _attack_bonus(), self)
		successful = true
	if successful:
		_ring_flash = 1.0
		parried.emit()
		_show_feedback("PARRY!", Color(1.0, 1.0, 0.2))
	else:
		_ring_flash = -1.0

func update_stats(radius_boost: float, damage_boost: int) -> void:
	parry_radius = 110.0 + radius_boost
	parry_damage = 10 + damage_boost

func _draw() -> void:
	if not _in_combat():
		return
	var color: Color = Color(0.3, 0.8, 1.0, 0.2)
	if _ring_flash > 0.0:
		color = Color(1.0, 1.0, 0.2, 0.3 + _ring_flash * 0.3)
	elif _ring_flash < 0.0:
		color = Color(1.0, 0.3, 0.3, 0.3)
	draw_arc(Vector2.ZERO, parry_radius, 0.0, TAU, 32, color, 2.0)
	if _can_parry:
		draw_arc(Vector2.ZERO, parry_radius - 4.0, 0.0, TAU, 32, Color(1, 1, 1, 0.15), 1.0)

func _in_combat() -> bool:
	var controller: Node = get_tree().root.get_node_or_null("GameController")
	var level: Node = controller.get("current_level") if controller != null else null
	return level is CombatArena

func _attack_bonus() -> int:
	var equipment: Node = get_tree().root.get_node_or_null("Equipment")
	return int(equipment.get("total_attack_boost")) if equipment != null else 0

func _show_feedback(text: String, color: Color) -> void:
	var label := Label.new()
	label.text = text
	label.position = Vector2(270, 160)
	label.modulate = color
	label.add_theme_font_size_override("font_size", 24)
	get_tree().root.add_child(label)
	var tween := create_tween()
	tween.set_parallel(true)
	tween.tween_property(label, "scale", Vector2(1.4, 1.4), 0.4)
	tween.tween_property(label, "modulate:a", 0.0, 0.6)
	tween.chain().tween_callback(label.queue_free)
