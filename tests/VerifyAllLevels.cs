using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Headless verifier for ALL level scenes in the project.
//
// Replaces the old VerifySceneLoads.cs, which only called ResourceLoader.Load
// (never Instantiate) and used a hardcoded 15-scene list. That design missed
// the instantiation-time failure class (e.g. "node does not specify its parent
// node") and silently skipped new zones.
//
// What this verifier does:
//   1. AUTO-DISCOVERS every level scene: recursive walk of res://levels,
//      keeping only *.tscn files whose path contains "/maps/" (excludes
//      components: BaseLevel.tscn, LevelTransition.tscn, npcs/, props/,
//      sample/).
//   2. For each level: Load -> Instantiate -> assert root is BaseLevel with a
//      non-empty CoreTilemapLayer used rect. Instantiate() is the critical
//      step — it fails on invalid scene hierarchies that Load() tolerates.
//   3. Transition wiring: every direct-root LevelTransition must point at an
//      existing scene that instantiates and contains a direct-root
//      LevelTransition with the matching TargetTransitionName.
//   4. Warp registration: every WarpDatabase.All entry must resolve to a
//      loadable, instantiable scene containing its TargetTransitionName.
//   5. Engine errors: ResourceLoader.Load returns null and Instantiate throws
//      on malformed scenes, so a failure here means a real scene bug. Check
//      stderr for any ERROR: lines after the run for engine-level noise.
//
// Run: godot --headless --path . --script res://tests/VerifyAllLevels.cs
// Exit: 0 = all checks passed, 1 = at least one failure.

public partial class VerifyAllLevels : SceneTree
{
    private readonly List<string> _failures = new();
    private int _levelsChecked;
    private int _transitionsChecked;
    private int _warpsChecked;

    public override async void _Initialize()
    {
        GD.Print("[verify-all-levels] Discovering level scenes under res://levels ...");

        var scenes = DiscoverLevelScenes();
        GD.Print($"[verify-all-levels] Found {scenes.Count} level scene(s)");

        foreach (var path in scenes)
        {
            await VerifyLevel(path);
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        }

        await VerifyTransitions(scenes);
        await VerifyWarps();

        GD.Print("");
        GD.Print($"[verify-all-levels] {_levelsChecked} levels, " +
                 $"{_transitionsChecked} transitions, {_warpsChecked} warp entries checked");
        if (_failures.Count == 0)
        {
            GD.Print("[verify-all-levels] ALL CHECKS PASSED");
            Quit(0);
        }
        else
        {
            foreach (var f in _failures)
                GD.PrintErr($"[verify-all-levels] FAIL: {f}");
            GD.PrintErr($"[verify-all-levels] {_failures.Count} FAILURE(S)");
            Quit(1);
        }
    }

    // -----------------------------------------------------------------------
    // Discovery
    // -----------------------------------------------------------------------

    /// <summary>
    /// Recursively walk res://levels and return every *.tscn whose path
    /// contains "/maps/" — the project convention for level scenes.
    /// </summary>
    private static List<string> DiscoverLevelScenes()
    {
        var result = new List<string>();
        CollectScenes("res://levels", result);
        result.Sort();
        return result;
    }

    private static void CollectScenes(string dir, List<string> result)
    {
        using var d = DirAccess.Open(dir);
        if (d == null)
            return;

        d.ListDirBegin();
        string name;
        while ((name = d.GetNext()) != "")
        {
            if (name == "." || name == ".." || name.EndsWith(".uid"))
                continue;

            string full = dir + "/" + name;
            if (d.CurrentIsDir())
                CollectScenes(full, result);
            else if (name.EndsWith(".tscn") && full.Contains("/maps/"))
                result.Add(full);
        }
        d.ListDirEnd();
    }

    // -----------------------------------------------------------------------
    // Level checks
    // -----------------------------------------------------------------------

