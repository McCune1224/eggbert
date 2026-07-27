@tool
extends EditorPlugin

const CUTSCENE_SCRIPT := preload("res://resources/cutscene/CutsceneResource.cs")
const DIALOG_BRANCH_SCRIPT := preload("res://resources/dialog/DialogBranch.cs")
const CARDS := preload("res://addons/cutscene_inspector/cards.gd")

var _cutscene_inspector: CutsceneResourceInspector
var _dialog_branch_inspector: DialogBranchInspector
var _puzzle_inspector: PuzzleCrossRefInspector

func _enter_tree() -> void:
	_cutscene_inspector = CutsceneResourceInspector.new()
	add_inspector_plugin(_cutscene_inspector)

	_dialog_branch_inspector = DialogBranchInspector.new()
	add_inspector_plugin(_dialog_branch_inspector)

	_puzzle_inspector = PuzzleCrossRefInspector.new()
	add_inspector_plugin(_puzzle_inspector)

func _exit_tree() -> void:
	if _cutscene_inspector != null:
		remove_inspector_plugin(_cutscene_inspector)
	if _dialog_branch_inspector != null:
		remove_inspector_plugin(_dialog_branch_inspector)
	if _puzzle_inspector != null:
		remove_inspector_plugin(_puzzle_inspector)


# ----------------------------- CutsceneResource --------------------------------

class CutsceneResourceInspector extends EditorInspectorPlugin:
	func _can_handle(object: Object) -> bool:
		if object == null:
			return false
		return object.get_script() == CUTSCENE_SCRIPT

	func _parse_property(object: Object, type: int, name: String, hint_type: int, hint_string: String, usage_flags: int, wide: bool) -> bool:
		if name == "Steps":
			return true
		return false

	func _parse_begin(object: Object) -> void:
		var helper: CutsceneCards = CARDS.new()
		helper.on_move = Callable(self, "_move_step")
		helper.on_remove = Callable(self, "_remove_step")
		helper.on_add = Callable(self, "_add_step")
		helper.on_edit_step = Callable(self, "_edit_step")
		add_custom_control(CARDS.build_steps_view(object, helper))

	func _edit_step(step: Resource) -> void:
		# Open the step's own inspector so the user can edit Lines, Responses,
		# Condition, DialogBranch, TargetNode, etc. The default property editor
		# handles every exported field on CutsceneStep.
		if step == null:
			return
		EditorInterface.edit_resource(step)

	func _move_step(parent_resource: Resource, steps_array: Array, index: int, direction: int) -> void:
		var target := index + direction
		if target < 0 or target >= steps_array.size():
			return
		var swapped := steps_array.duplicate()
		var temp = swapped[index]
		swapped[index] = swapped[target]
		swapped[target] = temp
		_apply_steps_change(parent_resource, steps_array, swapped, "Move Cutscene Step")

	func _remove_step(parent_resource: Resource, steps_array: Array, index: int) -> void:
		if index < 0 or index >= steps_array.size():
			return
		var removed: Resource = steps_array[index]
		var next := steps_array.duplicate()
		next.remove_at(index)
		_apply_steps_change(parent_resource, steps_array, next, "Remove Cutscene Step")
		if removed != null:
			removed.take_over_path("")

	func _add_step(parent_resource: Resource, steps_array: Array, type_menu: OptionButton) -> void:
		var ordinal: int = type_menu.get_selected_id()
		if ordinal < 0 or ordinal >= CARDS.STEP_TYPE_DATA.size():
			ordinal = 0
		var new_step: Resource = CARDS.instantiate_step()
		if new_step == null:
			EditorInterface.get_editor_toaster().push_toast("Could not create step.", EditorToaster.SEVERITY_ERROR)
			return
		# C# enum binding: must pass the integer ordinal, not the name string.
		new_step.set("Type", ordinal)
		var next := steps_array.duplicate()
		next.append(new_step)
		_apply_steps_change(parent_resource, steps_array, next, "Add Cutscene Step")

	# Mutates the parent resource's Steps array through UndoRedo so the
	# operation is undoable and the .tres marks itself dirty for save.
	# `old_array` is the pre-mutation array; `new_array` is the result.
	static func _apply_steps_change(parent_resource: Resource, old_array: Array, new_array: Array, action_label: String) -> void:
		var undo_redo := EditorInterface.get_editor_undo_redo()
		undo_redo.create_action(action_label)
		undo_redo.add_do_method(parent_resource, "set", "Steps", new_array)
		undo_redo.add_do_method(parent_resource, "emit_changed")
		undo_redo.add_undo_method(parent_resource, "set", "Steps", old_array)
		undo_redo.add_undo_method(parent_resource, "emit_changed")
		# Always re-mark for inspector + filesystem refresh, both directions.
		undo_redo.add_do_method(EditorInterface, "edit_resource", parent_resource)
		undo_redo.add_undo_method(EditorInterface, "edit_resource", parent_resource)
		undo_redo.commit_action()
		EditorInterface.get_resource_filesystem().scan()


# ----------------------------- DialogBranch -----------------------------------

