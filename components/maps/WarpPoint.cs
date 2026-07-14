using Godot;

public partial class WarpPoint : Area2D
{
	[Export] public string WarpId = "";
	[Export] public string PromptText = "Press E to unlock warp";

	private Area2D _promptArea;
	private Label _interactionLabel;
	private Sprite2D _promptSprite;
	private bool _unlocked = false;

	private Tween _floatTween;
	public override void _Ready()
	{
		_unlocked = WarpDatabase.IsUnlocked(WarpId);
		_promptArea = GetNode<Area2D>("PromptArea2D");
		_interactionLabel = _promptArea.GetNode<Label>("Label");
		_promptSprite = _promptArea.GetNode<Sprite2D>("Sprite2D");

		_promptSprite.Visible = false;
		_promptArea.BodyEntered += OnBodyEntered;
		_promptArea.BodyExited += OnBodyExited;

		if (_unlocked)
			HidePrompt();

		var crystal = GetNode<ColorRect>("WarpCrystal");
		_floatTween = CreateTween().SetLoops();
		_floatTween.TweenProperty(crystal, "position:y", -4.0f, 0.75f)
			.AsRelative().SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
		_floatTween.TweenProperty(crystal, "position:y", 4.0f, 0.75f)
			.AsRelative().SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!body.IsInGroup("player"))
			return;
		ShowPrompt();
	}

	private void OnBodyExited(Node2D body)
	{
		if (!body.IsInGroup("player"))
			return;
		HidePrompt();
	}

	public override void _Process(double delta)
	{
		if (_unlocked) return;
		if (IsPromptVisible() && Input.IsActionJustPressed("interact"))
		{
			_unlocked = true;
			WarpDatabase.Unlock(WarpId);
			HidePrompt();
			if (WarpDatabase.All.TryGetValue(WarpId, out var dest))
				DialogManager.Instance.StartDialog(
					new System.Collections.Generic.List<string> { $"Warp unlocked: {dest.Name}" });
		}
	}

	public bool IsPromptVisible() => _interactionLabel?.Visible ?? false;

	public void HidePrompt()
	{
		_interactionLabel.Visible = false;
		_promptSprite.Visible = false;
	}

	public void ShowPrompt()
	{
		_interactionLabel.Visible = true;
		_interactionLabel.Text = PromptText;
		_promptSprite.Visible = true;
	}
}
