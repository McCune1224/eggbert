using Godot;

public partial class Door : StaticBody2D
{
    [Export] public bool StartOpen = false;

    private CollisionShape2D _collision;

    public override void _Ready()
    {
        _collision = GetNode<CollisionShape2D>("CollisionShape2D");
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
