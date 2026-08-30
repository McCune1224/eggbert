@tool
extends EditorPlugin

# importers
const NoopImportPlugin = preload("importers/noop_import_plugin.gd")
const SpriteFramesImportPlugin = preload("importers/sprite_frames_import_plugin.gd")
const TilesetTextureImportPlugin = preload("importers/tileset_texture_import_plugin.gd")
const TextureImportPlugin = preload("importers/static_texture_import_plugin.gd")
const FileSystemHelper = preload("importers/file_system_helper.gd")

# export
const ExportPlugin = preload("export/metadata_export_plugin.gd")
# interface
const ConfigDialog = preload('config/config_dialog.tscn')
const WizardWindow = preload("interface/docks/wizard/as_wizard_dock_container.tscn")
const AsepriteDockImportsWindow = preload('interface/imports_manager/aseprite_imports_manager.tscn')
const ImportsManagerPanels = preload('interface/imports_manager/import_panels.tscn')

const AnimatedSpriteInspectorPlugin = preload("interface/docks/animated_sprite/inspector_plugin.gd")
const SpriteInspectorPlugin = preload("interface/docks/sprite/inspector_plugin.gd")

const tool_menu_name = "Aseprite Wizard"
const menu_item_name = "Spritesheet Wizard Dock..."
const config_menu_item_name = "Config..."
const import_menu_item_name = "Imports Manager..."

var config = preload("config/config.gd").new()
var window: TabContainer
var wizard_dock: EditorDock
var imports_list_dock: EditorDock
var config_window: PopupPanel
var imports_list_window: Window
var imports_list_panel: MarginContainer
var export_plugin : EditorExportPlugin
var sprite_inspector_plugin: EditorInspectorPlugin
var animated_sprite_inspector_plugin: EditorInspectorPlugin

var file_system_helper

var _exporter_enabled = false

var _importers = []

var _is_import_list_docked = false

func _enter_tree():
	_load_config()
	_setup_menu_entries()
	_setup_importer()
	_setup_exporter()
	_setup_animated_sprite_inspector_plugin()
	_setup_sprite_inspector_plugin()


func _exit_tree():
	_disable_plugin()


func _disable_plugin():
	_remove_menu_entries()
	_remove_importer()
	_remove_exporter()
	_remove_wizard_dock()
	_remove_inspector_plugins()


func _load_config():
	config.initialize_project_settings()


func _setup_menu_entries():
	var submenu = PopupMenu.new()
	add_tool_submenu_item(tool_menu_name, submenu)
	submenu.add_item(menu_item_name)
	submenu.add_item(import_menu_item_name)
	submenu.add_item(config_menu_item_name)
	submenu.index_pressed.connect(_on_tool_menu_pressed)


func _remove_menu_entries():
	remove_tool_menu_item(tool_menu_name)


func _setup_importer():
	file_system_helper = FileSystemHelper.new()
	add_child(file_system_helper)
	_importers = [
		NoopImportPlugin.new(),
		SpriteFramesImportPlugin.new(file_system_helper),
		TilesetTextureImportPlugin.new(file_system_helper),
		TextureImportPlugin.new(file_system_helper),
	]

	for i in _importers:
		add_import_plugin(i)


func _remove_importer():
	for i in _importers:
		remove_import_plugin(i)

	if file_system_helper != null:
		file_system_helper.queue_free()
		file_system_helper = null


func _setup_exporter():
	if config.is_exporter_enabled():
		export_plugin = ExportPlugin.new()
		add_export_plugin(export_plugin)
		_exporter_enabled = true


func _remove_exporter():
	if _exporter_enabled:
		remove_export_plugin(export_plugin)
		_exporter_enabled = false


func _setup_sprite_inspector_plugin():
	if sprite_inspector_plugin == null:
		sprite_inspector_plugin = SpriteInspectorPlugin.new()
		add_inspector_plugin(sprite_inspector_plugin)


func _setup_animated_sprite_inspector_plugin():
	if animated_sprite_inspector_plugin == null:
		animated_sprite_inspector_plugin = AnimatedSpriteInspectorPlugin.new()
		add_inspector_plugin(animated_sprite_inspector_plugin)


