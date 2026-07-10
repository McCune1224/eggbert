using Godot;

public partial class OatmealArena : CombatArena
{
    private static readonly PackedScene _enemyScene = ResourceLoader.Load<PackedScene>("res://combat/enemies/CombatOatmeal.tscn");

    private struct SpawnDef
    {
        public CombatOatmeal.OatmealFlavor Flavor;
        public Vector2 Position;
        public string DisplayName;
    }

    private static readonly SpawnDef[] Spawns =
    {
        new() { Flavor = CombatOatmeal.OatmealFlavor.Vanilla,     Position = new Vector2(1, -130),   DisplayName = "Vanilla" },
        new() { Flavor = CombatOatmeal.OatmealFlavor.Strawberry,  Position = new Vector2(140, -70),  DisplayName = "Strawberry" },
        new() { Flavor = CombatOatmeal.OatmealFlavor.Chocolate,   Position = new Vector2(-140, -70), DisplayName = "Chocolate" },
        new() { Flavor = CombatOatmeal.OatmealFlavor.Mint,        Position = new Vector2(0, -190),   DisplayName = "Mint" },
    };

    public override void _Ready()
    {
        PlayerSpawnPosition = new Vector2(0, 50);
        base._Ready();

        if (HasNode("Oatmeal"))
            GetNode("Oatmeal").QueueFree();

        EnemiesRemaining = Spawns.Length;

        foreach (var def in Spawns)
        {
            var enemy = _enemyScene.Instantiate<CombatOatmeal>();
            enemy.Position = def.Position;
            enemy.Flavor = def.Flavor;
            AddChild(enemy);
            enemy.ApplyFlavor();

            HUD.AddEnemy($"{def.DisplayName} Oatmeal", enemy.Health);
        }

        HUD.SetPlayerHealthComponent(Player.Instance.HealthComponent);
    }
}
