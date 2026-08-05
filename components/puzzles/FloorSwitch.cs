using Godot;

[GlobalClass]
[Tool]
public partial class FloorSwitch : Area2D
{

    [Signal] public delegate void PressedEventHandler();
    [Signal] public delegate void ReleasedEventHandler();

    /// <summary>
    /// NodePath to the <see cref="Door"/> this switch controls. Must be set after both the
    /// FloorSwitch and the target Door nodes are placed in the scene so the path resolves
    /// correctly at runtime.
    /// </summary>
    [ExportGroup("Target")]
    [Export] public NodePath TargetDoorPath;
    /// <summary>If true, the door stays open after the switch is pressed once (doesn't close on release).</summary>
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

        GameLogger.Debug("FloorSwitch", $"'{Name}': latching={Latching}");
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (string.IsNullOrEmpty(TargetDoorPath))
            warnings.Add("TargetDoorPath is not set. The switch won't open any door.");
        return warnings.ToArray();
    }

    public override void _Draw()
    {
        if (!Engine.IsEditorHint()) return;
        // Gizmo: draw a line from the switch to its target door so wiring is visible in the editor.
        if (TargetDoorPath == null || TargetDoorPath.IsEmpty) return;
        var target = GetNodeOrNull<Door>(TargetDoorPath);
        if (target == null) return;
        DrawLine(Vector2.Zero, ToLocal(target.GlobalPosition), new Color(1, 1, 0, 0.5f), 2f);
        DrawCircle(ToLocal(target.GlobalPosition), 5f, new Color(1, 1, 0, 0.8f));
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) QueueRedraw();
    }


    private void OnBodyEntered(Node2D body)
    {
        if (_bodyCount == 0 && !_hasTriggered)
        {
            EmitSignal(SignalName.Pressed);
            _targetDoor?.Open();
            GameLogger.Info("FloorSwitch", $"'{Name}': pressed");
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
            GameLogger.Info("FloorSwitch", $"'{Name}': released");
            if (!Latching || !_hasTriggered)
                _targetDoor?.Close();
            if (!Latching)
                _hasTriggered = false;
        }
    }

    public void Reset()
    {
        _hasTriggered = false;
        _bodyCount = 0;
    }
}
