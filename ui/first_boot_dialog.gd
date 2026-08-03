extends CanvasLayer

signal completed

static var _shown_this_session: bool = false

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	layer = 200
	var flags := get_tree().root.get_node_or_null("WorldFlags")
	if _shown_this_session or (flags != null and bool(flags.call("has_flag", "first_boot_speed_chosen"))):
		completed.emit()
		queue_free()
		return
	_shown_this_session = true
	_build_ui()

func _build_ui() -> void:
	var backdrop := ColorRect.new()
	backdrop.color = Color(0.0, 0.0, 0.0, 0.7)
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	backdrop.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(backdrop)
	var panel := PanelContainer.new()
	panel.set_anchors_preset(Control.PRESET_CENTER)
	panel.position = Vector2(192.0, 108.0)
	panel.size = Vector2(256.0, 144.0)
	add_child(panel)
	var content := VBoxContainer.new()
	panel.add_child(content)
	var title := Label.new()
	title.text = "Text Speed"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.theme_type_variation = &"MenuLabelTitle"
	content.add_child(title)
	var description := Label.new()
	description.text = "How fast should dialog text appear?"
	description.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	description.theme_type_variation = &"MenuLabel"
	content.add_child(description)
	for option in [["Fast", 1, "Smooth and snappy"], ["Medium", 0, "A relaxed pace"], ["Instant", 2, "Text appears all at once"]]:
		var button := Button.new()
		button.text = "%s\n%s" % [option[0], option[2]]
		button.theme_type_variation = &"MenuButton"
		button.pressed.connect(_on_speed_chosen.bind(int(option[1])))
		content.add_child(button)

func _on_speed_chosen(speed: int) -> void:
	var dialog := get_tree().root.get_node_or_null("DialogManager")
	if dialog != null:
		dialog.set("current_text_speed", speed)
	var flags := get_tree().root.get_node_or_null("WorldFlags")
	if flags != null and flags.has_method("set_flag"):
		flags.call("set_flag", "first_boot_speed_chosen", true)
	completed.emit()
	queue_free()
