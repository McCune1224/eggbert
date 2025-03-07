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

    public void LoadCombatScene(string mapPath)
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

            // Clean up the OverworldPlayer for now
            var owPlayer = OverworldPlayer.Instance;
            if (owPlayer != null)
            {
                owPlayer.QueueFree();
            }
            PackedScene combatPlayerScene = ResourceLoader.Load<PackedScene>("res://scenes/combat/player/CombatPlayer.tscn");
            AddChild(combatPlayerScene.Instantiate());
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading Combat scene: {e.Message}");
        }
    }
    public void LoadOverworldScene(string mapPath)
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
                GD.PrintErr($"Failed to load Overworld scene: {mapPath}");
                return;
            }

            _currentMap = mapScene.Instantiate();
            AddChild(_currentMap);

            GD.Print($"Overworld Scene loaded: {mapPath}");

            // If we have a player reference, place them at the stored position
            var player = OverworldPlayer.Instance;
            if (player != null)
            {
                player.SetInitialPosition(_playerPosition);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading Overworld scene: {e.Message}");
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
