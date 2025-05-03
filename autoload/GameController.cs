using Godot;
using Godot.Collections;
using System;

public partial class GameController : Node
{
    // Singleton instance
    private static GameController _instance;
    public static GameController Instance => _instance;

    private Node _currentLevel;

    // Current map and player position
    private Control _menu;
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
        _menu = GetNode<Control>("Menu");
        var overworldMenu = ResourceLoader.Load<PackedScene>("res://ui/OverworldMenu.tscn");
        var canvasLayer = new CanvasLayer();
        canvasLayer.AddChild(overworldMenu.Instantiate());
        _menu.AddChild(canvasLayer);
        _currentLevel = GetNode("CurrentLevel");
        // DialogManagerScene = ResourceLoader.Load<PackedScene>("res://scripts/ui/DialogManager.tscn");
    }


    [Signal]
    public delegate void TileMapBoundsChangedEventHandler(Godot.Collections.Array<Vector2> bounds);
    public void ChangeTileMapBounds(Array<Vector2> bounds)
    {
        CurrentTileMapBounds = bounds;
        EmitSignal(nameof(TileMapBoundsChanged), bounds);
    }


    [Signal]
    public delegate void LevelLoadStartedEventHandler();
    public async void LoadLevel(string scenePath, Vector2 playerPosition)
    {
        GetTree().Paused = true;
        EmitSignal(nameof(LevelLoadStarted));

        await FadeTransition.Instance.PlayFadeOut();


        Node levelRoot = GetNode<Node>("CurrentLevel");

        foreach (Node child in levelRoot.GetChildren())
        {
            child.QueueFree();
        }

        await ToSignal(GetTree(), "process_frame"); // Wait for the next frame to ensure nodes are freed
        PackedScene mapScene = ResourceLoader.Load<PackedScene>(scenePath);

        Node loadedLevel = mapScene.Instantiate();
        levelRoot.AddChild(loadedLevel);
        _currentLevel = loadedLevel;



        // If we have a player reference, place them at the stored position
        var player = Player.Instance;
        player.Position = playerPosition;
        // if (DialogManager == null)
        // {
        //     DialogManager = DialogManagerScene.Instantiate<DialogManager>();
        //     AddChild(DialogManager);
        // }

        //epic

        await FadeTransition.Instance.PlayFadeIn();
        EmitSignal(nameof(LevelLoaded));
        GetTree().Paused = false;
    }

    public async void LoadLevel(string scenePath, string targetTransitionName)
    {
        GetTree().Paused = true;
        EmitSignal(nameof(LevelLoadStarted));
        await FadeTransition.Instance.PlayFadeOut();

        Node levelRoot = GetNode<Node>("CurrentLevel");

        foreach (Node child in levelRoot.GetChildren())
        {
            child.QueueFree();
        }

        await ToSignal(GetTree(), "process_frame"); // Wait for the next frame to ensure nodes are freed
        PackedScene mapScene = ResourceLoader.Load<PackedScene>(scenePath);

        Node loadedLevel = mapScene.Instantiate();
        levelRoot.AddChild(loadedLevel);
        _currentLevel = loadedLevel;



        // If we have a player reference, place them at the stored position
        LevelTransition transitionArea = _currentLevel.GetNode<LevelTransition>(targetTransitionName);
        switch (transitionArea.Side)
        {
            case TransitionSide.Left:
                Player.Instance.Position = new Vector2(transitionArea.GlobalPosition.X + 30, transitionArea.GlobalPosition.Y);
                break;
            case TransitionSide.Right:
                Player.Instance.Position = new Vector2(transitionArea.GlobalPosition.X - 30, transitionArea.GlobalPosition.Y);
                break;
            case TransitionSide.Up:
                Player.Instance.Position = new Vector2(transitionArea.GlobalPosition.X, transitionArea.GlobalPosition.Y + 50);
                break;
            case TransitionSide.Down:
                Player.Instance.Position = new Vector2(transitionArea.GlobalPosition.X, transitionArea.GlobalPosition.Y - 50);
                break;
        }

        var player = Player.Instance;
        // player.Position = playerPosition;
        // if (DialogManager == null)
        // {
        //     DialogManager = DialogManagerScene.Instantiate<DialogManager>();
        //     AddChild(DialogManager);
        // }

        //epic
        await FadeTransition.Instance.PlayFadeIn();
        EmitSignal(nameof(LevelLoaded));
        GetTree().Paused = false;
    }

    [Signal]
    public delegate void LevelLoadedEventHandler();


    // func ChangeTileMapBounds(List<Vector2> bounds)
    // {
    //     EmitSignal(nameof(TileMapBoundsChanged), bounds);
    // }
}
