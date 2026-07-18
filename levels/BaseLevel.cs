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

        GameLogger.Info("BaseLevel", $"'{LevelName}': _Ready — music={LevelMusic?.ResourcePath ?? "none"}, ambience={LevelAmbience?.ResourcePath ?? "none"}");
    }

    public override void _ExitTree()
    {
        if (LevelAmbience != null)
            AudioManager.Instance.StopAmbience();

        GameLogger.Debug("BaseLevel", $"'{LevelName}': _ExitTree — ambience stopped");
    }
}
