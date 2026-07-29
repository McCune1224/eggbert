using Godot;
using Godot.Collections;
/// <summary>
/// Serializable data-transfer resource for a single save game.
/// Contains the save point location, playtime timestamp, and a
/// dictionary of ISavable component data keyed by SaveKey.
/// </summary>

[GlobalClass]
public partial class SaveFile : Resource
{
    [Export]
    public string SavePointScenePath { get; set; } = "";

    [Export]
    public Vector2 SavePointPosition { get; set; }

    [Export]
    public string LocationName { get; set; } = "";

    [Export]
    public double SaveTimestamp { get; set; }

    [Export]
    public double PlayTimeSeconds { get; set; }

    [Export]
    public Dictionary<string, Dictionary<string, Variant>> ComponentData { get; set; } = new();
}
