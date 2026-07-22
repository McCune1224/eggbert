using Godot;

[GlobalClass]
[Tool]
public partial class TimedDoor : Door
{
    [ExportGroup("TimedDoor")]
    [Export] public float OpenDuration = 3.0f;
    [Export] public bool BlinkBeforeClose = true;

    private Timer _closeTimer;
    private Timer _warningTimer;
    private Tween _blinkTween;
    private int _openGeneration;
    private const float BlinkDuration = 1.0f;

    public override void _Ready()
    {
        _closeTimer = new Timer { OneShot = true };
        _closeTimer.Timeout += OnCloseTimerTimeout;
        AddChild(_closeTimer);

        _warningTimer = new Timer { OneShot = true };
        _warningTimer.Timeout += OnWarningTimerTimeout;
        AddChild(_warningTimer);

        base._Ready();
    }

    public override void Open()
    {
        _openGeneration++;
        StopBlinking();
        _closeTimer.Stop();
        _warningTimer.Stop();
        base.Open();
        GameLogger.Info("TimedDoor", $"{Name}: opened — duration={OpenDuration}s, blinkBeforeClose={BlinkBeforeClose}");

        if (OpenDuration <= 0.0f)
        {
            CallDeferred(nameof(CloseDeferred), _openGeneration);
            return;
        }

        _closeTimer.Start(OpenDuration);
        if (!BlinkBeforeClose)
            return;

        if (OpenDuration <= BlinkDuration)
            StartBlinking();
        else
            _warningTimer.Start(OpenDuration - BlinkDuration);
    }

    public override void Close()
    {
        if (!_closeTimer.IsStopped())
        {
            GameLogger.Debug("TimedDoor", $"{Name}: ignored external close while open interval remains.");
            return;
        }

        _warningTimer.Stop();
        StopBlinking();
        base.Close();
    }

    private void OnWarningTimerTimeout()
    {
        StartBlinking();
    }

    private void OnCloseTimerTimeout()
    {
        _warningTimer.Stop();
        StopBlinking();
        GameLogger.Info("TimedDoor", $"{Name}: auto-closing after {OpenDuration}s");
        base.Close();
    }

    private void CloseDeferred(int openGeneration)
    {
        if (openGeneration != _openGeneration)
            return;

        _warningTimer.Stop();
        StopBlinking();
        base.Close();
    }

    private void StartBlinking()
    {
        GameLogger.Info("TimedDoor", $"{Name}: blink started");
        StopBlinking();
        _blinkTween = CreateTween();
        _blinkTween.SetLoops(6);
        _blinkTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0.15f), BlinkDuration / 12f);
        _blinkTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0.45f), BlinkDuration / 12f);
    }

    private void StopBlinking()
    {
        _blinkTween?.Kill();
        _blinkTween = null;
    }
}
