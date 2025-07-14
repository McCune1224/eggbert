using Godot;
using System;

public partial class DashGhost : Sprite2D
{
    private AnimationPlayer _animationPlayer;
    private Tween _tween;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 1.0f);
        // _animationPlayer.Play("dash_ghost");
        _tween = CreateTween();
        _tween.SetTrans(Tween.TransitionType.Quart);
        _tween.SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(this, "modulate:a", 0.0f, 0.5f);
        _tween.TweenCallback(Callable.From(() =>
        {
            QueueFree(); // Remove the ghost after the animation completes
        }));
    }

}
