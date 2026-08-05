using Godot;
/// <summary>
/// Enemy bullet projectile. Moves in the firing direction at a fixed
/// speed and damages the player on contact via the PlayerHitbox layer.
/// </summary>

public partial class RedBullet : Area2D
{
    [Export] private float speed = 200.0f;
    [Export] private float lifetime = 3.0f;
    [Export] private Vector2 direction = Vector2.Right;

    private float aliveTime = 0.0f;
    private int _bouncesLeft = 0;

    public bool Reflected { get; set; } = false;
    public bool IsHoming { get; set; } = false;
    public Node2D FiredBy { get; set; } = null;

    private const float HomingStrength = 2.5f;

    public override void _Ready()
    {
        AddToGroup("bullet");
        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;
        _bouncesLeft = CombatStats.BounceCount;

        string firedByInfo = FiredBy != null ? $" firedBy='{FiredBy.Name}'" : "";
        GameLogger.Debug("Combat", $"RedBullet '{Name}': spawned{firedByInfo} — pos={Position}, dir={direction}, homing={IsHoming}, speed={speed}, lifetime={lifetime}");
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (IsHoming && !Reflected)
        {
            Vector2 toPlayer = (CombatTargeter.GetPlayerPosition() - GlobalPosition).Normalized();
            float strength = HomingStrength * (1f - CombatStats.HomingResistance);
            direction = direction.Lerp(toPlayer, strength * dt).Normalized();
        }

        // Equipment modifiers: enemy bullets can be slowed (Butter/Molasses/Egg Timer)
        // and by bullet-time zones (Hourglass); reflected bullets fly faster (Whisk).
        float zoneSlow = 1f;
        if (!Reflected && CombatStats.BulletTimeZoneSeconds > 0f)
            zoneSlow = GetZoneSlowMultiplier();
        float effectiveSpeed = Reflected
            ? speed * CombatStats.ReflectSpeedMultiplier
            : speed * CombatStats.BulletSlowMultiplier * zoneSlow;
        Position += direction.Normalized() * effectiveSpeed * dt;
        Rotation = Mathf.Atan2(direction.Y, direction.X);

        aliveTime += dt;
        if (aliveTime >= lifetime)
        {
            GameLogger.Debug("Combat", $"RedBullet '{Name}': despawned by timeout");
            QueueFree();
        }
    }

    public void SetDirection(Vector2 newDirection, float? newSpeed = null)
    {
        direction = newDirection.Normalized();
        if (newSpeed.HasValue)
            speed = newSpeed.Value;
    }

    public void ResetLifetime()
    {
        aliveTime = 0.0f;
    }

    private void OnAreaEntered(Area2D area)
    {
        if (Reflected && (area.CollisionLayer & CollisionConfig.EnemyLayer) != 0)
        {
            if (area is CombatOatmeal enemy && enemy.Health != null)
            {
                int dmg = 10 + Equipment.Instance.TotalAttackBoost;
                enemy.Health.TakeDamage(dmg, this);
                GameLogger.Debug("Combat", $"RedBullet '{Name}': reflected — hit enemy for {dmg} DMG");
                if (CombatStats.ReflectExplosionRadius > 0f)
                    Explode(dmg, enemy);
            }
            QueueFree();
            return;
        }

        GameLogger.Debug("Combat", $"RedBullet '{Name}': area hit — destroyed");
        QueueFree();
    }

    /// <summary>Frying Pan: splash damage to enemies near the impact point.</summary>
    private void Explode(int dmg, Node2D directTarget)
    {
        float radius = CombatStats.ReflectExplosionRadius;
        int hits = 0;
        foreach (Node node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is Node2D enemyNode && GodotObject.IsInstanceValid(enemyNode) && enemyNode != directTarget)
            {
                if (GlobalPosition.DistanceTo(enemyNode.GlobalPosition) <= radius)
                {
                    var health = enemyNode.GetNodeOrNull<HealthComponent>("HealthComponent");
                    if (health != null && !health.IsDead)
                    {
                        health.TakeDamage(dmg, this);
                        hits++;
                    }
                }
            }
        }
        GameLogger.Debug("Combat", $"RedBullet '{Name}': explosion hit {hits} additional enemy(ies) within {radius}px");
    }

    private void OnBodyEntered(Node2D body)
    {
        // Rubber Band: reflected bullets bounce off walls instead of dying.
        if (Reflected && _bouncesLeft > 0 && body is CollisionObject2D collider &&
            (collider.CollisionLayer & CollisionConfig.WallsLayer) != 0)
        {
            _bouncesLeft--;
            // Tile-based arenas are axis-aligned — flip the dominant velocity axis.
            if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
                direction = new Vector2(-direction.X, direction.Y);
            else
                direction = new Vector2(direction.X, -direction.Y);
            GameLogger.Debug("Combat", $"RedBullet '{Name}': bounced off wall — {_bouncesLeft} bounce(s) left");
            return;
        }

        if (!Reflected && body.IsInGroup("player"))
        {
            Player.Instance.HealthComponent?.TakeDamage(10, this);
            GameLogger.Debug("Combat", $"RedBullet '{Name}': hit player for 10 DMG");
        }
        GameLogger.Debug("Combat", $"RedBullet '{Name}': body hit — destroyed");
        QueueFree();
    }

    /// <summary>Lowest slow multiplier among bullet-time zones overlapping this bullet.</summary>
    private float GetZoneSlowMultiplier()
    {
        float slow = 1f;
        foreach (Node node in GetTree().GetNodesInGroup("bullet_time_zone"))
        {
            if (node is BulletTimeZone zone && GodotObject.IsInstanceValid(zone))
            {
                if (GlobalPosition.DistanceTo(zone.GlobalPosition) <= zone.Radius)
                    slow = Mathf.Min(slow, zone.SlowMultiplier);
            }
        }
        return slow;
    }
}
