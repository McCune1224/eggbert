using Godot;
using System.Collections.Generic;

public struct WarpDestination
{
    public string Id;
    public string Name;
    public string LevelPath;
    public Vector2 Position;
}

public static class WarpDatabase
{
    public static readonly Dictionary<string, WarpDestination> All = new()
    {
        { "the_great_beyond", new WarpDestination
            { Id = "the_great_beyond", Name = "The Great Beyond",
              LevelPath = "res://levels/overworld/maps/TheGreatBeyond.tscn", Position = Vector2.Zero } },
        { "courtyard", new WarpDestination
            { Id = "courtyard", Name = "Courtyard",
              LevelPath = "res://levels/overworld/maps/courtyard.tscn", Position = Vector2.Zero } },
    };

    public static bool IsUnlocked(string id) =>
        WorldFlags.Instance.HasFlag($"warp_{id}");

    public static void Unlock(string id) =>
        WorldFlags.Instance.SetFlag($"warp_{id}", true);

    public static List<WarpDestination> GetUnlocked()
    {
        var result = new List<WarpDestination>();
        foreach (var kvp in All)
            if (IsUnlocked(kvp.Key))
                result.Add(kvp.Value);
        return result;
    }
}
