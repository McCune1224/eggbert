using Godot;

/// <summary>
/// Tile that damages the player on contact.
/// </summary>
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

    private void OnBodyEntered(Node2D body)
    {
        if (_hasTriggered && OneShot) return;

        if (body is Player player)
        {
            player.HealthComponent?.TakeDamage(Damage, this);
            _hasTriggered = true;
        }
    }
}
