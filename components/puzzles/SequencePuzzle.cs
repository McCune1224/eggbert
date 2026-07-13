using Godot;

public partial class SequencePuzzle : Node
{
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
                _switches[i].Pressed += () => OnSwitchPressed(_switches[i]);
        }

        if (!TargetDoorPath.IsEmpty)
            _targetDoor = GetNodeOrNull<Door>(TargetDoorPath);
    }

    private void OnSwitchPressed(FloorSwitch pressedSwitch)
    {
        if (_completed) return;

        int pressedIndex = System.Array.IndexOf(_switches, pressedSwitch);
        if (pressedIndex < 0) return;

        if (pressedIndex == _currentIndex)
        {
            _currentIndex++;
            GD.Print($"SequencePuzzle: correct step {_currentIndex}/{_switches.Length}");

            if (_currentIndex >= _switches.Length)
            {
                _completed = true;
                _targetDoor?.Open();
                GD.Print("SequencePuzzle: complete!");
            }
        }
        else
        {
            GD.Print($"SequencePuzzle: wrong order (expected step {_currentIndex}, got {pressedIndex}). Resetting.");
            ResetAll();
        }
    }

    private void ResetAll()
    {
        _currentIndex = 0;
        _targetDoor?.Close();
    }
}
