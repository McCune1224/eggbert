@tool
extends RefCounted

## Quests tab: author QuestDefinition resources and register them in
## QuestManager (append-only). Logic ported unchanged from the old
## content_editors plugin.

var plugin: EditorPlugin

var _root: VBoxContainer
var _objectives: Array = []
var _current_quest = null
var _current_quest_path: String = ""
var _status: Label
var _objectives_list: VBoxContainer
var _quest_id_le: LineEdit
var _quest_title_le: LineEdit
var _quest_desc_le: LineEdit
var _quest_start_le: LineEdit


func _init(p: EditorPlugin) -> void:
	plugin = p


func build() -> Control:
	_root = VBoxContainer.new()
	_root.name = "QuestsTab"

	var hint := Label.new()
	hint.text = "Define a quest, then Save and Register in QuestManager."
	_root.add_child(hint)

	_quest_id_le = _labeled_line_edit("Id")
	_quest_title_le = _labeled_line_edit("Title")
	_quest_desc_le = _labeled_line_edit("Description")
	_quest_start_le = _labeled_line_edit("StartFlag")

	_root.add_child(_labeled("Objectives", null))
	_objectives_list = VBoxContainer.new()
	_root.add_child(_objectives_list)

	var add_obj_btn := Button.new()
	add_obj_btn.text = "Add Objective"
	add_obj_btn.pressed.connect(_on_add_objective)
	_root.add_child(add_obj_btn)

	var new_btn := Button.new()
	new_btn.text = "New Quest"
	new_btn.pressed.connect(_on_new_quest)
	_root.add_child(new_btn)

	var save_btn := Button.new()
	save_btn.text = "Save"
	save_btn.pressed.connect(_on_save_quest)
	_root.add_child(save_btn)

	var reg_btn := Button.new()
	reg_btn.text = "Register in QuestManager"
	reg_btn.pressed.connect(_on_register_quest)
	_root.add_child(reg_btn)

	_status = Label.new()
	_root.add_child(_status)

	return _root


func _labeled_line_edit(label_text: String) -> LineEdit:
	var le := LineEdit.new()
	le.placeholder_text = label_text
	_root.add_child(_labeled(label_text, le))
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
	_objectives = [{"id": "objective_1", "desc": "", "flag": ""}]
	_rebuild_objectives_ui()
	var q = _build_quest()
	if q != null:
		_current_quest = q
		plugin.get_editor_interface().get_inspector().edit(q)


func _rebuild_objectives_ui() -> void:
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
		_status.text = "Quest Id is required."
		return null
	var q = _instantiate("QuestDefinition", "res://resources/quests/QuestDefinition.cs")
	if q == null:
		_status.text = "Could not create QuestDefinition."
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
		_status.text = "Save failed (error %d)." % err
		return
	_current_quest_path = path
	_status.text = "Saved %s" % path
	plugin.get_editor_interface().get_resource_filesystem().scan()


func _on_register_quest() -> void:
	if _current_quest_path == "":
		_status.text = "Save the quest first."
		return
	var packed = load("res://autoload/QuestManager.tscn")
	if packed == null:
		_status.text = "QuestManager.tscn not found."
		return
	var scene = packed.instantiate()
	var quests = scene.get("Quests")
	if quests == null:
		quests = []
	var res = load(_current_quest_path)
	if res == null:
		_status.text = "Could not load saved quest."
		scene.free()
		return
	for q in quests:
		if q != null and str(q.resource_path) == _current_quest_path:
			_status.text = "Already registered."
			scene.free()
			return
	quests.append(res)
	scene.set("Quests", quests)
	var new_packed := PackedScene.new()
	var perr := new_packed.pack(scene)
	if perr != OK:
		_status.text = "Pack failed (error %d)." % perr
		scene.free()
		return
	var serr := ResourceSaver.save(new_packed, "res://autoload/QuestManager.tscn")
	if serr != OK:
		_status.text = "Register failed (error %d)." % serr
	else:
		_status.text = "Registered %s in QuestManager." % _current_quest_path
	scene.free()
	plugin.get_editor_interface().get_resource_filesystem().scan()


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
