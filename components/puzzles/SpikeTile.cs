using Godot;

/// <summary>
/// Tile that damages the player on contact.
/// </summary>
public partial class SpikeTile : Area2D
{
    [Export] public int Damage { get; set; } = 1;
    [Export] public bool OneShot { get; set; } = false;

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
