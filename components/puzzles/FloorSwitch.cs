using Godot;

public partial class FloorSwitch : Area2D
{
    [Signal] public delegate void PressedEventHandler();
    [Signal] public delegate void ReleasedEventHandler();

    private int _bodyCount = 0;

    public bool IsPressed => _bodyCount > 0;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer | CollisionConfig.InteractableLayer;
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_bodyCount == 0)
            EmitSignal(SignalName.Pressed);
        _bodyCount++;
    }

    private void OnBodyExited(Node2D body)
    {
        _bodyCount--;
        if (_bodyCount <= 0)
        {
            _bodyCount = 0;
            EmitSignal(SignalName.Released);
        }
    }
}
