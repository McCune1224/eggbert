using Godot;

public partial class RedBullet : Area2D
{
    [Export] private float speed = 200.0f;
    [Export] private float lifetime = 3.0f;
    [Export] private Vector2 direction = Vector2.Right;

    private float aliveTime = 0.0f;

    public bool Reflected { get; set; } = false;
    public bool IsHoming { get; set; } = false;
    public Node2D FiredBy { get; set; } = null;

    private const float HomingStrength = 2.5f;

    public override void _Ready()
    {
        AddToGroup("bullet");
        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;

        string firedByInfo = FiredBy != null ? $" firedBy='{FiredBy.Name}'" : "";
        GameLogger.Debug("Combat", $"RedBullet '{Name}': spawned{firedByInfo} — pos={Position}, dir={direction}, homing={IsHoming}, speed={speed}, lifetime={lifetime}");
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (IsHoming && !Reflected)
        {
            Vector2 toPlayer = (CombatTargeter.GetPlayerPosition() - GlobalPosition).Normalized();
            direction = direction.Lerp(toPlayer, HomingStrength * dt).Normalized();
        }

        Position += direction.Normalized() * speed * dt;
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
                enemy.Health.TakeDamage(10 + Equipment.Instance.TotalAttackBoost, this);
                GameLogger.Debug("Combat", $"RedBullet '{Name}': reflected — hit enemy for {10 + Equipment.Instance.TotalAttackBoost} DMG");
            }
            QueueFree();
            return;
        }

        GameLogger.Debug("Combat", $"RedBullet '{Name}': area hit — destroyed");
        QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!Reflected && body.IsInGroup("player"))
        {
            Player.Instance.HealthComponent?.TakeDamage(10, this);
            GameLogger.Debug("Combat", $"RedBullet '{Name}': hit player for 10 DMG");
        }
        GameLogger.Debug("Combat", $"RedBullet '{Name}': body hit — destroyed");
        QueueFree();
    }
}
