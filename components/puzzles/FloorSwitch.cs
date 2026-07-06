using Godot;

public partial class FloorSwitch : Area2D
{
    [Signal] public delegate void PressedEventHandler();
    [Signal] public delegate void ReleasedEventHandler();

    [Export] public NodePath TargetDoorPath;

    private int _bodyCount = 0;
    private Door _targetDoor;

    public bool IsPressed => _bodyCount > 0;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer | CollisionConfig.InteractableLayer;
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        if (!string.IsNullOrEmpty(TargetDoorPath))
            _targetDoor = GetNodeOrNull<Door>(TargetDoorPath);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_bodyCount == 0)
        {
            EmitSignal(SignalName.Pressed);
            _targetDoor?.Open();
        }
        _bodyCount++;
    }

    private void OnBodyExited(Node2D body)
    {
        _bodyCount--;
        if (_bodyCount <= 0)
        {
            _bodyCount = 0;
            EmitSignal(SignalName.Released);
            _targetDoor?.Close();
        }
    }
}
