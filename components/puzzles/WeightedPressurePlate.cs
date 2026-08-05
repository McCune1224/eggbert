using Godot;

/// <summary>
/// Pressure plate that stays pressed while a body (player or PushBlock) is on it.
/// Emits signals for connected doors/gates to open/close.
///
/// Usage: place in a level, assign TargetDoorPath, ensure collision layer
/// detects player + pushable objects.
/// </summary>
[GlobalClass]
[Tool]
public partial class WeightedPressurePlate : Area2D
{
    [Signal]
    public delegate void PlatePressedEventHandler();

    [Signal]
    public delegate void PlateReleasedEventHandler();

    /// <summary>Path to a Door node that opens while the plate is pressed.</summary>
    [ExportGroup("Target")]
    [Export] public NodePath TargetDoorPath { get; set; }

    /// <summary>WorldFlag set when a pushable block is resting on the plate (e.g. "tutorial_crate_gate_open").</summary>
    [ExportGroup("Progression")]
    [Export] public string PushablePressedFlag { get; set; } = "";

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

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (TargetDoorPath == null || TargetDoorPath.IsEmpty)
            warnings.Add("TargetDoorPath is not set. The plate will emit signals but won't open any door.");
        return warnings.ToArray();
    }

    public override void _Draw()
    {
        if (!Engine.IsEditorHint()) return;
        // Gizmo: draw a line to the target door so wiring is visible in the editor.
        if (TargetDoorPath == null || TargetDoorPath.IsEmpty) return;
        var target = GetNodeOrNull<Door>(TargetDoorPath);
        if (target == null) return;
        DrawLine(Vector2.Zero, ToLocal(target.GlobalPosition), new Color(1, 0.5f, 0, 0.5f), 2f);
        DrawCircle(ToLocal(target.GlobalPosition), 5f, new Color(1, 0.5f, 0, 0.8f));
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) QueueRedraw();
    }
    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player") && !body.IsInGroup("pushable")) return;

        _bodyCount++;
        if (body.IsInGroup("pushable") && !string.IsNullOrEmpty(PushablePressedFlag))
            WorldFlags.Instance.SetFlag(PushablePressedFlag, true);

        if (_bodyCount == 1)
            Press();
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
        GameLogger.Info("WeightedPressurePlate", $"{Name}: pressed — bodyCount={_bodyCount}, target='{_targetDoor?.Name ?? "none"}'");
    }

    private void Release()
    {
        if (_sprite != null)
            _sprite.Position = Vector2.Zero;

        _targetDoor?.Close();
        EmitSignal(SignalName.PlateReleased);
        GameLogger.Info("WeightedPressurePlate", $"{Name}: released — bodyCount={_bodyCount}, target='{_targetDoor?.Name ?? "none"}'");
    }

}
