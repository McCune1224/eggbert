using Godot;

public partial class BaseLevel : Node2D
{
    // Level metadata
    [Export]
    public string LevelName = "";

    [Export]
    public AudioStream LevelMusic;

    [Export]
    public AudioStream LevelAmbience;

    // // UI
    // protected CanvasLayer _uiLayer;

    // Signals
    [Signal] public delegate void LevelStartedEventHandler();
    [Signal] public delegate void LevelEndedEventHandler();

    public override void _Ready()
    {
        if (LevelMusic != null)
            AudioManager.Instance.PlayMusic(LevelMusic);

        if (LevelAmbience != null)
            AudioManager.Instance.PlayAmbience(LevelAmbience);

        if (LevelName == "")
        {
            LevelName = Name;
        }

        EmitSignal(SignalName.LevelStarted);
    }

    public override void _ExitTree()
    {
        if (LevelAmbience != null)
            AudioManager.Instance.StopAmbience();
    }
}
