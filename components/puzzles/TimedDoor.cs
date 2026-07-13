using Godot;

public partial class TimedDoor : Door
{
    [Export] public float OpenDuration = 3.0f;
    [Export] public bool BlinkBeforeClose = true;

    private Timer _timer;
    private Tween _blinkTween;
    private float _blinkDuration = 1.0f;
    private bool _isTimedOpen = false;

    public override void _Ready()
    {
        base._Ready();
        _timer = new Timer();
        _timer.OneShot = true;
        _timer.Timeout += OnTimerTimeout;
        AddChild(_timer);
    }

    public override void Open()
    {
        base.Open();
        _isTimedOpen = true;

        float timerSeconds = OpenDuration;
        float blinkOffset = OpenDuration - _blinkDuration;
        if (BlinkBeforeClose && blinkOffset > 0.3f)
        {
            timerSeconds = blinkOffset;
            CallDeferred(nameof(StartBlinkDeferred));
        }

        _timer.Start(timerSeconds);
    }

    private void StartBlinkDeferred()
    {
        _blinkTween?.Kill();
        _blinkTween = CreateTween();
        _blinkTween.SetLoops(6);
        _blinkTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0.15f), _blinkDuration / 12f);
        _blinkTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0.45f), _blinkDuration / 12f);
    }

    private void OnTimerTimeout()
    {
        _blinkTween?.Kill();
        _isTimedOpen = false;
        base.Close();
    }

    public override void Close()
    {
        _blinkTween?.Kill();
        _isTimedOpen = false;
        base.Close();
    }
}
