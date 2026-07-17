using Godot;
using Godot.Collections;

/// <summary>
/// Interface for components that can be saved/loaded via SaveManager.
/// Each implementer provides a unique SaveKey and self-serializes to/from a Dictionary.
/// </summary>
public interface ISavable
{
    /// <summary>
    /// Unique key for this component's data in the save file.
    /// Examples: "player", "inventory", "equipment", "world_flags"
    /// </summary>
    string SaveKey { get; }

    /// <summary>
    /// Serializes the current state into a Dictionary for storage.
    /// </summary>
    Dictionary<string, Variant> Serialize();

    /// <summary>
    /// Restores state from a previously serialized Dictionary.
    /// </summary>
    void Deserialize(Dictionary<string, Variant> data);

    /// <summary>
    /// Gets the load priority. Higher values load first.
    /// Player (10) must load before Equipment (5), Inventory (0), WorldFlags (0).
    /// </summary>
    public int GetLoadPriority()
    {
        return 0;
    }
}
