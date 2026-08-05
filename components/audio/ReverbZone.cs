using Godot;

/// <summary>
/// Area that applies reverb audio effect when the player enters.
/// </summary>
public partial class ReverbZone : Area2D
{
    /// <summary>How much of the wet (echoed) signal is mixed in. 0 = none, 1 = full.</summary>
    [ExportGroup("Reverb")]
    [Export(PropertyHint.Range, "0,1,0.05")] public float ReverbWet { get; set; } = 0.4f;
    /// <summary>How much of the dry (direct) signal is kept. 0 = none, 1 = full.</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float ReverbDry { get; set; } = 0.6f;
    /// <summary>Simulated room size. Larger = longer, boomier echo.</summary>
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float ReverbRoomSize { get; set; } = 0.6f;

    private AudioEffectReverb _reverb;
    private int _reverbIndex = -1;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer;

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;

        GameLogger.Debug("ReverbZone", $"'{Name}': player entered");
        AddReverb();
    }

    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;

        GameLogger.Debug("ReverbZone", $"'{Name}': player exited");
        RemoveReverb();
    }

    private void AddReverb()
    {
        if (_reverbIndex >= 0) return;

        _reverbIndex = AudioServer.GetBusIndex("SFX");
        if (_reverbIndex < 0)
        {
            GameLogger.Error("ReverbZone", $"'{Name}': SFX bus not found!");
            return;
        }

        _reverb = new AudioEffectReverb
        {
            Wet = ReverbWet,
            Dry = ReverbDry,
            RoomSize = ReverbRoomSize
        };

        AudioServer.AddBusEffect(_reverbIndex, _reverb, 0);
        GameLogger.Info("ReverbZone", $"'{Name}': reverb added (wet={ReverbWet}, dry={ReverbDry}, room={ReverbRoomSize})");
    }

    private void RemoveReverb()
    {
        if (_reverbIndex < 0 || _reverb == null) return;

        AudioServer.RemoveBusEffect(_reverbIndex, 0);
        GameLogger.Info("ReverbZone", $"'{Name}': reverb removed");

        _reverbIndex = -1;
        _reverb = null;
    }
}
