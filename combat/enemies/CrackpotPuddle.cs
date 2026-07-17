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
    private Polygon2D _puddle;

    public override void _Ready()
    {
        // Visual: tinted puddle circle that grows with lifetime (replaces ColorRect placeholder).
        const int segs = 24;
        const float radius = 24f;
        var poly = new Vector2[segs];
        for (int i = 0; i < segs; i++)
        {
            float a = i * Mathf.Tau / segs;
            poly[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }
        _puddle = new Polygon2D
        {
            Polygon = poly,
            Color = new Color(0.8f, 0.4f, 0.1f, 0.6f),
        };
        AddChild(_puddle);

        // Collision
        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(48, 48) }
        };
        AddChild(shape);

        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer;

        AreaEntered += OnAreaEntered;

        GameLogger.Debug("Combat", $"CrackpotPuddle '{Name}': spawned (damage={Damage}, lifetime={Lifetime}s)");
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _aliveTime += dt;

        // Grow radius slightly over lifetime; fade out in the final second.
        float grow = 1f + (_aliveTime / Lifetime) * 0.3f;
        float alpha = _aliveTime > Lifetime - 1f
            ? Mathf.Lerp(0.6f, 0f, (_aliveTime - (Lifetime - 1f)) / 1f)
            : 0.6f;
        _puddle.Scale = new Vector2(grow, grow);
        _puddle.Color = new Color(0.8f, 0.4f, 0.1f, alpha);

        if (_aliveTime >= Lifetime)
        {
            GameLogger.Debug("Combat", $"CrackpotPuddle '{Name}': despawned after {Lifetime}s");
            QueueFree();
        }

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
                    GameLogger.Debug("Combat", $"CrackpotPuddle '{Name}': damaged player for {Damage} HP");
                }
            }
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        // Could trigger enter effects
    }
}
