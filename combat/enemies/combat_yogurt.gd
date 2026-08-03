class_name CombatYogurt
extends CombatEnemy

enum State { IDLE, TELEGRAPH, ATTACK, COOLDOWN }

const BULLET_SCENE: PackedScene = preload("res://combat/bullets/RedBullet.tscn")
const SPIRAL_SHOTS: int = 12
const SPIRAL_DELAY: float = 0.08
const SPIRAL_STEP: float = deg_to_rad(24.0)

var state: State = State.IDLE
var _state_timer: float = 0.0
var _state_duration: float = 1.5
var _spiral_angle: float = 0.0
var _shots_fired: int = 0
var _shot_timer: float = 0.0
var _sprite: Sprite2D

func _ready() -> void:
	max_hp = 60
	contact_damage = 8
	super._ready()
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	if _sprite != null:
		_sprite.modulate = Color(0.95, 0.95, 0.82)
	_enter_state(State.IDLE)

func _process(delta: float) -> void:
	_state_timer += delta
	match state:
		State.IDLE:
			if _state_timer >= _state_duration:
				_enter_state(State.TELEGRAPH)
		State.TELEGRAPH:
			if _sprite != null:
				_sprite.modulate = Color(1, 0.4, 0.5)
			if _state_timer >= _state_duration:
				_enter_state(State.ATTACK)
		State.ATTACK:
			_shot_timer += delta
			if _shot_timer >= SPIRAL_DELAY and _shots_fired < SPIRAL_SHOTS:
				_fire_spiral_bullet()
				_shots_fired += 1
				_shot_timer = 0.0
			if _shots_fired >= SPIRAL_SHOTS or _state_timer >= _state_duration * 3.0:
				_enter_state(State.COOLDOWN)
		State.COOLDOWN:
			if _state_timer >= _state_duration:
				_enter_state(State.IDLE)

func _enter_state(next_state: State) -> void:
	state = next_state
	_state_timer = 0.0
	_shots_fired = 0
	_shot_timer = 0.0
	match next_state:
		State.IDLE:
			_state_duration = 1.2
		State.TELEGRAPH:
			_state_duration = 0.8
		State.ATTACK:
			_state_duration = 2.0
		State.COOLDOWN:
			_state_duration = 1.8
			if _sprite != null:
				_sprite.modulate = Color(0.95, 0.95, 0.82)

func _fire_spiral_bullet() -> void:
	var bullet := BULLET_SCENE.instantiate() as RedBullet
	get_parent().add_child(bullet)
	bullet.collision_mask = RedBullet.PLAYER_BULLET_MASK
	bullet.collision_layer = RedBullet.BULLET_LAYER
	bullet.global_position = global_position
	bullet.fired_by = self
	bullet.reset_lifetime()
	bullet.set_direction(Vector2.from_angle(_spiral_angle), 180.0)
	_spiral_angle = fmod(_spiral_angle + SPIRAL_STEP, TAU)
