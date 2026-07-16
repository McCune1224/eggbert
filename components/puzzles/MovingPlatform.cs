using Godot;

/// <summary>
/// Platform that follows an AnimationPlayer path. Player rides it via AnimatableBody2D.
/// </summary>
public partial class MovingPlatform : AnimatableBody2D
{
    [ExportGroup("Platform")]
    [Export]
    /// Speed multiplier for the platform animation. 1.0 = normal speed.
    public float Speed { get; set; } = 1.0f;
    private AnimationPlayer _animationPlayer;
    private bool _movingForward = true;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.WallsLayer;

        _animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (_animationPlayer != null && _animationPlayer.HasAnimation("move"))
        {
            _animationPlayer.Play("move");
            _animationPlayer.SpeedScale = Speed * (_movingForward ? 1f : -1f);
        }
    }

    public override void _Process(double delta)
    {
        if (_animationPlayer == null) return;

        // Ping-pong animation
        if (_movingForward && _animationPlayer.CurrentAnimationPosition >= _animationPlayer.CurrentAnimationLength - 0.01f)
        {
            _movingForward = false;
            _animationPlayer.SpeedScale = Speed * -1f;
        }
        else if (!_movingForward && _animationPlayer.CurrentAnimationPosition <= 0.01f)
        {
            _movingForward = true;
            _animationPlayer.SpeedScale = Speed;
        }
    }
}
