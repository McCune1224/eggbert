@tool
extends RefCounted

## Items tab: author Item resources as .tres without touching code.
## Logic ported unchanged from the old content_editors plugin.

var plugin: EditorPlugin

var _root: VBoxContainer
var _current_item = null
var _status: Label
var _items_list: VBoxContainer
var _counter: int = 0


func _init(p: EditorPlugin) -> void:
	plugin = p


func build() -> Control:
	_root = VBoxContainer.new()
	_root.name = "ItemsTab"

	var hint := Label.new()
	hint.text = "Create an Item, edit it in the Inspector, then Save."
	_root.add_child(hint)

	var new_btn := Button.new()
	new_btn.text = "New Item"
	new_btn.pressed.connect(_on_new_item)
	_root.add_child(new_btn)

	var save_btn := Button.new()
	save_btn.text = "Save"
	save_btn.pressed.connect(_on_save_item)
	_root.add_child(save_btn)

	_status = Label.new()
	_root.add_child(_status)

	var boot_note := Label.new()
	boot_note.text = "Items load at boot via ItemDatabase.LoadExternalItems()."
	_root.add_child(boot_note)

	var list_label := Label.new()
	list_label.text = "Existing items (click to load):"
	_root.add_child(list_label)

	_items_list = VBoxContainer.new()
	_root.add_child(_items_list)

	var refresh_btn := Button.new()
	refresh_btn.text = "Refresh"
	refresh_btn.pressed.connect(_refresh_items)
	_root.add_child(refresh_btn)

	_refresh_items()
	return _root


func _on_new_item() -> void:
	var item = _instantiate("Item", "res://components/items/Item.cs")
	if item == null:
		_status.text = "Could not create Item (ClassDB/Item missing)."
		return
	item.Id = _unique_id("new_item")
	_current_item = item
	plugin.get_editor_interface().get_inspector().edit(item)
	_status.text = "Created '%s' - edit fields in the Inspector, then Save." % item.Id


func _on_save_item() -> void:
	if _current_item == null:
		_status.text = "Create a New Item first."
		return
	var id: String = str(_current_item.Id)
	if id.strip_edges() == "":
		_status.text = "Item Id is required before saving."
		return
	_ensure_dir("res://resources/items")
	var path: String = "res://resources/items/%s.tres" % id
	var err := ResourceSaver.save(_current_item, path)
	if err != OK:
		_status.text = "Save failed (error %d)." % err
		return
	_status.text = "Saved %s" % path
	_refresh_items()
	plugin.get_editor_interface().get_resource_filesystem().scan()


func _refresh_items() -> void:
	for c in _items_list.get_children():
		c.queue_free()
	var dir = DirAccess.open("res://resources/items")
	if dir == null:
		return
	dir.list_dir_begin()
	var fname: String = dir.get_next()
	while fname != "":
		if not dir.current_is_dir() and fname.ends_with(".tres"):
			var full: String = "res://resources/items/" + fname
			var btn := Button.new()
			btn.text = fname
			btn.pressed.connect(_load_item.bind(full))
			_items_list.add_child(btn)
		fname = dir.get_next()
	dir.list_dir_end()


func _load_item(path: String) -> void:
	var item = load(path)
	if item == null:
		_status.text = "Could not load %s" % path
		return
	_current_item = item
	plugin.get_editor_interface().get_inspector().edit(item)
	_status.text = "Loaded %s" % path


func _instantiate(class_name_str: String, script_path: String):
	var inst = null
	var scr = load(script_path)
	if scr != null and scr.can_instantiate():
		inst = scr.new()
	if inst == null:
		inst = ClassDB.instantiate(class_name_str)
	return inst


func _ensure_dir(res_dir: String) -> void:
	var da := DirAccess.open("res://")
	if da != null:
		da.make_dir_recursive(res_dir)


func _unique_id(prefix: String) -> String:
	_counter += 1
	return "%s_%d" % [prefix, _counter]
