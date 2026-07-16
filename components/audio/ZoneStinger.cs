using Godot;

/// <summary>
/// Plays a brief musical sting when entering a new zone.
/// Attach to a BaseLevel's stingerSfx export or add as a child.
/// </summary>
public partial class ZoneStinger : AudioStreamPlayer2D
{
    [Export] public AudioStream StingerSfx { get; set; }

    public override void _Ready()
    {
        Bus = "SFX";
        if (StingerSfx != null)
        {
            Stream = StingerSfx;
            Play();
        }
    }
}
