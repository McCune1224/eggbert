using Godot;

public partial class FloorSwitch : Area2D
{

    [Signal] public delegate void PressedEventHandler();
    [Signal] public delegate void ReleasedEventHandler();

    [Export] public NodePath TargetDoorPath;
    [Export] public bool Latching = false;

    private int _bodyCount = 0;
    private Door _targetDoor;
    private bool _hasTriggered = false;

    public bool IsPressed => _bodyCount > 0 || (Latching && _hasTriggered);

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
        if (_bodyCount == 0 && !_hasTriggered)
        {
            EmitSignal(SignalName.Pressed);
            _targetDoor?.Open();
        }
        _bodyCount++;
        _hasTriggered = true;
    }

    private void OnBodyExited(Node2D body)
    {
        _bodyCount--;
        if (_bodyCount <= 0)
        {
            _bodyCount = 0;
            EmitSignal(SignalName.Released);
            if (!Latching || !_hasTriggered)
                _targetDoor?.Close();
        }
    }

    public void Reset()
    {
        _hasTriggered = false;
        _bodyCount = 0;
    }
}
