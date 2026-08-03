class_name CombatCereal
extends CombatEnemy

enum State { IDLE, TELEGRAPH, ATTACK, COOLDOWN }

const BULLET_SCENE: PackedScene = preload("res://combat/bullets/RedBullet.tscn")
const RINGS_PER_VOLLEY: int = 2
const RING_DELAY: float = 0.45

@export var bullets_per_ring: int = 14
@export var bullet_speed: float = 160.0

var state: State = State.IDLE
var _state_timer: float = 0.0
var _state_duration: float = 1.5
var _rings_fired: int = 0
var _ring_delay_timer: float = 0.0
var _ring_angle_offset: float = 0.0
var _sprite: Sprite2D

func _ready() -> void:
	max_hp = 70
	contact_damage = 8
	super._ready()
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	if _sprite != null:
		_sprite.modulate = Color(0.95, 0.7, 0.4)
	_enter_state(State.IDLE)

func _process(delta: float) -> void:
	_state_timer += delta
	match state:
		State.IDLE:
			if _state_timer >= _state_duration:
				_enter_state(State.TELEGRAPH)
		State.TELEGRAPH:
			if _sprite != null:
				_sprite.modulate = Color(1, 0.4, 0.3)
			if _state_timer >= _state_duration:
				_enter_state(State.ATTACK)
		State.ATTACK:
			if _rings_fired == 0:
				_fire_ring()
				_rings_fired = 1
				_ring_delay_timer = 0.0
			else:
				_ring_delay_timer += delta
				if _ring_delay_timer >= RING_DELAY:
					_fire_ring()
					_rings_fired += 1
					_ring_delay_timer = 0.0
			if _rings_fired >= RINGS_PER_VOLLEY or _state_timer >= _state_duration * 3.0:
				_enter_state(State.COOLDOWN)
		State.COOLDOWN:
			if _state_timer >= _state_duration:
				_enter_state(State.IDLE)

func _enter_state(next_state: State) -> void:
	state = next_state
	_state_timer = 0.0
	_rings_fired = 0
	_ring_delay_timer = 0.0
	match next_state:
		State.IDLE:
			_state_duration = 1.0
		State.TELEGRAPH:
			_state_duration = 0.7
		State.ATTACK:
			_state_duration = 2.0
		State.COOLDOWN:
			_state_duration = 1.6
			if _sprite != null:
				_sprite.modulate = Color(0.95, 0.7, 0.4)

func _fire_ring() -> void:
	var count: int = maxi(1, bullets_per_ring)
	var angle_step := TAU / float(count)
	var start_angle := _ring_angle_offset
	_ring_angle_offset = fmod(_ring_angle_offset + angle_step * 0.5, TAU)
	for index in count:
		_spawn_bullet(Vector2.from_angle(start_angle + angle_step * float(index)), bullet_speed)

func _spawn_bullet(bullet_direction: Vector2, speed: float) -> void:
	var bullet := BULLET_SCENE.instantiate() as RedBullet
	get_parent().add_child(bullet)
	bullet.collision_mask = RedBullet.PLAYER_BULLET_MASK
	bullet.collision_layer = RedBullet.BULLET_LAYER
	bullet.global_position = global_position
	bullet.fired_by = self
	bullet.reset_lifetime()
	bullet.set_direction(bullet_direction, speed)
