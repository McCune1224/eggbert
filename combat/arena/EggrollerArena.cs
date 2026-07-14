using Godot;

public partial class EggrollerArena : CombatArena
{
    private static readonly PackedScene _enemyScene = ResourceLoader.Load<PackedScene>("res://combat/enemies/RollingEgg.tscn");

    public override void _Ready()
    {
        PlayerSpawnPosition = new Vector2(0, 80);
        base._Ready();

        EnemiesRemaining = 2;

        SpawnEnemy(new Vector2(-100, -80));
        SpawnEnemy(new Vector2(100, -80));

        HUD.SetPlayerHealthComponent(Player.Instance.HealthComponent);
    }

    private void SpawnEnemy(Vector2 position)
    {
        var enemy = _enemyScene.Instantiate<RollingEgg>();
        enemy.Position = position;
        AddChild(enemy);
        HUD.AddEnemy("Eggroller", enemy.Health);
    }
}
