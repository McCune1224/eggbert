using Godot;

/// <summary>
/// Generic, reusable combat arena. Spawns a single enemy and builds code-defined
/// bounds so it works standalone without per-scene wall tile collision. Set
/// <see cref="EnemyScene"/> to override the default CombatOatmeal enemy, and
/// tune <see cref="ArenaSize"/> / spawn positions per level as needed.
/// </summary>
public partial class GenericArena : CombatArena
{
    private static readonly PackedScene DefaultEnemyScene =
        ResourceLoader.Load<PackedScene>("res://combat/enemies/CombatOatmeal.tscn");

    private const float WallThickness = 16f;

    [Export] public PackedScene EnemyScene { get; set; }
    [Export] public Vector2 ArenaSize { get; set; } = new Vector2(480, 320);
    [Export] public Vector2 PlayerSpawnOverride { get; set; } = new Vector2(0, 100);
    [Export] public Vector2 EnemySpawnPosition { get; set; } = new Vector2(0, -120);

    public override void _Ready()
    {
        PlayerSpawnPosition = PlayerSpawnOverride;
        base._Ready();

        BuildBounds(ArenaSize);

        var scene = EnemyScene ?? DefaultEnemyScene;
        var enemy = scene.Instantiate<CombatOatmeal>();
        enemy.Position = EnemySpawnPosition;
        AddChild(enemy);
        enemy.ApplyFlavor();

        EnemiesRemaining = 1;
        HUD.AddEnemy(enemy.Name, enemy.Health);
        HUD.SetPlayerHealthComponent(Player.Instance.HealthComponent);
    }

    /// <summary>Builds four static walls on the Walls layer so the player cannot leave the arena.</summary>
    private void BuildBounds(Vector2 size)
    {
        float halfW = size.X / 2f;
        float halfH = size.Y / 2f;

        var bounds = new StaticBody2D { Name = "ArenaBounds" };
        bounds.CollisionLayer = CollisionConfig.WallsLayer;
        bounds.CollisionMask = 0;
        AddChild(bounds);

        AddWall(bounds, new Vector2(0, -halfH), new Vector2(size.X, WallThickness));   // top
        AddWall(bounds, new Vector2(0, halfH),  new Vector2(size.X, WallThickness));    // bottom
        AddWall(bounds, new Vector2(-halfW, 0), new Vector2(WallThickness, size.Y));     // left
        AddWall(bounds, new Vector2(halfW, 0),  new Vector2(WallThickness, size.Y));     // right
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