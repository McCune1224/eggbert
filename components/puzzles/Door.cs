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

using Godot;

[GlobalClass]
[Tool]
public partial class Door : StaticBody2D
{

    [ExportGroup("Door")]
    [Export] public bool StartOpen = false;
    [Export] public AudioStream OpenSfx { get; set; }
    [Export] public AudioStream CloseSfx { get; set; }

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
        CallDeferred(nameof(SetCollisionEnabled), false);
        Modulate = new Color(1, 1, 1, 0.3f);
        GameLogger.Info("Door", $"'{Name}': opened");
    }

    public virtual void Close()
    {
        if (CloseSfx != null)
            AudioManager.Instance.PlaySfx(CloseSfx);
        CallDeferred(nameof(SetCollisionEnabled), true);
        Modulate = Colors.White;
        GameLogger.Info("Door", $"'{Name}': closed");
    }

    private void SetCollisionEnabled(bool enabled)
    {
        _collision.Disabled = !enabled;
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
