extends RichTextLabel

@export var update_interval := 0.5
var _elapsed := 0.0

func _ready() -> void:
	bbcode_enabled = true
	fit_content = true

func _process(delta: float) -> void:
	_elapsed += delta
	if _elapsed < update_interval:
		return
	_elapsed = 0.0
	text = "FPS: %d" % Engine.get_frames_per_second()
