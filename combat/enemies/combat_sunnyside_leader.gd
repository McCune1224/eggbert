class_name CombatSunnysideLeader
extends CombatEnemy

enum State { IDLE, TELEGRAPH, ATTACK, COOLDOWN }

const BULLET_SCENE: PackedScene = preload("res://combat/bullets/RedBullet.tscn")
const PHASE3_SHOTS: int = 16
const SPIRAL_STEP: float = deg_to_rad(22.5)
const SPIRAL_DELAY: float = 0.07

var state: State = State.IDLE
var phase: int = 1
var _state_timer: float = 0.0
var _state_duration: float = 1.5
var _spiral_angle: float = 0.0
var _spiral_shots: int = 0
var _spiral_timer: float = 0.0
var _sprite: Sprite2D

func _ready() -> void:
	max_hp = 120
	contact_damage = 10
	super._ready()
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	if health != null:
		health.damaged.connect(_on_health_damaged)
	_update_phase_visuals()
	_enter_state(State.IDLE)

func _process(delta: float) -> void:
	_state_timer += delta
	match state:
		State.IDLE:
			if _state_timer >= _state_duration:
				_enter_state(State.TELEGRAPH)
		State.TELEGRAPH:
			if _sprite != null:
				_sprite.modulate = Color(1, 0.25, 0.3)
			if _state_timer >= _state_duration:
				_enter_state(State.ATTACK)
		State.ATTACK:
			_execute_attack(delta)
		State.COOLDOWN:
			if _state_timer >= _state_duration:
				_enter_state(State.IDLE)

func _execute_attack(delta: float) -> void:
	if phase == 1:
		_attack_aimed_spread()
		_enter_state(State.COOLDOWN)
	elif phase == 2:
		_attack_ring()
		_enter_state(State.COOLDOWN)
	else:
		_spiral_timer += delta
		if _spiral_timer >= SPIRAL_DELAY and _spiral_shots < PHASE3_SHOTS:
			_fire_spiral_bullet()
			_spiral_shots += 1
			_spiral_timer = 0.0
		if _spiral_shots >= PHASE3_SHOTS:
			_attack_aimed_spread()
			_enter_state(State.COOLDOWN)
	if _state_timer >= _state_duration * 2.5:
		_enter_state(State.COOLDOWN)

func _enter_state(next_state: State) -> void:
	state = next_state
	_state_timer = 0.0
	_spiral_shots = 0
	_spiral_timer = 0.0
	match next_state:
		State.IDLE:
			_state_duration = 0.6 if phase == 3 else 1.0
		State.TELEGRAPH:
			_state_duration = 0.5 if phase == 3 else 0.7
		State.ATTACK:
			_state_duration = 1.8
		State.COOLDOWN:
			_state_duration = 1.0 if phase == 3 else 1.5

func _on_health_damaged(_amount: int, _source: Node) -> void:
	if health == null or health.max_hp <= 0:
		return
	var ratio := float(health.current_hp) / float(health.max_hp)
	var next_phase: int = 3 if ratio <= 0.33 else (2 if ratio <= 0.66 else 1)
	if next_phase != phase:
		phase = next_phase
		_update_phase_visuals()

func _update_phase_visuals() -> void:
	if _sprite == null:
		return
	_sprite.modulate = Color(1, 0.95, 0.7) if phase == 1 else (Color(1, 0.7, 0.5) if phase == 2 else Color(1, 0.4, 0.4))

func _attack_aimed_spread() -> void:
	var direction_to_player := global_position.direction_to(CombatTargeter.get_player_position(global_position))
	var base_angle := direction_to_player.angle() - deg_to_rad(12.5)
	for index in 3:
		var angle := base_angle + deg_to_rad(12.5 * float(index))
		_spawn_bullet(Vector2.from_angle(angle), 220.0)

func _attack_ring() -> void:
	for index in 12:
		_spawn_bullet(Vector2.from_angle(TAU * float(index) / 12.0), 170.0)

func _fire_spiral_bullet() -> void:
	_spawn_bullet(Vector2.from_angle(_spiral_angle), 200.0)
	_spiral_angle = fmod(_spiral_angle + SPIRAL_STEP, TAU)

func _spawn_bullet(bullet_direction: Vector2, speed: float) -> void:
	var bullet := BULLET_SCENE.instantiate() as RedBullet
	get_parent().add_child(bullet)
	bullet.collision_mask = RedBullet.PLAYER_BULLET_MASK
	bullet.collision_layer = RedBullet.BULLET_LAYER
	bullet.global_position = global_position
	bullet.fired_by = self
	bullet.reset_lifetime()
	bullet.set_direction(bullet_direction, speed)
