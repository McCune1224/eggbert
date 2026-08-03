extends CanvasLayer

signal line_complete

const NORMAL_CPS: float = 40.0
const FAST_CPS: float = 80.0
const ADVANCE_COOLDOWN: float = 0.15

var _text_label: Label
var _speaker_label: Label
var _arrow: Label
var _full_text: String = ""
var _visible_characters: int = 0
var _character_accumulator: float = 0.0
var _current_cps: float = NORMAL_CPS
var _typing: bool = false
var _last_advance_time: int = 0

func _ready() -> void:
	layer = 128
	_build_dialog_bar()
	var dialog := get_tree().root.get_node_or_null("DialogManager")
	if dialog != null and dialog.has_signal("line_started"):
		dialog.line_started.connect(_on_line_started)
	visible = false

func _build_dialog_bar() -> void:
	var bar := PanelContainer.new()
	bar.position = Vector2(8.0, 245.0)
	bar.size = Vector2(624.0, 105.0)
	bar.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bar)
	var margin := MarginContainer.new()
	for property in ["margin_left", "margin_top", "margin_right", "margin_bottom"]:
		margin.add_theme_constant_override(property, 8)
	bar.add_child(margin)
	var content := VBoxContainer.new()
	margin.add_child(content)
	_speaker_label = Label.new()
	_speaker_label.theme_type_variation = &"MenuLabelSmall"
	content.add_child(_speaker_label)
	_text_label = Label.new()
	_text_label.theme_type_variation = &"MenuLabel"
	_text_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_text_label.size_flags_vertical = Control.SIZE_EXPAND_FILL
	content.add_child(_text_label)
	_arrow = Label.new()
	_arrow.text = "▼"
	_arrow.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_arrow.visible = false
	content.add_child(_arrow)

func _on_line_started(text: String, speaker: String) -> void:
	display_text(text, null, speaker)

func display_text(text: String, voice: Variant = null, speaker: String = "") -> void:
	_full_text = text
	_visible_characters = 0
	_character_accumulator = 0.0
	_typing = true
	visible = true
	if _speaker_label != null:
		_speaker_label.text = speaker if not speaker.is_empty() else _voice_speaker(voice)
	if _text_label != null:
		_text_label.text = _full_text
		_text_label.visible_characters = 0
	_arrow.visible = false
	_current_cps = _global_text_speed()
	DialogLog.append_line(_speaker_label.text, text)

func _voice_speaker(voice: Variant) -> String:
	if voice == null:
		return ""
	return str(voice.get("speaker_name"))

func _global_text_speed() -> float:
	var dialog := get_tree().root.get_node_or_null("DialogManager")
	if dialog == null:
		return NORMAL_CPS
	var speed := int(dialog.get("current_text_speed"))
	if speed == 2:
		return 1000000.0
	if speed == 1:
		return FAST_CPS
	return NORMAL_CPS

func _process(delta: float) -> void:
	if not _typing:
		return
	_character_accumulator += _current_cps * delta
	while _character_accumulator >= 1.0 and _typing:
		_character_accumulator -= 1.0
		_visible_characters += 1
		_text_label.visible_characters = _visible_characters
		if _visible_characters >= _full_text.length():
			_typing = false
			_arrow.visible = true

func _input(event: InputEvent) -> void:
	if not visible or not event.is_action_pressed("interact"):
		return
	var now := Time.get_ticks_msec()
	if now - _last_advance_time < int(ADVANCE_COOLDOWN * 1000.0):
		return
	_last_advance_time = now
	if _typing:
		_typing = false
		_visible_characters = _full_text.length()
		_text_label.visible_characters = _visible_characters
		_arrow.visible = true
	else:
		visible = false
		_arrow.visible = false
		line_complete.emit()
	get_viewport().set_input_as_handled()
