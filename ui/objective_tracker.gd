extends CanvasLayer

@onready var panel: PanelContainer = get_node_or_null("Panel") as PanelContainer
@onready var quest_title_label: Label = get_node_or_null("Panel/VBoxContainer/QuestTitleLabel") as Label
@onready var objective_label: Label = get_node_or_null("Panel/VBoxContainer/ObjectiveLabel") as Label

var _level_ready: bool = false
var _showing_completion: bool = false
var _cached_quest_id: String = ""
var _cached_objective_id: String = ""
var _cached_objective_text: String = ""
var _display_version: int = 0
var _last_logged_quest_id: String = ""
var _last_logged_objective_id: String = ""

func _ready() -> void:
	if panel != null:
		panel.visible = false
	var controller := _autoload("GameController")
	if controller != null:
		if controller.has_signal("level_load_started"):
			controller.level_load_started.connect(_on_level_load_started)
		if controller.has_signal("level_loaded"):
			controller.level_loaded.connect(_on_level_loaded)
	var quests := _autoload("QuestManager")
	if quests != null and quests.has_signal("quest_state_changed"):
		quests.quest_state_changed.connect(_on_quest_state_changed)

func _exit_tree() -> void:
	_display_version += 1

func _on_level_load_started() -> void:
	_level_ready = false
	_showing_completion = false
	_clear_cached_objective()
	if panel != null:
		panel.visible = false

func _on_level_loaded() -> void:
	var controller: Node = _autoload("GameController")
	var level: Variant = controller.get("current_level") if controller != null else null
	_level_ready = level != null and (level is BaseLevel or level.has_method("get_load_priority"))
	refresh()

func _on_quest_state_changed() -> void:
	if _level_ready:
		refresh()

func refresh() -> void:
	if not _level_ready or _showing_completion:
		if panel != null:
			panel.visible = false
		return
	var quests := _autoload("QuestManager")
	if quests == null or not quests.has_method("get_pinned_quest"):
		GameLogger.debug("ObjectiveTracker", "QuestManager missing or no get_pinned_quest; hiding")
		return
	var quest = quests.call("get_pinned_quest")
	var objective = quests.call("get_current_objective", quest) if quest != null and quests.has_method("get_current_objective") else null
	if quest == null or objective == null:
		_clear_cached_objective()
		if panel != null:
			panel.visible = false
		GameLogger.debug("ObjectiveTracker", "No pinned quest or current objective; hiding")
		return
	_cached_quest_id = str(quest.get("id"))
	_cached_objective_id = str(objective.get("id"))
	_cached_objective_text = str(objective.get("description"))
	if quest_title_label != null:
		quest_title_label.text = str(quest.get("title"))
	if objective_label != null:
		objective_label.text = _cached_objective_text
	if panel != null:
		panel.visible = true
	if _cached_quest_id != _last_logged_quest_id or _cached_objective_id != _last_logged_objective_id:
		GameLogger.info("ObjectiveTracker", "Showing quest '%s' objective '%s': %s" % [_cached_quest_id, _cached_objective_id, _cached_objective_text])
		_last_logged_quest_id = _cached_quest_id
		_last_logged_objective_id = _cached_objective_id

func _clear_cached_objective() -> void:
	_cached_quest_id = ""
	_cached_objective_id = ""
	_cached_objective_text = ""

func _autoload(node_name: String) -> Node:
	return get_tree().root.get_node_or_null(node_name)
