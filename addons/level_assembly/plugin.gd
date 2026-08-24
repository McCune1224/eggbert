@tool
extends EditorPlugin

const LevelFactory = preload("res://addons/level_wizard/level_factory.gd")

const COMPONENTS := [
	["Transitions", "Level Transition", "res://levels/LevelTransition.tscn", "Configure Level and TargetTransitionName in the Inspector."],
	["Progress", "Save Point", "res://saves/SavePoint.tscn", "Set LocationName, then position it beside an arrival route."],
	["Puzzles", "Door", "res://components/puzzles/Door.tscn", "Pair with a Floor Switch or a timed controller."],
	["Puzzles", "Key Door", "res://components/puzzles/KeyDoor.tscn", "Set RequiredFlag and its locked message."],
	["Puzzles", "Timed Door", "res://components/puzzles/TimedDoor.tscn", "Use with a nearby timed switch route."],
	["Puzzles", "Floor Switch", "res://components/puzzles/FloorSwitch.tscn", "Set TargetDoorPath after placing its door."],
	["Puzzles", "Push Block", "res://components/puzzles/PushBlock.tscn", "Keep a full tile of clearance around its route."],
	["Traversal", "Teleport Pad", "res://components/puzzles/TeleportPad.tscn", "Place a pair and set each TargetPadPath."],
	["Traversal", "Conveyor", "res://components/puzzles/ConveyorTile.tscn", "Set direction and speed; reserve an escape route."],
	["Traversal", "Moving Platform", "res://components/puzzles/MovingPlatform.tscn", "Set endpoints before testing movement."],
	["Hazards", "Timed Spikes", "res://components/puzzles/TimedSpikes.tscn", "Place on an optional or clearly telegraphed route."],
	["Hazards", "Spike Tile", "res://components/puzzles/SpikeTile.tscn", "Do not place on an arrival point or required interaction."],
	["Hazards", "Weighted Plate", "res://components/puzzles/WeightedPressurePlate.tscn", "Use with a block or movable object."],
	["Items", "Pickup Item", "res://components/items/PickupItem.tscn", "Set ItemId from the item list. Player walks over to collect."],
	["Items", "Conditional Item", "res://components/items/ConditionalItem.tscn", "Set ItemId and RequiredFlag. Appears only when the flag condition is met."],
	["Story", "Readable Object", "res://components/npcs/ReadableObject.tscn", "Place a readable sign/poster. Set DialogLines in the Inspector."],
	["Story", "Cutscene Trigger", "res://components/npcs/CutsceneTrigger.tscn", "Set DialogLines or a Cutscene resource; flavor choices are optional."],
	["Story", "Dialog Branch Trigger", "res://components/npcs/DialogBranchTrigger.tscn", "Set a DialogBranch resource for multi-choice NPC conversations."],
	["Story", "NPC (Dialog)", "res://components/npcs/DialogBranchTrigger.tscn", "Place an NPC dialog trigger. Assign a DialogBranch resource in the Inspector."],
]

var _dock: VBoxContainer
var _status: Label
var _search: LineEdit
var _category_container: VBoxContainer
var _category_rows: Dictionary = {}

# New Level popup state
var _nl_popup: PopupPanel = null
var _nl_name: LineEdit = null
var _nl_tileset: OptionButton = null
var _nl_music: OptionButton = null
var _nl_ambience: OptionButton = null


func _enter_tree() -> void:
	_dock = VBoxContainer.new()
	_dock.name = "Level Assembly"
	_dock.size_flags_vertical = Control.SIZE_EXPAND_FILL

	var title := Label.new()
	title.text = "Level Assembly"
	title.add_theme_font_size_override("font_size", 18)
	_dock.add_child(title)

	var nl_button := Button.new()
	nl_button.text = "New Level…"
	nl_button.pressed.connect(_open_new_level_popup)
	_dock.add_child(nl_button)

	var hint := Label.new()
	hint.text = "Adds a configured scene instance at (0, 0). Move it, configure exports, then save the level."
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_dock.add_child(hint)

	_search = LineEdit.new()
	_search.placeholder_text = "Search components..."
	_search.text_changed.connect(_on_search_changed)
	_dock.add_child(_search)

	_category_container = VBoxContainer.new()
	_category_container.add_theme_constant_override("separation", 6)
	_dock.add_child(_category_container)

	_build_categories()

	var new_quest := Button.new()
	new_quest.text = "New Quest…"
	new_quest.pressed.connect(_on_new_quest_pressed)
	_dock.add_child(new_quest)

	_status = Label.new()
	_status.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_dock.add_child(_status)

	add_control_to_dock(DOCK_SLOT_RIGHT_UL, _dock)


func _exit_tree() -> void:
	if _nl_popup != null and is_instance_valid(_nl_popup):
		_nl_popup.queue_free()
	if _dock != null:
		remove_control_from_docks(_dock)
		_dock.queue_free()


func _build_categories() -> void:
	for component in COMPONENTS:
		var category: String = component[0]
		if not _category_rows.has(category):
			var section := Label.new()
			section.text = category
			section.add_theme_font_size_override("font_size", 14)
			_category_container.add_child(section)
			_category_rows[category] = section
		var button := Button.new()
		button.text = component[1]
		button.tooltip_text = component[3]
		button.pressed.connect(_add_component.bind(component[1], component[2]))
		button.set_meta("name", component[1].to_lower())
		button.set_meta("category", category)
		_category_container.add_child(button)


