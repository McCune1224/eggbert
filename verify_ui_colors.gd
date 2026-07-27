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
	var f := FileAccess.open("res://ui/FirstBootDialog.cs", FileAccess.READ)
	if f == null:
		_fail("FirstBootDialog.cs not found")
		return

	var src := f.get_as_text()
	f.close()

	var checks := {
		"MenuButton variation on speed buttons": "ThemeTypeVariation = \"MenuButton\"",
		"MenuLabelTitle variation on title":     "ThemeTypeVariation = \"MenuLabelTitle\"",
		"MenuLabel variation on description":    "ThemeTypeVariation = \"MenuLabel\"",
		"PanelContainer wrapper present":         "var panel = new PanelContainer",
		"Panel anchors on parent (not child)":    "panel.SetAnchor",
	}

	for entry: Variant in checks:
		var label: String = entry as String
		var needle: String = checks[entry] as String
		if src.find(needle) >= 0:
			_pass("FirstBootDialog: " + label)
		else:
			_fail("FirstBootDialog: missing " + label + " (expected: " + needle + ")")

func _check_combat_hud_source() -> void:
	var f := FileAccess.open("res://combat/ui/CombatHUD.cs", FileAccess.READ)
	if f == null:
		_fail("CombatHUD.cs not found")
		return

	var src := f.get_as_text()
	f.close()

	var checks := {
		"Player bar uses theme orange":     "0.9098039f, 0.72156864f, 0.3764706f",
		"Enemy bar uses theme danger pink": "0.8784314f, 0.40784314f, 0.40784314f",
		"Low-HP uses distinct amber":         "0.91f, 0.45f, 0.1f",
		"Enemy dead nameplate themed":        "0.53333336f, 0.53333336f, 0.53333336f",
	}

	for entry: Variant in checks:
		var label: String = entry as String
		var needle: String = checks[entry] as String
		if src.find(needle) >= 0:
			_pass("CombatHUD: " + label)
		else:
			_fail("CombatHUD: missing " + label + " (expected: " + needle + ")")
