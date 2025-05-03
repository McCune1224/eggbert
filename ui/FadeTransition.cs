using Godot;
using System.Threading.Tasks;

public partial class FadeTransition : CanvasLayer
{
    private static FadeTransition _instance;
    public static FadeTransition Instance => _instance;

    private AnimationPlayer _animationPlayer;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("Control/AnimationPlayer");
        if (_instance == null)
        {
            _instance = this;
        }
    }

    public async Task PlayFadeOut()
    {
        _animationPlayer.Play("fade_out");
        await ToSignal(_animationPlayer, AnimationPlayer.SignalName.AnimationFinished);
    }
    public async Task PlayFadeIn()
    {
        _animationPlayer.Play("fade_in");
        await ToSignal(_animationPlayer, AnimationPlayer.SignalName.AnimationFinished);
    }
}
