extends SceneTree

# Structural verification of the CutsceneCards helper. Confirms:
#   - Int-ordinal Type binding for CutsceneStep (no string-name path).
#   - Card structure: VBoxContainer wrapping a PanelContainer.
#   - Each card has Title, ↑, ↓, Edit, ✕ buttons.
#   - The Edit button is connected to on_edit_step / on_edit_node.
#   - Dropdown has 14 items, DialogBranch id=13, Stop id=12.
#   - DialogBranch node cards show id, response count, target node.

func _initialize() -> void:
	var Cards: GDScript = load("res://addons/cutscene_inspector/cards.gd")
	if Cards == null:
		push_error("Failed to load cards.gd")
		quit(1)
		return

	# ---- Build a CutsceneResource with one SayDialog + one DialogBranch step.
	var cutscene: Resource = load("res://resources/cutscene/CutsceneResource.cs").new()
	var step1: Resource = load("res://resources/cutscene/CutsceneStep.cs").new()
	step1.set("Type", 0)
	step1.set("DialogLines", ["Hello, friend.", "Welcome to the demo."])
	var cond: Resource = load("res://resources/cutscene/CutsceneCondition.cs").new()
	cond.set("Type", 1)
	cond.set("FlagKey", "demo_flag")
	step1.set("Condition", cond)

	var step2: Resource = load("res://resources/cutscene/CutsceneStep.cs").new()
	step2.set("Type", 13)
	var branch: Resource = load("res://resources/dialog/DialogBranch.cs").new()
	step2.set("DialogBranchResource", branch)
	step2.set("StartNodeId", "greeting")
	cutscene.set("Steps", [step1, step2])

	if int(step1.get("Type")) != 0 or int(step2.get("Type")) != 13:
		push_error("Int-ordinal Type binding did not round-trip")
		quit(1)
		return
	print("[cards] + CutsceneStep.set('Type', <int>) round-trips: 0=SayDialog, 13=DialogBranch")

	# ---- Build the steps view with a wired Edit callback.
	var edit_target: Array = [null]
	var helper: RefCounted = Cards.new()
	helper.on_edit_step = func(step): edit_target[0] = step
	helper.on_edit_node = func(node): edit_target[0] = node

	var steps_view: Control = Cards.call("build_steps_view", cutscene, helper)
	if steps_view == null or steps_view.get_child_count() != 4:
		push_error("Expected 4 children in steps view, got %d" % steps_view.get_child_count())
		quit(1)
		return
	print("[cards] build_steps_view produced %s with %d children" % [steps_view.get_class(), steps_view.get_child_count()])

	var card0: Control = steps_view.get_child(1)
	var card1: Control = steps_view.get_child(2)
	if not (card0 is VBoxContainer) or not (card1 is VBoxContainer):
		push_error("Step cards should be VBoxContainer, got %s / %s" % [card0.get_class(), card1.get_class()])
		quit(1)
		return
	if not (card0.get_child(0) is PanelContainer):
		push_error("Step cards should contain PanelContainer children")
		quit(1)
		return

	# ---- Verify each step card has the four buttons in this order: ↑ ↓ Edit ✕.
	for card_index in [0, 1]:
		var card: Control = [card0, card1][card_index]
		var buttons: Array = _find_buttons(card)
		var labels: Array = buttons.map(func(b): return b.text)
		if labels != ["↑", "↓", "Edit", "✕"]:
			push_error("Card %d buttons should be ['↑','↓','Edit','✕'], got %s" % [card_index, str(labels)])
			quit(1)
			return
	print("[cards] + Each step card carries ↑/↓/Edit/✕ buttons in order")

	# ---- Verify the Edit button on card0 is connected and routes the step.
	var edit_button0: Button = _find_buttons(card0)[2]
	var connections: Array = edit_button0.pressed.get_connections()
	if connections.size() != 1:
		push_error("Edit button on card0 should have 1 connection, got %d" % connections.size())
		quit(1)
		return
	# Drive the Edit button via the in-tree path.
	get_root().add_child(steps_view)
	edit_button0.pressed.emit()
	if edit_target[0] != step1:
		push_error("Edit button on card0 should pass step1, got %s" % str(edit_target[0]))
		quit(1)
		return
	print("[cards] + Edit button on step card0 invokes on_edit_step(step1)")

	var edit_button1: Button = _find_buttons(card1)[2]
	edit_button1.pressed.emit()
	if edit_target[0] != step2:
		push_error("Edit button on card1 should pass step2, got %s" % str(edit_target[0]))
		quit(1)
		return
	print("[cards] + Edit button on step card1 invokes on_edit_step(step2)")

	# ---- Title, condition tag, and dropdown wiring.
	var first_label0 := _first_label(card0)
	var first_label1 := _first_label(card1)
	if not first_label0.text.contains("SayDialog") or not first_label1.text.contains("DialogBranch"):
		push_error("Card titles should map to enum names via int ordinal")
		quit(1)
		return
	print("[cards] + Cards display '💬 SayDialog' and '🌿 DialogBranch' via int ordinal lookup")

	if not _all_text(card0).contains("demo_flag"):
		push_error("Card 0 condition tag should mention 'demo_flag'")
		quit(1)
		return
	print("[cards] + Condition tag renders the flag key for FlagSet")

	var add_row: Control = steps_view.get_child(3)
	var menu: OptionButton = _find_option_buttons(add_row)[0]
	if menu.item_count != 14 or menu.get_item_id(13) != 13 or menu.get_item_text(13) != "🌿 DialogBranch":
		push_error("Dropdown should have 14 items, DialogBranch at id=13")
	if menu.get_item_id(12) != 12 or menu.get_item_text(12) != "⛔ Stop":
		push_error("Dropdown item 12 should be Stop id=12")
	print("[cards] + Add Step dropdown: 14 items, DialogBranch=13, Stop=12 (int ordinals)")

	# ---- DialogBranch node cards: same shape and Edit button wiring.
	var greeting: Resource = load("res://resources/dialog/DialogNode.cs").new()
	greeting.set("Id", "greeting")
	greeting.set("Lines", ["Hi!", "Need help?"])
	var yes_resp: Resource = load("res://resources/dialog/DialogResponse.cs").new()
	yes_resp.set("Text", "Yes, I'll help")
	yes_resp.set("NextNodeId", "farewell")
	yes_resp.set("SetFlagOnSelect", "npc_helped")
	var no_resp: Resource = load("res://resources/dialog/DialogResponse.cs").new()
	no_resp.set("Text", "No thanks")
	no_resp.set("NextNodeId", "")
	no_resp.set("SetFlagOnSelect", "npc_declined")
	greeting.set("Responses", [yes_resp, no_resp])

	var farewell: Resource = load("res://resources/dialog/DialogNode.cs").new()
	farewell.set("Id", "farewell")
	farewell.set("Lines", ["See you later."])
	branch.set("Nodes", [greeting, farewell])

	edit_target[0] = null
	var nodes_view: Control = Cards.call("build_nodes_view", branch, helper)
	if nodes_view.get_child_count() != 4:
		push_error("DialogBranch view expected 4 children, got %d" % nodes_view.get_child_count())
		quit(1)
		return

	var greeting_card: Control = nodes_view.get_child(1)
	var farewell_card: Control = nodes_view.get_child(2)
	for card_index in [0, 1]:
		var card: Control = [greeting_card, farewell_card][card_index]
		var labels: Array = _find_buttons(card).map(func(b): return b.text)
		if labels != ["↑", "↓", "Edit", "✕"]:
			push_error("Node card %d buttons should be ['↑','↓','Edit','✕'], got %s" % [card_index, str(labels)])
			quit(1)
			return
	print("[cards] + Each DialogBranch node card carries ↑/↓/Edit/✕ buttons in order")

	if not _all_text(greeting_card).contains("greeting"):
		push_error("Greeting card should show id 'greeting'")
		quit(1)
		return
	if not _all_text(greeting_card).contains("Yes, I'll help") or not _all_text(greeting_card).contains("farewell"):
		push_error("Greeting card should show first response text and target 'farewell'")
		quit(1)
		return
	if not _all_text(greeting_card).contains("Responses: 2"):
		push_error("Greeting card should report 'Responses: 2'")
		quit(1)
		return
	print("[cards] + DialogBranch card shows id, response text, target, and response count")

	if not _all_text(farewell_card).contains("farewell") or not _all_text(farewell_card).contains("Responses: 0"):
		push_error("Farewell card should show id 'farewell' and 'Responses: 0'")
		quit(1)
		return
	print("[cards] + Second node card renders independently with correct counts")

	# ---- Drive the Edit button on a node card.
	get_root().add_child(nodes_view)
	var node_edit_button: Button = _find_buttons(greeting_card)[2]
	node_edit_button.pressed.emit()
	if edit_target[0] != greeting:
		push_error("Edit button on greeting card should pass greeting, got %s" % str(edit_target[0]))
		quit(1)
		return
	print("[cards] + Edit button on node card invokes on_edit_node(greeting)")

	print("[cards] ALL OK")
	quit(0)

func _first_label(node: Node) -> Label:
	for child in node.get_children():
		if child is Label:
			return child
		var nested := _first_label(child)
		if nested != null:
			return nested
	return null

func _find_buttons(node: Node) -> Array:
	var out: Array = []
	if node is Button:
		out.append(node)
	for child in node.get_children():
		out.append_array(_find_buttons(child))
	return out

func _find_option_buttons(node: Node) -> Array:
	var out: Array = []
	if node is OptionButton:
		out.append(node)
	for child in node.get_children():
		out.append_array(_find_option_buttons(child))
	return out

func _find_labels(node: Node) -> Array:
	var out: Array = []
	if node is Label:
		out.append(node)
	for child in node.get_children():
		out.append_array(_find_labels(child))
	return out

func _all_text(node: Node) -> String:
	var pieces: Array = []
	for lbl in _find_labels(node):
		pieces.append(lbl.text)
	return " ".join(pieces)
