@tool
extends EditorPlugin

## Eggbert Toolkit — single-dock, single-plugin replacement for the five
## separate editor plugins (level_assembly, level_wizard, content_editors,
## transition_audit, cutscene_inspector).
##
## Why one dock: Godot 4.7's dock system misbehaves when several plugins each
## register their own docks (slot contention + the deprecated bottom-panel
## shim). One plugin owning ONE EditorDock with tabs inside it removes that
## entire failure class.

const LevelAssemblyTab := preload("res://addons/eggbert_toolkit/tabs/level_assembly.gd")
const ItemEditorTab := preload("res://addons/eggbert_toolkit/tabs/item_editor.gd")
const QuestEditorTab := preload("res://addons/eggbert_toolkit/tabs/quest_editor.gd")
const TransitionAuditTab := preload("res://addons/eggbert_toolkit/tabs/transition_audit.gd")
const InspectorPlugins := preload("res://addons/eggbert_toolkit/inspector_plugins.gd")

var _dock: EditorDock
var _audit_tab: TransitionAuditTab
var _inspector_plugins: Array = []


func _enter_tree() -> void:
	_dock = EditorDock.new()
	_dock.title = "Eggbert Toolkit"
	_dock.layout_key = "eggbert_toolkit"
	_dock.default_slot = EditorDock.DOCK_SLOT_RIGHT_UL

	var scroll := ScrollContainer.new()
	scroll.name = "ToolkitScroll"
	# CRITICAL: bound the dock's minimum height. A dock whose min-height exceeds
	# the window pushes the bottom panel + FileSystem dock off-screen
	# (godot#121574). Autowrapped Labels derive min-height from width, which is
	# what made this dock blow up the layout. Everything scrolls instead.
	scroll.custom_minimum_size = Vector2(260, 200)
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED

	var tabs := TabContainer.new()
	tabs.name = "ToolkitTabs"
	scroll.add_child(tabs)
	_dock.add_child(scroll)

	var level_tab: LevelAssemblyTab = LevelAssemblyTab.new(self)
	tabs.add_child(level_tab.build())
	tabs.set_tab_title(0, "Level Assembly")

	var item_tab: ItemEditorTab = ItemEditorTab.new(self)
	tabs.add_child(item_tab.build())
	tabs.set_tab_title(1, "Items")

	var quest_tab: QuestEditorTab = QuestEditorTab.new(self)
	tabs.add_child(quest_tab.build())
	tabs.set_tab_title(2, "Quests")

	_audit_tab = TransitionAuditTab.new(self)
	tabs.add_child(_audit_tab.build())
	tabs.set_tab_title(3, "Transition Audit")

	add_dock(_dock)

	_inspector_plugins = InspectorPlugins.create_all()
	for p in _inspector_plugins:
		add_inspector_plugin(p)

	# Re-scan the audit tab whenever the edited scene changes.
	var ei := get_editor_interface()
	if ei != null and ei.has_signal("scene_changed"):
		ei.scene_changed.connect(func(_root): _audit_tab.rescan())


func _exit_tree() -> void:
	for p in _inspector_plugins:
		remove_inspector_plugin(p)
		# EditorInspectorPlugin is RefCounted — release the ref, never free().
	_inspector_plugins.clear()

	if _dock != null:
		if _dock.is_inside_tree():
			remove_dock(_dock)
		_dock.free()
		_dock = null
