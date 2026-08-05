using Godot;

/// <summary>
/// Plays a brief musical sting when entering a new zone.
/// Attach to a BaseLevel's stingerSfx export or add as a child.
/// </summary>
public partial class ZoneStinger : AudioStreamPlayer2D
{
    /// <summary>The short musical sting to play when the zone is entered.</summary>
    [Export] public AudioStream StingerSfx { get; set; }

    public override void _Ready()
    {
        Bus = "SFX";
        if (StingerSfx != null)
        {
            Stream = StingerSfx;
            Play();
            GameLogger.Debug("ZoneStinger", $"'{Name}': playing stinger");
        }
        else
        {
            GameLogger.Warn("ZoneStinger", $"'{Name}': StingerSfx is null — nothing to play");
        }
    }
}
