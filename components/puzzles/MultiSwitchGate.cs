using Godot;

public enum GateMode
{
    And,
    Or
}

[GlobalClass]
[Tool]
public partial class MultiSwitchGate : Node
{
    [ExportGroup("Targets")]
    [Export] public NodePath[] SwitchPaths = System.Array.Empty<NodePath>();
    [Export] public NodePath TargetDoorPath;
    [Export] public GateMode Mode = GateMode.And;
    [Export] public bool LatchOpen = false;

    private FloorSwitch[] _switches;
    private Door _targetDoor;
    private bool _hasOpened = false;


    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (SwitchPaths == null || SwitchPaths.Length == 0)
            warnings.Add("SwitchPaths is empty. No switches are connected.");
        if (TargetDoorPath == null || TargetDoorPath.IsEmpty)
            warnings.Add("TargetDoorPath is not set. The gate won't open any door.");
        return warnings.ToArray();
    }
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

        GameLogger.Debug("MultiSwitchGate", $"{Name}: Evaluate triggered — mode={Mode}, shouldOpen={shouldOpen}, hasOpened={_hasOpened}");

        if (shouldOpen)
        {
            _hasOpened = true;
            _targetDoor.Open();
            GameLogger.Info("MultiSwitchGate", $"{Name}: opening door '{_targetDoor.Name}' (mode={Mode})");
        }
        else if (!LatchOpen)
        {
            _targetDoor.Close();
            GameLogger.Info("MultiSwitchGate", $"{Name}: closing door '{_targetDoor.Name}' (mode={Mode})");
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