class DialogBranchInspector extends EditorInspectorPlugin:
	func _can_handle(object: Object) -> bool:
		if object == null:
			return false
		return object.get_script() == DIALOG_BRANCH_SCRIPT

	func _parse_property(object: Object, type: int, name: String, hint_type: int, hint_string: String, usage_flags: int, wide: bool) -> bool:
		if name == "Nodes":
			return true
		return false

	func _parse_begin(object: Object) -> void:
		var helper: CutsceneCards = CARDS.new()
		helper.on_add_node = Callable(self, "_add_node")
		helper.on_move_node = Callable(self, "_move_node")
		helper.on_remove_node = Callable(self, "_remove_node")
		helper.on_edit_node = Callable(self, "_edit_node")
		add_custom_control(CARDS.build_nodes_view(object, helper))

	func _edit_node(node: Resource) -> void:
		# Open the node's own inspector so the user can edit Id, Lines, Responses,
		# Condition, SetFlagsOnEnter, etc. via the default property editor.
		if node == null:
			return
		EditorInterface.edit_resource(node)
	func _add_node(parent_resource: Resource, nodes_array: Array) -> void:
		var new_node: Resource = CARDS.instantiate_node()
		if new_node == null:
			EditorInterface.get_editor_toaster().push_toast("DialogNode script missing.", EditorToaster.SEVERITY_ERROR)
			return
		new_node.set("Id", "node_%d" % nodes_array.size())
		var next := nodes_array.duplicate()
		next.append(new_node)
		_apply_nodes_change(parent_resource, nodes_array, next, "Add Dialog Node")

	func _remove_node(parent_resource: Resource, nodes_array: Array, index: int) -> void:
		if index < 0 or index >= nodes_array.size():
			return
		var removed: Resource = nodes_array[index]
		var next := nodes_array.duplicate()
		next.remove_at(index)
		_apply_nodes_change(parent_resource, nodes_array, next, "Remove Dialog Node")
		if removed != null:
			removed.take_over_path("")

	func _move_node(parent_resource: Resource, nodes_array: Array, index: int, direction: int) -> void:
		var target := index + direction
		if target < 0 or target >= nodes_array.size():
			return
		var swapped := nodes_array.duplicate()
		var temp = swapped[index]
		swapped[index] = swapped[target]
		swapped[target] = temp
		_apply_nodes_change(parent_resource, nodes_array, swapped, "Move Dialog Node")

	static func _apply_nodes_change(parent_resource: Resource, old_array: Array, new_array: Array, action_label: String) -> void:
		var undo_redo := EditorInterface.get_editor_undo_redo()
		undo_redo.create_action(action_label)
		undo_redo.add_do_method(parent_resource, "set", "Nodes", new_array)
		undo_redo.add_do_method(parent_resource, "emit_changed")
		undo_redo.add_undo_method(parent_resource, "set", "Nodes", old_array)
		undo_redo.add_undo_method(parent_resource, "emit_changed")
		undo_redo.add_do_method(EditorInterface, "edit_resource", parent_resource)
		undo_redo.add_undo_method(EditorInterface, "edit_resource", parent_resource)
		undo_redo.commit_action()
		EditorInterface.get_resource_filesystem().scan()


# ----------------------------- Cross references -------------------------------

class PuzzleCrossRefInspector extends EditorInspectorPlugin:
	const TRIGGER_PATHS := [
		"res://components/npcs/CutsceneTrigger.cs",
		"res://components/npcs/DialogBranchTrigger.cs",
	]
	const PUZZLE_PATHS := [
		"res://components/puzzles/FloorSwitch.cs",
		"res://components/puzzles/KeyDoor.cs",
		"res://components/puzzles/Door.cs",
	]

	func _can_handle(object: Object) -> bool:
		if object == null:
			return false
		var script: Script = object.get_script()
		if script == null:
			return false
		var path: String = script.resource_path
		return TRIGGER_PATHS.has(path) or PUZZLE_PATHS.has(path)

	func _parse_property(object: Object, type: int, name: String, hint_type: int, hint_string: String, usage_flags: int, wide: bool) -> bool:
		return false

	func _parse_begin(object: Object) -> void:
		var section := VBoxContainer.new()
		section.add_theme_constant_override("separation", 4)
		var header := Label.new()
		header.text = "📎 Cross References"
		header.add_theme_font_size_override("font_size", 12)
		section.add_child(header)

		var links: Array = _links_for(object)
		if links.is_empty():
			var note := Label.new()
			note.text = "(No linked resources)"
			note.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7))
			section.add_child(note)
		else:
			for entry in links:
				var row := HBoxContainer.new()
				var kind_label := Label.new()
				kind_label.text = "%s: " % entry["kind"]
				kind_label.custom_minimum_size = Vector2(140, 0)
				row.add_child(kind_label)
				# `target` holds either a Node (for NodePath links) or a String
				# resource path. When unresolved, the label is shown disabled.
				var target = entry.get("target")
				var display: String = entry.get("display", "")
				if target == null or (target is String and target == ""):
					var placeholder := Label.new()
					placeholder.text = display
					placeholder.add_theme_color_override("font_color", Color(0.55, 0.55, 0.6))
					placeholder.size_flags_horizontal = Control.SIZE_EXPAND_FILL
					row.add_child(placeholder)
				else:
					var button := Button.new()
					button.text = display
					button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
					button.pressed.connect(_on_link_pressed.bind(target))
					row.add_child(button)
				section.add_child(row)
		add_custom_control(section)

	func _on_link_pressed(target: Variant) -> void:
		# Node target: select it in the scene tree.
		if target is Node:
			var sel := EditorInterface.get_selection()
			sel.clear()
			sel.add_node(target)
			return
		# String target: a file path. Try as a resource, then as a scene.
		if target is String and target != "":
			if ResourceLoader.exists(target):
				var res := ResourceLoader.load(target)
				if res != null:
					EditorInterface.edit_resource(res)
					return
			if FileAccess.file_exists(target):
				EditorInterface.open_scene_from_path(target)

	func _links_for(object: Object) -> Array:
		return CARDS.build_cross_refs(object)
