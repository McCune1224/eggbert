extends Node

var is_playing := false
var cancelled := false
var last_choice_index := -1

func stop() -> void:
	cancelled = true
	is_playing = false

func play_cutscene(resource: CutsceneResource) -> void:
	is_playing = true
	cancelled = false
	last_choice_index = -1
	for step in resource.steps:
		if cancelled:
			break
		if step.should_execute(WorldFlags, last_choice_index):
			await step.execute(self)
	is_playing = false

func run_dialog_branch(branch: DialogBranch, start_node_id: String = "") -> void:
	var node_id := start_node_id
	var nodes: Array = branch.get("nodes")
	if node_id.is_empty() and not nodes.is_empty():
		node_id = str(nodes[0].get("id"))
	while not node_id.is_empty() and not cancelled:
		var node: Resource = branch.get_node_by_id(node_id)
		if node == null:
			return
		var condition: Resource = node.get("condition")
		if condition == null or condition.is_met(WorldFlags, last_choice_index):
			for flag in node.get("set_flags_on_enter"):
				WorldFlags.set_flag(flag, true)
			var dialog := get_tree().root.get_node_or_null("DialogManager")
			if dialog != null:
				dialog.start_dialog(node.get("lines"), node.get("voice"))
				await dialog.dialog_finished
			var responses: Array = node.get("responses")
			if responses.is_empty():
				return
			last_choice_index = 0
			var response: Resource = responses[last_choice_index]
			var flag := str(response.get("set_flag_on_select"))
			if not flag.is_empty():
				WorldFlags.set_flag(flag, true)
			node_id = str(response.get("next_node_id"))
		else:
			return
