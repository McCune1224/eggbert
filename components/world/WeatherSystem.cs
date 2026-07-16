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
        _timer.Start((float)GD.RandRange(MinInterval, MaxInterval));
    }

    private void StartRain()
    {
        if (_isRaining) return;
        _isRaining = true;

        if (_rainParticles != null)
        {
            _rainParticles.Emitting = true;
            _rainParticles.Amount = 100;
        }

        // Add dark overlay to root
        var root = GetTree().Root;
        root.AddChild(_darkOverlay);
        _darkOverlay.Visible = true;

        _tween = CreateTween();
        _tween.TweenProperty(_darkOverlay, "color", new Color(0, 0, 0, 0.15f), 2f);

        // Schedule end
        var endTimer = new Timer { OneShot = true };
        AddChild(endTimer);
        endTimer.Timeout += StopRain;
        endTimer.Start(RainDuration);
    }

    private void StopRain()
    {
        _isRaining = false;

        if (_rainParticles != null)
            _rainParticles.Emitting = false;

        _tween = CreateTween();
        _tween.TweenProperty(_darkOverlay, "color", new Color(0, 0, 0, 0), 2f);
        _tween.TweenCallback(Callable.From(() =>
        {
            _darkOverlay.Visible = false;
            _darkOverlay.GetParent()?.RemoveChild(_darkOverlay);
        }));

        // Schedule next
        _timer.Start((float)GD.RandRange(MinInterval, MaxInterval));
    }
}
