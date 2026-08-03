class_name MainMenu
extends CanvasLayer

const SETTINGS_PATH: String = "user://settings.cfg"
const FACTORY_OPENING_PATH: String = "res://levels/factory/maps/OpeningZone.tscn"

@onready var menu_panel: PanelContainer = get_node_or_null("MenuPanel") as PanelContainer
@onready var settings_panel: PanelContainer = get_node_or_null("SettingsPanel") as PanelContainer
@onready var new_game_button: Button = get_node_or_null("MenuPanel/VBoxContainer/NewGameButton") as Button
@onready var continue_button: Button = get_node_or_null("MenuPanel/VBoxContainer/ContinueButton") as Button
@onready var settings_button: Button = get_node_or_null("MenuPanel/VBoxContainer/SettingsButton") as Button
@onready var quit_button: Button = get_node_or_null("MenuPanel/VBoxContainer/QuitButton") as Button
@onready var music_slider: HSlider = get_node_or_null("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/MusicSlider") as HSlider
@onready var sfx_slider: HSlider = get_node_or_null("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/SfxSlider") as HSlider
@onready var fullscreen_check: CheckButton = get_node_or_null("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/FullscreenBox/FullscreenCheck") as CheckButton
@onready var scale_option: OptionButton = get_node_or_null("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/ScaleBox/ScaleOption") as OptionButton
@onready var settings_back_button: Button = get_node_or_null("SettingsPanel/VBoxContainer/BackButton") as Button

var _settings_visible: bool = false

func _ready() -> void:
	layer = 100
	process_mode = Node.PROCESS_MODE_ALWAYS
	_connect_button(new_game_button, _on_new_game_pressed)
	_connect_button(continue_button, _on_continue_pressed)
	_connect_button(settings_button, _on_settings_pressed)
	_connect_button(quit_button, _on_quit_pressed)
	_connect_button(settings_back_button, _on_settings_back_pressed)
	if music_slider != null:
		music_slider.value_changed.connect(_on_music_volume_changed)
	if sfx_slider != null:
		sfx_slider.value_changed.connect(_on_sfx_volume_changed)
	if fullscreen_check != null:
		fullscreen_check.toggled.connect(_on_fullscreen_toggled)
	if scale_option != null:
		scale_option.item_selected.connect(_on_scale_selected)
		for scale in range(1, 5):
			scale_option.add_item("%dx" % scale, scale)
		scale_option.select(0)
	_load_settings()
	_update_continue_button()
	_show_menu()
	if new_game_button != null:
		new_game_button.grab_focus()

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("menu_pause") and _settings_visible:
		_on_settings_back_pressed()
		get_viewport().set_input_as_handled()

func _connect_button(button: Button, callback: Callable) -> void:
	if button != null and not button.pressed.is_connected(callback):
		button.pressed.connect(callback)

func _show_menu() -> void:
	_settings_visible = false
	if menu_panel != null:
		menu_panel.visible = true
	if settings_panel != null:
		settings_panel.visible = false

func _show_settings() -> void:
	_settings_visible = true
	if menu_panel != null:
		menu_panel.visible = false
	if settings_panel != null:
		settings_panel.visible = true
	if music_slider != null:
		music_slider.grab_focus()

func _update_continue_button() -> void:
	if continue_button == null:
		return
	var save_manager := _autoload("SaveManager")
	continue_button.disabled = save_manager == null or not bool(save_manager.call("has_save"))

func _on_new_game_pressed() -> void:
	var save_manager := _autoload("SaveManager")
	if save_manager != null and save_manager.has_method("delete_save"):
		save_manager.call("delete_save")
	var flags := _autoload("WorldFlags")
	if flags != null and flags.has_method("clear_all"):
		flags.call("clear_all")
	_set_buttons_disabled(true)
	var controller := _autoload("GameController")
	if controller == null or not controller.has_method("load_level_at_position"):
		return
	controller.call("load_level_at_position", FACTORY_OPENING_PATH, Vector2.ZERO)
	if controller.has_signal("level_loaded"):
		await controller.level_loaded
	queue_free()

func _on_continue_pressed() -> void:
	var save_manager := _autoload("SaveManager")
	if save_manager == null or not bool(save_manager.call("has_save")):
		return
	_set_buttons_disabled(true)
	if not bool(save_manager.call("load_game")):
		_set_buttons_disabled(false)
		_update_continue_button()
		return
	var controller := _autoload("GameController")
	if controller != null and controller.has_signal("level_loaded"):
		await controller.level_loaded
	queue_free()

func _set_buttons_disabled(disabled: bool) -> void:
	for button in [new_game_button, continue_button, settings_button, quit_button]:
		if button != null:
			button.disabled = disabled

func _on_settings_pressed() -> void:
	_show_settings()

func _on_settings_back_pressed() -> void:
	_save_settings()
	_show_menu()
	if settings_button != null:
		settings_button.grab_focus()

func _on_quit_pressed() -> void:
	get_tree().quit()

func _on_music_volume_changed(value: float) -> void:
	_set_bus_volume("MUSIC", value)

func _on_sfx_volume_changed(value: float) -> void:
	_set_bus_volume("SFX", value)

func _set_bus_volume(bus_name: String, value: float) -> void:
	var bus_index := AudioServer.get_bus_index(bus_name)
	if bus_index >= 0:
		AudioServer.set_bus_volume_db(bus_index, linear_to_db(maxf(value / 100.0, 0.001)))

func _on_fullscreen_toggled(enabled: bool) -> void:
	get_window().mode = Window.MODE_FULLSCREEN if enabled else Window.MODE_WINDOWED

func _on_scale_selected(index: int) -> void:
	if scale_option == null:
		return
	var scale := int(scale_option.get_item_id(index))
	var window_size := Vector2i(640 * scale, 360 * scale)
	get_window().size = window_size

func _load_settings() -> void:
	var config := ConfigFile.new()
	if config.load(SETTINGS_PATH) != OK:
		return
	if music_slider != null:
		music_slider.value = float(config.get_value("audio", "music_volume", 100.0))
	if sfx_slider != null:
		sfx_slider.value = float(config.get_value("audio", "sfx_volume", 100.0))
	if fullscreen_check != null:
		fullscreen_check.button_pressed = bool(config.get_value("display", "fullscreen", false))
	if scale_option != null:
		var saved_scale := int(config.get_value("display", "window_scale", 1))
		for index in scale_option.item_count:
			if int(scale_option.get_item_id(index)) == saved_scale:
				scale_option.select(index)
				break
	if music_slider != null:
		_on_music_volume_changed(music_slider.value)
	if sfx_slider != null:
		_on_sfx_volume_changed(sfx_slider.value)

func _save_settings() -> void:
	var config := ConfigFile.new()
	if music_slider != null:
		config.set_value("audio", "music_volume", music_slider.value)
	if sfx_slider != null:
		config.set_value("audio", "sfx_volume", sfx_slider.value)
	if fullscreen_check != null:
		config.set_value("display", "fullscreen", fullscreen_check.button_pressed)
	if scale_option != null:
		config.set_value("display", "window_scale", scale_option.get_item_id(scale_option.selected))
	config.save(SETTINGS_PATH)

func _autoload(node_name: String) -> Node:
	return get_tree().root.get_node_or_null(node_name)
