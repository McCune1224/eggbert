using Godot;

[GlobalClass]
[Tool]
public partial class SequencePuzzle : Node
{
    [ExportGroup("Sequence")]
    [Export] public NodePath[] SwitchSequence;
    [Export] public NodePath TargetDoorPath;
    [Export] public bool LatchOnComplete = true;
    private FloorSwitch[] _switches;
    private Door _targetDoor;
    private int _currentIndex = 0;
    private bool _completed = false;

    public override void _Ready()
    {
        _switches = new FloorSwitch[SwitchSequence.Length];
        for (int i = 0; i < SwitchSequence.Length; i++)
        {
            _switches[i] = GetNodeOrNull<FloorSwitch>(SwitchSequence[i]);
            if (_switches[i] != null)
            {
                int capturedIndex = i;
                _switches[i].Pressed += () => OnSwitchPressed(_switches[capturedIndex]);
            }
        }

        if (!TargetDoorPath.IsEmpty)
            _targetDoor = GetNodeOrNull<Door>(TargetDoorPath);
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (SwitchSequence == null || SwitchSequence.Length == 0)
            warnings.Add("SwitchSequence is empty. No switches are connected.");
        if (TargetDoorPath == null || TargetDoorPath.IsEmpty)
            warnings.Add("TargetDoorPath is not set. The puzzle won't open any door.");
        return warnings.ToArray();
    }

    private void OnSwitchPressed(FloorSwitch pressedSwitch)
    {
        if (_completed) return;

        int pressedIndex = System.Array.IndexOf(_switches, pressedSwitch);
        if (pressedIndex < 0) return;

        if (pressedIndex == _currentIndex)
        {
            _currentIndex++;
            GameLogger.Debug("Puzzle", $"SequencePuzzle '{Name}': correct step {_currentIndex}/{_switches.Length}");

            if (_currentIndex >= _switches.Length)
            {
                _completed = true;
                _targetDoor?.Open();
                GameLogger.Info("Puzzle", $"SequencePuzzle '{Name}': complete!");
            }
        }
        else
        {
            GameLogger.Debug("Puzzle", $"SequencePuzzle '{Name}': wrong order (expected step {_currentIndex}, got {pressedIndex}). Resetting.");
            ResetAll();
        }
    }

    private void ResetAll()
    {
        _currentIndex = 0;
        foreach (var s in _switches)
            s?.Reset();
        _targetDoor?.Close();
    }
}
