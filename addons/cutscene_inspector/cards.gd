@tool
class_name CutsceneCards
extends RefCounted

# Pure Control builders for the cutscene/dialog-branch inspectors.
# Extracted into a RefCounted helper so tests can instantiate them
# outside the editor (EditorInspectorPlugin itself is editor-only).

# Indexed by StepType enum ordinal:
# SayDialog=0, MoveNpc=1, MovePlayer=2, FaceDirection=3, PlayAnimation=4,
# CameraMove=5, Wait=6, SetFlag=7, Fade=8, PromptChoice=9,
# LockPlayer=10, UnlockPlayer=11, Stop=12, DialogBranch=13.
# Order preserves the pre-DialogBranch enum so shipped .tres files
# that store Type=12 (Stop) still deserialize as Stop.
const STEP_TYPE_DATA := [
	[0, "💬 SayDialog", "say_dialog"],
	[1, "🚶 MoveNpc", "move_npc"],
	[2, "🎮 MovePlayer", "move_player"],
	[3, "🧭 FaceDirection", "face_direction"],
	[4, "🎞 PlayAnimation", "play_animation"],
	[5, "📷 CameraMove", "camera_move"],
	[6, "⏱ Wait", "wait"],
	[7, "🚩 SetFlag", "set_flag"],
	[8, "🌑 Fade", "fade"],
	[9, "❓ PromptChoice", "prompt_choice"],
	[10, "🔒 LockPlayer", ""],
	[11, "🔓 UnlockPlayer", ""],
	[12, "⛔ Stop", ""],
	[13, "🌿 DialogBranch", "dialog_branch"],
]

# Optional callback hooks the EditorInspectorPlugin uses to mutate the resource
# when buttons are pressed. The pure builder is a no-op when these are unset.
var on_move: Callable = Callable()
var on_remove: Callable = Callable()
var on_add: Callable = Callable()
var on_add_node: Callable = Callable()
var on_move_node: Callable = Callable()
var on_remove_node: Callable = Callable()
var on_edit_step: Callable = Callable()
var on_edit_node: Callable = Callable()


# ----------------------------- cutscene steps ---------------------------------

static func build_steps_view(parent_resource: Resource, helper: CutsceneCards = null) -> Control:
	if helper == null:
		helper = (load("res://addons/cutscene_inspector/cards.gd") as GDScript).new()
	var root := VBoxContainer.new()
	root.name = "CutsceneStepsView"
	root.add_theme_constant_override("separation", 4)

	var header := Label.new()
	header.text = "🎬 Cutscene Steps"
	header.add_theme_font_size_override("font_size", 14)
	root.add_child(header)

	var steps_array: Array = parent_resource.get("steps")
	if steps_array == null:
		steps_array = []

	for index in range(steps_array.size()):
		root.add_child(helper.build_step_card(parent_resource, steps_array, index))
	root.add_child(helper.build_add_controls(parent_resource, steps_array))
	return root

func build_step_card(parent_resource: Resource, steps_array: Array, index: int) -> Control:
	var step: Resource = steps_array[index]
	var card := VBoxContainer.new()
	card.add_theme_constant_override("separation", 4)

	var style := StyleBoxFlat.new()
	style.bg_color = _card_color_for_step(step)
	style.border_color = Color(0.4, 0.4, 0.45, 1.0)
	style.set_corner_radius_all(4)
	style.content_margin_left = 6
	style.content_margin_right = 6
	style.content_margin_top = 4
	style.content_margin_bottom = 4
	var panel := PanelContainer.new()
	panel.add_theme_stylebox_override("panel", style)
	card.add_child(panel)

	var body := VBoxContainer.new()
	body.add_theme_constant_override("separation", 2)
	panel.add_child(body)

	var header_row := HBoxContainer.new()
	var title := Label.new()
	title.text = "#%d  %s" % [index, label_for_step(step)]
	title.add_theme_font_size_override("font_size", 13)
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header_row.add_child(title)

	var up_button := Button.new()
	up_button.text = "↑"
	up_button.tooltip_text = "Move up"
	up_button.disabled = index == 0
	if on_move.is_valid():
		up_button.pressed.connect(on_move.bind(parent_resource, steps_array, index, -1))
	header_row.add_child(up_button)

	var down_button := Button.new()
	down_button.text = "↓"
	down_button.tooltip_text = "Move down"
	down_button.disabled = index == steps_array.size() - 1
	if on_move.is_valid():
		down_button.pressed.connect(on_move.bind(parent_resource, steps_array, index, 1))
	header_row.add_child(down_button)

	var edit_button := Button.new()
	edit_button.text = "Edit"
	edit_button.tooltip_text = "Edit step properties (Lines, Responses, Condition, etc.) in the Inspector"
	if on_edit_step.is_valid():
		edit_button.pressed.connect(on_edit_step.bind(step))
	header_row.add_child(edit_button)

	var remove_button := Button.new()
	remove_button.text = "✕"
	remove_button.tooltip_text = "Remove step"
	if on_remove.is_valid():
		remove_button.pressed.connect(on_remove.bind(parent_resource, steps_array, index))
	header_row.add_child(remove_button)

	body.add_child(header_row)

	var summary := _summary_for_step(step)
	if summary != "":
		var summary_label := Label.new()
		summary_label.text = summary
		summary_label.add_theme_color_override("font_color", Color(0.78, 0.78, 0.85))
		summary_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		body.add_child(summary_label)

	var condition_tag := _condition_tag_for_step(step)
	if condition_tag != "":
		var tag_label := Label.new()
		tag_label.text = condition_tag
		tag_label.add_theme_color_override("font_color", Color(0.95, 0.85, 0.4))
		body.add_child(tag_label)

	return card

