using Godot;

public partial class CerealEncounterTrigger : Area2D
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
            GameLogger.Info("Combat", $"CerealEncounterTrigger '{Name}': triggered — starting combat at '{ArenaPath}'");
            CombatController.Instance.EnterCombat(ArenaPath, PlayerSpawn);
        }
    }
}
