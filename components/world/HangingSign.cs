using Godot;
/// <summary>
/// Decorative hanging sign with a sine-wave pendulum swing.
/// Purely cosmetic — no interaction or game-state impact.
/// </summary>

public partial class HangingSign : Sprite2D
{
    /// <summary>How fast the sign swings, in cycles per second.</summary>
    [ExportGroup("Swing")]
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float SwingSpeed { get; set; } = 2.0f;
    /// <summary>Maximum swing angle in degrees from vertical.</summary>
    [Export(PropertyHint.Range, "0,30,1")] public float SwingAngle { get; set; } = 5.0f;

    private float _time = 0f;

    public override void _Process(double delta)
    {
        _time += (float)delta * SwingSpeed;
        Rotation = Mathf.DegToRad(Mathf.Sin(_time) * SwingAngle);
    }
}
