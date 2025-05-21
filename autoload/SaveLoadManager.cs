using Godot;
using System;

public partial class SaveLoadManager : Node
{

    const string SAVE_PATH = "user://";

    [Signal]
    public delegate void GameLoadedEventHandler();

    [Signal]
    public delegate void GameSavedEventHandler();

    private Godot.Collections.Dictionary _currentSave = new();
}
