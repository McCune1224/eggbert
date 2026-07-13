using Godot;

public enum GateMode
{
    And,
    Or
}

public partial class MultiSwitchGate : Node
{
    [Export] public NodePath[] SwitchPaths = System.Array.Empty<NodePath>();
    [Export] public NodePath TargetDoorPath;
    [Export] public GateMode Mode = GateMode.And;
    [Export] public bool LatchOpen = false;

    private FloorSwitch[] _switches;
    private Door _targetDoor;
    private bool _hasOpened = false;

    public override void _Ready()
    {
        _switches = new FloorSwitch[SwitchPaths.Length];
        for (int i = 0; i < SwitchPaths.Length; i++)
        {
            _switches[i] = GetNodeOrNull<FloorSwitch>(SwitchPaths[i]);
            if (_switches[i] != null)
            {
                _switches[i].Pressed += OnSwitchChanged;
                _switches[i].Released += OnSwitchChanged;
            }
        }

        if (!TargetDoorPath.IsEmpty)
            _targetDoor = GetNodeOrNull<Door>(TargetDoorPath);

        Evaluate();
    }

    private void OnSwitchChanged()
    {
        Evaluate();
    }

    private void Evaluate()
    {
        if (_switches == null || _switches.Length == 0) return;
        if (_targetDoor == null) return;
        if (LatchOpen && _hasOpened) return;

        bool shouldOpen = Mode == GateMode.And
            ? AreAllPressed()
            : IsAnyPressed();

        if (shouldOpen)
        {
            _hasOpened = true;
            _targetDoor.Open();
        }
        else if (!LatchOpen)
        {
            _targetDoor.Close();
        }
    }

    private bool AreAllPressed()
    {
        foreach (var s in _switches)
            if (s == null || !s.IsPressed) return false;
        return true;
    }

    private bool IsAnyPressed()
    {
        foreach (var s in _switches)
            if (s != null && s.IsPressed) return true;
        return false;
    }
}
