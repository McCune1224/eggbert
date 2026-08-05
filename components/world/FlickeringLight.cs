using Godot;
/// <summary>
/// Light node that flickers at a configurable rate/amplitude.
/// Useful for atmospheric effects (candles, emergency lights, etc.).
/// </summary>

public partial class FlickeringLight : PointLight2D
{
    /// <summary>Lowest light energy in the flicker cycle.</summary>
    [ExportGroup("Flicker")]
    [Export(PropertyHint.Range, "0,1,0.05")] public float MinEnergy { get; set; } = 0.2f;
    /// <summary>Highest light energy in the flicker cycle.</summary>
    [Export(PropertyHint.Range, "0,2,0.05")] public float MaxEnergy { get; set; } = 1.0f;
    /// <summary>Flicker speed in cycles per second.</summary>
    [Export(PropertyHint.Range, "0.5,20,0.5")] public float FlickerSpeed { get; set; } = 5.0f;
    /// <summary>Optional electrical buzz sound played while the light is on.</summary>
    [Export] public AudioStream BuzzSfx { get; set; }

    private AudioStreamPlayer2D _buzzPlayer;
    private float _time = 0f;

    public override void _Ready()
    {
        if (BuzzSfx != null)
        {
            _buzzPlayer = new AudioStreamPlayer2D
            {
                Stream = BuzzSfx,
                Bus = "SFX"
            };
            AddChild(_buzzPlayer);
            _buzzPlayer.Play();
            GameLogger.Debug("FlickeringLight", $"'{Name}': buzz SFX started");
        }
    }

    public override void _Process(double delta)
    {
        _time += (float)delta * FlickerSpeed;
        Energy = MinEnergy + (Mathf.Sin(_time) * 0.5f + 0.5f) * (MaxEnergy - MinEnergy);
    }
}
