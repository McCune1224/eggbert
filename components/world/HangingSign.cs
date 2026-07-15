using Godot;

public partial class HangingSign : Sprite2D
{
    [Export] public float SwingSpeed { get; set; } = 2.0f;
    [Export] public float SwingAngle { get; set; } = 5.0f;

    private float _time = 0f;

    public override void _Process(double delta)
    {
        _time += (float)delta * SwingSpeed;
        Rotation = Mathf.DegToRad(Mathf.Sin(_time) * SwingAngle);
    }
}
