extends CanvasLayer

const SETTINGS_PATH: String = "user://settings.cfg"

@onready var animation_player: AnimationPlayer = get_node_or_null("AnimationPlayer") as AnimationPlayer
@onready var main_panel: PanelContainer = get_node_or_null("MainPanel") as PanelContainer
@onready var settings_panel: PanelContainer = get_node_or_null("SettingsPanel") as PanelContainer
@onready var map_panel: PanelContainer = get_node_or_null("MapPanel") as PanelContainer
@onready var inventory_panel: PanelContainer = get_node_or_null("InventoryPanel") as PanelContainer
@onready var help_panel: PanelContainer = get_node_or_null("HelpPanel") as PanelContainer
@onready var quest_panel: PanelContainer = get_node_or_null("QuestPanel") as PanelContainer

var _current_panel: String = "main"
var _menu_open: bool = false
var _settings_music: HSlider
var _settings_sfx: HSlider
var _fullscreen: CheckButton
var _scale_option: OptionButton
var _map_grid: GridContainer
var _item_list: ItemList
var _item_description: Label
var _help_vbox: VBoxContainer
var _quest_list: ItemList
var _quest_title: Label
var _quest_description: Label

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_settings_music = get_node_or_null("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/MusicBox/MusicSlider") as HSlider
	_settings_sfx = get_node_or_null("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/SfxBox/SfxSlider") as HSlider
	_fullscreen = get_node_or_null("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/FullscreenBox/FullscreenCheck") as CheckButton
	_scale_option = get_node_or_null("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/ScaleBox/ScaleOption") as OptionButton
	_map_grid = get_node_or_null("MapPanel/VBoxContainer/WarpGrid") as GridContainer
	_item_list = get_node_or_null("InventoryPanel/VBoxContainer/ContentRow/ItemList") as ItemList
	_item_description = get_node_or_null("InventoryPanel/VBoxContainer/ContentRow/DetailPanel/DetailVBox/DescriptionLabel") as Label
	_help_vbox = get_node_or_null("HelpPanel/VBoxContainer/ScrollContainer/HelpVBox") as VBoxContainer
	_quest_list = get_node_or_null("QuestPanel/VBoxContainer/ContentRow/QuestList") as ItemList
	_quest_title = get_node_or_null("QuestPanel/VBoxContainer/ContentRow/DetailPanel/DetailVBox/QuestTitleLabel") as Label
	_quest_description = get_node_or_null("QuestPanel/VBoxContainer/ContentRow/DetailPanel/DetailVBox/QuestDescriptionLabel") as Label
	_connect("MainPanel/VBoxContainer/ResumeButton", _on_resume_pressed)
	_connect("MainPanel/VBoxContainer/GridRow1/InventoryButton", _on_inventory_pressed)
	_connect("MainPanel/VBoxContainer/GridRow1/MapButton", _on_map_pressed)
	_connect("MainPanel/VBoxContainer/GridRow2/HelpButton", _on_help_pressed)
	_connect("MainPanel/VBoxContainer/GridRow2/QuestsButton", _on_quests_pressed)
	_connect("MainPanel/VBoxContainer/GridRow3/SettingsButton", _on_settings_pressed)
	_connect("MainPanel/VBoxContainer/GridRow3/QuitButton", _on_quit_pressed)
	_connect("SettingsPanel/VBoxContainer/BackButton", _on_settings_back_pressed)
	_connect("MapPanel/VBoxContainer/MapBackButton", _on_map_back_pressed)
	_connect("InventoryPanel/VBoxContainer/ButtonRow/InventoryBackButton", _on_inventory_back_pressed)
	_connect("HelpPanel/VBoxContainer/HelpBackButton", _on_help_back_pressed)
	_connect("QuestPanel/VBoxContainer/ButtonRow/QuestBackButton", _on_quest_back_pressed)
	if _settings_music != null:
		_settings_music.value_changed.connect(_on_music_volume_changed)
	if _settings_sfx != null:
		_settings_sfx.value_changed.connect(_on_sfx_volume_changed)
	if _fullscreen != null:
		_fullscreen.toggled.connect(_on_fullscreen_toggled)
	if _scale_option != null:
		_scale_option.item_selected.connect(_on_scale_selected)
		for scale in range(1, 5):
			_scale_option.add_item("%dx" % scale, scale)
	_load_settings()
	_hide_menu()

func _input(event: InputEvent) -> void:
	if not event.is_action_pressed("menu_pause"):
		return
	if _menu_open:
		if _current_panel != "main":
			_show_panel("main")
		else:
			_resume()
	else:
		_pause()
	get_viewport().set_input_as_handled()

func _connect(path: NodePath, callback: Callable) -> void:
	var button := get_node_or_null(path) as BaseButton
	if button != null and not button.pressed.is_connected(callback):
		button.pressed.connect(callback)

func _show_panel(panel_name: String) -> void:
	_current_panel = panel_name
	if main_panel != null:
		main_panel.visible = panel_name == "main"
	if settings_panel != null:
		settings_panel.visible = panel_name == "settings"
	if map_panel != null:
		map_panel.visible = panel_name == "map"
	if inventory_panel != null:
		inventory_panel.visible = panel_name == "inventory"
	if help_panel != null:
		help_panel.visible = panel_name == "help"
	if quest_panel != null:
		quest_panel.visible = panel_name == "quests"

