class_name DialogLog
extends CanvasLayer

const MAX_LINES: int = 50
static var _buffer: Array[String] = []
var _container: Control
var _log_label: RichTextLabel
var _visible: bool = false

func _ready() -> void:
	layer = 150
	_container = Control.new()
	_container.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_container.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_container)
	var backdrop := ColorRect.new()
	backdrop.color = Color(0.0, 0.0, 0.0, 0.8)
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	backdrop.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_container.add_child(backdrop)
	_log_label = RichTextLabel.new()
	_log_label.position = Vector2(20.0, 20.0)
	_log_label.size = Vector2(600.0, 320.0)
	_log_label.bbcode_enabled = true
	_log_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_log_label.add_theme_font_size_override("normal_font_size", 10)
	_container.add_child(_log_label)
	_container.visible = false

func _input(event: InputEvent) -> void:
	var dialog := get_tree().root.get_node_or_null("DialogManager")
	if dialog == null or not bool(dialog.get("is_dialog_active")):
		return
	if event.is_action_pressed("ui_focus_next") or event.is_action_pressed("menu_pause"):
		_visible = not _visible
		_container.visible = _visible
		if _visible:
			_log_label.text = "\n".join(_buffer)
			_log_label.scroll_to_line(maxi(0, _log_label.get_line_count() - 1))
		get_viewport().set_input_as_handled()

static func append_line(speaker: String, text: String) -> void:
	var line := text
	if not speaker.is_empty():
		line = "[b]%s:[/b] %s" % [speaker, text]
	_buffer.append(line)
	if _buffer.size() > MAX_LINES:
		_buffer.pop_front()
