using Godot;
using Godot.Collections;

public partial class GameController : Node
{
	private static GameController _instance;
	public static GameController Instance => _instance;


	private Control _menu;

	public Array<Vector2> CurrentTileMapBounds;
	public Node CurrentLevel;

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
			GD.PrintErr("Multiple instances of GameController detected!");
		}
		_menu = GetNode<Control>("Menu");
		PackedScene overworldMenu = ResourceLoader.Load<PackedScene>("res://ui/OverworldMenu.tscn");
		CanvasLayer canvasLayer = new CanvasLayer();
		canvasLayer.AddChild(overworldMenu.Instantiate());
		_menu.AddChild(canvasLayer);
		CurrentLevel = GetNode("CurrentLevel");
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
		GameLogger.Info("GameController", $"Loading level (pos): {scenePath}");
		CutsceneController.Instance.Stop();
		DialogManager.Instance.Reset();
		GetTree().Paused = true;
		EmitSignal(nameof(LevelLoadStarted));

		await FadeTransition.Instance.PlayFadeOut();

		Node levelRoot = GetNode<Node>("CurrentLevel");

		foreach (Node child in levelRoot.GetChildren())
		{
			child.QueueFree();
		}

		await ToSignal(GetTree(), "process_frame");
		PackedScene mapScene = ResourceLoader.Load<PackedScene>(scenePath);
		if (mapScene == null)
		{
			GameLogger.Error("GameController", $"Failed to load scene: {scenePath}");
			GetTree().Paused = false;
			return;
		}

		Node loadedLevel = mapScene.Instantiate();
		levelRoot.AddChild(loadedLevel);
		CurrentLevel = loadedLevel;

		// Place player at the stored position
		var player = Player.Instance;
		player.Position = playerPosition;

		await FadeTransition.Instance.PlayFadeIn();
		if (CurrentLevel is BaseLevel baseLevel && !string.IsNullOrEmpty(baseLevel.LevelName))
			FadeTransition.Instance.ShowLocation(baseLevel.LevelName);
		EmitSignal(nameof(LevelLoaded));
		GetTree().Paused = false;
	}

	/// <summary>
	/// Loads a level and places the player at a transition area.
	/// </summary>
	public async void LoadLevel(string scenePath, string targetTransitionName)
	{
		CutsceneController.Instance.Stop();
		GameLogger.Info("GameController", $"Loading level (transition): {scenePath}");
		DialogManager.Instance.Reset();
		GetTree().Paused = true;
		EmitSignal(nameof(LevelLoadStarted));
		await FadeTransition.Instance.PlayFadeOut();

		Node levelRoot = GetNode<Node>("CurrentLevel");

		foreach (Node child in levelRoot.GetChildren())
		{
			child.QueueFree();
		}

		await ToSignal(GetTree(), "process_frame");
		PackedScene mapScene = ResourceLoader.Load<PackedScene>(scenePath);
		if (mapScene == null)
		{
			GameLogger.Error("GameController", $"Failed to load scene: {scenePath}");
			GetTree().Paused = false;
			return;
		}

		Node loadedLevel = mapScene.Instantiate();
		levelRoot.AddChild(loadedLevel);
		CurrentLevel = loadedLevel;

		// Place player at the transition area
		LevelTransition transitionArea = CurrentLevel.GetNodeOrNull<LevelTransition>(targetTransitionName);
		if (transitionArea == null)
		{
			GameLogger.Error("GameController", $"Transition '{targetTransitionName}' not found in {scenePath}");
			GetTree().Paused = false;
			return;
		}
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
		if (CurrentLevel is BaseLevel baseLevel && !string.IsNullOrEmpty(baseLevel.LevelName))
			FadeTransition.Instance.ShowLocation(baseLevel.LevelName);
		EmitSignal(nameof(LevelLoaded));
		GetTree().Paused = false;
	}
}