func _pause() -> void:
	var cutscene := _autoload("CutsceneController")
	if cutscene != null and cutscene.has_method("stop"):
		cutscene.call("stop")
	var dialog := _autoload("DialogManager")
	if dialog != null and dialog.has_method("stop_dialog"):
		dialog.call("stop_dialog")
	get_tree().paused = true
	_menu_open = true
	visible = true
	_show_panel("main")
	var resume := get_node_or_null("MainPanel/VBoxContainer/ResumeButton") as Button
	if resume != null:
		resume.grab_focus()

func _resume() -> void:
	get_tree().paused = false
	_menu_open = false
	_hide_menu()

func _hide_menu() -> void:
	_menu_open = false
	visible = false
	_show_panel("main")

func _on_resume_pressed() -> void:
	_resume()

func _on_inventory_pressed() -> void:
	_refresh_inventory()
	_show_panel("inventory")

func _on_map_pressed() -> void:
	_refresh_warps()
	_show_panel("map")

func _on_help_pressed() -> void:
	_refresh_help()
	_show_panel("help")

func _on_quests_pressed() -> void:
	_refresh_quests()
	_show_panel("quests")

func _on_settings_pressed() -> void:
	_show_panel("settings")
	if _settings_music != null:
		_settings_music.grab_focus()

func _on_settings_back_pressed() -> void:
	_save_settings()
	_show_panel("main")

func _on_map_back_pressed() -> void:
	_show_panel("main")

func _on_inventory_back_pressed() -> void:
	_show_panel("main")

func _on_help_back_pressed() -> void:
	_show_panel("main")

func _on_quest_back_pressed() -> void:
	_show_panel("main")

func _on_quit_pressed() -> void:
	get_tree().quit()

func _refresh_warps() -> void:
	if _map_grid == null:
		return
	for child in _map_grid.get_children():
		child.queue_free()
	var database := _autoload("WarpDatabase")
	if database == null or not database.has_method("get_unlocked"):
		var empty := Label.new()
		empty.text = "No warps discovered"
		_map_grid.add_child(empty)
		return
	var unlocked: Array = database.call("get_unlocked")
	if unlocked.is_empty():
		var empty_label := Label.new()
		empty_label.text = "No warps discovered"
		_map_grid.add_child(empty_label)
		return
	for warp in unlocked:
		var button := Button.new()
		button.text = str(warp.get("name"))
		button.pressed.connect(_warp_to.bind(str(warp.get("level_path")), str(warp.get("target_transition_name"))))
		_map_grid.add_child(button)

func _warp_to(level_path: String, target_transition_name: String) -> void:
	_resume()
	var controller := _autoload("GameController")
	if controller == null:
		return
	if controller.has_method("load_level_at_transition"):
		controller.call("load_level_at_transition", level_path, target_transition_name)

func _refresh_inventory() -> void:
	if _item_list == null:
		return
	_item_list.clear()
	var inventory := _autoload("Inventory")
	if inventory == null:
		return
	var items: Dictionary = inventory.get("items")
	for item_id in items:
		_item_list.add_item("%s x%d" % [str(item_id), int(items[item_id])])
	if _item_description != null:
		_item_description.text = "Select an item to inspect it."

func _refresh_help() -> void:
	if _help_vbox == null:
		return
	for child in _help_vbox.get_children():
		child.queue_free()
	var keybinds := _autoload("KeybindManager")
	var actions: Array = keybinds.get("rebindable_actions") if keybinds != null else ["player_up", "player_down", "player_left", "player_right", "interact", "menu_pause"]
	for action in actions:
		var label := Label.new()
		label.text = "%s" % action
		_help_vbox.add_child(label)

func _refresh_quests() -> void:
	if _quest_list == null:
		return
	_quest_list.clear()
	var quests := _autoload("QuestManager")
	if quests == null:
		return
	var active: Dictionary = quests.get("active_quests")
	for quest_id in active:
		_quest_list.add_item(str(quest_id))
	if active.is_empty():
		_quest_list.add_item("No quests yet.")
	if _quest_title != null:
		_quest_title.text = "Quests"
	if _quest_description != null:
		_quest_description.text = "Track your current objectives here."

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
	if _scale_option == null:
		return
	var scale := int(_scale_option.get_item_id(index))
	get_window().size = Vector2i(640 * scale, 360 * scale)

func _load_settings() -> void:
	var config := ConfigFile.new()
	if config.load(SETTINGS_PATH) != OK:
		return
	if _settings_music != null:
		_settings_music.value = float(config.get_value("audio", "music_volume", 100.0))
	if _settings_sfx != null:
		_settings_sfx.value = float(config.get_value("audio", "sfx_volume", 100.0))
	if _fullscreen != null:
		_fullscreen.button_pressed = bool(config.get_value("display", "fullscreen", false))

func _save_settings() -> void:
	var config := ConfigFile.new()
	if _settings_music != null:
		config.set_value("audio", "music_volume", _settings_music.value)
	if _settings_sfx != null:
		config.set_value("audio", "sfx_volume", _settings_sfx.value)
	if _fullscreen != null:
		config.set_value("display", "fullscreen", _fullscreen.button_pressed)
	if _scale_option != null:
		config.set_value("display", "window_scale", _scale_option.get_item_id(_scale_option.selected))
	config.save(SETTINGS_PATH)

func _autoload(node_name: String) -> Node:
	return get_tree().root.get_node_or_null(node_name)
