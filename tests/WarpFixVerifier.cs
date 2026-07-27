using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

// Exercises the actual C# menu-warp path: GameController.LoadLevel(string, string).
// Runs as the main scene (root Node), awaits LevelLoaded between calls, and
// verifies the player lands PAST the destination HubArrival (not inside it).

public partial class WarpFixVerifier : Node
{
	private readonly List<string> _failures = new();
	private bool _levelLoaded;
	private string _lastLoadedPath = "";

	public override void _Ready()
	{
		_ = RunAsync();
	}

	private async Task RunAsync()
	{
		// Wait for autoloads.
		for (int i = 0; i < 120 && (GameController.Instance == null
			|| Player.Instance == null
			|| GameController.Instance.CurrentLevel == null); i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		var gc = GameController.Instance;
		var pl = Player.Instance;
		if (gc == null || pl == null)
		{
			GD.PrintErr("[v] autoloads missing");
			GetTree().Quit(1);
			return;
		}

		GameController.Instance.LevelLoaded += OnLevelLoaded;

		// Set flags so the door is accessible and the warps show in the menu.
		WorldFlags.Instance.SetFlag("tutorial_clocked_out", true);
		WorldFlags.Instance.SetFlag("warp_overworld_entry", true);
		WorldFlags.Instance.SetFlag("warp_factory_gate", true);
		WorldFlags.Instance.SetFlag("warp_courtyard", true);

		// Test 1: OpeningZone door fires when player walks in.
		await LoadAndAwait(gc, "res://levels/factory/maps/OpeningZone.tscn", Vector2.Zero, 90);
		if (!_lastLoadedPath.Contains("OpeningZone"))
			_failures.Add("Initial OpeningZone load failed: " + _lastLoadedPath);
		else
			GD.Print("[v] OpeningZone loaded");

		pl.GlobalPosition = new Vector2(576, 0);
		await WaitFrames(120, () => _lastLoadedPath.Contains("SortingFloor"));
		if (!_lastLoadedPath.Contains("SortingFloor"))
			_failures.Add("Door to SortingFloor did not fire");
		else
			GD.Print("[v] door to SortingFloor fired");

		// Test 2: The C# string-overload menu path → Overworld.
		await LoadAndAwait(gc, "res://levels/overworld/maps/Overworld.tscn", "HubArrival", 120);
		if (!_lastLoadedPath.Contains("Overworld") || _lastLoadedPath.Contains("OpeningZone"))
			_failures.Add("Warp to Overworld failed: " + _lastLoadedPath);
		else
			GD.Print("[v] C# menu-warp to Overworld: " + _lastLoadedPath);

		var arrival = gc.CurrentLevel?.GetNodeOrNull<LevelTransition>("HubArrival");
		if (arrival == null)
			_failures.Add("Overworld missing HubArrival");
		else
		{
			var dist = arrival.GlobalPosition.DistanceTo(pl.GlobalPosition);
			GD.Print($"[v] Overworld: HubArrival={arrival.GlobalPosition} player={pl.GlobalPosition} dist={dist:F1}");
			if (dist < 16.0f)
				_failures.Add($"Player ON Overworld HubArrival (dist={dist:F1})");

			// Sit for 30 frames; if monitoring were true, BodyEntered would re-fire
			// and the player would bounce back to OpeningZone.
			for (int i = 0; i < 30; i++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				if (_lastLoadedPath != "res://levels/overworld/maps/Overworld.tscn")
				{
					_failures.Add($"BOUNCE from Overworld after {i + 1} frames → {_lastLoadedPath}");
					break;
				}
			}
			if (_lastLoadedPath == "res://levels/overworld/maps/Overworld.tscn")
				GD.Print("[v] Overworld stable (no bounce)");
		}

		// Test 3: Sub-level warp → courtyard.
		await LoadAndAwait(gc, "res://levels/courtyard/maps/courtyard.tscn", "HubArrival", 120);
		if (!_lastLoadedPath.Contains("courtyard"))
			_failures.Add("Warp to courtyard failed: " + _lastLoadedPath);

		var cyArrival = gc.CurrentLevel?.GetNodeOrNull<LevelTransition>("HubArrival");
		if (cyArrival == null)
			_failures.Add("courtyard missing HubArrival");
		else
		{
			var dist = cyArrival.GlobalPosition.DistanceTo(pl.GlobalPosition);
			GD.Print($"[v] courtyard: HubArrival={cyArrival.GlobalPosition} player={pl.GlobalPosition} dist={dist:F1}");
			if (dist < 16.0f)
				_failures.Add($"Player ON courtyard HubArrival (dist={dist:F1})");

			for (int i = 0; i < 30; i++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				if (_lastLoadedPath == null || !_lastLoadedPath.Contains("courtyard"))
				{
					_failures.Add($"BOUNCE from courtyard after {i + 1} frames");
					break;
				}
			}
		}

		// Report.
		if (_failures.Count == 0)
		{
			GD.Print("[v] ALL OK");
			GetTree().Quit(0);
		}
		else
		{
			foreach (var f in _failures) GD.PrintErr("[FAIL] " + f);
			GetTree().Quit(1);
		}
	}

	private void OnLevelLoaded()
	{
		_levelLoaded = true;
		var cl = GameController.Instance?.CurrentLevel;
		_lastLoadedPath = cl?.SceneFilePath ?? "";
	}

	// Calls LoadLevel(string, Vector2) — the (pos) overload.
	private async Task LoadAndAwait(GameController gc, string scenePath, Vector2 pos, int maxFrames)
	{
		_levelLoaded = false;
		gc.LoadLevel(scenePath, pos);
		for (int i = 0; i < maxFrames; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (_levelLoaded && _lastLoadedPath == scenePath) return;
		}
	}

	// Calls LoadLevel(string, string) — the actual C# menu-warp overload.
	private async Task LoadAndAwait(GameController gc, string scenePath, string targetTransitionName, int maxFrames)
	{
		_levelLoaded = false;
		gc.LoadLevel(scenePath, targetTransitionName);
		for (int i = 0; i < maxFrames; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (_levelLoaded && _lastLoadedPath == scenePath) return;
		}
	}

	private async Task WaitFrames(int n, System.Func<bool> pred)
	{
		for (int i = 0; i < n; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (pred()) return;
		}
	}
}
