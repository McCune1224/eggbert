extends SceneTree

const DIALOG_BRANCH_SCRIPT := preload("res://resources/dialog/dialog_branch.gd")
const DIALOG_NODE_SCRIPT := preload("res://resources/dialog/dialog_node.gd")

func _initialize() -> void:
	var branch: Resource = DIALOG_BRANCH_SCRIPT.new()
	var node: Resource = DIALOG_NODE_SCRIPT.new()
	node.set("id", "start")
	branch.get("nodes").append(node)
	assert(branch.call("get_node_by_id", "start") == node)
	print("Dialog branch checks passed")
	quit(0)
