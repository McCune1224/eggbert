using Godot;
/// <summary>
/// An area in a level the player can warp between once unlocked.
/// Tracks unlock state via WorldFlags and exposes the target
/// level path + arrival transition name to SaveManager/GameController.
/// </summary>

public partial class WarpPoint : Area2D
{
	/// <summary>Stable warp id matching a WarpDatabase entry. This is the API key that connects the warp crystal to its destination.</summary>
	[Export] public string WarpId = "";

	public override string[] _GetConfigurationWarnings()
	{
		var warnings = new System.Collections.Generic.List<string>();
		if (string.IsNullOrEmpty(WarpId))
			warnings.Add("WarpId is empty — this warp point is not connected to any destination. Set it to a WarpDatabase id.");
		else if (!WarpDatabase.All.ContainsKey(WarpId))
			warnings.Add($"WarpId '{WarpId}' has no matching entry in WarpDatabase.All — the warp has no destination.");
		return warnings.ToArray();
	}

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
