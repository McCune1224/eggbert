using Godot;

public partial class DashGhost : Sprite2D
{
    private Tween _tween;

    public override void _Ready()
    {
        Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 1.0f);
        _tween = CreateTween();
        _tween.SetTrans(Tween.TransitionType.Quart);
        _tween.SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(this, "modulate:a", 0.0f, 0.5f);
        _tween.TweenCallback(Callable.From(() =>
        {
            QueueFree();
        }));
    }

}
