using Godot;

/// <summary>
/// Arena for the Yogurt mini-boss (Warden's Quarters, story beat 5).
/// Spawns a single CombatYogurt and builds code-defined bounds.
/// </summary>
public partial class YogurtArena : CombatArena
{
    private static readonly PackedScene EnemyScene =
        ResourceLoader.Load<PackedScene>("res://combat/enemies/CombatYogurt.tscn");

    private const float WallThickness = 16f;

    [Export] public Vector2 ArenaSize { get; set; } = new Vector2(480, 320);
    [Export] public Vector2 EnemySpawn { get; set; } = new Vector2(0, -120);

    public override void _Ready()
    {
        PlayerSpawnPosition = new Vector2(0, 100);
        base._Ready();

        BuildBounds(ArenaSize);

        var enemy = EnemyScene.Instantiate<CombatYogurt>();
        enemy.Position = EnemySpawn;
        AddChild(enemy);

        EnemiesRemaining = 1;
        HUD.AddEnemy("Yogurt", enemy.Health);
        HUD.SetPlayerHealthComponent(Player.Instance.HealthComponent);

        GameLogger.Info("Combat", $"YogurtArena '{Name}': ready — Yogurt at {EnemySpawn}");
    }

    private void BuildBounds(Vector2 size)
    {
        float halfW = size.X / 2f;
        float halfH = size.Y / 2f;

        var bounds = new StaticBody2D { Name = "ArenaBounds" };
        bounds.CollisionLayer = CollisionConfig.WallsLayer;
        bounds.CollisionMask = 0;
        AddChild(bounds);

        AddWall(bounds, new Vector2(0, -halfH), new Vector2(size.X, WallThickness));
        AddWall(bounds, new Vector2(0, halfH), new Vector2(size.X, WallThickness));
        AddWall(bounds, new Vector2(-halfW, 0), new Vector2(WallThickness, size.Y));
        AddWall(bounds, new Vector2(halfW, 0), new Vector2(WallThickness, size.Y));
    }

    private void AddWall(StaticBody2D parent, Vector2 position, Vector2 rectSize)
    {
        var shape = new CollisionShape2D
        {
            Position = position,
            Shape = new RectangleShape2D { Size = rectSize }
        };
        parent.AddChild(shape);
    }
}