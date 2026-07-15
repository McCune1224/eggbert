using Godot;

/// <summary>
/// Tile that pushes the player (and pushable objects) in a set direction.
/// Player can move against the conveyor with sprint.
/// </summary>
public partial class ConveyorTile : Area2D
{
    [Export] public Vector2 ConveyorDirection { get; set; } = Vector2.Right;
    [Export] public float ConveyorSpeed { get; set; } = 80f;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer | CollisionConfig.InteractableLayer;

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is PushBlock block)
        {
            // Push the block in conveyor direction
            block.TryPush(ConveyorDirection);
        }
    }

    private void OnBodyExited(Node2D body)
    {
        // No cleanup needed — conveyor effect stops when body leaves area
    }

    public Vector2 GetConveyorVelocity(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            // Apply conveyor velocity, but let player sprint override
            var player = body as Player;
            if (player != null && Input.IsActionPressed("player_sprint"))
                return Vector2.Zero;
        }
        return ConveyorDirection.Normalized() * ConveyorSpeed;
    }
}
