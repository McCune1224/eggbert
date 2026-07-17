using Godot;

/// <summary>
/// Area2D trigger that starts the final boss combat encounter on player contact.
/// Replaces the former FinalBossTrigger.gd (only remaining GDScript game-logic file).
/// ArenaPath is exported so the SunnysideLeader arena (content issue C10) can be wired
/// without editing code; defaults to GenericArena to preserve prior runtime behavior.
/// </summary>
public partial class FinalBossTrigger : Area2D
{
    [Export] public string ArenaPath = "res://combat/arena/GenericArena.tscn";
    [Export] public Vector2 PlayerSpawn = Vector2.Zero;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.TriggerAreaLayer;
        CollisionMask = CollisionConfig.PlayerLayer;
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body)
    {
        if (body == Player.Instance)
        {
            GameLogger.Info("Combat", $"FinalBossTrigger '{Name}': triggered — starting combat at '{ArenaPath}'");
            CombatController.Instance.EnterCombat(ArenaPath, PlayerSpawn);
        }
    }
}