func build_add_controls(parent_resource: Resource, steps_array: Array) -> Control:
	var row := HBoxContainer.new()

	var type_menu := OptionButton.new()
	type_menu.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	for entry in STEP_TYPE_DATA:
		# id == ordinal so the menu returns the integer enum value.
		type_menu.add_item(entry[1], entry[0])
	row.add_child(type_menu)

	var add_button := Button.new()
	add_button.text = "＋ Add Step"
	if on_add.is_valid():
		add_button.pressed.connect(on_add.bind(parent_resource, steps_array, type_menu))
	row.add_child(add_button)

	return row

# ----------------------------- dialog-branch nodes ---------------------------

static func build_nodes_view(parent_resource: Resource, helper: CutsceneCards = null) -> Control:
	if helper == null:
		helper = (load("res://addons/cutscene_inspector/cards.gd") as GDScript).new()
	var root := VBoxContainer.new()
	root.name = "DialogBranchNodesView"
	root.add_theme_constant_override("separation", 4)

	var header := Label.new()
	header.text = "🌿 Dialog Branch Nodes"
	header.add_theme_font_size_override("font_size", 14)
	root.add_child(header)

	var nodes_array: Array = parent_resource.get("nodes")
	if nodes_array == null:
		nodes_array = []

	for index in range(nodes_array.size()):
		root.add_child(helper.build_node_card(parent_resource, nodes_array, index))
	root.add_child(helper.build_node_add_controls(parent_resource, nodes_array))
	return root

