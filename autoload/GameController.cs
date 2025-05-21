using Godot;
using Godot.Collections;

public partial class GameController : Node
{
    private static GameController _instance;
    public static GameController Instance => _instance;

    private Node _currentLevel;
    private Control _menu;
    public Array<Vector2> CurrentTileMapBounds;

    [Signal]
    public delegate void TileMapBoundsChangedEventHandler(Array<Vector2> bounds);

    [Signal]
    public delegate void LevelLoadStartedEventHandler();

    [Signal]
    public delegate void LevelLoadedEventHandler();

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
        PackedScene overworldMenu = ResourceLoader.Load<PackedScene>("res://ui/OverworldMenu.tscn");
        CanvasLayer canvasLayer = new CanvasLayer();
        canvasLayer.AddChild(overworldMenu.Instantiate());
        _menu.AddChild(canvasLayer);
        _currentLevel = GetNode("CurrentLevel");
        // DialogManagerScene = ResourceLoader.Load<PackedScene>("res://scripts/ui/DialogManager.tscn");
    }

    /// <summary>
    /// Emits a signal and updates the current tile map bounds.
    /// </summary>
    public void ChangeTileMapBounds(Array<Vector2> bounds)
    {
        CurrentTileMapBounds = bounds;
        EmitSignal(nameof(TileMapBoundsChanged), bounds);
    }

    /// <summary>
    /// Loads a level and places the player at a specific position.
    /// </summary>
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

        // Place player at the stored position
        var player = Player.Instance;
        player.Position = playerPosition;

        await FadeTransition.Instance.PlayFadeIn();
        EmitSignal(nameof(LevelLoaded));
        GetTree().Paused = false;
    }

    /// <summary>
    /// Loads a level and places the player at a transition area.
    /// </summary>
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

        // Place player at the transition area
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

        await FadeTransition.Instance.PlayFadeIn();
        EmitSignal(nameof(LevelLoaded));
        GetTree().Paused = false;
    }
}
