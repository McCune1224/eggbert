using Godot;

/// <summary>
/// Makes an NPC flee when the player gets too close.
/// Can be chased for unique dialog. Respawns at original position after fleeing.
/// </summary>
[GlobalClass]
[Tool]
public partial class FleeComponent : Node
{
    /// <summary>Distance at which the NPC starts fleeing from the player.</summary>
    [ExportGroup("Flee")]
    [Export(PropertyHint.Range, "16,400,8")] public float FleeRadius { get; set; } = 120f;
    /// <summary>Movement speed while fleeing.</summary>
    [Export(PropertyHint.Range, "16,400,8")] public float FleeSpeed { get; set; } = 100f;
    /// <summary>Node the NPC flees toward. Empty = a random point near its start position.</summary>
    [Export] public NodePath FleeTargetPath { get; set; }
    /// <summary>Dialog shown when the player catches the NPC.</summary>
    [Export] public string[] CatchDialogLines { get; set; }
    /// <summary>Voice style for the catch dialog.</summary>
    [Export] public DialogVoiceResource CatchVoice { get; set; }
    /// <summary>WorldFlag set when the NPC is caught (one-shot catch, e.g. "caught_scuttler").</summary>
    [Export] public string CaughtFlag { get; set; } = "";

    private Node2D _parent;
    private Vector2 _originalPosition;
    private bool _isFleeing = false;
    private bool _hasBeenCaught = false;
    private Vector2 _fleeTarget;

    public override void _Ready()
    {
        _parent = GetParent<Node2D>();
        if (_parent == null)
        {
            GameLogger.Error("FleeComponent", $"'{Name}': parent is null!");
            SetProcess(false);
            return;
        }

        _originalPosition = _parent.GlobalPosition;

        if (!string.IsNullOrEmpty(CaughtFlag) && WorldFlags.Instance.HasFlag(CaughtFlag))
        {
            _hasBeenCaught = true;
            SetProcess(false);
            GameLogger.Debug("FleeComponent", $"'{Name}': already caught (flag '{CaughtFlag}') — disabled");
            return;
        }

        if (FleeTargetPath != null && !FleeTargetPath.IsEmpty)
        {
            var targetNode = _parent.GetNodeOrNull<Node2D>(FleeTargetPath);
            if (targetNode != null)
                _fleeTarget = targetNode.GlobalPosition;
            else
                _fleeTarget = _originalPosition + new Vector2(
                    (float)GD.RandRange(-200, 200),
                    (float)GD.RandRange(-200, 200)
                );
        }
        else
        {
            _fleeTarget = _originalPosition + new Vector2(
                (float)GD.RandRange(-200, 200),
                (float)GD.RandRange(-200, 200)
            );
        }

        SetProcess(true);
        GameLogger.Debug("FleeComponent", $"'{Name}': _Ready — radius={FleeRadius}, speed={FleeSpeed}, target={_fleeTarget}");
    }

    public override void _Process(double delta)
    {
        if (_parent == null || _hasBeenCaught) return;

        var player = Player.Instance;
        if (player == null) return;

        float dist = _parent.GlobalPosition.DistanceTo(player.GlobalPosition);

        if (_isFleeing)
        {
            // Move toward flee target
            Vector2 dir = _fleeTarget - _parent.GlobalPosition;
            if (dir.LengthSquared() < 16f)
            {
                // Reached flee point — reset
                ResetFlee();
                return;
            }

            dir = dir.Normalized();
            _parent.GlobalPosition += dir * FleeSpeed * (float)delta;

            // Check if player caught up (very close)
            if (dist < 30f)
            {
                OnCaught();
                return;
            }

            // If player left, stop fleeing
            if (dist > FleeRadius * 2)
            {
                ResetFlee();
                return;
            }
        }
        else
        {
            // Check if player is too close
            if (dist < FleeRadius)
            {
                _isFleeing = true;
                GameLogger.Debug("FleeComponent", $"'{Name}': started fleeing (player dist={dist:F1})");
            }
        }
    }

    private void OnCaught()
    {
        _hasBeenCaught = true;
        _isFleeing = false;

        if (!string.IsNullOrEmpty(CaughtFlag))
            WorldFlags.Instance.SetFlag(CaughtFlag, true);

        if (CatchDialogLines != null && CatchDialogLines.Length > 0)
        {
            CutsceneController.Instance.StartDialog(CatchDialogLines, CatchVoice);
        }

        SetProcess(false);
        GameLogger.Info("FleeComponent", $"'{Name}': caught! flag='{CaughtFlag}', dialog={CatchDialogLines?.Length ?? 0} lines");
    }

    private void ResetFlee()
    {
        _isFleeing = false;
        _hasBeenCaught = false;
        // Return to original position
        _parent.GlobalPosition = _originalPosition;
        GameLogger.Debug("FleeComponent", $"'{Name}': reset — returned to {_originalPosition}");
    }
}
