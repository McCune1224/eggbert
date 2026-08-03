extends Node

signal battle_won
signal battle_lost

var in_combat: bool = false
var overworld_position: Vector2 = Vector2.ZERO
var _return_level_path: String = ""
var _current_arena: CombatArena

func enter_combat(arena_path: String, return_position: Vector2) -> void:
	if in_combat:
		return
	var current_level: Node = GameController.current_level
	if current_level == null:
		return
	GameLogger.info("Combat", "Entering combat arena %s (from %s)" % [arena_path, current_level.name])
	_return_level_path = str(current_level.get("scene_file_path"))
	if _return_level_path.is_empty():
		_return_level_path = str(current_level.get("scene_path"))
	overworld_position = return_position
	in_combat = true
	GameController.load_level_at_position(arena_path, Vector2.ZERO)
	await GameController.level_loaded
	_current_arena = GameController.current_level as CombatArena
	if _current_arena == null:
		in_combat = false
		return
	_current_arena.battle_won.connect(_on_battle_won)
	_current_arena.battle_lost.connect(_on_battle_lost)

func exit_combat() -> void:
	if not in_combat:
		return
	_on_battle_won()

func return_to_overworld() -> void:
	if _return_level_path.is_empty():
		return
	GameController.load_level_at_position(_return_level_path, overworld_position)

func _on_battle_won() -> void:
	GameLogger.info("Combat", "Battle won")
	_unhook_arena()
	in_combat = false
	battle_won.emit()
	return_to_overworld()

func _on_battle_lost() -> void:
	GameLogger.info("Combat", "Battle lost")
	_unhook_arena()
	in_combat = false
	battle_lost.emit()
	var save_manager: Node = get_tree().root.get_node_or_null("SaveManager")
	if save_manager != null and save_manager.has_method("load_game"):
		save_manager.call("load_game")

func _unhook_arena() -> void:
	if _current_arena == null:
		return
	if _current_arena.battle_won.is_connected(_on_battle_won):
		_current_arena.battle_won.disconnect(_on_battle_won)
	if _current_arena.battle_lost.is_connected(_on_battle_lost):
		_current_arena.battle_lost.disconnect(_on_battle_lost)
	_current_arena = null
