using Godot;
using System.Threading.Tasks;

public partial class FadeTransition : CanvasLayer
{
    private static FadeTransition _instance;
    public static FadeTransition Instance => _instance;

    private AnimationPlayer _animationPlayer;
    private Label _bannerLabel;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("Control/AnimationPlayer");
        _bannerLabel = GetNode<Label>("LocationBanner/Label");
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            QueueFree();
            return;
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

    public async void ShowLocation(string locationName)
    {
        _bannerLabel.Text = locationName;
        _animationPlayer.Play("banner_in");
        await ToSignal(_animationPlayer, AnimationPlayer.SignalName.AnimationFinished);
        await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
        _animationPlayer.Play("banner_out");
    }
}
