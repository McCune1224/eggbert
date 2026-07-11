using Godot;
using Godot.Collections;

/// <summary>
/// Global world-state flags for dialog branching, quest progression, warp unlocks, etc.
/// Autoload singleton — access via WorldFlags.Instance.
/// </summary>
public partial class WorldFlags : Node, ISavable
{
    private static WorldFlags _instance;
    public static WorldFlags Instance => _instance;

    private Dictionary<string, Variant> _flags = new();

    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            GD.PrintErr("Multiple instances of WorldFlags detected!");
            QueueFree();
            return;
        }

        AddToGroup("persist");
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>Set a flag to a value (bool, int, string, etc.).</summary>
    public void SetFlag(string key, Variant value)
    {
        _flags[key] = value;
    }

    /// <summary>Get a flag value, or default if not set.</summary>
    public Variant GetFlag(string key, Variant defaultValue = default)
    {
        return _flags.TryGetValue(key, out Variant value) ? value : defaultValue;
    }

    /// <summary>Check if a boolean flag is set and true.</summary>
    public bool HasFlag(string key)
    {
        return _flags.TryGetValue(key, out Variant value) && value.AsBool();
    }

    /// <summary>Remove a flag entirely.</summary>
    public void ClearFlag(string key)
    {
        _flags.Remove(key);
    }

    /// <summary>Remove all flags (used when starting a new game).</summary>
    public void ClearAll()
    {
        _flags.Clear();
    }

    // ponytail: Dictionary serializes via Godot's native Variant serialization in .tres
    public SaveResource Save(SaveResource newSave)
    {
        newSave.WorldFlagsData = new SaveDataWorldFlags
        {
            Flags = new Dictionary<string, Variant>(_flags)
        };
        return newSave;
    }

    public void Load(SaveResource data)
    {
        if (data.WorldFlagsData?.Flags != null)
        {
            _flags = new Dictionary<string, Variant>(data.WorldFlagsData.Flags);
        }
    }
}
