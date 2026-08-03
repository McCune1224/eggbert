extends SceneTree

const MODEL_PATHS := [
	"res://resources/cutscene/cutscene_condition.gd",
	"res://resources/cutscene/cutscene_resource.gd",
	"res://resources/cutscene/cutscene_step.gd",
	"res://resources/dialog/dialog_branch.gd",
	"res://resources/dialog/dialog_node.gd",
	"res://resources/dialog/dialog_response.gd",
	"res://resources/dialog/dialog_voice_resource.gd",
	"res://resources/quests/quest_definition.gd",
	"res://resources/quests/quest_objective.gd",
	"res://components/items/item.gd",
	"res://components/items/item_database.gd",
]

func _initialize() -> void:
	var scripts: Array[Script] = []
	for path in MODEL_PATHS:
		var script := ResourceLoader.load(path) as Script
		assert(script != null, path)
		scripts.append(script)
	for script in scripts:
		assert(script.new() != null)
	var condition = scripts[0].new()
	assert(condition.get("type") == 0)
	var cutscene = scripts[1].new()
	cutscene.get("steps").append(scripts[2].new())
	assert(cutscene.get("steps").size() == 1)
	var step = scripts[2].new()
	assert(step.get("type") == 0)
	var branch = scripts[3].new()
	var node = scripts[4].new()
	node.set("id", "start")
	branch.get("nodes").append(node)
	assert(branch.call("get_node_by_id", "start") == node)
	var response = scripts[5].new()
	response.set("text", "Continue")
	node.get("responses").append(response)
	assert(node.get("responses").size() == 1)
	var voice = scripts[6].new()
	assert(voice.get("speaker_name") == "")
	var quest = scripts[7].new()
	quest.get("objectives").append(scripts[8].new())
	assert(quest.get("objectives").size() == 1)
	var database = scripts[10]
	var rusty_key = database.get("all").get("rusty_key")
	assert(rusty_key.get("display_name") == "Rusty Key")
	print("GDScript model smoke test passed")
	quit(0)
