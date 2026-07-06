using Godot;

public partial class PushBlock : CharacterBody2D
{
    [Export] public float PushSpeed = 200f;

    [Export]
    private Texture2D _texture;
    public Texture2D Texture
    {
        get => _texture;
        set { _texture = value; if (_sprite != null) _sprite.Texture = value; }
    }

    private Sprite2D _sprite;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.InteractableLayer;
        CollisionMask = CollisionConfig.WallsLayer | CollisionConfig.PlayerLayer;
        AddToGroup("pushable");
        _sprite = GetNode<Sprite2D>("Sprite2D");
        if (_texture != null) _sprite.Texture = _texture;
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