func _remove_inspector_plugins():
	# Guarded: Godot errors with "Trying to remove nonexistent inspector
	# plugin" when teardown runs for plugins that were never registered
	# (e.g. plugin toggled before the C# assembly was built).
	if sprite_inspector_plugin != null:
		remove_inspector_plugin(sprite_inspector_plugin)
		sprite_inspector_plugin = null
	if animated_sprite_inspector_plugin != null:
		remove_inspector_plugin(animated_sprite_inspector_plugin)
		animated_sprite_inspector_plugin = null


func _remove_wizard_dock():
	if wizard_dock:
		if is_instance_valid(window):
			wizard_dock.remove_child(window)
			window.queue_free()
		if wizard_dock.is_inside_tree():
			remove_dock(wizard_dock)
		wizard_dock.queue_free()
		wizard_dock = null
		window = null


func _open_window():
	if window:
		wizard_dock.make_visible()
		return

	window = WizardWindow.instantiate()
	window.connect("close_requested",Callable(self,"_on_window_closed"))
	# Register as a proper EditorDock instead of the deprecated
	# add_control_to_bottom_panel() shim, which jams the bottom dock slot
	# shut in Godot 4.7 (panel renders with zero height, Alt+O dead).
	wizard_dock = EditorDock.new()
	wizard_dock.title = "Aseprite Wizard"
	wizard_dock.layout_key = "as_wizard_dock"
	wizard_dock.default_slot = EditorDock.DOCK_SLOT_BOTTOM
	wizard_dock.add_child(window)
	add_dock(wizard_dock)
	wizard_dock.make_visible()


func _open_config_dialog():
	if is_instance_valid(config_window):
		config_window.queue_free()

	config_window = ConfigDialog.instantiate()
	get_editor_interface().get_base_control().add_child(config_window)
	config_window.popup_centered()


func _open_import_list_dialog():
	if is_instance_valid(imports_list_window):
		imports_list_window.queue_free()

	if is_instance_valid(imports_list_panel):
		if _is_import_list_docked:
			_remove_imports_list_dock()
			_is_import_list_docked = false
		imports_list_panel.queue_free()
		imports_list_panel = null

	imports_list_panel = ImportsManagerPanels.instantiate()
	imports_list_panel.dock_requested.connect(_on_import_list_dock_requested)
	_create_imports_manager_window(imports_list_panel)


func _on_window_closed():
	if window:
		if is_instance_valid(wizard_dock):
			wizard_dock.remove_child(window)
			if wizard_dock.is_inside_tree():
				remove_dock(wizard_dock)
			wizard_dock.queue_free()
			wizard_dock = null
		window.queue_free()
		window = null


func _on_tool_menu_pressed(index):
	match index:
		0: # wizard dock
			_open_window()
		1: # imports
			_open_import_list_dialog()
		2: # config
			_open_config_dialog()


func _remove_imports_list_dock():
	if imports_list_dock == null:
		return
	if is_instance_valid(imports_list_panel):
		imports_list_dock.remove_child(imports_list_panel)
	if imports_list_dock.is_inside_tree():
		remove_dock(imports_list_dock)
	imports_list_dock.queue_free()
	imports_list_dock = null


func _on_import_list_dock_requested():
	if _is_import_list_docked:
		_remove_imports_list_dock()
		_is_import_list_docked = false
		_create_imports_manager_window(imports_list_panel)
		imports_list_panel.show()
		imports_list_panel.anchors_preset = Control.PRESET_FULL_RECT
		imports_list_panel.size_flags_vertical = Control.SIZE_EXPAND_FILL
		imports_list_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		imports_list_panel.set_as_floating()
		return

	_is_import_list_docked = true
	imports_list_panel.set_as_docked()
	imports_list_window.remove_child(imports_list_panel)
	imports_list_window.queue_free()
	# Proper EditorDock instead of the deprecated bottom-panel shim (see
	# _open_window) — the shim breaks the bottom dock slot on Godot 4.7.
	imports_list_dock = EditorDock.new()
	imports_list_dock.title = "Aseprite Imports Manager"
	imports_list_dock.layout_key = "as_imports_manager_dock"
	imports_list_dock.default_slot = EditorDock.DOCK_SLOT_BOTTOM
	imports_list_dock.add_child(imports_list_panel)
	add_dock(imports_list_dock)
	imports_list_dock.make_visible()


func _create_imports_manager_window(panel: MarginContainer):
	imports_list_window = AsepriteDockImportsWindow.instantiate()
	imports_list_window.add_child(panel)
	get_editor_interface().get_base_control().add_child(imports_list_window)
	imports_list_window.popup_centered_ratio(0.5)
