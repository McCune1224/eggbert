using Godot;
using System;

public partial class BaseLevel : Node2D
{
    // Level metadata
    [Export]
    public string LevelName = "";

    [Export]
    public AudioStream LevelMusic;
    // [Export] public Vector2 DefaultSpawnPoint = Vector2.Zero;

    // // UI
    // protected CanvasLayer _uiLayer;

    // Signals
    [Signal] public delegate void LevelStartedEventHandler();
    [Signal] public delegate void LevelEndedEventHandler();

    public override void _Ready()
    {
        // Play music if set
        if (LevelMusic != null)
        {
            AudioManager.Instance.PlayMusic(LevelMusic);
        }

        EmitSignal(SignalName.LevelStarted);
    }
}
