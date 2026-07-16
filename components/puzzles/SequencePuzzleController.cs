using Godot;

public partial class SequencePuzzleController : Node
{
    [ExportGroup("Targets")]
    /// Array of NodePaths to SequencePressurePlate nodes, in expected press order.
    [Export] public NodePath[] PlatePaths { get; set; }
    /// Door node that opens when the sequence is solved.
    [Export] public NodePath TargetDoorPath { get; set; }
    [ExportGroup("Timing")]
    /// Seconds the player has to press the next plate before the sequence resets.
    [Export] public float TimeWindow { get; set; } = 5.0f;

    private int _nextExpectedIndex = 0;
    private Timer _timer;
    private Door _targetDoor;
    private SequencePressurePlate[] _plates;

    public override void _Ready()
    {
        _timer = new Timer { OneShot = true };
        AddChild(_timer);
        _timer.Timeout += OnTimerExpired;

        if (TargetDoorPath != null && !TargetDoorPath.IsEmpty)
            _targetDoor = GetNodeOrNull<Door>(TargetDoorPath);

        _plates = new SequencePressurePlate[PlatePaths.Length];
        for (int i = 0; i < PlatePaths.Length; i++)
        {
            if (PlatePaths[i] != null && !PlatePaths[i].IsEmpty)
            {
                _plates[i] = GetNodeOrNull<SequencePressurePlate>(PlatePaths[i]);
                if (_plates[i] != null)
                    _plates[i]._controller = this;
            }
        }
    }

    public void StepPressed(int index)
    {
        if (index == _nextExpectedIndex)
        {
            _plates[index]?.Flash(true);
            _nextExpectedIndex++;
            _timer.Stop();
            _timer.Start(TimeWindow);

            if (_nextExpectedIndex >= PlatePaths.Length)
            {
                // All plates pressed in correct order
                _targetDoor?.Open();
                _timer.Stop();
                GameLogger.Info("SequencePuzzle", "Puzzle solved — door opened.");
            }
        }
        else
        {
            // Wrong plate — reset
            _plates[index]?.Flash(false);
            ResetPuzzle();
        }
    }

    private void OnTimerExpired()
    {
        ResetPuzzle();
    }

    private void ResetPuzzle()
    {
        _nextExpectedIndex = 0;
        _timer.Stop();

        foreach (var plate in _plates)
        {
            plate?.Flash(false);
        }

        GameLogger.Debug("SequencePuzzle", "Puzzle reset.");
    }
}
