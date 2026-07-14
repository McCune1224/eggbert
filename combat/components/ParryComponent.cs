using Godot;

public partial class ParryComponent : Node2D
{
    [Signal]
    public delegate void ParriedEventHandler();

    [Export] public float ParryRadius { get; set; } = 110f;
    [Export] public int ParryDamage { get; set; } = 10;
    [Export] public float Cooldown { get; set; } = 0.5f;

    private float _cooldownTimer = 0f;
    private bool _canParry = true;
    private float _ringFlash = 0f;

    private static readonly Color RingColor = new Color(0.3f, 0.8f, 1f, 0.2f);
    private static readonly Color MissColor = new Color(1f, 0.3f, 0.3f, 0.3f);
    private static readonly Color ReflectedTint = new Color(0f, 1f, 1f);

    public override void _Process(double delta)
    {
        bool inCombat = GameController.Instance?.CurrentLevel is CombatArena;

        if (!_canParry)
        {
            _cooldownTimer -= (float)delta;
            if (_cooldownTimer <= 0f)
                _canParry = true;
        }

        if (inCombat && _canParry && Input.IsActionJustPressed("combat_parry"))
            TryParry();

        if (_ringFlash > 0f)
            _ringFlash -= (float)delta * 4f;
        else
            _ringFlash = 0f;

        QueueRedraw();
    }

    private void TryParry()
    {
        _canParry = false;
        _cooldownTimer = Cooldown;

        bool anyReflected = false;

        foreach (Node node in GetTree().GetNodesInGroup("bullet"))
        {
            if (node is RedBullet bullet && GodotObject.IsInstanceValid(bullet) && !bullet.Reflected)
            {
                float dist = GlobalPosition.DistanceTo(bullet.GlobalPosition);
                if (dist <= ParryRadius)
                {
                    Vector2 targetPos;
                    if (bullet.FiredBy != null && GodotObject.IsInstanceValid(bullet.FiredBy))
                        targetPos = bullet.FiredBy.GlobalPosition;
                    else
                        targetPos = FindNearestEnemyPosition(bullet.GlobalPosition);

                    Vector2 toTarget = (targetPos - bullet.GlobalPosition).Normalized();
                    bullet.SetDirection(toTarget, 400f);
                    bullet.Reflected = true;
                    bullet.IsHoming = false;
                    bullet.CollisionMask = CollisionConfig.EnemyLayer;
                    bullet.Modulate = ReflectedTint;
                    bullet.ResetLifetime();
                    anyReflected = true;
                }
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is RollingEgg egg && GodotObject.IsInstanceValid(egg) && egg.Health != null && !egg.Health.IsDead)
            {
                float dist = GlobalPosition.DistanceTo(egg.GlobalPosition);
                if (dist <= ParryRadius)
                {
                    Vector2 knockback = GlobalPosition.DirectionTo(egg.GlobalPosition) * 300f;
                    egg.OnParried(knockback);
                    egg.Health.TakeDamage(ParryDamage, this);
                    anyReflected = true;
                }
            }
        }

        if (anyReflected)
            OnParrySuccess();
        else
            OnParryMiss();
    }

    private Vector2 FindNearestEnemyPosition(Vector2 from)
    {
        Vector2 nearest = Vector2.Zero;
        float minDist = float.MaxValue;

        foreach (Node node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is Node2D enemyNode && enemyNode.HasNode("HealthComponent"))
            {
                var health = enemyNode.GetNode<HealthComponent>("HealthComponent");
                if (health != null && !health.IsDead)
                {
                    float d = from.DistanceTo(enemyNode.GlobalPosition);
                    if (d < minDist)
                    {
                        minDist = d;
                        nearest = enemyNode.GlobalPosition;
                    }
                }
            }
        }

        return nearest;
    }

    private void OnParrySuccess()
    {
        EmitSignal(SignalName.Parried);
        _ringFlash = 1f;

        var vfx = new Label
        {
            Text = "PARRY!",
            Position = new Vector2(270, 160),
            Scale = new Vector2(3, 3),
            Modulate = new Color(1, 1, 0)
        };
        vfx.AddThemeFontSizeOverride("font_size", 24);
        GetTree().Root.AddChild(vfx);

        var tween = CreateTween();
        tween.TweenProperty(vfx, "modulate:a", 0f, 0.6f);
        tween.TweenCallback(Callable.From(vfx.QueueFree));
    }

    private void OnParryMiss()
    {
        _ringFlash = -1f;
    }

    public override void _Draw()
    {
        bool inCombat = GameController.Instance?.CurrentLevel is CombatArena;
        if (!inCombat) return;

        Color drawColor;
        if (_ringFlash > 0f)
            drawColor = new Color(1f, 1f, 0.2f, 0.3f + _ringFlash * 0.3f);
        else if (_ringFlash < 0f)
            drawColor = MissColor;
        else
            drawColor = RingColor;

        DrawArc(Vector2.Zero, ParryRadius, 0, Mathf.Tau, 32, drawColor, 2f);

        if (_canParry)
            DrawArc(Vector2.Zero, ParryRadius - 4f, 0, Mathf.Tau, 32,
                new Color(1f, 1f, 1f, 0.15f), 1f);
    }

    public void UpdateStats(float radiusBoost, int damageBoost)
    {
        ParryRadius = 110f + radiusBoost;
        ParryDamage = 10 + damageBoost;
    }
}
