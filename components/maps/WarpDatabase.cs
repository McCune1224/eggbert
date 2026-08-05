using Godot;
using System.Collections.Generic;
/// <summary>
/// Static registry of WarpPoint definitions, keyed by warp id.
/// Code-defined entries — no .tres files needed. Add new entries
/// here when adding a new warp point to a level.
/// </summary>

public struct WarpDestination
{
    public string Id;
    public string Name;
    public string LevelPath;
    /// <summary>
    /// Name of a direct-root LevelTransition in the destination scene.
    /// LoadLevel uses this to place the player just past the transition per its Side,
    /// never at the raw transition position (which would re-fire the trigger).
    /// </summary>
    public string TargetTransitionName;
}

public static class WarpDatabase
{
    public static readonly Dictionary<string, WarpDestination> All = new()
    {
        // Only the tutorial-chain warps remain: the factory gate (editor traversal /
        // New Game reference) and the Eggs Isle intake, unlocked by the arrest handoff
        // (FactoryOpeningFlow). All zone warps were removed with their levels in the
        // 2026-08 test-content cleanup (issue #174).
        { "factory_gate", new WarpDestination
            { Id = "factory_gate", Name = "Factory Gate",
              LevelPath = "res://levels/factory/maps/OpeningZone.tscn", TargetTransitionName = "HubArrival" } },
        { "eggsile_area1", new WarpDestination
            { Id = "eggsile_area1", Name = "Eggsile — Area 1",
              LevelPath = "res://levels/eggsile/maps/EggsIsle.tscn", TargetTransitionName = "HubArrival" } },
    };
    public static bool IsUnlocked(string id) =>
        WorldFlags.Instance.HasFlag($"warp_{id}");

    public static void Unlock(string id)
    {
        WorldFlags.Instance.SetFlag($"warp_{id}", true);
        GameLogger.Info("WarpDatabase", $"Unlocked: '{id}'");
    }

    public static List<WarpDestination> GetUnlocked()
    {
        var result = new List<WarpDestination>();
        foreach (var kvp in All)
            if (IsUnlocked(kvp.Key))
                result.Add(kvp.Value);
        GameLogger.Debug("WarpDatabase", $"GetUnlocked: {result.Count}/{All.Count} warps available");
        return result;
    }
}
