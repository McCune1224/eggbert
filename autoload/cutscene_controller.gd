extends Node

var is_playing := false
var cancelled := false
var last_choice_index := -1

func stop() -> void:
	cancelled = true
	is_playing = false

func start_dialog(lines: Array[String], voice: DialogVoiceResource = null) -> void:
	if is_playing or lines.is_empty():
		return
	GameLogger.debug("Cutscene", "Starting dialog-only (%d lines)" % lines.size())
	is_playing = true
	cancelled = false
	var dialog := get_tree().root.get_node_or_null("DialogManager")
	if dialog != null and dialog.has_method("start_dialog"):
		dialog.call("start_dialog", lines, voice)
		await dialog.dialog_finished
	is_playing = false
	GameLogger.debug("Cutscene", "DoDialog finished")

func play_cutscene(resource: CutsceneResource) -> void:
	is_playing = true
	cancelled = false
	last_choice_index = -1
	GameLogger.info("Cutscene", "Playing cutscene %s (%d steps)" % [resource.resource_path, resource.steps.size()])
	for step in resource.steps:
		if cancelled:
			GameLogger.info("Cutscene", "Cutscene cancelled")
			break
		if step.should_execute(WorldFlags, last_choice_index):
			GameLogger.debug("Cutscene", "Executing step %s" % step.resource_path)
			await step.execute(self)
	is_playing = false
	GameLogger.info("Cutscene", "Cutscene finished")

func run_dialog_branch(branch: DialogBranch, start_node_id: String = "") -> void:
	var node_id := start_node_id
	var nodes: Array = branch.get("nodes")
	if node_id.is_empty() and not nodes.is_empty():
		node_id = str(nodes[0].get("id"))
	GameLogger.info("Dialog", "Running dialog branch %s from node '%s'" % [branch.resource_path, node_id])
	while not node_id.is_empty() and not cancelled:
		var node: Resource = branch.get_node_by_id(node_id)
		if node == null:
			GameLogger.warn("Dialog", "Dialog branch node '%s' not found" % node_id)
			return
		var condition: Resource = node.get("condition")
		if condition == null or condition.is_met(WorldFlags, last_choice_index):
			for flag in node.get("set_flags_on_enter"):
				WorldFlags.set_flag(flag, true)
				GameLogger.debug("Dialog", "Branch node '%s' set flag %s" % [node_id, flag])
			var dialog := get_tree().root.get_node_or_null("DialogManager")
			if dialog != null:
				dialog.start_dialog(node.get("lines"), node.get("voice"))
				await dialog.dialog_finished
			var responses: Array = node.get("responses")
			if responses.is_empty():
				GameLogger.debug("Dialog", "Branch node '%s' terminal (no responses)" % node_id)
				return
			last_choice_index = 0
			var response: Resource = responses[last_choice_index]
			var flag := str(response.get("set_flag_on_select"))
			if not flag.is_empty():
				WorldFlags.set_flag(flag, true)
				GameLogger.info("Dialog", "Branch choice %s set flag %s" % [response.resource_path, flag])
			node_id = str(response.get("next_node_id"))
		else:
			GameLogger.debug("Dialog", "Branch node '%s' condition unmet; stopping" % node_id)
			return
