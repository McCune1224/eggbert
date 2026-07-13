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
        { "overworld_entry", new WarpDestination
            { Id = "overworld_entry", Name = "Overworld",
              LevelPath = "res://levels/overworld/maps/Overworld.tscn", Position = Vector2.Zero } },
        { "the_great_beyond", new WarpDestination
            { Id = "the_great_beyond", Name = "The Great Beyond",
              LevelPath = "res://levels/overworld/maps/TheGreatBeyond.tscn", Position = Vector2.Zero } },
        { "courtyard", new WarpDestination
            { Id = "courtyard", Name = "Courtyard",
              LevelPath = "res://levels/courtyard/maps/courtyard.tscn", Position = Vector2.Zero } },
        { "eggsile_area1", new WarpDestination
            { Id = "eggsile_area1", Name = "Eggsile — Area 1",
              LevelPath = "res://levels/eggsile/maps/area1.tscn", Position = Vector2.Zero } },
        { "prison", new WarpDestination
            { Id = "prison", Name = "Prison",
              LevelPath = "res://levels/prison/maps/prison.tscn", Position = Vector2.Zero } },
        { "factory_gate", new WarpDestination
            { Id = "factory_gate", Name = "Factory Gate",
              LevelPath = "res://levels/factory/maps/OpeningZone.tscn", Position = Vector2.Zero } },
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
