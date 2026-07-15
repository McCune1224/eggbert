using Godot;

public partial class Door : StaticBody2D
{

    [Export] public bool StartOpen = false;

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
            Open();
    }


    public virtual void Open()
    {
        CallDeferred(nameof(SetCollisionEnabled), false);
        Modulate = new Color(1, 1, 1, 0.3f);
    }

    public virtual void Close()
    {
        CallDeferred(nameof(SetCollisionEnabled), true);
        Modulate = Colors.White;
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
    }
}
