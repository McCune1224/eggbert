/// <summary>
/// Root node for every level. Exports the level name, music, and ambience. Auto-plays music and
/// ambience on <see cref="_Ready"/> and stops ambience on <see cref="_ExitTree"/>.
/// </summary>
/// <remarks>
/// <b>LevelName:</b> Set via the Inspector or defaults to the node's <see cref="Node.Name"/>.
/// <b>LevelMusic / LevelAmbience:</b> Played automatically when the level loads. Music loops
/// indefinitely; ambience stops when the level exits the tree.
/// <b>Signal lifecycle:</b> <see cref="LevelStarted"/> fires at the end of <see cref="_Ready"/>;
/// <see cref="LevelEnded"/> should be emitted by subclasses or call sites when the level is
/// being unloaded.
/// </remarks>

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
