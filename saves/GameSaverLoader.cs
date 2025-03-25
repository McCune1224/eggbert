using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public partial class GameSaverLoader : Node
{
    private void SaveGame()
    {
        SavedGame newSaveState = new();
        SceneTree tree = GetTree();


        Godot.Collections.Array<SavedData> saveDataList = new();
        tree.CallGroup("save", "OnSaveGame", saveDataList);
    }

    private void LoadGame()
    {
        SavedGame loadedGame = ResourceLoader.Load("user://savegame.tres") as SavedGame;
    }
}
