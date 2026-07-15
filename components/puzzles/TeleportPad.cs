using Godot;

/// <summary>
/// Paired warp tile. Step on one → appear at the other.
/// Supports player and PushBlock teleportation with cooldown.
/// </summary>
public partial class TeleportPad : Area2D
{
    [Export] public NodePath TargetPadPath { get; set; }
    [Export] public float CooldownSeconds { get; set; } = 0.5f;

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

    public override void _Process(double delta)
    {
        if (_onCooldown)
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

        // Brief visual feedback at both pads
        if (_targetPad._onCooldown == false)
        {
            _targetPad._onCooldown = true;
            _targetPad._cooldownTimer = CooldownSeconds;
        }

        await FadeTransition.Instance.PlayFadeOut();
        body.GlobalPosition = _targetPad.GlobalPosition;
        await FadeTransition.Instance.PlayFadeIn();
    }
}
