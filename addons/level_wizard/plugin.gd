@tool
extends EditorPlugin

const LevelFactory := preload("res://addons/level_wizard/level_factory.gd")

var _name_edit: LineEdit
var _tileset_btn: OptionButton
var _music_btn: OptionButton
var _ambience_btn: OptionButton
var _status: Label
var _dock: VBoxContainer


func _enter_tree() -> void:
	_dock = VBoxContainer.new()
	_dock.name = "New Level"
	_dock.size_flags_vertical = Control.SIZE_EXPAND_FILL

	var title := Label.new()
	title.text = "New Level Wizard"
	title.add_theme_font_size_override("font_size", 18)
	_dock.add_child(title)

	var hint := Label.new()
	hint.text = "Scaffolds a BaseLevel scene with two tilemap layers + music/ambience, then opens it."
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_dock.add_child(hint)

	_name_edit = LineEdit.new()
	_name_edit.placeholder_text = "Level name (e.g. BoilerRoom)"
	_dock.add_child(_name_edit)

	_tileset_btn = OptionButton.new()
	_populate_tilesets()
	_dock.add_child(_labeled("Tileset", _tileset_btn))

	_music_btn = OptionButton.new()
	_populate_audio(_music_btn, "res://assets/audio/music")
	_dock.add_child(_labeled("Music", _music_btn))

	_ambience_btn = OptionButton.new()
	_populate_audio(_ambience_btn, "res://assets/audio/music/generated")
	_dock.add_child(_labeled("Ambience", _ambience_btn))

	var create := Button.new()
	create.text = "Create Level"
	create.pressed.connect(_on_create)
	_dock.add_child(create)

	_status = Label.new()
	_status.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_dock.add_child(_status)

	add_control_to_dock(EditorPlugin.DOCK_SLOT_RIGHT_UL, _dock)


func _exit_tree() -> void:
	if _dock != null:
		remove_control_from_docks(_dock)
		_dock.queue_free()


func _on_create() -> void:
	var lvl_name := _name_edit.text
	if lvl_name.strip_edges() == "":
		_set_status("Enter a level name first.")
		return
	var ts: String = _tileset_btn.get_selected_metadata()
	var music: String = _music_btn.get_selected_metadata()
	var amb: String = _ambience_btn.get_selected_metadata()
	var path := LevelFactory.create_level(lvl_name, ts, music, amb)
	if path == "":
		_set_status("Failed to create level — see the editor Output log.")
	else:
		_set_status("Created and opened: %s" % path)


func _populate_tilesets() -> void:
	_tileset_btn.add_item("None", 0)
	_tileset_btn.set_item_metadata(0, "")
	var idx := 1
	for f in _collect_files("res://assets/tilemaps", "tres"):
		_tileset_btn.add_item(f.get_file().trim_suffix(".tres"), idx)
		_tileset_btn.set_item_metadata(idx, f)
		if f == "res://assets/tilemaps/factory_tileset.tres":
			_tileset_btn.select(idx)
		idx += 1
	if _tileset_btn.selected == -1:
		_tileset_btn.select(0)


func _populate_audio(btn: OptionButton, base_dir: String) -> void:
	btn.add_item("None", 0)
	btn.set_item_metadata(0, "")
	var idx := 1
	var dirs := PackedStringArray([base_dir])
	if base_dir == "res://assets/audio/music":
		dirs.append("res://assets/audio/music/generated")
	for d in dirs:
		for f in _collect_files(d, "ogg"):
			btn.add_item(f.get_file().trim_suffix(".ogg"), idx)
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
		elif f.get_extension() == ext:
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


func _set_status(text: String) -> void:
	_status.text = text