func build_node_card(parent_resource: Resource, nodes_array: Array, index: int) -> Control:
	var node: Resource = nodes_array[index]
	var card := VBoxContainer.new()
	card.add_theme_constant_override("separation", 2)

	var style := StyleBoxFlat.new()
	style.bg_color = _card_color_for_node(node)
	style.border_color = Color(0.4, 0.4, 0.45, 1.0)
	style.set_corner_radius_all(4)
	style.content_margin_left = 6
	style.content_margin_right = 6
	style.content_margin_top = 4
	style.content_margin_bottom = 4
	var panel := PanelContainer.new()
	panel.add_theme_stylebox_override("panel", style)
	card.add_child(panel)

	var body := VBoxContainer.new()
	body.add_theme_constant_override("separation", 2)
	panel.add_child(body)

	var header_row := HBoxContainer.new()
	var id_text := str(node.get("id")) if node else "(unnamed)"
	var title := Label.new()
	title.text = "#%d  🆔 %s" % [index, id_text]
	title.add_theme_font_size_override("font_size", 13)
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header_row.add_child(title)

	var up_button := Button.new()
	up_button.text = "↑"
	up_button.disabled = index == 0
	if on_move_node.is_valid():
		up_button.pressed.connect(on_move_node.bind(parent_resource, nodes_array, index, -1))
	header_row.add_child(up_button)

	var down_button := Button.new()
	down_button.text = "↓"
	down_button.disabled = index == nodes_array.size() - 1
	if on_move_node.is_valid():
		down_button.pressed.connect(on_move_node.bind(parent_resource, nodes_array, index, 1))
	header_row.add_child(down_button)

	var edit_button := Button.new()
	edit_button.text = "Edit"
	edit_button.tooltip_text = "Edit node properties (Lines, Responses, Condition, SetFlagsOnEnter) in the Inspector"
	if on_edit_node.is_valid():
		edit_button.pressed.connect(on_edit_node.bind(node))
	header_row.add_child(edit_button)

	var remove_button := Button.new()
	remove_button.text = "✕"
	if on_remove_node.is_valid():
		remove_button.pressed.connect(on_remove_node.bind(parent_resource, nodes_array, index))
	header_row.add_child(remove_button)

	body.add_child(header_row)

	if node == null:
		var placeholder := Label.new()
		placeholder.text = "(empty entry)"
		placeholder.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7))
		body.add_child(placeholder)
		return card

	var lines: Array = node.get("lines")
	var responses: Array = node.get("responses")
	var speaker := str(node.get("speaker_name"))
	var summary := Label.new()
	summary.text = "Speaker: %s  ·  Lines: %d  ·  Responses: %d" % [speaker if speaker != "" else "—", lines.size() if lines != null else 0, responses.size() if responses != null else 0]
	summary.add_theme_color_override("font_color", Color(0.78, 0.78, 0.85))
	body.add_child(summary)

	if responses != null and not responses.is_empty():
		var arrow_list := ""
		for response_index in range(responses.size()):
			var response: Resource = responses[response_index]
			if response == null:
				continue
			var next_id := str(response.get("NextNodeId"))
			var text := str(response.get("Text"))
			arrow_list += "  ↳ “%s” → %s\n" % [text, "‘" + next_id + "’" if next_id != "" else "(end)"]
		var response_label := Label.new()
		response_label.text = arrow_list.rstrip("\n")
		response_label.add_theme_color_override("font_color", Color(0.6, 0.8, 1.0))
		response_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		body.add_child(response_label)

	var condition_tag := _condition_tag_for_node(node)
	if condition_tag != "":
		var tag_label := Label.new()
		tag_label.text = condition_tag
		tag_label.add_theme_color_override("font_color", Color(0.95, 0.85, 0.4))
		body.add_child(tag_label)

	return card

func build_node_add_controls(parent_resource: Resource, nodes_array: Array) -> Control:
	var row := HBoxContainer.new()
	var add_button := Button.new()
	add_button.text = "＋ Add Node"
	if on_add_node.is_valid():
		add_button.pressed.connect(on_add_node.bind(parent_resource, nodes_array))
	row.add_child(add_button)
	return row

# ----------------------------- introspection ---------------------------------

static func instantiate_step() -> Resource:
	var step_script := load("res://resources/cutscene/cutscene_step.gd") as Script
	if step_script == null:
		return null
	return step_script.new()

static func instantiate_node() -> Resource:
	var node_script := load("res://resources/dialog/dialog_node.gd") as Script
	if node_script == null:
		return null
	return node_script.new()

static func label_for_step(step: Resource) -> String:
	var ordinal: int = int(step.get("type"))
	for entry in STEP_TYPE_DATA:
		if int(entry[0]) == ordinal:
			return entry[1]
	return "❔ Unknown (%d)" % ordinal

static func ordinal_for_menu_index(menu_index: int) -> int:
	if menu_index < 0 or menu_index >= STEP_TYPE_DATA.size():
		return 0
	return int(STEP_TYPE_DATA[menu_index][0])

# ----------------------------- helpers ---------------------------------------

