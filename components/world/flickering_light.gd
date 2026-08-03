extends PointLight2D

@export var min_energy: float = 0.2
@export var max_energy: float = 1.0
@export var flicker_speed: float = 5.0
@export var buzz_sfx: AudioStream

var _buzz_player: AudioStreamPlayer2D
var _time: float = 0.0

func _ready() -> void:
	if buzz_sfx != null:
		_buzz_player = AudioStreamPlayer2D.new()
		_buzz_player.stream = buzz_sfx
		_buzz_player.bus = "SFX"
		add_child(_buzz_player)
		_buzz_player.play()

func _process(delta: float) -> void:
	_time += delta * flicker_speed
	energy = min_energy + (sin(_time) * 0.5 + 0.5) * (max_energy - min_energy)
