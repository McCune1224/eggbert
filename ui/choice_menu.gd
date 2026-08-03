extends CanvasLayer

signal choice_selected(index: int)

const CURSOR_SLOT_SIZE: int = 12
const CURSOR_BOB_SPEED: float = 4.0
const CURSOR_BOB_AMPLITUDE: float = 2.0

var _buttons: Array[Button] = []
var _cursors: Array[Sprite2D] = []
var _choice_texts: Array[String] = []
var _selected_index: int = 0
var _cursor_time: float = 0.0
var _choice_container: VBoxContainer

func _ready() -> void:
	layer = 129
	_build_menu()

func _build_menu() -> void:
	var root := Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(root)
	var backdrop := ColorRect.new()
	backdrop.color = Color(0.0, 0.0, 0.0, 0.5)
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	backdrop.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(backdrop)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	center.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(center)
	var panel := PanelContainer.new()
	panel.mouse_filter = Control.MOUSE_FILTER_STOP
	panel.add_theme_stylebox_override("panel", StyleBoxFlat.new())
	center.add_child(panel)
	var margin := MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 16)
	margin.add_theme_constant_override("margin_top", 16)
	margin.add_theme_constant_override("margin_right", 16)
	margin.add_theme_constant_override("margin_bottom", 16)
	panel.add_child(margin)
	_choice_container = VBoxContainer.new()
	_choice_container.add_theme_constant_override("separation", 8)
	margin.add_child(_choice_container)

func set_choices(choices: Array[String]) -> void:
	for child in _choice_container.get_children():
		child.queue_free()
	_buttons.clear()
	_cursors.clear()
	_choice_texts = choices.duplicate()
	_selected_index = 0
	for index in choices.size():
		var row := HBoxContainer.new()
		row.add_theme_constant_override("separation", 6)
		var slot := Control.new()
		slot.custom_minimum_size = Vector2(CURSOR_SLOT_SIZE, CURSOR_SLOT_SIZE)
		slot.mouse_filter = Control.MOUSE_FILTER_IGNORE
		var cursor := Sprite2D.new()
		cursor.texture = load("res://assets/ui/cursor_arrow.png") as Texture2D
		cursor.position = Vector2(CURSOR_SLOT_SIZE / 2.0, CURSOR_SLOT_SIZE / 2.0)
		slot.add_child(cursor)
		row.add_child(slot)
		_cursors.append(cursor)
		var button := Button.new()
		button.text = choices[index]
		button.theme_type_variation = &"MenuButton"
		button.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
		button.mouse_entered.connect(_on_button_hover.bind(index))
		button.pressed.connect(_on_button_pressed.bind(index))
		row.add_child(button)
		_buttons.append(button)
		_choice_container.add_child(row)
	_update_selection()

func _process(delta: float) -> void:
	if _cursors.is_empty():
		return
	_cursor_time += delta
	var offset := sin(_cursor_time * CURSOR_BOB_SPEED) * CURSOR_BOB_AMPLITUDE
	for index in _cursors.size():
		_cursors[index].position.y = CURSOR_SLOT_SIZE / 2.0 + offset

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_up"):
		_select_index(_selected_index - 1)
		get_viewport().set_input_as_handled()
	elif event.is_action_pressed("ui_down"):
		_select_index(_selected_index + 1)
		get_viewport().set_input_as_handled()
	elif event.is_action_pressed("interact") or event.is_action_pressed("ui_accept"):
		if not _buttons.is_empty():
			_on_button_pressed(_selected_index)
		get_viewport().set_input_as_handled()

func _on_button_hover(index: int) -> void:
	_select_index(index)

func _on_button_pressed(index: int) -> void:
	if index < 0 or index >= _choice_texts.size():
		return
	choice_selected.emit(index)

func _select_index(index: int) -> void:
	if _buttons.is_empty():
		return
	_selected_index = clampi(index, 0, _buttons.size() - 1)
	_update_selection()

func _update_selection() -> void:
	for index in _cursors.size():
		_cursors[index].visible = index == _selected_index