static func _summary_for_step(step: Resource) -> String:
	var ordinal: int = int(step.get("type"))
	var kind: String = ""
	for entry in STEP_TYPE_DATA:
		if int(entry[0]) == ordinal:
			kind = entry[2]
			break
	match kind:
		"say_dialog":
			var lines: Array = step.get("DialogLines")
			if lines == null or lines.is_empty():
				return "(no lines)"
			return "Lines: %d — “%s%s”" % [lines.size(), lines[0], "…" if lines.size() > 1 else ""]
		"move_npc", "move_player":
			return "Target: %s → %s over %.1fs" % [str(step.get("TargetNode")), str(step.get("MoveTarget")), float(step.get("MoveDuration"))]
		"face_direction":
			return "Node: %s  Direction: %s" % [str(step.get("AnimationNode")), str(step.get("AnimationName"))]
		"play_animation":
			return "Node: %s  Animation: %s" % [str(step.get("AnimationNode")), str(step.get("AnimationName"))]
		"camera_move":
			return "Offset → %s over %.1fs" % [str(step.get("MoveTarget")), float(step.get("MoveDuration"))]
		"wait":
			return "Seconds: %.2f" % float(step.get("WaitSeconds"))
		"set_flag":
			return "Key: %s = %s" % [str(step.get("SetFlagKey")), str(step.get("SetFlagValue"))]
		"fade":
			return "Direction: %s" % str(step.get("FadeDirection"))
		"prompt_choice":
			var opts: Array = step.get("ChoiceOptions")
			return "Choices: %d" % (opts.size() if opts != null else 0)
		"dialog_branch":
			var branch: Resource = step.get("DialogBranchResource")
			if branch == null:
				return "(no branch resource)"
			return "Branch: %s  Start: '%s'" % [branch.resource_path if branch.resource_path != "" else "(unsaved)", str(step.get("StartNodeId"))]
		_:
			return ""

static func _condition_tag_for_step(step: Resource) -> String:
	var condition = step.get("condition")
	if condition == null:
		return ""
	var condition_type: int = int(condition.get("type"))
	match condition_type:
		1:
			return "🟢 if flag '%s' is set" % str(condition.get("FlagKey"))
		2:
			return "🔴 if flag '%s' is NOT set" % str(condition.get("FlagKey"))
		3:
			return "❓ if lastChoice = %s" % str(condition.get("ChoiceIndex"))
	return ""

static func _card_color_for_step(step: Resource) -> Color:
	if step.get("condition") != null:
		return Color(0.22, 0.20, 0.10, 1.0)
	return Color(0.18, 0.18, 0.22, 1.0)

static func _condition_tag_for_node(node: Resource) -> String:
	var condition = node.get("condition")
	if condition == null:
		return ""
	var condition_type: int = int(condition.get("type"))
	match condition_type:
		1:
			return "🟢 runs if flag '%s' is set" % str(condition.get("FlagKey"))
		2:
			return "🔴 runs if flag '%s' is NOT set" % str(condition.get("FlagKey"))
		3:
			return "❓ runs if lastChoice = %s" % str(condition.get("ChoiceIndex"))
	return ""

static func _card_color_for_node(node: Resource) -> Color:
	if node != null and node.get("condition") != null:
		return Color(0.22, 0.20, 0.10, 1.0)
	return Color(0.18, 0.18, 0.22, 1.0)


# ----------------------------- cross references -----------------------------

# Builds the structured list of cross-reference links for the given object.
# Each entry is a Dictionary with `kind`, `display`, and `target` fields.
# `target` is either a Node (for resolved NodePath links) or a String
# (for resource path links). Unresolved NodePaths get target=null so the
# caller can render a disabled label instead of a clickable button.
static func build_cross_refs(object: Object) -> Array:
	var links: Array = []
	var cutscene: Resource = object.get("Cutscene")
	if cutscene != null and cutscene.resource_path != "":
		links.append({"kind": "Cutscene", "display": cutscene.resource_path, "target": cutscene.resource_path})
	var branch: Resource = object.get("DialogBranch")
	if branch != null and branch.resource_path != "":
		links.append({"kind": "Dialog Branch", "display": branch.resource_path, "target": branch.resource_path})
	var target_door = object.get("TargetDoorPath")
	var target_door_path_str := str(target_door) if target_door != null else ""
	if target_door != null and not target_door_path_str.is_empty():
		var resolved := resolve_scene_node(object, target_door)
		var display := target_door_path_str if resolved == null else "%s (%s)" % [target_door_path_str, resolved.name]
		links.append({"kind": "Target Door", "display": display, "target": resolved})
	var required_flag = object.get("RequiredFlag")
	if required_flag != null and str(required_flag) != "":
		links.append({"kind": "Required Flag", "display": str(required_flag), "target": null})
	return links

# Resolves a NodePath from a source node. Tries the source itself, then walks
# up the parent chain. Returns null if the path can't be resolved.
static func resolve_scene_node(source: Object, path: NodePath) -> Node:
	if not (source is Node):
		return null
	var node := source as Node
	var found := node.get_node_or_null(path)
	if found != null:
		return found
	var parent := node.get_parent()
	while parent != null:
		var candidate := parent.get_node_or_null(path)
		if candidate != null:
			return candidate
		parent = parent.get_parent()
	return null