extends Area2D


@onready var rich_text_scene = $RichTextLabel
var displayed_text = "Item collected!"
func _ready():
    # Connect the body entered signal
	body_entered.connect(_on_body_entered)
	pass

func _on_body_entered(body):
	if body.is_in_group("player"): # Optional: check if it's the player
		var rich_text = RichTextLabel.new()
		rich_text.text = displayed_text
		rich_text.position = Vector2(100, 100) # Adjust position as needed
		rich_text.size = Vector2(200, 50) # Adjust size as needed
		# Add the RichTextLabel to the scene
		get_tree().root.add_child(rich_text)
		# Optional: Remove the text after a delay
		var timer = get_tree().create_timer(2.0)
		timer.timeout.connect(func(): rich_text.queue_free())
		pass
