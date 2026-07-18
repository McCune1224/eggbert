using Godot;

/// <summary>
/// Makes an NPC patrol between waypoints.
/// Pauses movement and faces player when interacted with.
/// Resumes patrol after the dialog ends.
/// </summary>
[GlobalClass]
[Tool]
public partial class PatrolComponent : Node
{
    [Export] public NodePath[] Waypoints { get; set; }
    [Export] public float Speed { get; set; } = 40f;
    [Export] public float PauseSeconds { get; set; } = 1.0f;

    private Node2D _parent;
    private int _currentWaypoint = 0;
    private bool _isPaused = false;
    private float _pauseTimer = 0f;
    private bool _waitingAfterDialog = false;
    private int _lastMissingWaypoint = -1;

    public override void _Ready()
    {
        _parent = GetParent<Node2D>();
        if (_parent == null)
        {
            GameLogger.Error("PatrolComponent", $"'{Name}': parent is null!");
            SetProcess(false);
            return;
        }

        if (Waypoints == null || Waypoints.Length == 0)
        {
            GameLogger.Warn("PatrolComponent", $"'{Name}': no waypoints assigned — disabled");
            SetProcess(false);
            return;
        }

        // Listen for dialog completion via signal on the parent
        var trigger = _parent.GetNodeOrNull<CutsceneTrigger>("CutsceneTrigger");
        if (trigger != null)
        {
            trigger.Triggered += OnInteractionStarted;
        }

        SetProcess(true);
        GameLogger.Debug("PatrolComponent", $"'{Name}': _Ready — {Waypoints.Length} waypoints, speed={Speed}, pause={PauseSeconds}s");
    }

    public override void _Process(double delta)
    {
        if (_isPaused || _parent == null)
            return;

        if (_waitingAfterDialog)
        {
            _pauseTimer += (float)delta;
            if (_pauseTimer >= PauseSeconds)
            {
                _waitingAfterDialog = false;
                _pauseTimer = 0f;
            }
            return;
        }

        if (Waypoints == null || Waypoints.Length == 0 || _currentWaypoint >= Waypoints.Length)
            return;

        var missingWaypoint = _currentWaypoint;
        var targetNode = _parent.GetNodeOrNull<Node2D>(Waypoints[missingWaypoint]);
        if (targetNode == null)
        {
            _currentWaypoint = (_currentWaypoint + 1) % Waypoints.Length;
            if (_lastMissingWaypoint != missingWaypoint)
            {
                _lastMissingWaypoint = missingWaypoint;
                GameLogger.Warn("PatrolComponent", $"'{Name}': waypoint {missingWaypoint} node not found — skipping");
            }
            return;
        }
        _lastMissingWaypoint = -1;

        Vector2 targetPos = targetNode.GlobalPosition;
        Vector2 dir = targetPos - _parent.GlobalPosition;

        if (dir.LengthSquared() < 4f)
        {
            // Reached waypoint - wait then move to next
            _pauseTimer += (float)delta;
            if (_pauseTimer >= PauseSeconds)
            {
                _pauseTimer = 0f;
                _currentWaypoint = (_currentWaypoint + 1) % Waypoints.Length;
                GameLogger.Debug("PatrolComponent", $"'{Name}': reached waypoint {_currentWaypoint} (of {Waypoints.Length})");
            }
            return;
        }

        dir = dir.Normalized();
        _parent.GlobalPosition += dir * Speed * (float)delta;
    }

    private void OnInteractionStarted()
    {
        _isPaused = true;
        GameLogger.Debug("PatrolComponent", $"'{Name}': paused for interaction");
    }

    /// <summary>
    /// Called externally when dialog finishes — resumes patrol.
    /// </summary>
    public void ResumePatrol()
    {
        _isPaused = false;
        _waitingAfterDialog = true;
        _pauseTimer = 0f;
        GameLogger.Debug("PatrolComponent", $"'{Name}': resuming patrol");
    }
}
