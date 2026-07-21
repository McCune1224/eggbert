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

    [Signal]
    public delegate void StateChangedEventHandler();

    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            GameLogger.Error("WorldFlags", "Multiple instances of WorldFlags detected!");
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
        GameLogger.Debug("WorldFlags", $"Flag set: {key} = {value}");
        EmitSignal(SignalName.StateChanged);
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
        if (_flags.Remove(key))
        {
            GameLogger.Debug("WorldFlags", $"Flag cleared: {key}");
            EmitSignal(SignalName.StateChanged);
        }
    }

    /// <summary>Return all flags (for debug overlay / inspection).</summary>
    public Dictionary<string, Variant> GetAllFlags()
    {
        return new Dictionary<string, Variant>(_flags);
    }

    /// <summary>Remove all flags (used when starting a new game).</summary>
    public void ClearAll()
    {
        _flags.Clear();
        GameLogger.Info("WorldFlags", "All flags cleared.");
        EmitSignal(SignalName.StateChanged);
    }

    public string SaveKey => "world_flags";

    public Godot.Collections.Dictionary<string, Variant> Serialize()
    {
        GameLogger.Debug("WorldFlags", $"Serialize: {_flags.Count} flags");
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["flags"] = new Godot.Collections.Dictionary<string, Variant>(_flags)
        };
    }

    public void Deserialize(Godot.Collections.Dictionary<string, Variant> data)
    {
        if (data.TryGetValue("flags", out var flagsVar))
        {
            _flags = new Godot.Collections.Dictionary<string, Variant>(flagsVar.AsGodotDictionary());
            GameLogger.Debug("WorldFlags", $"Deserialize: loaded {_flags.Count} flags");
            EmitSignal(SignalName.StateChanged);
        }
        else
        {
            GameLogger.Warn("WorldFlags", "Deserialize: no 'flags' key found — starting fresh");
        }
    }
}
