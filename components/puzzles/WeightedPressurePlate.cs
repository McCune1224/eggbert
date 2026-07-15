using Godot;

/// <summary>
/// Pressure plate that stays pressed while a body (player or PushBlock) is on it.
/// Emits signals for connected doors/gates to open/close.
/// </summary>
public partial class WeightedPressurePlate : Area2D
{
    [Signal]
    public delegate void PlatePressedEventHandler();

    [Signal]
    public delegate void PlateReleasedEventHandler();

    [Export] public NodePath TargetDoorPath { get; set; }

    private int _bodyCount = 0;
    private Door _targetDoor;
    private Sprite2D _sprite;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer | CollisionConfig.InteractableLayer;

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");

        if (TargetDoorPath != null && !TargetDoorPath.IsEmpty)
            _targetDoor = GetNodeOrNull<Door>(TargetDoorPath);

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player") && !body.IsInGroup("pushable")) return;

        _bodyCount++;
        if (_bodyCount == 1)
        {
            Press();
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player") && !body.IsInGroup("pushable")) return;

        _bodyCount--;
        if (_bodyCount <= 0)
        {
            _bodyCount = 0;
            Release();
        }
    }

    private void Press()
    {
        if (_sprite != null)
            _sprite.Position = new Vector2(0, 4);

        _targetDoor?.Open();
        EmitSignal(SignalName.PlatePressed);
    }

    private void Release()
    {
        if (_sprite != null)
            _sprite.Position = Vector2.Zero;

        _targetDoor?.Close();
        EmitSignal(SignalName.PlateReleased);
    }
}
