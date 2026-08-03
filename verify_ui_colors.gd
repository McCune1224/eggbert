extends SceneTree

var _failures: Array = []
var _passes: int = 0

func _initialize() -> void:
	_check_theme_colors()
	_check_main_menu_quit_button()
	_check_overworld_menu_quit_button()
	_check_first_boot_dialog_source()
	_check_combat_hud_source()

	if _failures.size() > 0:
		for f in _failures:
			push_error(f)
		print("[UI-VERIFY] FAILED — %d failure(s)" % _failures.size())
		quit(1)
	else:
		print("[UI-VERIFY] All %d checks passed." % _passes)
		quit(0)

func _pass(msg: String) -> void:
	_passes += 1
	print("[UI-VERIFY] PASS: " + msg)

func _fail(msg: String) -> void:
	_failures.append(msg)
	push_error(msg)

func _check_theme_colors() -> void:
	var theme: Theme = load("res://assets/themes/eggbert_theme.tres")
	if theme == null:
		_fail("Theme resource missing at res://assets/themes/eggbert_theme.tres")
		return

	var cream := Color(0.83137256, 0.76862746, 0.627451)
	var danger := Color(0.8784314, 0.40784314, 0.40784314)

	var btn_fc := theme.get_color("font_color", "MenuButton")
	if btn_fc.is_equal_approx(cream):
		_pass("Theme MenuButton font_color = cream")
	else:
		_fail("Theme MenuButton font_color is %s, expected cream" % btn_fc)

	var danger_fc := theme.get_color("font_color", "MenuButtonDanger")
	if danger_fc.is_equal_approx(danger):
		_pass("Theme MenuButtonDanger font_color = danger pink")
	else:
		_fail("Theme MenuButtonDanger font_color is %s, expected danger pink" % danger_fc)

func _check_main_menu_quit_button() -> void:
	var scene: PackedScene = load("res://ui/MainMenu.tscn")
	if scene == null:
		_fail("MainMenu.tscn not found")
		return

	var menu: Node = scene.instantiate()
	if menu == null:
		_fail("MainMenu.tscn did not instantiate")
		return

	var quit_btn: Button = menu.get_node_or_null("MenuPanel/VBoxContainer/QuitButton") as Button
	if quit_btn == null:
		_fail("MainMenu QuitButton node not found")
	else:
		if quit_btn.theme_type_variation == "MenuButtonDanger":
			_pass("MainMenu QuitButton theme_type_variation = MenuButtonDanger")
		else:
			_fail("MainMenu QuitButton variation = %s, expected MenuButtonDanger" % quit_btn.theme_type_variation)

func _check_overworld_menu_quit_button() -> void:
	var scene: PackedScene = load("res://ui/OverworldMenu.tscn")
	if scene == null:
		_fail("OverworldMenu.tscn not found")
		return

	var menu: Node = scene.instantiate()
	if menu == null:
		_fail("OverworldMenu.tscn did not instantiate")
		return

	var quit_btn: Button = menu.get_node_or_null("MainPanel/VBoxContainer/GridRow3/QuitButton") as Button
	if quit_btn == null:
		_fail("OverworldMenu active QuitButton node not found")
	else:
		if quit_btn.theme_type_variation == "MenuButtonDanger":
			_pass("OverworldMenu active QuitButton theme_type_variation = MenuButtonDanger")
		else:
			_fail("OverworldMenu active QuitButton variation = %s, expected MenuButtonDanger" % quit_btn.theme_type_variation)

func _check_first_boot_dialog_source() -> void:
	var f := FileAccess.open("res://ui/first_boot_dialog.gd", FileAccess.READ)
	if f == null:
		_fail("first_boot_dialog.gd not found")
		return

	var src := f.get_as_text()
	f.close()

	# Built in GDScript: assert the real property assignments, not C# literals.
	if src.find("PanelContainer.new()") >= 0:
		_pass("FirstBootDialog: PanelContainer wrapper present")
	else:
		_fail("FirstBootDialog: missing PanelContainer.new()")

	if src.find("set_anchors_preset") >= 0:
		_pass("FirstBootDialog: Panel anchors set on panel")
	else:
		_fail("FirstBootDialog: missing set_anchors_preset")

	# Theme type variations must match the eggbert theme.
	if src.find("&" + "\"MenuButton\"") >= 0 or src.find("\"MenuButton\"") >= 0:
		_pass("FirstBootDialog: MenuButton variation on speed buttons")
	else:
		_fail("FirstBootDialog: missing MenuButton theme_type_variation on speed buttons")

	if src.find("&" + "\"MenuLabelTitle\"") >= 0 or src.find("\"MenuLabelTitle\"") >= 0:
		_pass("FirstBootDialog: MenuLabelTitle variation on title")
	else:
		_fail("FirstBootDialog: missing MenuLabelTitle theme_type_variation on title")

	if src.find("&" + "\"MenuLabel\"") >= 0 or src.find("\"MenuLabel\"") >= 0:
		_pass("FirstBootDialog: MenuLabel variation present")
	else:
		_fail("FirstBootDialog: missing MenuLabel theme_type_variation")

func _check_combat_hud_source() -> void:
	var f := FileAccess.open("res://combat/ui/combat_hud.gd", FileAccess.READ)
	if f == null:
		_fail("combat_hud.gd not found")
		return

	var src := f.get_as_text()
	f.close()

	# GDScript float literals drop the trailing 'f' used by the old C# sources.
	if src.find("0.91, 0.72, 0.38") >= 0:
		_pass("CombatHUD: Player bar uses theme orange")
	else:
		_fail("CombatHUD: missing player bar orange (0.91, 0.72, 0.38)")

	if src.find("0.88, 0.41, 0.41") >= 0:
		_pass("CombatHUD: Enemy bar uses theme danger pink")
	else:
		_fail("CombatHUD: missing enemy bar danger pink (0.88, 0.41, 0.41)")

	if src.find("0.91, 0.45, 0.1") >= 0:
		_pass("CombatHUD: Low-HP uses distinct amber")
	else:
		_fail("CombatHUD: missing low-HP amber (0.91, 0.45, 0.1)")

	if src.find("Color(0.53, 0.53, 0.53)") >= 0:
		_pass("CombatHUD: Enemy dead nameplate themed")
	else:
		_fail("CombatHUD: missing enemy dead nameplate grey (0.53, 0.53, 0.53)")
