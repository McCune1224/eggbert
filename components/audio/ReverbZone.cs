using Godot;

/// <summary>
/// Area that applies reverb audio effect when the player enters.
/// </summary>
public partial class ReverbZone : Area2D
{
    [Export] public float ReverbWet { get; set; } = 0.4f;
    [Export] public float ReverbDry { get; set; } = 0.6f;
    [Export] public float ReverbRoomSize { get; set; } = 0.6f;

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
