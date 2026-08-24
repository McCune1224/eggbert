@tool
extends EditorPlugin

# Content Editors — author Item and QuestDefinition resources as .tres files.
# Items save into res://resources/items/ (loaded at boot by
# ItemDatabase.LoadExternalItems()). Quests save into res://resources/quests/
# and can be registered into the QuestManager autoload scene (append-only).

var _item_dock: VBoxContainer
var _quest_dock: VBoxContainer

# Item editor state
var _current_item = null
var _item_status: Label
var _items_list: VBoxContainer

# Quest editor state
var _objectives: Array = []
var _current_quest = null
var _current_quest_path: String = ""
var _quest_status: Label
var _objectives_list: VBoxContainer
var _quest_id_le: LineEdit
var _quest_title_le: LineEdit
var _quest_desc_le: LineEdit
var _quest_start_le: LineEdit

var _counter: int = 0


func _enter_tree() -> void:
	_build_item_dock()
	_build_quest_dock()


func _exit_tree() -> void:
	if _item_dock != null:
		remove_control_from_docks(_item_dock)
		_item_dock.free()
	if _quest_dock != null:
		remove_control_from_docks(_quest_dock)
		_quest_dock.free()


# ---------------------------------------------------------------------------
# Item editor
# ---------------------------------------------------------------------------

func _build_item_dock() -> void:
	_item_dock = VBoxContainer.new()
	_item_dock.name = "Item Editor"
	_item_dock.add_theme_constant_override("separation", 4)

	var title := Label.new()
	title.text = "Item Editor"
	title.add_theme_font_size_override("font_size", 18)
	_item_dock.add_child(title)

	var hint := Label.new()
	hint.text = "Create an Item, edit it in the Inspector, then Save."
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_item_dock.add_child(hint)

	var new_btn := Button.new()
	new_btn.text = "New Item"
	new_btn.pressed.connect(_on_new_item)
	_item_dock.add_child(new_btn)

	var save_btn := Button.new()
	save_btn.text = "Save"
	save_btn.pressed.connect(_on_save_item)
	_item_dock.add_child(save_btn)

	_item_status = Label.new()
	_item_status.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_item_dock.add_child(_item_status)

	var boot_note := Label.new()
	boot_note.text = "Items load at boot via ItemDatabase.LoadExternalItems()."
	boot_note.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_item_dock.add_child(boot_note)

	var list_label := Label.new()
	list_label.text = "Existing items (click to load):"
	_item_dock.add_child(list_label)

	_items_list = VBoxContainer.new()
	_item_dock.add_child(_items_list)

	var refresh_btn := Button.new()
	refresh_btn.text = "Refresh"
	refresh_btn.pressed.connect(_refresh_items)
	_item_dock.add_child(refresh_btn)

	add_control_to_dock(EditorPlugin.DOCK_SLOT_RIGHT_UL, _item_dock)
	_refresh_items()


func _on_new_item() -> void:
	var item = _instantiate("Item", "res://components/items/Item.cs")
	if item == null:
		_item_status.text = "Could not create Item (ClassDB/Item missing)."
		return
	item.Id = _unique_id("new_item")
	_current_item = item
	get_editor_interface().get_inspector().edit(item)
	_item_status.text = "Created '%s' — edit fields in the Inspector, then Save." % item.Id


func _on_save_item() -> void:
	if _current_item == null:
		_item_status.text = "Create a New Item first."
		return
	var id: String = str(_current_item.Id)
	if id.strip_edges() == "":
		_item_status.text = "Item Id is required before saving."
		return
	_ensure_dir("res://resources/items")
	var path: String = "res://resources/items/%s.tres" % id
	var err := ResourceSaver.save(_current_item, path)
	if err != OK:
		_item_status.text = "Save failed (error %d)." % err
		return
	_item_status.text = "Saved %s" % path
	_refresh_items()
	get_editor_interface().get_resource_filesystem().scan()


func _refresh_items() -> void:
	if _items_list == null:
		return
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
		_item_status.text = "Could not load %s" % path
		return
	_current_item = item
	get_editor_interface().get_inspector().edit(item)
	_item_status.text = "Loaded %s" % path


# ---------------------------------------------------------------------------
# Quest editor
# ---------------------------------------------------------------------------

func _build_quest_dock() -> void:
	_quest_dock = VBoxContainer.new()
	_quest_dock.name = "Quest Editor"
	_quest_dock.add_theme_constant_override("separation", 4)

	var title := Label.new()
	title.text = "Quest Editor"
	title.add_theme_font_size_override("font_size", 18)
	_quest_dock.add_child(title)

	var hint := Label.new()
	hint.text = "Define a quest, then Save and Register in QuestManager."
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_quest_dock.add_child(hint)

	_quest_id_le = _labeled_line_edit("Id")
	_quest_title_le = _labeled_line_edit("Title")
	_quest_desc_le = _labeled_line_edit("Description")
	_quest_start_le = _labeled_line_edit("StartFlag")

	_quest_dock.add_child(_labeled("Objectives", null))
	_objectives_list = VBoxContainer.new()
	_quest_dock.add_child(_objectives_list)

	var add_obj_btn := Button.new()
	add_obj_btn.text = "Add Objective"
	add_obj_btn.pressed.connect(_on_add_objective)
	_quest_dock.add_child(add_obj_btn)

	var new_btn := Button.new()
	new_btn.text = "New Quest"
	new_btn.pressed.connect(_on_new_quest)
	_quest_dock.add_child(new_btn)

	var save_btn := Button.new()
	save_btn.text = "Save"
	save_btn.pressed.connect(_on_save_quest)
	_quest_dock.add_child(save_btn)

	var reg_btn := Button.new()
	reg_btn.text = "Register in QuestManager"
	reg_btn.pressed.connect(_on_register_quest)
	_quest_dock.add_child(reg_btn)

	_quest_status = Label.new()
	_quest_status.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_quest_dock.add_child(_quest_status)

	add_control_to_dock(EditorPlugin.DOCK_SLOT_LEFT_UL, _quest_dock)