    private async Task VerifyLevel(string path)
    {
        string label = $"[level] {path}";
        _levelsChecked++;

        if (!FileAccess.FileExists(path))
        {
            Fail($"{label} file not found");
            return;
        }

        PackedScene scene;
        try
        {
            scene = ResourceLoader.Load<PackedScene>(path);
        }
        catch (Exception e)
        {
            Fail($"{label} ResourceLoader.Load threw: {e.Message}");
            return;
        }

        if (scene == null)
        {
            Fail($"{label} could not load PackedScene (parse error / missing ext_resource)");
            return;
        }

        Node root;
        try
        {
            root = scene.Instantiate();
        }
        catch (Exception e)
        {
            Fail($"{label} Instantiate threw (invalid scene hierarchy): {e.Message}");
            return;
        }

        if (root == null)
        {
            Fail($"{label} Instantiate returned null");
            return;
        }

        if (root is not BaseLevel)
        {
            Fail($"{label} root '{root.Name}' is not BaseLevel");
            root.Free();
            return;
        }

        // Camera bounds come from LevelTileMapLayer._Ready -> GameController.
        // Search the whole tree (some scenes nest it, e.g. OpeningZone puts it
        // under WarpPoint) — what matters is that one exists with tile data.
        var boundsLayer = FindLevelTileMapLayer(root);
        if (boundsLayer == null)
        {
            Fail($"{label} no LevelTileMapLayer anywhere in scene (camera bounds will never update)");
        }
        else if (boundsLayer.GetUsedRect().Size == Vector2I.Zero)
        {
            Fail($"{label} LevelTileMapLayer '{boundsLayer.Name}' has empty used rect");
        }
        else if (boundsLayer.GetParent() != root)
        {
            GD.Print($"{label} WARN: LevelTileMapLayer '{boundsLayer.Name}' is nested under '{boundsLayer.GetParent().Name}' (convention: direct root)");
        }

        GD.Print($"{label} OK ({root.Name}, {(root as BaseLevel)?.LevelName ?? "?"})");
        root.Free();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
    }

    private static LevelTileMapLayer FindLevelTileMapLayer(Node node)
    {
        if (node is LevelTileMapLayer layer)
            return layer;
        foreach (Node child in node.GetChildren())
        {
            var found = FindLevelTileMapLayer(child);
            if (found != null)
                return found;
        }
        return null;
    }

    // -----------------------------------------------------------------------
    // Transition wiring
    // -----------------------------------------------------------------------

    private async Task VerifyTransitions(List<string> scenes)
    {
        GD.Print("");
        GD.Print("[verify-all-levels] Verifying transition wiring ...");

        foreach (var path in scenes)
        {
            var scene = ResourceLoader.Load<PackedScene>(path);
            var root = scene?.Instantiate() as Node2D;
            if (root == null)
                continue;

            foreach (Node child in root.GetChildren())
            {
                if (child is not LevelTransition t)
                    continue;

                _transitionsChecked++;
                string label = $"[transition] {path}/{t.Name}";

                if (string.IsNullOrEmpty(t.Level))
                {
                    Fail($"{label} has empty Level");
                    continue;
                }
                if (!FileAccess.FileExists(t.Level))
                {
                    Fail($"{label} target '{t.Level}' does not exist");
                    continue;
                }
                if (string.IsNullOrEmpty(t.TargetTransitionName))
                {
                    Fail($"{label} has empty TargetTransitionName");
                    continue;
                }

                var targetScene = ResourceLoader.Load<PackedScene>(t.Level);
                var targetRoot = targetScene?.Instantiate() as Node2D;
                if (targetRoot == null)
                {
                    Fail($"{label} target '{t.Level}' cannot load/instantiate");
                    continue;
                }

                var target = targetRoot.GetNodeOrNull<LevelTransition>(t.TargetTransitionName);
                if (target == null)
                    Fail($"{label} -> {t.Level}/{t.TargetTransitionName} NOT FOUND");
                else
                    GD.Print($"{label} -> {t.Level}/{t.TargetTransitionName} OK");

                targetRoot.Free();
                await ToSignal(this, SceneTree.SignalName.ProcessFrame);
            }

            root.Free();
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        }
    }

    // -----------------------------------------------------------------------
    // Warp registration
    // -----------------------------------------------------------------------

    private async Task VerifyWarps()
    {
        GD.Print("");
        GD.Print("[verify-all-levels] Verifying WarpDatabase registration ...");

        foreach (var kvp in WarpDatabase.All)
        {
            _warpsChecked++;
            string id = kvp.Key;
            var dest = kvp.Value;
            string label = $"[warp] {id}";

            if (!FileAccess.FileExists(dest.LevelPath))
            {
                Fail($"{label} LevelPath missing: {dest.LevelPath}");
                continue;
            }

            var packed = ResourceLoader.Load<PackedScene>(dest.LevelPath);
            var root = packed?.Instantiate() as Node2D;
            if (root == null)
            {
                Fail($"{label} destination '{dest.LevelPath}' cannot load/instantiate");
                continue;
            }

            if (root.GetNodeOrNull<LevelTransition>(dest.TargetTransitionName) == null)
                Fail($"{label} TargetTransitionName '{dest.TargetTransitionName}' not found in {dest.LevelPath}");
            else
                GD.Print($"{label} -> {dest.LevelPath}/{dest.TargetTransitionName} OK");

            root.Free();
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        }
    }

    private void Fail(string msg) => _failures.Add(msg);
}
