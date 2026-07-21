using Godot;

public partial class WarpPoint : Area2D
{
	[Export] public string WarpId = "";

	private Area2D _promptArea;
	private bool _playerNear = false;
	private bool _unlocked = false;

	public override void _Ready()
	{
		_unlocked = WarpDatabase.IsUnlocked(WarpId);
		_promptArea = GetNode<Area2D>("PromptArea2D");
		_promptArea.BodyEntered += OnBodyEntered;
		_promptArea.BodyExited += OnBodyExited;

		UpdateInteractionPrompt();

		var crystal = GetNode<ColorRect>("WarpCrystal");
		var floatTween = CreateTween().SetLoops();
		floatTween.TweenProperty(crystal, "position:y", -4.0f, 0.75f)
			.AsRelative().SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
		floatTween.TweenProperty(crystal, "position:y", 4.0f, 0.75f)
			.AsRelative().SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);

		GameLogger.Debug("WarpPoint", $"'{Name}': _Ready — id='{WarpId}', unlocked={_unlocked}");
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!body.IsInGroup("player"))
			return;

		_playerNear = true;
		UpdateInteractionPrompt();
	}

	private void OnBodyExited(Node2D body)
	{
		if (!body.IsInGroup("player"))
			return;

		_playerNear = false;
		UpdateInteractionPrompt();
	}

	public override void _Process(double delta)
	{
		if (_unlocked) return;
		if (_playerNear && Input.IsActionJustPressed("interact"))
		{
			_unlocked = true;
			WarpDatabase.Unlock(WarpId);
			UpdateInteractionPrompt();
			GameLogger.Info("WarpPoint", $"'{Name}': unlocked (id='{WarpId}')");
			if (WarpDatabase.All.TryGetValue(WarpId, out var dest))
				DialogManager.Instance.StartDialog(
					new System.Collections.Generic.List<string> { $"Warp unlocked: {dest.Name}" });
		}
	}
	private void UpdateInteractionPrompt()
	{
		Player.Instance?.InteractionPrompt?.SetInteractableAvailable(this, _playerNear && !_unlocked);
	}

}
