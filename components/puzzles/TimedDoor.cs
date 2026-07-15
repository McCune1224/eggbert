using Godot;

public partial class TimedDoor : Door
{
    [Export] public float OpenDuration = 3.0f;
    [Export] public bool BlinkBeforeClose = true;

    private Timer _timer;
    private Tween _blinkTween;
    private const float BlinkDuration = 1.0f;

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

        float timerSeconds = OpenDuration;
        float blinkOffset = OpenDuration - BlinkDuration;
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
        _blinkTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0.15f), BlinkDuration / 12f);
        _blinkTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0.45f), BlinkDuration / 12f);
    }

    private void OnTimerTimeout()
    {
        _blinkTween?.Kill();
        base.Close();
    }

    public override void Close()
    {
        _blinkTween?.Kill();
        base.Close();
    }
}
