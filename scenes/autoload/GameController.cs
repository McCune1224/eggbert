using Godot;
using System;

public partial class GameController : Node
{
    // Singleton instance
    private static GameController _instance;
    public static GameController Instance => _instance;
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
            // Disable the player's camera if it exists
            var player = Player.Instance;
            if (player != null)
            {
                Camera2D playerCamera = player.GetNode<Camera2D>("Camera2D");
                if (playerCamera != null)
                {
                    playerCamera.Enabled = false;
                }
            }

            // Rest of your existing code...
            if (_currentMap != null)
            {
                _currentMap.QueueFree();
                _currentMap = null;
            }

            var mapScene = ResourceLoader.Load<PackedScene>(mapPath);
            if (mapScene == null)
            {
                GD.PrintErr($"Failed to load map scene: {mapPath}");
                return;
            }



            _currentMap = mapScene.Instantiate();
            AddChild(_currentMap);

            GD.Print($"Map loaded: {mapPath}");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading Combat scene: {e.Message}");
        }
    }

    public void LoadOverworldScene(string scenePath)
    {
        try
        {
            // Rest of your existing code...
            if (_currentMap != null)
            {
                _currentMap.QueueFree();
                _currentMap = null;
            }

            var mapScene = ResourceLoader.Load<PackedScene>(scenePath);
            if (mapScene == null)
            {
                GD.PrintErr($"Failed to load Overworld scene: {scenePath}");
                return;
            }

            _currentMap = mapScene.Instantiate();
            AddChild(_currentMap);


            var overworldMenu = ResourceLoader.Load<PackedScene>("res://scenes/ui/OverworldMenu.tscn");
            var canvasLayer = new CanvasLayer();
            canvasLayer.AddChild(overworldMenu.Instantiate());
            AddChild(canvasLayer);

            GD.Print($"Overworld Scene loaded: {_currentMap.Name}");

            // If we have a player reference, place them at the stored position
            var player = Player.Instance;
            if (player != null)
            {
                player.SetInitialPosition(_playerPosition);

                // Re-enable the player's camera
                var playerCamera = player.GetNode<Camera2D>("Camera2D");
                if (playerCamera != null)
                {
                    playerCamera.Enabled = true;
                    playerCamera.MakeCurrent();
                }
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

    public void SetCurrentArea(string area)
    {
        CurrentArea = area;
    }
}
