class_name GameInit
extends Node

func _ready() -> void:
	call_deferred("_boot")

func _boot() -> void:
	GameLogger.initialize_from_env()
	Settings.load_settings()
	GameLogger.info("GameInit", "Boot initialized logger and settings.")
	_log_autoload_manifest()

func _log_autoload_manifest() -> void:
	var expected := [
		"WorldFlags", "QuestManager", "GameController", "DialogManager", "AudioManager",
		"Player", "FadeTransition", "CutsceneController", "DebugOverlay", "SaveManager",
		"Inventory", "Equipment", "CombatController", "KeybindManager", "FactoryOpeningFlow",
	]
	for autoload_name in expected:
		var node := get_tree().root.get_node_or_null(autoload_name)
		if node == null:
			GameLogger.error("GameInit", "Autoload '%s' MISSING at boot" % autoload_name)
		else:
			GameLogger.info("GameInit", "Autoload '%s' loaded (%s)" % [autoload_name, node.get_class()])

	var skip_menu := OS.get_environment("EGGBERT_SKIP_MENU") == "1"
	if skip_menu and SaveManager.has_save():
		GameLogger.info("GameInit", "Skip-menu requested and save exists.")
		if SaveManager.load_game():
			if GameController.has_signal("level_loaded"):
				await GameController.level_loaded
			return
		GameLogger.warn("GameInit", "Save could not be loaded; opening main menu.")

	var dialog_log_script: Script = load("res://ui/dialog_log.gd") as Script
	if dialog_log_script != null:
		var dialog_log: Node = dialog_log_script.new() as Node
		get_tree().root.add_child(dialog_log)
	var menu_scene := load("res://ui/MainMenu.tscn") as PackedScene
	if menu_scene == null:
		GameLogger.error("GameInit", "Failed to load MainMenu.tscn.")
		return
	get_tree().root.add_child(menu_scene.instantiate())
