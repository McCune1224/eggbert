using Godot;
/// <summary>
/// Light node that flickers at a configurable rate/amplitude.
/// Useful for atmospheric effects (candles, emergency lights, etc.).
/// </summary>

public partial class FlickeringLight : PointLight2D
{
    [Export] public float MinEnergy { get; set; } = 0.2f;
    [Export] public float MaxEnergy { get; set; } = 1.0f;
    [Export] public float FlickerSpeed { get; set; } = 5.0f;
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
