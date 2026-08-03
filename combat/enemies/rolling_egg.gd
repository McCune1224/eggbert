class_name RollingEgg
extends CombatEnemy

enum State { IDLE, TELEGRAPH, ATTACKING, COOLDOWN, STUNNED }

@export var charge_speed: float = 250.0
@export var wall_bounces_before_cooldown: int = 3

var state: State = State.IDLE
var _state_timer: float = 0.0
var _state_duration: float = 1.0
var _move_direction: Vector2 = Vector2.DOWN
var _wall_bounces: int = 0
var _knockback_velocity: Vector2 = Vector2.ZERO
var _sprite: Sprite2D
var _base_tint: Color = Color(0.9, 0.2, 0.2)

func _ready() -> void:
	max_hp = 40
	contact_damage = 10
	super._ready()
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	if _sprite != null:
		_sprite.modulate = _base_tint
	_move_direction = Vector2.RIGHT.rotated(randf_range(0.0, TAU))
	_enter_state(State.IDLE)

func _process(delta: float) -> void:
	if _knockback_velocity.length_squared() > 1.0:
		global_position += _knockback_velocity * delta
		_knockback_velocity *= 0.85
		if _knockback_velocity.length_squared() <= 1.0:
			_knockback_velocity = Vector2.ZERO
		return
	_state_timer += delta
	match state:
		State.IDLE:
			if _state_timer >= _state_duration:
				_enter_state(State.TELEGRAPH)
		State.TELEGRAPH:
			if _sprite != null:
				var pulse := (sin(clampf(_state_timer / _state_duration, 0.0, 1.0) * PI * 4.0) * 0.5 + 0.5)
				_sprite.modulate = _base_tint.lerp(Color(1, 1, 0.3), pulse)
			if _state_timer >= _state_duration:
				_enter_state(State.ATTACKING)
		State.ATTACKING:
			global_position += _move_direction * charge_speed * delta
			_bounce_at_bounds()
			if _wall_bounces >= wall_bounces_before_cooldown or _state_timer >= 4.0:
				_enter_state(State.COOLDOWN)
		State.COOLDOWN, State.STUNNED:
			if _state_timer >= _state_duration:
				_enter_state(State.IDLE)

func _enter_state(next_state: State) -> void:
	state = next_state
	_state_timer = 0.0
	match next_state:
		State.IDLE:
			_state_duration = randf_range(0.8, 1.5)
			if _sprite != null:
				_sprite.modulate = _base_tint
			_move_direction = (CombatTargeter.get_player_position(global_position) - global_position).normalized() if randf() < 0.6 else Vector2.RIGHT.rotated(randf_range(0.0, TAU))
		State.TELEGRAPH:
			_state_duration = 0.6
			_wall_bounces = 0
		State.COOLDOWN:
			_state_duration = randf_range(0.5, 1.0)
			if _sprite != null:
				_sprite.modulate = _base_tint
		State.STUNNED:
			_state_duration = 0.8
			if _sprite != null:
				_sprite.modulate = Color(0.5, 0.5, 1.0)

func _bounce_at_bounds() -> void:
	var half_width := 240.0
	var half_height := 160.0
	if global_position.x < -half_width or global_position.x > half_width:
		global_position.x = clampf(global_position.x, -half_width, half_width)
		_move_direction.x = -_move_direction.x
		_wall_bounces += 1
	if global_position.y < -half_height or global_position.y > half_height:
		global_position.y = clampf(global_position.y, -half_height, half_height)
		_move_direction.y = -_move_direction.y
		_wall_bounces += 1

func on_parried(knockback: Vector2) -> void:
	_knockback_velocity = knockback
	_enter_state(State.STUNNED)

func _on_body_entered(body: Node2D) -> void:
	if body.is_in_group("player") and state == State.ATTACKING:
		super._on_body_entered(body)
		_enter_state(State.COOLDOWN)
