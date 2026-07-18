using Godot;

/// <summary>
/// Tile that damages the player on contact.
/// </summary>
[GlobalClass]
[Tool]
public partial class SpikeTile : Area2D
{
    [ExportGroup("Damage")]
    [Export]
    /// HP lost on contact.
    public int Damage { get; set; } = 1;
    [Export]
    /// If true, the spike only damages the player once, then deactivates.
    public bool OneShot { get; set; } = false;
    private bool _hasTriggered = false;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer;

        BodyEntered += OnBodyEntered;
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (Damage <= 0)
            warnings.Add("Damage is zero or negative — spike tile has no effect.");
        return warnings.ToArray();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_hasTriggered && OneShot)
        {
            GameLogger.Debug("SpikeTile", $"{Name}: already triggered, one-shot active — skipping");
            return;
        }

        if (body is Player player)
        {
            player.HealthComponent?.TakeDamage(Damage, this);
            _hasTriggered = true;
            GameLogger.Info("SpikeTile", $"{Name}: dealt {Damage} damage to player '{player.Name}' — oneShot={OneShot}, hasTriggered={_hasTriggered}");
        }
    }
}
