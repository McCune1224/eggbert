@tool
extends RefCounted

## Transition Audit tab: validates and auto-wires the NodePath exports that
## link puzzle/switch components to their targets in the edited scene.
## Logic ported unchanged from the old transition_audit plugin.

var plugin: EditorPlugin

var _rows: VBoxContainer
var _status: Label
var _components: Array = []  # Array of Dictionaries: {node, type, export, control}


func _init(p: EditorPlugin) -> void:
	plugin = p


func build() -> Control:
	var root := VBoxContainer.new()
	root.name = "TransitionAuditTab"

	var hint := Label.new()
	hint.text = "Validates and auto-wires puzzle/switch NodePath exports in the edited scene."
	root.add_child(hint)

	var btn_row := HBoxContainer.new()
	var scan_btn := Button.new()
	scan_btn.text = "Scan"
	scan_btn.pressed.connect(rescan)
	var validate_btn := Button.new()
	validate_btn.text = "Validate"
	validate_btn.pressed.connect(_on_validate)
	var apply_btn := Button.new()
	apply_btn.text = "Apply wiring"
	apply_btn.pressed.connect(_on_apply)
	btn_row.add_child(scan_btn)
	btn_row.add_child(validate_btn)
	btn_row.add_child(apply_btn)
	root.add_child(btn_row)

	_status = Label.new()
	_status.custom_minimum_size = Vector2(0, 0)
	root.add_child(_status)

	var scroll := ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_rows = VBoxContainer.new()
	scroll.add_child(_rows)
	root.add_child(scroll)

	return root


func rescan() -> void:
	if _rows == null:
		return
	_components.clear()
	for c in _rows.get_children():
		c.queue_free()

	var root: Node = plugin.get_editor_interface().get_edited_scene_root()
	if root == null:
		_set_status("No scene is open in the editor. Open a level scene to audit its wiring.")
		return

	var doors := []
	_collect(root, "Door", doors)
	var pads := []
	_collect(root, "TeleportPad", pads)

	var comps := []
	_collect_components(root, comps)

	var count := 0
	for comp in comps:
		var t := _component_type(comp)
		var export_name := ""
		var candidates := []
		if t == "FloorSwitch":
			export_name = "TargetDoorPath"
			candidates = doors
		elif t == "TeleportPad":
			export_name = "TargetPadPath"
			candidates = pads
		elif t == "LevelTransition":
			export_name = "TargetTransitionName"
			candidates = []
		_build_row(comp, t, export_name, candidates)
		count += 1

	if count == 0:
		_set_status("Scan complete: no FloorSwitch / TeleportPad / LevelTransition nodes found in this scene.")
	else:
		_set_status("Scan complete: found %d component(s). Choose targets, then Apply wiring." % count)


func _on_validate() -> void:
	if _components.is_empty():
		_set_status("Nothing to validate - run Scan first.")
		return
	var empty := 0
	var dead := 0
	for row in _components:
		var node: Node = row["node"]
		var type: String = row["type"]
		if type == "LevelTransition":
			var v: String = row["control"].text
			if v.strip_edges() == "":
				empty += 1
		else:
			var v = node.get(row["export"])
			if _value_empty(v):
				empty += 1
			elif node.get_node_or_null(v) == null:
				dead += 1
	_set_status("Validation: %d empty, %d dead (unresolvable) link(s)." % [empty, dead])


func _on_apply() -> void:
	if _components.is_empty():
		_set_status("Nothing to apply - run Scan first.")
		return
	var ur = plugin.get_undo_redo()
	ur.create_action("Wire transition components")
	for row in _components:
		var node: Node = row["node"]
		var export_name: String = row["export"]
		if row["type"] == "LevelTransition":
			var new_value: String = row["control"].text
			ur.add_do_property(node, export_name, new_value)
			ur.add_undo_property(node, export_name, node.get(export_name))
		else:
			var ob: OptionButton = row["control"]
			var target = ob.get_selected_metadata()
			var new_value
			if target == null:
				new_value = NodePath()
			else:
				new_value = node.get_path_to(target)
			ur.add_do_property(node, export_name, new_value)
			ur.add_undo_property(node, export_name, node.get(export_name))
	ur.commit_action()
	_set_status("Applied wiring for %d component(s). (Undoable)" % _components.size())


func _build_row(node: Node, type: String, export_name: String, candidates: Array) -> void:
	var hbox := HBoxContainer.new()

	var name_lbl := Label.new()
	name_lbl.text = node.name
	name_lbl.custom_minimum_size = Vector2(160, 0)
	hbox.add_child(name_lbl)

	var exp_lbl := Label.new()
	exp_lbl.text = export_name
	exp_lbl.custom_minimum_size = Vector2(150, 0)
	hbox.add_child(exp_lbl)

	if type == "LevelTransition":
		var le := LineEdit.new()
		le.text = node.get(export_name)
		le.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		hbox.add_child(le)
		_rows.add_child(hbox)
		_components.append({"node": node, "type": type, "export": export_name, "control": le})
		return

	var ob := OptionButton.new()
	ob.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	ob.add_item("- none -", 0)
	ob.set_item_metadata(0, null)

	var current = node.get(export_name)
	var current_node = node.get_node_or_null(current) if not _value_empty(current) else null
	var sel_idx := 0
	var idx := 1
	for cand in candidates:
		if cand == node:
			continue
		ob.add_item(cand.name, idx)
		ob.set_item_metadata(idx, cand)
		if cand == current_node:
			sel_idx = idx
		idx += 1
	ob.select(sel_idx)

	hbox.add_child(ob)
	_rows.add_child(hbox)
	_components.append({"node": node, "type": type, "export": export_name, "control": ob})


func _collect(node: Node, type_name: String, out: Array) -> void:
	if node != null and node.is_class(type_name):
		out.append(node)
	for c in node.get_children():
		_collect(c, type_name, out)


func _collect_components(node: Node, out: Array) -> void:
	var t := _component_type(node)
	if t != "":
		out.append(node)
	for c in node.get_children():
		_collect_components(c, out)


func _component_type(node: Node) -> String:
	if node.is_class("FloorSwitch"):
		return "FloorSwitch"
	if node.is_class("TeleportPad"):
		return "TeleportPad"
	if node.is_class("LevelTransition"):
		return "LevelTransition"
	return ""


func _value_empty(v) -> bool:
	if v == null:
		return true
	if v is NodePath:
		return v.is_empty()
	if v is String:
		return v == ""
	return false


func _set_status(t: String) -> void:
	_status.text = t
