using Godot;
using System;
using System.Collections.Generic;

// Headless UI theme/structure verifier (C# port of verify_ui_colors.gd).
// Checks theme colors, danger-button variations, FirstBootDialog source
// conventions, and CombatHUD bar colors.
// Run with: godot --headless --path . --script res://tests/VerifyUiTheme.cs

public partial class VerifyUiTheme : SceneTree
{
    private readonly List<string> _failures = new();
    private int _passes;

    public override void _Initialize()
    {
        CheckThemeColors();
        CheckMainMenuQuitButton();
        CheckOverworldMenuQuitButton();
        CheckFirstBootDialogSource();
        CheckCombatHudSource();

        if (_failures.Count > 0)
        {
            foreach (var f in _failures)
                GD.PushError(f);
            GD.PrintErr($"[UI-VERIFY] FAILED — {_failures.Count} failure(s)");
            Quit(1);
        }
        else
        {
            GD.Print($"[UI-VERIFY] All {_passes} checks passed.");
            Quit(0);
        }
    }

    private void Pass(string msg) { _passes++; GD.Print("[UI-VERIFY] PASS: " + msg); }
    private void Fail(string msg) => _failures.Add(msg);

    private void CheckThemeColors()
    {
        var theme = ResourceLoader.Load<Theme>("res://assets/themes/eggbert_theme.tres");
        if (theme == null)
        {
            Fail("Theme resource missing at res://assets/themes/eggbert_theme.tres");
            return;
        }

        var cream = new Color(0.83137256f, 0.76862746f, 0.627451f);
        var danger = new Color(0.8784314f, 0.40784314f, 0.40784314f);

        var btnFc = theme.GetColor("font_color", "MenuButton");
        if (btnFc.IsEqualApprox(cream))
            Pass("Theme MenuButton font_color = cream");
        else
            Fail($"Theme MenuButton font_color is {btnFc}, expected cream");

        var dangerFc = theme.GetColor("font_color", "MenuButtonDanger");
        if (dangerFc.IsEqualApprox(danger))
            Pass("Theme MenuButtonDanger font_color = danger pink");
        else
            Fail($"Theme MenuButtonDanger font_color is {dangerFc}, expected danger pink");
    }

    private void CheckMainMenuQuitButton()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://ui/MainMenu.tscn");
        if (scene == null) { Fail("MainMenu.tscn not found"); return; }
        var menu = scene.Instantiate();
        if (menu == null) { Fail("MainMenu.tscn did not instantiate"); return; }

        var quitBtn = menu.GetNodeOrNull<Button>("MenuPanel/VBoxContainer/QuitButton");
        if (quitBtn == null)
            Fail("MainMenu QuitButton node not found");
        else if (quitBtn.ThemeTypeVariation == "MenuButtonDanger")
            Pass("MainMenu QuitButton theme_type_variation = MenuButtonDanger");
        else
            Fail($"MainMenu QuitButton variation = {quitBtn.ThemeTypeVariation}, expected MenuButtonDanger");
        menu.Free();
    }

    private void CheckOverworldMenuQuitButton()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://ui/OverworldMenu.tscn");
        if (scene == null) { Fail("OverworldMenu.tscn not found"); return; }
        var menu = scene.Instantiate();
        if (menu == null) { Fail("OverworldMenu.tscn did not instantiate"); return; }

        var quitBtn = menu.GetNodeOrNull<Button>("MainPanel/VBoxContainer/GridRow3/QuitButton");
        if (quitBtn == null)
            Fail("OverworldMenu active QuitButton node not found");
        else if (quitBtn.ThemeTypeVariation == "MenuButtonDanger")
            Pass("OverworldMenu active QuitButton theme_type_variation = MenuButtonDanger");
        else
            Fail($"OverworldMenu active QuitButton variation = {quitBtn.ThemeTypeVariation}, expected MenuButtonDanger");
        menu.Free();
    }

    private void CheckFirstBootDialogSource()
    {
        if (!FileAccess.FileExists("res://ui/FirstBootDialog.cs"))
        {
            Fail("FirstBootDialog.cs not found");
            return;
        }
        var src = FileAccess.GetFileAsString("res://ui/FirstBootDialog.cs");

        var checks = new Dictionary<string, string>
        {
            ["MenuButton variation on speed buttons"] = "ThemeTypeVariation = \"MenuButton\"",
            ["MenuLabelTitle variation on title"] = "ThemeTypeVariation = \"MenuLabelTitle\"",
            ["MenuLabel variation on description"] = "ThemeTypeVariation = \"MenuLabel\"",
            ["PanelContainer wrapper present"] = "var panel = new PanelContainer",
            ["Panel anchors on parent (not child)"] = "panel.SetAnchor",
        };

        foreach (var kvp in checks)
        {
            if (src.Contains(kvp.Value, StringComparison.Ordinal))
                Pass("FirstBootDialog: " + kvp.Key);
            else
                Fail("FirstBootDialog: missing " + kvp.Key + " (expected: " + kvp.Value + ")");
        }
    }

    private void CheckCombatHudSource()
    {
        if (!FileAccess.FileExists("res://combat/ui/CombatHUD.cs"))
        {
            Fail("CombatHUD.cs not found");
            return;
        }
        var src = FileAccess.GetFileAsString("res://combat/ui/CombatHUD.cs");

        var checks = new Dictionary<string, string>
        {
            ["Player bar uses theme orange"] = "0.9098039f, 0.72156864f, 0.3764706f",
            ["Enemy bar uses theme danger pink"] = "0.8784314f, 0.40784314f, 0.40784314f",
            ["Low-HP uses distinct amber"] = "0.91f, 0.45f, 0.1f",
            ["Enemy dead nameplate themed"] = "0.53333336f, 0.53333336f, 0.53333336f",
        };

        foreach (var kvp in checks)
        {
            if (src.Contains(kvp.Value, StringComparison.Ordinal))
                Pass("CombatHUD: " + kvp.Key);
            else
                Fail("CombatHUD: missing " + kvp.Key + " (expected: " + kvp.Value + ")");
        }
    }
}
