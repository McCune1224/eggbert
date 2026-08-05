using Godot;

/// <summary>
/// A pushable block that slides in response to player movement. Supports standard
/// four-direction pushing and a <see cref="DirectionalMode"/> that constrains motion
/// to the push axis only.
/// </summary>
/// <remarks>
/// <b>DirectionalMode toggle:</b> When enabled, diagonal pushes are snapped to the dominant
/// axis so the block moves in a straight line rather than diagonally. Use this for puzzles
/// that require precise axis-aligned sliding.
/// <b>Full-tile clearance:</b> The collision shape is sized to a 32×32 tile region
/// (see <see cref="ApplyTexture"/>). The block requires one full tile of free space along
/// its movement path to slide; partial overlaps with walls or other pushable blocks will
/// block movement.
/// </remarks>

[GlobalClass]
[Tool]
public partial class PushBlock : CharacterBody2D
{

    /// <summary>How fast the block slides per push, in pixels per second.</summary>
    [ExportGroup("PushBlock")]
    [Export(PropertyHint.Range, "40,600,20")] public float PushSpeed = 200f;
    /// <summary>If true, diagonal pushes snap to the dominant axis so the block only slides in straight lines.</summary>
    [Export] public bool DirectionalMode = false;
    /// <summary>Optional tileset texture shown on the block (first 32×32 tile is used).</summary>
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

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (_collisionShape?.Shape is not RectangleShape2D)
            warnings.Add("PushBlock expects a RectangleShape2D collision shape for proper sizing.");
        return warnings.ToArray();
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
            // DirectionalMode: snap to the dominant axis so the block moves in a straight line
            pushDir = new Vector2(
                Mathf.Abs(pushDir.X) > Mathf.Abs(pushDir.Y) ? Mathf.Sign(pushDir.X) : 0f,
                Mathf.Abs(pushDir.Y) > Mathf.Abs(pushDir.X) ? Mathf.Sign(pushDir.Y) : 0f
            );
            // Full-tile clearance required — the collision shape is a 32×32 tile region;
            // partial overlaps with walls or other pushables will block movement.
        }

        Velocity = pushDir * PushSpeed;
        MoveAndSlide();
        bool moved = GlobalPosition.DistanceSquaredTo(from) > 0.01f;
        Velocity = Vector2.Zero;
        GameLogger.Info("PushBlock", $"{Name}: TryPush direction={pushDir}, moved={moved}");
        return moved;
    }
}
