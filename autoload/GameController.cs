using Godot;
using Godot.Collections;
using System;

public partial class GameController : Node
{
    // Singleton instance
    private static GameController _instance;
    public static GameController Instance => _instance;


    // Current map and player position
    private Node _currentMap;
    //FIXME: This should prob be handled per level, not globally
    // private Vector2 _playerPosition = Vector2.Zero;

    public string CurrentArea { get; private set; } = "starting_area";
    public Array<Vector2> CurrentTileMapBounds;

    // Dialog Managment
    // public PackedScene DialogManagerScene;
    // private PackedScene DialogManagerScene;
    // public DialogManager DialogManager;

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

        // DialogManagerScene = ResourceLoader.Load<PackedScene>("res://scripts/ui/DialogManager.tscn");
    }

    public void AttachScene()
    {
    }

    public void ChangeTileMapBounds(Array<Vector2> bounds)
    {
        CurrentTileMapBounds = bounds;
        EmitSignal(nameof(TileMapBoundsChanged), bounds);
    }

    [Signal]
    public delegate void TileMapBoundsChangedEventHandler(Godot.Collections.Array<Vector2> bounds);

    // func ChangeTileMapBounds(List<Vector2> bounds)
    // {
    //     EmitSignal(nameof(TileMapBoundsChanged), bounds);
    // }


    public void LoadOverworldScene(string scenePath, Vector2 playerPosition)
    {
        try
        {
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


            var overworldMenu = ResourceLoader.Load<PackedScene>("res://ui/OverworldMenu.tscn");
            var canvasLayer = new CanvasLayer();
            canvasLayer.AddChild(overworldMenu.Instantiate());
            AddChild(canvasLayer);

            // If we have a player reference, place them at the stored position
            var player = Player.Instance;
            if (player != null)
            {
                player.SetInitialPosition(playerPosition);

                // Re-enable the player's camera
                var playerCamera = player.GetNode<Camera2D>("Camera2D");
                if (playerCamera != null)
                {
                    playerCamera.Enabled = true;
                    playerCamera.MakeCurrent();
                }
            }
            // if (DialogManager == null)
            // {
            //     DialogManager = DialogManagerScene.Instantiate<DialogManager>();
            //     AddChild(DialogManager);
            // }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading Overworld scene: {e.Message}");
        }
        //epic
    }


    public void SetCurrentArea(string area)
    {
        CurrentArea = area;
    }
}
