using Godot;

/// <summary>
/// Toggleable door that switches between open and closed states. Controls a
/// <see cref="CollisionShape2D"/> disable/enabled state and a visual <see cref="Modulate"/>
/// fade. Fires audio SFX on state change.
/// </summary>
/// <remarks>
/// <b>Toggle / Door state pattern:</b> <see cref="IsOpen"/> reflects the disabled state of
/// the collision shape. <see cref="Open"/> disables collision and fades the sprite;
/// <see cref="Close"/> re-enables collision and restores full opacity. Both are wrapped in
/// <see cref="CallDeferred"/> to ensure collision changes are thread-safe.
/// <b>StartOpen:</b> When true, the door opens immediately on <see cref="_Ready"/>.
/// <b>Signals:</b> This class does not emit custom signals for open/close; call
/// <see cref="Open"/> or <see cref="Close"/> directly and observe <see cref="IsOpen"/>.
/// </remarks>

[GlobalClass]
[Tool]
public partial class Door : StaticBody2D
{

    /// <summary>If true, the door starts already open when the level loads.</summary>
    [ExportGroup("Door")]
    [Export] public bool StartOpen = false;
    /// <summary>Sound played when the door opens.</summary>
    [Export] public AudioStream OpenSfx { get; set; }
    /// <summary>Sound played when the door closes.</summary>
    [Export] public AudioStream CloseSfx { get; set; }

    /// <summary>Optional texture to display on the door sprite. Leave empty to keep the scene's own sprite.</summary>
    [Export]
    private Texture2D _texture;
    public Texture2D Texture
    {
        get => _texture;
        set { _texture = value; if (_sprite != null) _sprite.Texture = value; }
    }

    private CollisionShape2D _collision;
    private Sprite2D _sprite;

    public bool IsOpen => _collision.Disabled;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.WallsLayer;
        _collision = GetNode<CollisionShape2D>("CollisionShape2D");
        _sprite = GetNode<Sprite2D>("Sprite2D");
        if (_texture != null) _sprite.Texture = _texture;
        if (StartOpen)
        {
            Open();
            GameLogger.Debug("Door", $"'{Name}': StartOpen is true; door opened on ready");
        }
    }


    public virtual void Open()
    {
        if (OpenSfx != null)
            AudioManager.Instance.PlaySfx(OpenSfx);
        // Disable collision both by disabling the shape and by clearing the layer,
        // so physics queries no longer detect this body. CallDeferred ensures
        // thread-safety if called from a physics callback (e.g. pressure plate).
        CallDeferred(nameof(SetCollisionEnabled), false);
        CallDeferred(nameof(SetCollisionLayerZero), 0);
        Modulate = new Color(1, 1, 1, 0.3f);
        GameLogger.Info("Door", $"'{Name}': opened");
    }

    public virtual void Close()
    {
        if (CloseSfx != null)
            AudioManager.Instance.PlaySfx(CloseSfx);
        CallDeferred(nameof(SetCollisionEnabled), true);
        CallDeferred(nameof(SetCollisionLayerZero), CollisionConfig.WallsLayer);
        Modulate = Colors.White;
        GameLogger.Info("Door", $"'{Name}': closed");
    }

    private void SetCollisionEnabled(bool enabled)
    {
        _collision.Disabled = !enabled;
    }

    /// <summary>
    /// Helper called via CallDeferred to set the StaticBody2D's CollisionLayer
    /// to or from zero. This reliably removes/restores the body in the physics
    /// space, complementing the CollisionShape2D.Disabled toggle.
    /// </summary>
    private void SetCollisionLayerZero(uint layer)
    {
        CollisionLayer = layer;
    }

    public void Toggle()
    {
        if (_collision.Disabled)
            Close();
        else
            Open();
        GameLogger.Debug("Door", $"'{Name}': toggled (now {(IsOpen ? "open" : "closed")})");
    }
}
