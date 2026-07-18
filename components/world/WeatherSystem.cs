using Godot;

/// <summary>
/// Timer-driven weather events (rain, etc.) that pass through outdoor zones.
/// Spawns overlay particles, darkens screen briefly.
/// </summary>
public partial class WeatherSystem : Node
{
    [Export] public float MinInterval { get; set; } = 60f;
    [Export] public float MaxInterval { get; set; } = 180f;
    [Export] public float RainDuration { get; set; } = 30f;

    private Timer _timer;
    private bool _isRaining = false;
    private GpuParticles2D _rainParticles;
    private ColorRect _darkOverlay;
    private Tween _tween;

    public override void _Ready()
    {
        _rainParticles = GetNodeOrNull<GpuParticles2D>("RainParticles");
        if (_rainParticles != null)
            _rainParticles.Emitting = false;
        else
            GameLogger.Debug("WeatherSystem", $"'{Name}': RainParticles node not found");

        _darkOverlay = new ColorRect
        {
            Color = new Color(0, 0, 0, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false
        };
        _darkOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        _timer = new Timer { OneShot = true };
        AddChild(_timer);
        _timer.Timeout += StartRain;
        float delay = (float)GD.RandRange(MinInterval, MaxInterval);
        _timer.Start(delay);

        GameLogger.Debug("WeatherSystem", $"'{Name}': _Ready — interval=[{MinInterval},{MaxInterval}]s, first rain in {delay:F1}s");
    }

    private void StartRain()
    {
        if (_isRaining) return;
        _isRaining = true;

        GameLogger.Info("WeatherSystem", $"'{Name}': rain started (duration={RainDuration}s)");

        if (_rainParticles != null)
        {
            _rainParticles.Emitting = true;
            _rainParticles.Amount = 100;
        }

        var root = GetTree().Root;
        root.AddChild(_darkOverlay);
        _darkOverlay.Visible = true;

        _tween = CreateTween();
        _tween.TweenProperty(_darkOverlay, "color", new Color(0, 0, 0, 0.15f), 2f);

        // Reuse main timer for rain duration
        _timer.Timeout -= StartRain;
        _timer.Timeout += StopRain;
        _timer.Start(RainDuration);
    }

    private void StopRain()
    {
        _isRaining = false;

        GameLogger.Info("WeatherSystem", $"'{Name}': rain stopped");

        if (_rainParticles != null)
            _rainParticles.Emitting = false;

        _tween = CreateTween();
        _tween.TweenProperty(_darkOverlay, "color", new Color(0, 0, 0, 0), 2f);
        _tween.TweenCallback(Callable.From(() =>
        {
            _darkOverlay.Visible = false;
            _darkOverlay.GetParent()?.RemoveChild(_darkOverlay);
        }));

        // Schedule next rain
        _timer.Timeout -= StopRain;
        _timer.Timeout += StartRain;
        float nextDelay = (float)GD.RandRange(MinInterval, MaxInterval);
        _timer.Start(nextDelay);

        GameLogger.Debug("WeatherSystem", $"'{Name}': next rain scheduled in {nextDelay:F1}s");
    }
}
