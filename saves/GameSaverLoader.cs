using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public partial class GameSaverLoader : Node
{
    public SceneTree currTree { get; private set; }
    public GameSaverLoader(SceneTree scene)
    {
        currTree = scene;
    }

    public void SaveGame()
    {
        SavedGame newSaveState = new();


        Godot.Collections.Array<SavedData> saveDataList = new();
        // if (currTree is null)
        // {
        //     GD.PrintErr("WHAT", currTree);
        // }
        currTree.CallGroup("save", "OnSaveGame", saveDataList);
        newSaveState.SaveData = saveDataList;
        newSaveState.PlayerPosition = OverworldPlayer.Instance.GlobalPosition;
        ResourceSaver.Save(newSaveState, "user://savegame.tres");
    }

    public void LoadGame()
    {
        if (ResourceLoader.Exists("user://savegame.tres"))
        {
            SavedGame loadedGame = ResourceLoader.Load<SavedGame>("user://savegame.tres");
            OverworldPlayer.Instance.GlobalPosition = loadedGame.PlayerPosition;
        }
    }
}
