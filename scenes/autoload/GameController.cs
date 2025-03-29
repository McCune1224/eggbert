using Godot;
using System;

public partial class GameController : Node
{
    // Current map and player position
    private Node _currentMap;
    private Vector2 _playerPosition = Vector2.Zero;

    // Singleton instance
    private static GameController _instance;
    public static GameController Instance => _instance;
    public string CurrentArea { get; private set; } = "starting_area";

    // Dialog Managment
    // public PackedScene DialogManagerScene;
    private PackedScene DialogManagerScene;
    public DialogManager DialogManager;

    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            GD.PrintErr("Multiple instances of OverworldManager detected!");
        }

        DialogManagerScene = ResourceLoader.Load<PackedScene>("res://scripts/ui/DialogManager.tscn");
    }

    public void LoadCombatScene(string mapPath)
    {
        try
        {
            // Disable the player's camera if it exists
            var player = OverworldPlayer.Instance;
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
            if (DialogManager == null)
            {
                DialogManager = DialogManagerScene.Instantiate<DialogManager>();
                AddChild(DialogManager);
            }
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
            _currentMap.AddToGroup("overworld");
            AddChild(_currentMap);


            var overworldMenu = ResourceLoader.Load<PackedScene>("res://scenes/ui/OverworldMenu.tscn");
            var canvasLayer = new CanvasLayer();
            canvasLayer.AddChild(overworldMenu.Instantiate());
            AddChild(canvasLayer);


            // If we have a player reference, place them at the stored position
            var player = OverworldPlayer.Instance;
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
            if (DialogManager == null)
            {
                DialogManager = DialogManagerScene.Instantiate<DialogManager>();
                AddChild(DialogManager);
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