func _labeled_line_edit(label_text: String) -> LineEdit:
	var le := LineEdit.new()
	le.placeholder_text = label_text
	_quest_dock.add_child(_labeled(label_text, le))
	return le


func _labeled(text: String, control: Control) -> VBoxContainer:
	var box := VBoxContainer.new()
	var l := Label.new()
	l.text = text
	box.add_child(l)
	if control != null:
		box.add_child(control)
	return box


func _on_add_objective() -> void:
	_objectives.append({"id": "objective_%d" % (_objectives.size() + 1), "desc": "", "flag": ""})
	_rebuild_objectives_ui()


func _on_new_quest() -> void:
	_quest_id_le.text = _unique_id("new_quest")
	_quest_title_le.text = ""
	_quest_desc_le.text = ""
	_quest_start_le.text = ""
	_objectives = [{"id": "objective_1", "desc": "", "flag": ""}]
	_rebuild_objectives_ui()
	var q = _build_quest()
	if q != null:
		_current_quest = q
		get_editor_interface().get_inspector().edit(q)


func _rebuild_objectives_ui() -> void:
	if _objectives_list == null:
		return
	for c in _objectives_list.get_children():
		c.queue_free()
	for i in _objectives.size():
		var d: Dictionary = _objectives[i]
		var row := HBoxContainer.new()
		var le_id := LineEdit.new()
		le_id.placeholder_text = "Id"
		le_id.text = d["id"]
		var le_desc := LineEdit.new()
		le_desc.placeholder_text = "Description"
		le_desc.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		le_desc.text = d["desc"]
		var le_flag := LineEdit.new()
		le_flag.placeholder_text = "CompletionFlag"
		le_flag.text = d["flag"]
		var rm := Button.new()
		rm.text = "X"
		var idx: int = i
		le_id.text_changed.connect(func(t): _objectives[idx]["id"] = t)
		le_desc.text_changed.connect(func(t): _objectives[idx]["desc"] = t)
		le_flag.text_changed.connect(func(t): _objectives[idx]["flag"] = t)
		rm.pressed.connect(func(): _remove_objective(idx))
		row.add_child(le_id)
		row.add_child(le_desc)
		row.add_child(le_flag)
		row.add_child(rm)
		_objectives_list.add_child(row)


func _remove_objective(idx: int) -> void:
	if idx >= 0 and idx < _objectives.size():
		_objectives.remove_at(idx)
		_rebuild_objectives_ui()


func _build_quest():
	var id: String = _quest_id_le.text.strip_edges()
	if id == "":
		_quest_status.text = "Quest Id is required."
		return null
	var q = _instantiate("QuestDefinition", "res://resources/quests/QuestDefinition.cs")
	if q == null:
		_quest_status.text = "Could not create QuestDefinition."
		return null
	q.Id = id
	q.Title = _quest_title_le.text
	q.Description = _quest_desc_le.text
	q.StartFlag = _quest_start_le.text
	var objs: Array = []
	for d in _objectives:
		var o = _instantiate("QuestObjective", "res://resources/quests/QuestObjective.cs")
		if o == null:
			continue
		o.Id = str(d["id"])
		o.Description = str(d["desc"])
		o.CompletionFlag = str(d["flag"])
		objs.append(o)
	q.Objectives = objs
	return q


func _on_save_quest() -> void:
	var q = _build_quest()
	if q == null:
		return
	_ensure_dir("res://resources/quests")
	var path: String = "res://resources/quests/%s.tres" % q.Id
	var err := ResourceSaver.save(q, path)
	if err != OK:
		_quest_status.text = "Save failed (error %d)." % err
		return
	_current_quest_path = path
	_quest_status.text = "Saved %s" % path
	get_editor_interface().get_resource_filesystem().scan()


func _on_register_quest() -> void:
	if _current_quest_path == "":
		_quest_status.text = "Save the quest first."
		return
	var packed = load("res://autoload/QuestManager.tscn")
	if packed == null:
		_quest_status.text = "QuestManager.tscn not found."
		return
	var scene = packed.instantiate()
	var quests = scene.get("Quests")
	if quests == null:
		quests = []
	var res = load(_current_quest_path)
	if res == null:
		_quest_status.text = "Could not load saved quest."
		scene.free()
		return
	for q in quests:
		if q != null and str(q.resource_path) == _current_quest_path:
			_quest_status.text = "Already registered."
			scene.free()
			return
	quests.append(res)
	scene.set("Quests", quests)
	var new_packed := PackedScene.new()
	var perr := new_packed.pack(scene)
	if perr != OK:
		_quest_status.text = "Pack failed (error %d)." % perr
		scene.free()
		return
	var serr := ResourceSaver.save(new_packed, "res://autoload/QuestManager.tscn")
	if serr != OK:
		_quest_status.text = "Register failed (error %d)." % serr
	else:
		_quest_status.text = "Registered %s in QuestManager." % _current_quest_path
	scene.free()
	get_editor_interface().get_resource_filesystem().scan()


# ---------------------------------------------------------------------------
# Shared helpers
# ---------------------------------------------------------------------------

func _instantiate(class_name_str: String, script_path: String):
	var inst = null
	var script = load(script_path)
	if script != null and script.can_instantiate():
		inst = script.new()
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
