class_name TimedSpikes
extends Area2D

@export_group("Timing")
@export var damage: int = 1
@export var active_duration: float = 2.0
@export var inactive_duration: float = 2.0
@export var telegraph_duration: float = 0.5

var _collision: CollisionShape2D
var _sprite: Sprite2D
var _timer: Timer
var _is_active: bool = false

enum SpikeState {
	INACTIVE,
	TELEGRAPHING,
	ACTIVE,
}

var _state: SpikeState = SpikeState.INACTIVE

func _ready() -> void:
	collision_layer = 0
	collision_mask = CollisionConfig.PLAYER_LAYER
	_collision = get_node_or_null("CollisionShape2D") as CollisionShape2D
	_sprite = get_node_or_null("Sprite2D") as Sprite2D
	body_entered.connect(_on_body_entered)
	_timer = Timer.new()
	_timer.one_shot = true
	_timer.timeout.connect(_on_timer_timeout)
	add_child(_timer)
	_set_spike_state(false)
	_timer.start(inactive_duration)

func _on_timer_timeout() -> void:
	match _state:
		SpikeState.INACTIVE:
			_state = SpikeState.TELEGRAPHING
			if _sprite != null:
				_sprite.modulate = Color(1.0, 0.3, 0.3, 1.0)
			_timer.start(telegraph_duration)
		SpikeState.TELEGRAPHING:
			_state = SpikeState.ACTIVE
			_set_spike_state(true)
			_timer.start(active_duration)
		SpikeState.ACTIVE:
			_state = SpikeState.INACTIVE
			_set_spike_state(false)
			_timer.start(inactive_duration)

func _set_spike_state(active: bool) -> void:
	_is_active = active
	if _collision != null:
		_collision.disabled = not active
	if _sprite != null:
		_sprite.modulate = Color.WHITE if active else Color(0.5, 0.5, 0.5, 0.7)

func _on_body_entered(body: Node2D) -> void:
	if not _is_active:
		return
	if not body.is_in_group("player"):
		return
	var health := body.get("health_component") as Node
	if health != null and health.has_method("take_damage"):
		health.call("take_damage", damage, self)
