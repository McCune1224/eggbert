using Godot;

/// <summary>
/// Paired warp tile. Step on one → appear at the other.
/// Supports player and PushBlock teleportation with cooldown.
///
/// Usage: place two TeleportPads in a level, assign each other's TargetPadPath.
/// The pads will teleport anything in groups 'player' or 'pushable'.
/// </summary>
[GlobalClass]
[Tool]
public partial class TeleportPad : Area2D
{
    /// <summary>Path to the paired TeleportPad node that bodies warp to.</summary>
    [ExportGroup("Teleport")]
    [Export] public NodePath TargetPadPath { get; set; }

    /// <summary>Minimum seconds between consecutive teleports (prevents infinite loops).</summary>
    [Export(PropertyHint.Range, "0.1,5,0.1")]
    public float CooldownSeconds { get; set; } = 0.5f;

    private TeleportPad _targetPad;
    private float _cooldownTimer = 0f;
    private bool _onCooldown = false;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer | CollisionConfig.InteractableLayer;

        if (TargetPadPath != null && !TargetPadPath.IsEmpty)
            _targetPad = GetNodeOrNull<TeleportPad>(TargetPadPath);

        BodyEntered += OnBodyEntered;
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (TargetPadPath == null || TargetPadPath.IsEmpty)
            warnings.Add("TargetPadPath is not set. This teleport pad leads nowhere.");
        return warnings.ToArray();
    }

    public override void _Draw()
    {
        if (!Engine.IsEditorHint()) return;
        // Gizmo: draw a line to the paired pad so pairs are visible in the editor.
        if (TargetPadPath == null || TargetPadPath.IsEmpty) return;
        var target = GetNodeOrNull<TeleportPad>(TargetPadPath);
        if (target == null) return;
        DrawLine(Vector2.Zero, ToLocal(target.GlobalPosition), new Color(0.3f, 0.6f, 1, 0.6f), 2f);
        DrawCircle(ToLocal(target.GlobalPosition), 5f, new Color(0.3f, 0.6f, 1, 0.9f));
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) QueueRedraw();
        else if (_onCooldown)
        {
            _cooldownTimer -= (float)delta;
            if (_cooldownTimer <= 0f)
            {
                _onCooldown = false;
                _cooldownTimer = 0f;
            }
        }
    }

    private async void OnBodyEntered(Node2D body)
    {
        if (_onCooldown) return;
        if (_targetPad == null) return;
        if (!body.IsInGroup("player") && !body.IsInGroup("pushable")) return;

        _onCooldown = true;
        _cooldownTimer = CooldownSeconds;

        if (_targetPad._onCooldown == false)
        {
            _targetPad._onCooldown = true;
            _targetPad._cooldownTimer = CooldownSeconds;
        }

        GameLogger.Info("TeleportPad", $"{Name}: teleporting '{body.Name}' to '{_targetPad.Name}' at {_targetPad.GlobalPosition}");

        await FadeTransition.Instance.PlayFadeOut();
        body.GlobalPosition = _targetPad.GlobalPosition;
        await FadeTransition.Instance.PlayFadeIn();
    }
}
