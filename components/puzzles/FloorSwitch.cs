using Godot;

public partial class FloorSwitch : Area2D
{
    public static bool DebugVisible = false;

    [Signal] public delegate void PressedEventHandler();
    [Signal] public delegate void ReleasedEventHandler();

    [Export] public NodePath TargetDoorPath;
    [Export] public bool Latching = false;

    private int _bodyCount = 0;
    private Door _targetDoor;
    private Label _debugLabel;
    private bool _hasTriggered = false;

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

    public override void _Process(double delta)
    {
        if (_debugLabel == null && DebugVisible)
        {
            _debugLabel = new Label();
            _debugLabel.AddThemeFontSizeOverride("font_size", 10);
            _debugLabel.AddThemeColorOverride("font_color", Colors.Cyan);
            _debugLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
            _debugLabel.AddThemeConstantOverride("outline_size", 1);
            AddChild(_debugLabel);
            _debugLabel.Position = new Vector2(-48, -24);
        }
        if (_debugLabel != null)
        {
            _debugLabel.Visible = DebugVisible;
            if (DebugVisible)
                _debugLabel.Text = IsPressed
                    ? $"[Switch] PRESSED ({_bodyCount})"
                    : "[Switch] released";
        }
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
