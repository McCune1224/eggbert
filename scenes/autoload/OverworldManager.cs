// // File: scenes/autoload/OverworldManager.cs
// using Godot;
// using System;
//
// public partial class OverworldManager : Node
// {
//     // Singleton instance
//     private static OverworldManager _instance;
//     public static OverworldManager Instance => _instance;
//
//     // Current map reference
//     private Node _currentMap;
//     public Node CurrentMap => _currentMap;
//
//     // Current area identifier
//     public string CurrentArea { get; private set; } = "starting_area";
//
//     public override void _Ready()
//     {
//
//         GD.Print("Overworld _Ready starting");
//
//         try
//         {
//             // Connect to OverworldManager if needed
//             var overworldManager = OverworldManager.Instance;
//             GD.Print("Got OverworldManager instance");
//
//             // _player = OverworldPlayer.Instance;
//             // GD.Print("Got OverworldPlayer instance");
//
//             // Rest of your code
//
//             GD.Print("Overworld _Ready completed successfully");
//         }
//         catch (Exception ex)
//         {
//             GD.PrintErr("Error in Overworld._Ready: " + ex.Message);
//             GD.PrintErr(ex.StackTrace);
//         }
//     }
//
//     // Add this new method to handle the deferred call
//     private void DeferredAddMap(Node map)
//     {
//         AddChild(map);
//         // Any additional setup that needs to happen after adding the map
//     }
//
//     public void LoadMap(string mapPath)
//     {
//         // Unload current map if it exists
//         if (_currentMap != null)
//         {
//             _currentMap.QueueFree();
//         }
//
//         // Load new map
//         PackedScene mapScene = GD.Load<PackedScene>(mapPath);
//         _currentMap = mapScene.Instantiate();
//
//         // Add it to the scene tree
//         CallDeferred("DeferredAddMap", _currentMap);
//         GD.Print($"Map loaded: {mapPath}");
//     }
//
//     public void SetPlayerPosition(Vector2 position)
//     {
//         // No need to store position locally as it's now managed by OverworldPlayer
//         if (OverworldPlayer.Instance != null)
//         {
//             GD.Print($"Player position set to: {position}");
//             OverworldPlayer.Instance.SetInitialPosition(position);
//         }
//     }
//
//     public Vector2 GetPlayerPosition()
//     {
//         if (OverworldPlayer.Instance != null)
//         {
//             return OverworldPlayer.Instance.Position;
//         }
//         return Vector2.Zero;
//     }
//
//     public void SaveCurrentArea()
//     {
//         // Example save functionality
//         GD.Print($"Saving player state in area: {CurrentArea}");
//         // Implementation would connect to your save system
//     }
//
//     public void ChangeArea(string newArea)
//     {
//         CurrentArea = newArea;
//         GD.Print($"Changed to area: {CurrentArea}");
//     }
//
//     public void StartPlayerInteraction()
//     {
//         if (OverworldPlayer.Instance != null)
//         {
//             OverworldPlayer.Instance.StartInteraction();
//         }
//     }
//
//     public void EndPlayerInteraction()
//     {
//         if (OverworldPlayer.Instance != null)
//         {
//             OverworldPlayer.Instance.EndInteraction();
//         }
//     }
// }
//

using Godot;
using System;

public partial class OverworldManager : Node
{
    // Singleton instance
    private static OverworldManager _instance;
    public static OverworldManager Instance => _instance;
    public string CurrentArea { get; private set; } = "starting_area";

    // Current map and player position
    private Node _currentMap;
    private Vector2 _playerPosition = Vector2.Zero;

    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = this;
            GD.Print("OverworldManager singleton initialized");
        }
        else
        {
            GD.PrintErr("Multiple instances of OverworldManager detected!");
        }
    }

    public void LoadMap(string mapPath)
    {
        try
        {
            // Unload current map if it exists
            if (_currentMap != null)
            {
                _currentMap.QueueFree();
                _currentMap = null;
            }

            // Load new map
            var mapScene = ResourceLoader.Load<PackedScene>(mapPath);
            if (mapScene == null)
            {
                GD.PrintErr($"Failed to load map scene: {mapPath}");
                return;
            }

            _currentMap = mapScene.Instantiate();
            AddChild(_currentMap);

            GD.Print($"Map loaded: {mapPath}");

            // If we have a player reference, place them at the stored position
            var player = OverworldPlayer.Instance;
            if (player != null)
            {
                player.SetInitialPosition(_playerPosition);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading map: {e.Message}");
        }
    }

    public void SetPlayerPosition(Vector2 position)
    {
        _playerPosition = position;
    }

    public Vector2 GetPlayerPosition()
    {
        return _playerPosition;
    }

    public void ChangeArea(string newArea)
    {
        CurrentArea = newArea;
        GD.Print($"Changed to area: {CurrentArea}");
    }
}
