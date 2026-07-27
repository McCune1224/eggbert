using Godot;

/// <summary>
/// Area2D trigger that starts a one-shot combat encounter on player contact.
/// On entry, sets <see cref="BeatFlag"/> and <see cref="OnceFlag"/> (if configured)
/// before launching combat so a win persists; a loss reverts the flags via save
/// reload (Undertale-style). On level reload, the trigger self-frees if
/// <see cref="OnceFlag"/> is set, preventing the player from re-entering the
/// just-won encounter.
/// </summary>
public partial class CerealEncounterTrigger : Area2D
{
    [Export] public string ArenaPath = "res://combat/arena/GenericArena.tscn";
    [Export] public Vector2 PlayerSpawn = Vector2.Zero;
    /// <summary>World flag set to true on contact (before combat). A win persists; a loss reverts via save reload. e.g. "beat_prison_cereal".</summary>
    [Export] public string BeatFlag = "";
    /// <summary>If set, the trigger self-frees once this flag is true (one-shot encounters).</summary>
    [Export] public string OnceFlag = "";

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.TriggerAreaLayer;
        CollisionMask = CollisionConfig.PlayerLayer;
        BodyEntered += OnBodyEntered;

        if (!string.IsNullOrEmpty(OnceFlag) && WorldFlags.Instance.HasFlag(OnceFlag))
        {
            QueueFree();
            GameLogger.Debug("CerealEncounter", $"'{Name}': already resolved (OnceFlag='{OnceFlag}') — removed");
        }
    }

    private void OnBodyEntered(Node body)
    {
        if (body != Player.Instance) return;
        if (CombatController.Instance == null) return;

        GameLogger.Info("Combat", $"CerealEncounterTrigger '{Name}': triggered — starting combat at '{ArenaPath}'");
        if (!string.IsNullOrEmpty(BeatFlag))
            WorldFlags.Instance.SetFlag(BeatFlag, true);
        if (!string.IsNullOrEmpty(OnceFlag))
            WorldFlags.Instance.SetFlag(OnceFlag, true);
        CombatController.Instance.EnterCombat(ArenaPath, PlayerSpawn);
    }
}
