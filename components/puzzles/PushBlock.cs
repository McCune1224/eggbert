using Godot;

public partial class PushBlock : CharacterBody2D
{

    [Export] public float PushSpeed = 200f;
    [Export] public bool DirectionalMode = false;

    [Export]
    private Texture2D _texture;
    public Texture2D Texture
    {
        get => _texture;
        set { _texture = value; ApplyTexture(); }
    }

    private Sprite2D _sprite;
    private CollisionShape2D _collisionShape;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.InteractableLayer;
        CollisionMask = CollisionConfig.WallsLayer | CollisionConfig.PlayerLayer;
        AddToGroup("pushable");
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        ApplyTexture();
    }


    private void ApplyTexture()
    {
        Texture2D tex = _texture ?? _sprite?.Texture;
        if (_sprite != null && _texture != null)
        {
            _sprite.Texture = _texture;
            _sprite.RegionEnabled = true;
            // Show only the first 32x32 tile from the tileset
            _sprite.RegionRect = new Rect2(0, 0, 32, 32);
        }
        if (tex == null || _collisionShape?.Shape is not RectangleShape2D rect) return;
        // Scale collision to match the displayed tile region
        float region = 32f;
        rect.Size = Vector2.One * region * 0.6f;
    }

    /// <summary>Try sliding one step in the given direction. Returns false if blocked.</summary>
    public bool TryPush(Vector2 direction)
    {
        Vector2 from = GlobalPosition;
        Vector2 pushDir = direction.Normalized();

        if (DirectionalMode)
        {
            // In directional mode, constrain movement to the push axis only
            pushDir = new Vector2(
                Mathf.Abs(pushDir.X) > Mathf.Abs(pushDir.Y) ? Mathf.Sign(pushDir.X) : 0f,
                Mathf.Abs(pushDir.Y) > Mathf.Abs(pushDir.X) ? Mathf.Sign(pushDir.Y) : 0f
            );
        }

        Velocity = pushDir * PushSpeed;
        MoveAndSlide();
        bool moved = GlobalPosition.DistanceSquaredTo(from) > 0.01f;
        Velocity = Vector2.Zero;
        return moved;
    }
}
