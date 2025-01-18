extends RichTextLabel


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	var delta_str = str(delta)
	var log_string = "DLT: " + delta_str + "\nFPS: " + str(Engine.get_frames_per_second()).pad_decimals(0) 
	set_text(log_string)
