using Godot;

public partial class CombatArena : Node2D
{
    [Signal]
    public delegate void BattleWonEventHandler();
    [Signal]
    public delegate void BattleLostEventHandler();

    [Export] public Vector2 PlayerSpawnPosition { get; set; } = Vector2.Zero;

    protected CombatHUD HUD { get; private set; }

    public int EnemiesRemaining { get; set; } = 1;

    public override void _Ready()
    {
        var cam = GetNodeOrNull<Camera2D>("Camera2D");
        if (cam != null)
        {
            cam.MakeCurrent();
            cam.Position = Vector2.Zero;
        }

        HUD = new CombatHUD();
        AddChild(HUD);

        Player.Instance.Position = PlayerSpawnPosition;
        Player.Instance.HealthComponent.Died += OnPlayerDied;

    }

    public override void _ExitTree()
    {
        if (Player.Instance != null && GodotObject.IsInstanceValid(Player.Instance))
            Player.Instance.HealthComponent.Died -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        Player.Instance.HealthComponent.Died -= OnPlayerDied;
        EmitSignal(SignalName.BattleLost);
    }


    public void OnEnemyDefeated()
    {
        EnemiesRemaining--;
        GameLogger.Info("Combat", $"Enemy defeated — {EnemiesRemaining} remaining.");
        if (EnemiesRemaining <= 0)
        {
            HUD.QueueFree();
            EmitSignal(SignalName.BattleWon);
            GameLogger.Info("Combat", "All enemies defeated — battle won!");
        }
    }
}
