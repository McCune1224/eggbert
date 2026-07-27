using Godot;

/// <summary>
/// Area2D trigger that starts the final boss combat encounter on player contact.
/// Replaces the former FinalBossTrigger.gd (only remaining GDScript game-logic file).
/// ArenaPath is exported so the SunnysideLeader arena (content issue C10) can be wired
/// without editing code; defaults to GenericArena to preserve prior runtime behavior.
/// On entry, sets <see cref="BeatFlag"/> and <see cref="OnceFlag"/> (if configured)
/// before launching combat so a win persists; a loss reverts the flags via save
/// reload. On level reload, the trigger self-frees if <see cref="OnceFlag"/> is set.
/// </summary>
public partial class FinalBossTrigger : Area2D
{
    [Export] public string ArenaPath = "res://combat/arena/GenericArena.tscn";
    [Export] public Vector2 PlayerSpawn = Vector2.Zero;
    /// <summary>World flag set to true before entering combat (so a win persists; a loss reverts via save reload). e.g. "beat_leader".</summary>
    [Export] public string BeatFlag = "";
    /// <summary>If set, the trigger self-frees once this flag is true (one-shot encounters). Typically the same value as <see cref="BeatFlag"/>.</summary>
    [Export] public string OnceFlag = "";

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.TriggerAreaLayer;
        CollisionMask = CollisionConfig.PlayerLayer;
        BodyEntered += OnBodyEntered;

        if (!string.IsNullOrEmpty(OnceFlag) && WorldFlags.Instance.HasFlag(OnceFlag))
        {
            QueueFree();
            GameLogger.Debug("FinalBossTrigger", $"'{Name}': already resolved (OnceFlag='{OnceFlag}') — removed");
        }
    }

    private void OnBodyEntered(Node body)
    {
        if (body != Player.Instance) return;
        if (CombatController.Instance == null) return;

        GameLogger.Info("Combat", $"FinalBossTrigger '{Name}': triggered — starting combat at '{ArenaPath}'");
        if (!string.IsNullOrEmpty(BeatFlag))
            WorldFlags.Instance.SetFlag(BeatFlag, true);
        if (!string.IsNullOrEmpty(OnceFlag))
            WorldFlags.Instance.SetFlag(OnceFlag, true);
        CombatController.Instance.EnterCombat(ArenaPath, PlayerSpawn);
    }
}
