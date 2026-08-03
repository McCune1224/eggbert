extends CanvasLayer

@onready var animation_player: AnimationPlayer = get_node_or_null("Control/AnimationPlayer") as AnimationPlayer
@onready var banner_label: Label = get_node_or_null("LocationBanner/Label") as Label

func _ready() -> void:
	layer = 120
	process_mode = Node.PROCESS_MODE_ALWAYS

func play_fade_out() -> void:
	await _play_animation("fade_out")

func play_fade_in() -> void:
	await _play_animation("fade_in")

func show_location(location_name: String) -> void:
	if banner_label != null:
		banner_label.text = location_name
	await _play_animation("banner_in")
	await get_tree().create_timer(1.5).timeout
	await _play_animation("banner_out")

func _play_animation(animation_name: String) -> void:
	if animation_player == null or not animation_player.has_animation(animation_name):
		return
	animation_player.play(animation_name)
	await animation_player.animation_finished
