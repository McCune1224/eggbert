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
        { "overworld_entry", new WarpDestination
            { Id = "overworld_entry", Name = "Overworld",
              LevelPath = "res://levels/overworld/maps/Overworld.tscn", TargetTransitionName = "HubArrival" } },
        { "the_great_beyond", new WarpDestination
            { Id = "the_great_beyond", Name = "The Great Beyond",
              LevelPath = "res://levels/overworld/maps/TheGreatBeyond.tscn", TargetTransitionName = "HubArrival" } },
        { "courtyard", new WarpDestination
            { Id = "courtyard", Name = "Courtyard",
              LevelPath = "res://levels/courtyard/maps/courtyard.tscn", TargetTransitionName = "HubArrival" } },
        { "eggsile_area1", new WarpDestination
            { Id = "eggsile_area1", Name = "Eggsile — Area 1",
              LevelPath = "res://levels/eggsile/maps/area1.tscn", TargetTransitionName = "HubArrival" } },
        { "prison", new WarpDestination
            { Id = "prison", Name = "Prison",
              LevelPath = "res://levels/prison/maps/prison.tscn", TargetTransitionName = "HubArrival" } },
        { "factory_gate", new WarpDestination
            { Id = "factory_gate", Name = "Factory Gate",
              LevelPath = "res://levels/factory/maps/OpeningZone.tscn", TargetTransitionName = "HubArrival" } },
        { "courtyard_depths", new WarpDestination
            { Id = "courtyard_depths", Name = "Courtyard Depths",
              LevelPath = "res://levels/courtyard/maps/CourtyardDepths.tscn", TargetTransitionName = "HubArrival" } },
        { "prison_block_c", new WarpDestination
            { Id = "prison_block_c", Name = "Prison Block C",
              LevelPath = "res://levels/prison/maps/PrisonBlockC.tscn", TargetTransitionName = "HubArrival" } },
        { "kitchen", new WarpDestination
            { Id = "kitchen", Name = "Kitchen",
              LevelPath = "res://levels/kitchen/maps/Kitchen.tscn", TargetTransitionName = "HubArrival" } },
        { "wardens_quarters", new WarpDestination
            { Id = "wardens_quarters", Name = "Warden's Quarters",
              LevelPath = "res://levels/warden/maps/WardensQuarters.tscn", TargetTransitionName = "HubArrival" } },
        { "rec_room", new WarpDestination
            { Id = "rec_room", Name = "Rec Room",
              LevelPath = "res://levels/recroom/maps/RecRoom.tscn", TargetTransitionName = "HubArrival" } },
        { "secret_tunnels", new WarpDestination
            { Id = "secret_tunnels", Name = "Secret Tunnels",
              LevelPath = "res://levels/tunnels/maps/SecretTunnels.tscn", TargetTransitionName = "HubArrival" } },
        { "sunnyside_shrine", new WarpDestination
            { Id = "sunnyside_shrine", Name = "Sunnyside Shrine",
              LevelPath = "res://levels/shrine/maps/SunnysideShrine.tscn", TargetTransitionName = "HubArrival" } },
        { "solitary", new WarpDestination
            { Id = "solitary", Name = "Solitary",
              LevelPath = "res://levels/solitary/maps/Solitary.tscn", TargetTransitionName = "HubArrival" } },
        { "prison_tunnels", new WarpDestination
            { Id = "prison_tunnels", Name = "Prison Tunnels",
              LevelPath = "res://levels/prison/maps/prison.tscn", TargetTransitionName = "HubArrival" } },
        { "eggsile_sewers", new WarpDestination
            { Id = "eggsile_sewers", Name = "Eggsile Sewers",
              LevelPath = "res://levels/eggsile/maps/EggsileSewers.tscn", TargetTransitionName = "HubArrival" } },
        { "sandbox_hub", new WarpDestination
            { Id = "sandbox_hub", Name = "Sandbox Hub",
              LevelPath = "res://levels/sandbox/maps/SandboxHub.tscn", TargetTransitionName = "NorthToGrasslands" } },
        { "sandbox_grasslands", new WarpDestination
            { Id = "sandbox_grasslands", Name = "Sandbox Grasslands",
              LevelPath = "res://levels/sandbox/maps/SandboxGrasslands.tscn", TargetTransitionName = "SouthToHub" } },
        { "sandbox_depths", new WarpDestination
            { Id = "sandbox_depths", Name = "Sandbox Depths",
              LevelPath = "res://levels/sandbox/maps/SandboxDepths.tscn", TargetTransitionName = "EastToHub" } },
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
