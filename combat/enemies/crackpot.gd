class_name Crackpot
extends CombatEnemy

enum State { IDLE, TELEGRAPH, LEAPING, PUDDLE_ACTIVE, COOLDOWN }

const PUDDLE_SCENE: PackedScene = preload("res://combat/enemies/CrackpotPuddle.tscn")

@export var leap_speed: float = 300.0
@export var puddle_damage: int = 5
@export var puddle_lifetime: float = 4.0
@export var telegraph_duration: float = 0.8
@export var cooldown_duration: float = 1.2

var state: State = State.IDLE
var _state_timer: float = 0.0
var _target_position: Vector2
var _start_position: Vector2
var _leap_progress: float = 0.0
var _sprite: Sprite2D
var _base_tint := Color(0.6, 0.3, 0.1)
var _active_puddles: Array[Node2D] = []

func _ready() -> void:
	max_hp = 60
	contact_damage = 8
	super._ready()
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	if _sprite != null:
		_sprite.modulate = _base_tint
	_start_position = global_position
	_enter_state(State.IDLE)

func _process(delta: float) -> void:
	_state_timer += delta
	match state:
		State.IDLE:
			if _state_timer >= 0.8:
				_enter_state(State.TELEGRAPH)
		State.TELEGRAPH:
			if _sprite != null:
				_sprite.modulate = Color(1, 0.5, 0) if fmod(_state_timer, 0.15) < 0.075 else _base_tint
			if _state_timer >= telegraph_duration:
				_pick_target_position()
				_enter_state(State.LEAPING)
		State.LEAPING:
			_leap_progress = clampf(_state_timer / 0.4, 0.0, 1.0)
			var eased := _leap_progress * _leap_progress * (3.0 - 2.0 * _leap_progress)
			global_position = _start_position.lerp(_target_position, eased)
			global_position.y -= sin(_leap_progress * PI) * 60.0
			if _sprite != null:
				_sprite.rotation += delta * 8.0
			if _leap_progress >= 1.0:
				_spawn_puddle()
				_enter_state(State.PUDDLE_ACTIVE)
		State.PUDDLE_ACTIVE:
			if _state_timer >= puddle_lifetime:
				_enter_state(State.COOLDOWN)
		State.COOLDOWN:
			if _state_timer >= cooldown_duration:
				_enter_state(State.IDLE)

func _enter_state(next_state: State) -> void:
	state = next_state
	_state_timer = 0.0
	match next_state:
		State.IDLE:
			_start_position = global_position
			if _sprite != null:
				_sprite.modulate = _base_tint
				_sprite.rotation = 0.0
		State.TELEGRAPH:
			_state_timer = 0.0
		State.LEAPING:
			_leap_progress = 0.0
		State.PUDDLE_ACTIVE:
			if _sprite != null:
				_sprite.modulate = _base_tint
		State.COOLDOWN:
			if _sprite != null:
				_sprite.modulate = Color(0.8, 0.8, 0.8)

func _pick_target_position() -> void:
	_target_position = Vector2(randf_range(-300.0, 300.0), randf_range(-200.0, 200.0))
	if _sprite != null:
		_sprite.scale.x = 1.0 if _target_position.x >= global_position.x else -1.0

func _spawn_puddle() -> void:
	var puddle := PUDDLE_SCENE.instantiate() as Node2D
	if puddle == null:
		return
	puddle.set("damage", puddle_damage)
	puddle.set("lifetime", puddle_lifetime)
	puddle.global_position = global_position
	get_parent().add_child(puddle)
	_active_puddles.append(puddle)

func on_parried(knockback: Vector2) -> void:
	_cleanse_puddles()
	var tween := create_tween()
	tween.tween_property(self, "position", position + knockback * 0.3, 0.3)
	tween.tween_callback(func() -> void:
		if state != State.COOLDOWN:
			_enter_state(State.COOLDOWN)
	)
	if _sprite != null:
		_sprite.modulate = Color(0.5, 0.5, 1.0)

func _cleanse_puddles() -> void:
	for puddle: Node2D in _active_puddles:
		if is_instance_valid(puddle):
			puddle.queue_free()
	_active_puddles.clear()
