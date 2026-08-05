using Godot;

/// <summary>
/// Combat arena that tracks enemy eliminations via <see cref="OnEnemyDefeated"/>. Fires <see cref="BattleWon"/> when <see cref="EnemiesRemaining"/> reaches zero
/// and <see cref="BattleLost"/> when the player dies. Resets player position on ready.
/// </summary>
/// <remarks>
/// OnEnemyDefeated decrements <see cref="EnemiesRemaining"/>, logs the count, and when the counter hits zero,
/// frees the HUD, emits <see cref="BattleWon"/>, and logs victory. The counter starts at 1 and must be set
/// per-arena to match the actual enemy count placed in the scene.
/// </remarks>
public partial class CombatArena : Node2D
{
    [Signal]
    public delegate void BattleWonEventHandler();
    [Signal]
    public delegate void BattleLostEventHandler();

    [Export] public Vector2 PlayerSpawnPosition { get; set; } = Vector2.Zero;

    protected CombatHUD HUD { get; private set; }

    /// <summary>
    /// Number of enemies still alive in this arena. When it reaches zero, <see cref="BattleWon"/> fires.
    /// </summary>
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

        // Per-combat equipment state: block charges reset each battle (Bubble Wrap);
        // base iframes stay off unless gear grants them (Sunglasses).
        Player.Instance.HealthComponent.BlockChargesRemaining = CombatStats.BlockCharges;
        Player.Instance.HealthComponent.IframeSeconds = 0f;

        GameLogger.Info("Combat", $"Arena '{Name}': _Ready — player spawned at {PlayerSpawnPosition}, initial enemies={EnemiesRemaining}");
    }

    private float _regenAccumulator = 0f;

    public override void _Process(double delta)
    {
        // Combat regen from armor (Stained Apron) — heals per second while in the arena.
        if (CombatStats.RegenPerSecond <= 0f) return;
        _regenAccumulator += (float)delta;
        if (_regenAccumulator >= 1f)
        {
            _regenAccumulator = 0f;
            var hc = Player.Instance?.HealthComponent;
            if (hc != null && !hc.IsDead)
                hc.Heal(Mathf.Max(1, Mathf.RoundToInt(CombatStats.RegenPerSecond)));
        }
    }

    public override void _ExitTree()
    {
        if (Player.Instance != null && GodotObject.IsInstanceValid(Player.Instance))
            Player.Instance.HealthComponent.Died -= OnPlayerDied;

        GameLogger.Debug("Combat", $"Arena '{Name}': _ExitTree — cleanup done");
    }

    private void OnPlayerDied()
    {
        Player.Instance.HealthComponent.Died -= OnPlayerDied;
        GameLogger.Info("Combat", $"Arena '{Name}': player died — battle lost");
        EmitSignal(SignalName.BattleLost);
    }


    /// <summary>
    /// Called when an enemy is defeated. Decrements <see cref="EnemiesRemaining"/> and fires <see cref="BattleWon"/>
    /// when all enemies in the arena have been eliminated.
    /// </summary>
    public void OnEnemyDefeated()
    {
        EnemiesRemaining--;
        GameLogger.Info("Combat", $"Arena '{Name}': enemy defeated — {EnemiesRemaining} remaining.");
        if (EnemiesRemaining <= 0)
        {
            HUD.QueueFree();
            EmitSignal(SignalName.BattleWon);
            GameLogger.Info("Combat", $"Arena '{Name}': all enemies defeated — battle won!");
        }
    }
}
