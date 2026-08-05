using Godot;
using System;
using System.Collections.Generic;

// Headless generator for canonical dev save-state fixtures (tests/savestates/*.tres).
//
// For each fixture spec it: boots a real level, sets up story flags/inventory,
// saves the state to a named user slot via SaveManager, then copies the result
// into res://tests/savestates/ so every dev/agent machine can load the same
// checkpoints (EGGBERT_LOAD_STATE=<name>).
//
// Run: godot --headless --path . --script res://tests/GenerateSaveStateFixtures.cs
// NOTE: this script WRITES into res://tests/savestates/ — commit the .tres outputs.

public partial class GenerateSaveStateFixtures : SceneTree
{
    private struct FixtureSpec
    {
        public string Name;
        public string ScenePath;
        public string TransitionName; // "" = spawn at Vector2.Zero
        public string LocationName;
        public string[] Flags;
        public string[] Items;
    }

    private readonly List<FixtureSpec> _specs = new()
    {
        new FixtureSpec
        {
            Name = "mid-factory",
            ScenePath = "res://levels/factory/maps/OpeningZone.tscn",
            TransitionName = "",
            LocationName = "Factory — Opening Zone",
            Flags = new[] { "tutorial_clocked_out", "met_jamitor" },
            Items = new[] { "rusty_key", "hardboiled_egg" }
        },
        new FixtureSpec
        {
            Name = "post-arrest-eggsile",
            ScenePath = "res://levels/eggsile/maps/EggsIsle.tscn",
            TransitionName = "HubArrival",
            LocationName = "Eggs Isle — Arrival",
            Flags = new[] { "arrested", "cutscene_arrest", "warp_eggsile_area1" },
            Items = new[] { "rusty_key", "cell_key", "hardboiled_egg" }
        }
    };

    private int _frames;
    private int _specIndex;
    private bool _loadingLevel;
    private int _failures;

    public override void _Initialize()
    {
        GameLogger.InitializeFromEnv();
        GD.Print("[gen-fixtures] Starting — ensure res://tests/savestates/ exists");
        DirAccess.MakeDirRecursiveAbsolute("res://tests/savestates");
    }

    public override bool _Process(double delta)
    {
        _frames++;
        if (_frames < 3) return false; // let autoloads settle

        if (!_loadingLevel)
        {
            if (_specIndex >= _specs.Count)
            {
                GD.Print(_failures == 0
                    ? $"[gen-fixtures] ALL FIXTURES GENERATED ({_specs.Count})"
                    : $"[gen-fixtures] DONE with {_failures} FAILURE(S)");
                Quit(_failures == 0 ? 0 : 1);
                return true;
            }

            var spec = _specs[_specIndex];
            GD.Print($"[gen-fixtures] === Fixture {_specIndex + 1}/{_specs.Count}: '{spec.Name}' → {spec.ScenePath} ===");

            // Fresh story state for this fixture.
            WorldFlags.Instance.ClearAll();
            foreach (string flag in spec.Flags)
                WorldFlags.Instance.SetFlag(flag, true);

            // Load the level so Player.Serialize records the correct scene path.
            _loadingLevel = true;
            GameController.Instance.LevelLoaded += OnLevelLoaded;
            if (string.IsNullOrEmpty(spec.TransitionName))
                GameController.Instance.LoadLevel(spec.ScenePath, Vector2.Zero);
            else
                GameController.Instance.LoadLevel(spec.ScenePath, spec.TransitionName);
        }

        return false;
    }

    private async void OnLevelLoaded()
    {
        GameController.Instance.LevelLoaded -= OnLevelLoaded;
        var spec = _specs[_specIndex];

        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        // Ensure test items exist for this fixture's inventory state.
        foreach (string item in spec.Items)
        {
            if (!Inventory.Instance.Has(item))
                Inventory.Instance.Add(item);
        }

        Vector2 pos = Player.Instance.Position;
        SaveManager.Instance.SaveGame(spec.ScenePath, pos, spec.LocationName, spec.Name);

        // Copy the user:// slot into the repo fixtures dir (read-only committed copies).
        string from = $"user://saves/{SaveManager.SanitizeSlotName(spec.Name)}.tres";
        string to = $"res://tests/savestates/{SaveManager.SanitizeSlotName(spec.Name)}.tres";
        Error err = DirAccess.CopyAbsolute(from, to);
        if (err == Error.Ok)
        {
            GD.Print($"[gen-fixtures] ✔ '{spec.Name}' → {to} (pos={pos})");
        }
        else
        {
            GD.PrintErr($"[gen-fixtures] ✘ CopyAbsolute({from} → {to}) failed: {err}");
            _failures++;
        }

        _specIndex++;
        _loadingLevel = false;
    }
}
