using Godot;
using System;

// Headless runtime smoke test: boot the Factory tutorial OpeningZone as the
// current level (as MainMenu does for New Game), let the tree process for a
// few frames, and report any engine errors or missing-node failures.
// Run: godot --headless --path . --script res://tests/SmokeFactoryDemo.cs

public partial class SmokeFactoryDemo : SceneTree
{
    private int _frames;
    private bool _levelLoaded;

    public override void _Initialize()
    {
        GameLogger.InitializeFromEnv();
        GD.Print("[smoke-factory] Booting OpeningZone...");
    }

    public override bool _Process(double delta)
    {
        _frames++;
        if (_frames == 2 && WorldFlags.Instance != null)
        {
            WorldFlags.Instance.ClearAll();
            GameController.Instance.LevelLoaded += () => _levelLoaded = true;
            GameController.Instance.LoadLevel("res://levels/factory/maps/OpeningZone.tscn", Vector2.Zero);
        }

        if (_levelLoaded && _frames > 30)
        {
            var level = GameController.Instance.CurrentLevel;
            GD.Print($"[smoke-factory] Level '{level?.Name}' loaded OK after {_frames} frames");
            GD.Print($"[smoke-factory] Player at {Player.Instance.Position}, HP {Player.Instance.HealthComponent?.CurrentHP}");
            GD.Print("[smoke-factory] SMOKE PASSED");
            Quit(0);
            return true;
        }

        if (_frames > 240)
        {
            GD.PrintErr("[smoke-factory] Timed out waiting for level load");
            Quit(1);
            return true;
        }

        return false;
    }
}
