using Godot;

public partial class PushBlock : CharacterBody2D
{
    [Export] public float PushSpeed = 200f;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.InteractableLayer;
        CollisionMask = CollisionConfig.WallsLayer | CollisionConfig.PlayerLayer;
        AddToGroup("pushable");
    }

    /// <summary>Try sliding one step in the given direction. Returns false if blocked.</summary>
    public bool TryPush(Vector2 direction)
    {
        Vector2 from = GlobalPosition;
        Velocity = direction.Normalized() * PushSpeed;
        MoveAndSlide();
        bool moved = GlobalPosition.DistanceSquaredTo(from) > 0.01f;
        Velocity = Vector2.Zero;
        return moved;
    }
}
