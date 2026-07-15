using Godot;

/// <summary>
/// Puddle left by Crackpot when it lands. Damages player on contact.
/// Auto-removes after lifetime. Can be cleansed by parrying Crackpot.
/// </summary>
public partial class CrackpotPuddle : Area2D
{
    [Export] public int Damage = 5;
    [Export] public float Lifetime = 4f;
    [Export] public float DamageInterval = 0.5f;

    private float _aliveTime = 0f;
    private float _damageTimer = 0f;

    public override void _Ready()
    {
        // Visual: a colored rectangle (placeholder)
        var rect = new ColorRect
        {
            Size = new Vector2(48, 48),
            Color = new Color(0.8f, 0.4f, 0.1f, 0.6f),
            Position = new Vector2(-24, -24)
        };
        AddChild(rect);

        // Collision
        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(48, 48) }
        };
        AddChild(shape);

        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer;

        AreaEntered += OnAreaEntered;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _aliveTime += dt;

        // Fade out near end
        if (_aliveTime > Lifetime - 1f)
        {
            var rect = GetChildOrNull<ColorRect>(0);
            if (rect != null)
                rect.Modulate = new Color(1, 1, 1, Mathf.Lerp(0.6f, 0f, (_aliveTime - (Lifetime - 1f)) / 1f));
        }

        if (_aliveTime >= Lifetime)
            QueueFree();

        // Tick damage at intervals
        _damageTimer += dt;
        if (_damageTimer >= DamageInterval)
        {
            _damageTimer = 0f;
            // Check overlapping bodies
            foreach (Node2D body in GetOverlappingBodies())
            {
                if (body.IsInGroup("player"))
                {
                    Player.Instance.HealthComponent?.TakeDamage(Damage, this);
                }
            }
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        // Could trigger enter effects
    }
}
