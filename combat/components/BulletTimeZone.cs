using Godot;

/// <summary>
/// A stationary zone that slows enemy bullets passing through it (Hourglass).
/// Spawned at the player's dash position; self-destructs after its lifetime.
/// Enemy bullets query the "bullet_time_zone" group each frame.
/// </summary>
public partial class BulletTimeZone : Node2D
{
    public float SlowMultiplier { get; set; } = 0.5f;
    public float Radius { get; set; } = 80f;
    public float Lifetime { get; set; } = 2f;

    private float _age = 0f;

    public override void _Ready()
    {
        AddToGroup("bullet_time_zone");
        ZIndex = -1;
        GameLogger.Debug("Combat", $"BulletTimeZone: spawned — slow={SlowMultiplier}, radius={Radius}, lifetime={Lifetime}");
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= Lifetime)
            QueueFree();
        QueueRedraw();
    }

    public override void _Draw()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(_age * 6f);
        DrawCircle(Vector2.Zero, Radius, new Color(0.6f, 0.9f, 1f, 0.05f + 0.05f * pulse));
        DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 32, new Color(0.6f, 0.9f, 1f, 0.25f), 1f);
    }
}
