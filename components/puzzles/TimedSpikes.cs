using Godot;

/// <summary>
/// Spikes that retract and extend on a timer with telegraph animation.
/// </summary>
[GlobalClass]
[Tool]
public partial class TimedSpikes : Area2D
{
    [ExportGroup("Timing")]
    [Export]
    /// HP lost on contact while spikes are extended.
    public int Damage { get; set; } = 1;
    [Export]
    /// Seconds spikes remain extended and dangerous.
    public float ActiveDuration { get; set; } = 2.0f;
    [Export]
    /// Seconds spikes remain retracted (safe).
    public float InactiveDuration { get; set; } = 2.0f;
    [Export]
    /// Seconds of red telegraph flash before spikes extend.
    public float TelegraphDuration { get; set; } = 0.5f;

    private CollisionShape2D _collision;
    private Sprite2D _sprite;
    private Timer _timer;
    private bool _isActive = false;

    private enum SpikeState { Inactive, Telegraphing, Active }
    private SpikeState _state = SpikeState.Inactive;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer;

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");

        BodyEntered += OnBodyEntered;

        _timer = new Timer { OneShot = true };
        AddChild(_timer);
        _timer.Timeout += OnTimerTimeout;

        // Start inactive
        SetSpikeState(false);
        _timer.Start(InactiveDuration);
        GameLogger.Debug("TimedSpikes", $"'{Name}': delay={TelegraphDuration}s, activeTime={ActiveDuration}s, cooldown={InactiveDuration}s");
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (Damage <= 0)
            warnings.Add("Damage is zero or negative — timed spikes have no effect.");
        return warnings.ToArray();
    }

    private void OnTimerTimeout()
    {
        switch (_state)
        {
            case SpikeState.Inactive:
                // Telegraph before extending
                _state = SpikeState.Telegraphing;
                GameLogger.Debug("TimedSpikes", $"'{Name}': state -> Telegraphing");
                if (_sprite != null)
                    _sprite.Modulate = new Color(1, 0.3f, 0.3f, 1); // Flash red
                _timer.Start(TelegraphDuration);
                break;

            case SpikeState.Telegraphing:
                // Extend spikes
                _state = SpikeState.Active;
                GameLogger.Debug("TimedSpikes", $"'{Name}': state -> Active");
                SetSpikeState(true);
                _timer.Start(ActiveDuration);
                break;

            case SpikeState.Active:
                // Retract spikes
                _state = SpikeState.Inactive;
                GameLogger.Debug("TimedSpikes", $"'{Name}': state -> Inactive");
                SetSpikeState(false);
                _timer.Start(InactiveDuration);
                break;
        }
    }

    private void SetSpikeState(bool active)
    {
        _isActive = active;
        if (_collision != null)
            _collision.Disabled = !active;
        if (_sprite != null)
        {
            if (active)
                _sprite.Modulate = Colors.White;
            else
                _sprite.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Grayed out
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!_isActive) return;

        if (body is Player player)
        {
            player.HealthComponent?.TakeDamage(Damage, this);
            GameLogger.Debug("TimedSpikes", $"'{Name}': damaged player");
        }
    }
}
