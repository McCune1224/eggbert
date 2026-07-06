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

    public override void _Ready()
    {
        _collision = GetNode<CollisionShape2D>("CollisionShape2D");
        _sprite = GetNode<Sprite2D>("Sprite2D");
        if (_texture != null) _sprite.Texture = _texture;
        if (StartOpen)
            Open();
    }

    public void Open()
    {
        _collision.Disabled = true;
        Modulate = new Color(1, 1, 1, 0.3f);
    }

    public void Close()
    {
        _collision.Disabled = false;
        Modulate = Colors.White;
    }

    public void Toggle()
    {
        if (_collision.Disabled)
            Close();
        else
            Open();
    }
}
