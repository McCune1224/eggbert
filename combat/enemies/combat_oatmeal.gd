class_name CombatOatmeal
extends CombatEnemy

enum OatmealFlavor { VANILLA, STRAWBERRY, CHOCOLATE, MINT }
enum State { IDLE, TELEGRAPH, ATTACKING, COOLDOWN }

const BULLET_SCENE: PackedScene = preload("res://combat/bullets/RedBullet.tscn")

@export var flavor: OatmealFlavor = OatmealFlavor.VANILLA

var state: State = State.IDLE
var _state_timer: float = 0.0
var _state_duration: float = 1.0
var _sprite: Sprite2D
var _base_tint: Color = Color.WHITE

func _ready() -> void:
	max_hp = _flavor_hp()
	contact_damage = 0
	super._ready()
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	_apply_flavor_visuals()
	_enter_state(State.IDLE)

func apply_flavor() -> void:
	_apply_flavor_visuals()
	if health != null:
		health.set_max_hp(_flavor_hp(), true)

func _process(delta: float) -> void:
	_state_timer += delta
	match state:
		State.IDLE:
			if _state_timer >= _state_duration:
				_enter_state(State.TELEGRAPH)
		State.TELEGRAPH:
			if _sprite != null:
				var pulse := (sin(clampf(_state_timer / _state_duration, 0.0, 1.0) * PI * 6.0) * 0.5 + 0.5) * clampf(_state_timer / _state_duration, 0.0, 1.0)
				_sprite.modulate = _base_tint.lerp(Color(1, 0.35, 0.3), pulse)
			if _state_timer >= _state_duration:
				_enter_state(State.ATTACKING)
		State.ATTACKING:
			_attack()
			_enter_state(State.COOLDOWN)
		State.COOLDOWN:
			if _state_timer >= _state_duration:
				_enter_state(State.IDLE)

func _enter_state(next_state: State) -> void:
	state = next_state
	_state_timer = 0.0
	var profile := _timing_profile()
	match next_state:
		State.IDLE:
			_state_duration = randf_range(profile.x, profile.y)
			if _sprite != null:
				_sprite.modulate = _base_tint
		State.TELEGRAPH:
			_state_duration = profile.z
		State.COOLDOWN:
			_state_duration = profile.w

func _attack() -> void:
	match flavor:
		OatmealFlavor.STRAWBERRY:
			_attack_homing()
		OatmealFlavor.CHOCOLATE:
			_attack_aimed()
		OatmealFlavor.MINT:
			_attack_burst()
		OatmealFlavor.VANILLA:
			_attack_spread()

func _attack_spread() -> void:
	_fire_spread(3, 30.0, 250.0)

func _attack_homing() -> void:
	var target := CombatTargeter.get_player_position(global_position)
	for index in 2:
		var angle := (target - global_position).angle() + deg_to_rad(float(index) * 15.0 - 7.5)
		var bullet := _spawn_bullet(Vector2.from_angle(angle), 180.0)
		bullet.is_homing = true

func _attack_aimed() -> void:
	var target := CombatTargeter.get_player_position(global_position)
	for index in 2:
		var angle := (target - global_position).angle() + deg_to_rad(-8.0 if index == 0 else 8.0)
		_spawn_bullet(Vector2.from_angle(angle), 350.0)

func _attack_burst() -> void:
	_fire_spread(5, 20.0, 400.0)

func _fire_spread(count: int, spread_degrees: float, bullet_speed: float) -> void:
	var direction_to_player := global_position.direction_to(CombatTargeter.get_player_position(global_position))
	var base_angle := direction_to_player.angle() - deg_to_rad(spread_degrees * 0.5)
	for index in count:
		var angle := base_angle + deg_to_rad(spread_degrees * float(index) / float(maxi(1, count - 1)))
		_spawn_bullet(Vector2.from_angle(angle), bullet_speed)

func _spawn_bullet(bullet_direction: Vector2, bullet_speed: float) -> RedBullet:
	var bullet := BULLET_SCENE.instantiate() as RedBullet
	get_parent().add_child(bullet)
	bullet.collision_mask = RedBullet.PLAYER_BULLET_MASK
	bullet.collision_layer = RedBullet.BULLET_LAYER
	bullet.global_position = global_position
	bullet.fired_by = self
	bullet.reset_lifetime()
	bullet.set_direction(bullet_direction, bullet_speed)
	return bullet

func _flavor_hp() -> int:
	match flavor:
		OatmealFlavor.STRAWBERRY:
			return 25
		OatmealFlavor.CHOCOLATE:
			return 40
		OatmealFlavor.MINT:
			return 20
	return 30

func _timing_profile() -> Vector4:
	match flavor:
		OatmealFlavor.STRAWBERRY:
			return Vector4(0.7, 1.3, 0.45, 0.9)
		OatmealFlavor.CHOCOLATE:
			return Vector4(1.0, 2.0, 0.6, 1.2)
		OatmealFlavor.MINT:
			return Vector4(0.6, 1.0, 0.35, 1.4)
	return Vector4(0.8, 1.6, 0.5, 1.0)

func _apply_flavor_visuals() -> void:
	match flavor:
		OatmealFlavor.STRAWBERRY:
			_base_tint = Color(1, 0.6, 0.7)
		OatmealFlavor.CHOCOLATE:
			_base_tint = Color(0.5, 0.3, 0.15)
		OatmealFlavor.MINT:
			_base_tint = Color(0.5, 1, 0.7)
		OatmealFlavor.VANILLA:
			_base_tint = Color(1, 0.95, 0.8)
	if _sprite != null:
		_sprite.modulate = _base_tint