func _on_search_changed(query: String) -> void:
	var needle := query.strip_edges().to_lower()
	for child in _category_container.get_children():
		if child is Label:
			# Category header — toggled by filter result loop below.
			continue
		if child is Button:
			var name_match := str(child.get_meta("name", ""))
			var category: String = child.get_meta("category", "")
			var visible := needle == "" or name_match.contains(needle)
			child.visible = visible

	# Hide category headers whose buttons are all hidden.
	for category in _category_rows.keys():
		var section: Label = _category_rows[category]
		var has_visible := false
		for child in _category_container.get_children():
			if child is Button and child.get_meta("category", "") == category and child.visible:
				has_visible = true
				break
		section.visible = has_visible


func _add_component(display_name: String, scene_path: String) -> void:
	var root := get_editor_interface().get_edited_scene_root()
	if root == null:
		_set_status("Open a level scene before adding %s." % display_name)
		return
	if not root is Node2D:
		_set_status("%s must be added to a Node2D level scene." % display_name)
		return

	var packed: PackedScene = load(scene_path)
	if packed == null:
		_set_status("Could not load %s." % scene_path)
		return
	var instance := packed.instantiate()
	instance.name = display_name.replace(" ", "")

	var undo_redo := get_undo_redo()
	undo_redo.create_action("Add %s" % display_name)
	undo_redo.add_do_method(root, "add_child", instance)
	undo_redo.add_do_method(instance, "set_owner", root)
	undo_redo.add_undo_method(root, "remove_child", instance)
	undo_redo.add_undo_method(instance, "queue_free")
	undo_redo.commit_action()

	get_editor_interface().get_selection().clear()
	get_editor_interface().get_selection().add_node(instance)
	_set_status("Added %s. Configure its exported fields in the Inspector." % display_name)


func _on_new_quest_pressed() -> void:
	_set_status("Open the Content Editors dock → Quest Editor to create a quest; it registers itself in QuestManager automatically.")


# ---------------------------------------------------------------------------
# New Level popup
# ---------------------------------------------------------------------------

func _open_new_level_popup() -> void:
	if _nl_popup == null:
		_build_new_level_popup()
	_nl_popup.popup_centered(Vector2i(360, 320))


func _build_new_level_popup() -> void:
	var base := get_editor_interface().get_base_control()
	_nl_popup = PopupPanel.new()
	_nl_popup.title = "New Level"
	base.add_child(_nl_popup)

	var vbox := VBoxContainer.new()
	_nl_popup.add_child(vbox)

	var name_label := Label.new()
	name_label.text = "Level name (e.g. BoilerRoom)"
	vbox.add_child(name_label)
	_nl_name = LineEdit.new()
	_nl_name.placeholder_text = "Level name"
	vbox.add_child(_nl_name)

	_nl_tileset = OptionButton.new()
	vbox.add_child(_labeled("Tileset", _nl_tileset))

	_nl_music = OptionButton.new()
	vbox.add_child(_labeled("Music", _nl_music))

	_nl_ambience = OptionButton.new()
	vbox.add_child(_labeled("Ambience", _nl_ambience))

	_populate_tilesets()
	_populate_audio(_nl_music, PackedStringArray(["res://assets/audio/music"]))
	_populate_audio(_nl_ambience, PackedStringArray(["res://assets/audio/music/generated"]))

	var create := Button.new()
	create.text = "Create"
	create.pressed.connect(_create_level_pressed)
	vbox.add_child(create)


func _create_level_pressed() -> void:
	var lvl_name := _nl_name.text.strip_edges()
	if lvl_name == "":
		_set_status("Enter a level name first.")
		return
	var ts: String = _nl_tileset.get_selected_metadata()
	var music: String = _nl_music.get_selected_metadata()
	var amb: String = _nl_ambience.get_selected_metadata()
	var path := LevelFactory.create_level(lvl_name, ts, music, amb)
	if path == "":
		_set_status("Failed to create level — see the editor Output log.")
	else:
		_set_status("Created and opened: %s" % path)
	_nl_popup.hide()


func _populate_tilesets() -> void:
	_nl_tileset.add_item("None")
	_nl_tileset.set_item_metadata(0, "")
	var idx := 1
	for f in _collect_files("res://assets/tilemaps", "tres"):
		_nl_tileset.add_item(f.get_file().trim_suffix(".tres"))
		_nl_tileset.set_item_metadata(idx, f)
		idx += 1
	_nl_tileset.select(0)


func _populate_audio(btn: OptionButton, dirs: PackedStringArray) -> void:
	btn.add_item("None")
	btn.set_item_metadata(0, "")
	var idx := 1
	for dir in dirs:
		for f in _collect_files(dir, "ogg"):
			btn.add_item(f.get_file().trim_suffix(".ogg"))
			btn.set_item_metadata(idx, f)
			idx += 1
	btn.select(0)


func _collect_files(dir_path: String, ext: String) -> PackedStringArray:
	var out := PackedStringArray()
	var d := DirAccess.open(dir_path)
	if d == null:
		return out
	d.list_dir_begin()
	var f := d.get_next()
	while f != "":
		if d.current_is_dir():
			out += _collect_files(dir_path.path_join(f), ext)
		elif f.get_extension().to_lower() == ext:
			out.append(dir_path.path_join(f))
		f = d.get_next()
	d.list_dir_end()
	return out


func _labeled(text: String, control: Control) -> VBoxContainer:
	var box := VBoxContainer.new()
	var l := Label.new()
	l.text = text
	box.add_child(l)
	box.add_child(control)
	return box


func _set_status(message: String) -> void:
	_status.text = message
