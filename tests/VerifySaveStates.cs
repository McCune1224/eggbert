using Godot;
using System;
using System.Collections.Generic;

// Headless verifier for the dev save-state system (issue #168).
//
// Two parts:
//   1. ROUND-TRIP: capture the current state to a temp slot, mutate WorldFlags +
//      Inventory, load the slot back, and assert the state was restored.
//   2. FIXTURES: every res://tests/savestates/*.tres must load as a SaveFile with
//      a supported SchemaVersion whose SavePointScenePath resolves and instantiates.
//
// Run: godot --headless --path . --script res://tests/VerifySaveStates.cs
// Exit: 0 = all checks passed, 1 = at least one failure.

public partial class VerifySaveStates : SceneTree
{
    private readonly List<string> _failures = new();
    private int _fixturesChecked;
    private int _frames;
    private bool _started;

    private const string TestSlot = "verify_roundtrip";
    private const string TestScenePath = "res://levels/factory/maps/OpeningZone.tscn";

    public override void _Initialize()
    {
        GameLogger.InitializeFromEnv();
        GD.Print("[verify-save-states] Starting...");
    }

    public override bool _Process(double delta)
    {
        _frames++;
        // Autoloads are not guaranteed ready in _Initialize; wait a few frames
        // and null-guard singletons (same defensive pattern as SmokeFactoryDemo).
        if (!_started && _frames >= 3 &&
            WorldFlags.Instance != null &&
            GameController.Instance != null &&
            SaveManager.Instance != null)
        {
            _started = true;
            _ = RunAll();
        }

        // Watchdog: if RunAll hasn't finished after 60s of frames, fail loudly
        // instead of hanging the headless run.
        if (_started && _frames > 60 * 60)
        {
            GD.PrintErr("[verify-save-states] TIMEOUT — RunAll never finished");
            _failures.Add("Verifier timed out (60s watchdog)");
            Finish();
        }
        return false;
    }

    private async System.Threading.Tasks.Task RunAll()
    {
        await RunCapture();
        if (_failures.Count > 0)
        {
            Finish();
            return;
        }

        RunMutate();
        await RunLoad();
        if (_failures.Count > 0)
        {
            Finish();
            return;
        }

        RunVerify();
        await RunFixtureChecks();
        Finish();
    }

    // --- 1. Round-trip ---

    private async System.Threading.Tasks.Task RunCapture()
    {
        GD.Print("[verify-save-states] Phase 1: capture current state to temp slot");

        // Known baseline so the round-trip assertion is meaningful.
        WorldFlags.Instance.ClearAll();
        WorldFlags.Instance.SetFlag("verify_roundtrip_marker", true);
        WorldFlags.Instance.SetFlag("warp_test_unlock", true);
        if (!Inventory.Instance.Has("rusty_key"))
            Inventory.Instance.Add("rusty_key");

        // Load the level so Player.Serialize records the correct scene path.
        var levelLoaded = ToSignal(GameController.Instance, GameController.SignalName.LevelLoaded);
        GameController.Instance.LoadLevel(TestScenePath, new Vector2(64, 32));
        await levelLoaded;

        SaveManager.Instance.SaveGame(TestScenePath, Player.Instance.Position, "Verify Roundtrip", TestSlot);
        GD.Print($"[verify-save-states] Captured to slot '{TestSlot}' at {Player.Instance.Position}");

        if (!SaveManager.Instance.HasSave(TestSlot))
            _failures.Add($"Slot '{TestSlot}' missing immediately after capture");
    }

    private void RunMutate()
    {
        GD.Print("[verify-save-states] Phase 2: mutate state (clear flags, strip inventory)");
        WorldFlags.Instance.ClearAll();
        if (Inventory.Instance.Has("rusty_key"))
            Inventory.Instance.Remove("rusty_key");
    }

    private async System.Threading.Tasks.Task RunLoad()
    {
        GD.Print("[verify-save-states] Phase 3: load slot back");

        // Connect before LoadGame: Player.Deserialize triggers LoadLevel (async).
        var levelLoaded = ToSignal(GameController.Instance, GameController.SignalName.LevelLoaded);
        bool loaded = SaveManager.Instance.LoadGame(TestSlot);
        if (!loaded)
        {
            _failures.Add($"LoadGame('{TestSlot}') returned false");
            return;
        }

        await levelLoaded;
        GD.Print("[verify-save-states] Level reloaded after slot load");
    }

    private void RunVerify()
    {
        GD.Print("[verify-save-states] Phase 4: assert restored state");
        bool marker = WorldFlags.Instance.HasFlag("verify_roundtrip_marker");
        bool warp = WorldFlags.Instance.HasFlag("warp_test_unlock");
        bool key = Inventory.Instance.Has("rusty_key");

        GD.Print($"[verify-save-states]   marker={marker} warp={warp} rusty_key={key}");
        if (!marker) _failures.Add("WorldFlags marker not restored after load");
        if (!warp) _failures.Add("WorldFlags warp flag not restored after load");
        if (!key) _failures.Add("Inventory item not restored after load");

        // Cleanup
        SaveManager.Instance.DeleteSave(TestSlot);
    }

    // --- 2. Fixtures ---

    private async System.Threading.Tasks.Task RunFixtureChecks()
    {
        GD.Print("[verify-save-states] Phase 5: validate committed fixtures");

        var fixtureNames = SaveManager.Instance.ListFixtures();
        GD.Print($"[verify-save-states] Found {fixtureNames.Count} fixture(s) in res://tests/savestates/");
        if (fixtureNames.Count == 0)
        {
            _failures.Add("No fixtures found in res://tests/savestates/ — run GenerateSaveStateFixtures.cs");
            return;
        }

        foreach (string name in fixtureNames)
        {
            string path = $"res://tests/savestates/{name}.tres";
            var res = ResourceLoader.Load(path);
            if (res is not SaveFile saveFile)
            {
                _failures.Add($"Fixture '{path}' is not a SaveFile ({res?.GetType().Name})");
                continue;
            }

            if (saveFile.SchemaVersion > SaveFile.CurrentSchemaVersion)
            {
                _failures.Add($"Fixture '{path}' SchemaVersion {saveFile.SchemaVersion} > supported {SaveFile.CurrentSchemaVersion}");
                continue;
            }

            if (string.IsNullOrEmpty(saveFile.SavePointScenePath))
            {
                _failures.Add($"Fixture '{path}' has empty SavePointScenePath");
                continue;
            }

            var packed = ResourceLoader.Load<PackedScene>(saveFile.SavePointScenePath);
            if (packed == null)
            {
                _failures.Add($"Fixture '{path}' scene '{saveFile.SavePointScenePath}' failed to load");
                continue;
            }

            var instance = packed.Instantiate();
            if (instance == null)
            {
                _failures.Add($"Fixture '{path}' scene '{saveFile.SavePointScenePath}' failed to instantiate");
                continue;
            }
            instance.QueueFree();

            _fixturesChecked++;
            GD.Print($"[verify-save-states]   ✔ '{name}' → {saveFile.SavePointScenePath} (loc: {saveFile.LocationName})");
        }
    }

    private void Finish()
    {
        GD.Print($"[verify-save-states] Fixtures checked: {_fixturesChecked}");
        if (_failures.Count == 0)
        {
            GD.Print("[verify-save-states] ALL SAVE-STATE CHECKS PASSED");
            Quit(0);
        }
        else
        {
            foreach (string failure in _failures)
                GD.PrintErr($"[verify-save-states] FAIL: {failure}");
            GD.PrintErr($"[verify-save-states] {_failures.Count} FAILURE(S)");
            Quit(1);
        }
    }
}
