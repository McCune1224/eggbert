using Godot;

public partial class PushBlock : CharacterBody2D
{
    public static bool DebugVisible = false;

    [Export] public float PushSpeed = 200f;

    [Export]
    private Texture2D _texture;
    public Texture2D Texture
    {
        get => _texture;
        set { _texture = value; ApplyTexture(); }
    }

    private Sprite2D _sprite;
    private CollisionShape2D _collisionShape;
    private Label _debugLabel;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.InteractableLayer;
        CollisionMask = CollisionConfig.WallsLayer | CollisionConfig.PlayerLayer;
        AddToGroup("pushable");
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        ApplyTexture();
    }

    public override void _Process(double delta)
    {
        if (_debugLabel == null && DebugVisible)
        {
            _debugLabel = new Label();
            _debugLabel.AddThemeFontSizeOverride("font_size", 10);
            _debugLabel.AddThemeColorOverride("font_color", Colors.Orange);
            _debugLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
            _debugLabel.AddThemeConstantOverride("outline_size", 1);
            AddChild(_debugLabel);
            _debugLabel.Position = new Vector2(-40, -32);
        }
        if (_debugLabel != null)
        {
            _debugLabel.Visible = DebugVisible;
            if (DebugVisible)
                _debugLabel.Text = $"[Block] {Position.Round()}";
        }
    }

    private void ApplyTexture()
    {
        if (_sprite == null || _texture == null) return;
        _sprite.Texture = _texture;
        Vector2 texSize = _texture.GetSize() * 0.95f;
        if (_collisionShape?.Shape is RectangleShape2D rect)
            rect.Size = texSize;
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
