extends Node

signal quest_state_changed

const PINNED_QUEST_FLAG := "quest_pinned_id"
const UNPINNED_SENTINEL := "__unpinned__"
const QUESTS_DIRECTORY := "res://resources/quests"

enum QuestStatus { LOCKED, ACTIVE, COMPLETED }

var pinned_quest_id: String = ""
var active_quests: Dictionary[String, QuestDefinition] = {}

var _valid_quests: Dictionary[String, QuestDefinition] = {}
var _last_reported_status: Dictionary[String, int] = {}

func _ready() -> void:
	add_to_group("persist")
	process_mode = Node.PROCESS_MODE_ALWAYS
	_discover_quests()
	var flags := get_tree().root.get_node_or_null("WorldFlags")
	if flags != null and flags.has_signal("state_changed"):
		flags.state_changed.connect(_on_world_flags_state_changed)
	GameLogger.info("QuestManager", "Registered %d quest(s): %s" % [_valid_quests.size(), _valid_quests.keys()])
	_report_status_changes()

func _discover_quests() -> void:
	_valid_quests.clear()
	active_quests.clear()
	var directory := DirAccess.open(QUESTS_DIRECTORY)
	if directory == null:
		GameLogger.warn("QuestManager", "Quests directory not found: %s" % QUESTS_DIRECTORY)
		return
	var id_counts: Dictionary[String, int] = {}
	for filename in directory.get_files():
		if not filename.ends_with(".tres"):
			continue
		var path := QUESTS_DIRECTORY.path_join(filename)
		var quest := ResourceLoader.load(path) as QuestDefinition
		if quest == null or quest.id.is_empty():
			GameLogger.warn("QuestManager", "Skipping %s: not a QuestDefinition or empty id" % path)
			continue
		id_counts[quest.id] = int(id_counts.get(quest.id, 0)) + 1
		if id_counts[quest.id] > 1:
			GameLogger.error("QuestManager", "Duplicate quest id '%s' in %s" % [quest.id, path])
			continue
		if _is_definition_valid(quest):
			_valid_quests[quest.id] = quest
			active_quests[quest.id] = quest
			GameLogger.info("QuestManager", "Registered quest '%s' (%s)" % [quest.id, quest.title])

func _is_definition_valid(quest: QuestDefinition) -> bool:
	if quest.title.is_empty() or quest.description.is_empty():
		GameLogger.error("QuestManager", "Quest '%s' has empty title or description" % quest.id)
		return false
	if quest.objectives.is_empty():
		GameLogger.error("QuestManager", "Quest '%s' has no objectives" % quest.id)
		return false
	var objective_ids: Dictionary[String, int] = {}
	for objective: QuestObjective in quest.objectives:
		if objective.id.is_empty() or objective.description.is_empty():
			GameLogger.error("QuestManager", "Quest '%s' has an objective with empty id or description" % quest.id)
			return false
		objective_ids[objective.id] = int(objective_ids.get(objective.id, 0)) + 1
		if objective_ids[objective.id] > 1:
			GameLogger.error("QuestManager", "Quest '%s' has duplicate objective id '%s'" % [quest.id, objective.id])
			return false
	return true

func get_quest(quest_id: String) -> QuestDefinition:
	if quest_id.is_empty():
		return null
	return _valid_quests.get(quest_id)

func get_status(quest: QuestDefinition) -> int:
	if quest == null or not _valid_quests.has(quest.id):
		return QuestStatus.LOCKED
	var objectives: Array = quest.objectives
	if objectives.is_empty():
		return QuestStatus.LOCKED
	var final_objective := objectives[objectives.size() - 1] as QuestObjective
	if _has_flag(final_objective.completion_flag):
		return QuestStatus.COMPLETED
	if quest.start_flag.is_empty() or _has_flag(quest.start_flag):
		return QuestStatus.ACTIVE
	return QuestStatus.LOCKED

func get_current_objective(quest: QuestDefinition) -> QuestObjective:
	if quest == null or get_status(quest) != QuestStatus.ACTIVE:
		return null
	for objective: QuestObjective in quest.objectives:
		if not _has_flag(objective.completion_flag):
			return objective
	return null

func get_pinned_quest() -> QuestDefinition:
	var flags := get_tree().root.get_node_or_null("WorldFlags")
	if flags == null:
		return null
	var pinned_id := str(flags.call("get_flag", PINNED_QUEST_FLAG, ""))
	if pinned_id == UNPINNED_SENTINEL:
		return null
	var pinned: QuestDefinition = get_quest(pinned_id)
	if pinned != null and get_status(pinned) == QuestStatus.ACTIVE:
		return pinned
	return _first_active_quest()

func pin_quest(quest_id: String) -> void:
	var quest := get_quest(quest_id)
	if get_status(quest) != QuestStatus.ACTIVE:
		GameLogger.warn("QuestManager", "Cannot pin quest '%s': it is unknown, locked, or completed." % quest_id)
		return
	pinned_quest_id = quest.id
	_set_flag(PINNED_QUEST_FLAG, quest.id)
	GameLogger.info("QuestManager", "Pinned quest '%s' (%s)" % [quest.id, quest.title])

func unpin_quest() -> void:
	pinned_quest_id = ""
	_set_flag(PINNED_QUEST_FLAG, UNPINNED_SENTINEL)
	GameLogger.info("QuestManager", "Unpinned quest; objective HUD hidden.")

func get_save_key() -> String:
	return "quest_manager"

func serialize() -> Dictionary[String, Variant]:
	return {"pinned_quest_id": pinned_quest_id}

func deserialize(data: Dictionary[String, Variant]) -> void:
	pinned_quest_id = str(data.get("pinned_quest_id", ""))

func get_load_priority() -> int:
	return 10

func _on_world_flags_state_changed() -> void:
	_report_status_changes()
	quest_state_changed.emit()

func _report_status_changes() -> void:
	for quest_id: String in _valid_quests:
		var quest := _valid_quests[quest_id]
		var status := get_status(quest)
		if _last_reported_status.get(quest_id, -1) == status:
			continue
		_last_reported_status[quest_id] = status
		var status_name: String = ["LOCKED", "ACTIVE", "COMPLETED"][status]
		GameLogger.info("QuestManager", "Quest '%s' status -> %s" % [quest_id, status_name])
		if status == QuestStatus.ACTIVE:
			var objective := get_current_objective(quest)
			if objective != null:
				GameLogger.info("QuestManager", "Quest '%s' current objective: %s" % [quest_id, objective.description])
		elif status == QuestStatus.COMPLETED:
			GameLogger.info("QuestManager", "Quest '%s' completed" % quest_id)

func _first_active_quest() -> QuestDefinition:
	for quest: QuestDefinition in _valid_quests.values():
		if get_status(quest) == QuestStatus.ACTIVE:
			return quest
	return null

func _has_flag(flag_name: String) -> bool:
	var flags := get_tree().root.get_node_or_null("WorldFlags")
	return flags != null and flags.has_method("has_flag") and bool(flags.call("has_flag", flag_name))

func _set_flag(flag_name: String, value: Variant) -> void:
	var flags := get_tree().root.get_node_or_null("WorldFlags")
	if flags != null and flags.has_method("set_flag"):
		flags.call("set_flag", flag_name, value)
