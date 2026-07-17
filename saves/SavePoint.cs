using Godot;

/// <summary>
/// Undertale-style save point placed in levels. Player interacts (E key) to:
/// 1. Be fully healed
/// 2. Save the game (writes SaveFile to disk)
/// 3. See/hear save feedback (animation, SFX, "Game saved." popup)
///
/// Extends InteractableArea for player detection + prompt + interaction dispatch.
/// </summary>
public partial class SavePoint : InteractableArea
{
    [Export] public string LocationName { get; set; } = "Save Point";
    [Export] public AudioStream SaveSfx { get; set; }

    private Sprite2D _starSprite;
    private AnimationPlayer _animationPlayer;
    private Label _saveLabel;
    private bool _saving = false;

    public override void _Ready()
    {
        base._Ready(); // sets up prompt sprite, body detection

        _starSprite = GetNodeOrNull<Sprite2D>("StarSprite");
        _animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        _saveLabel = GetNodeOrNull<Label>("SaveLabel");

        if (_saveLabel != null)
            _saveLabel.Visible = false;

        if (_animationPlayer != null && _animationPlayer.HasAnimation("idle"))
            _animationPlayer.Play("idle");

        // Default SFX fallback
        if (SaveSfx == null)
            SaveSfx = ResourceLoader.Load<AudioStream>("res://assets/audio/sfx/meep.ogg");
    }

    protected override void OnInteract()
    {
        if (_saving) return;
        _saving = true;

        // 1. Save burst animation
        if (_animationPlayer != null && _animationPlayer.HasAnimation("save_burst"))
            _animationPlayer.Play("save_burst");

        // 2. Play SFX
        if (SaveSfx != null)
            AudioManager.Instance.PlaySfx(SaveSfx);

        // 3. Fully heal player
        Player.Instance.HealthComponent.CurrentHP = Player.Instance.HealthComponent.MaxHP;

        // 4. Save the game with save point metadata
        string scenePath = GameController.Instance.CurrentLevel?.SceneFilePath ?? "";
        SaveManager.Instance.SaveGame(scenePath, GlobalPosition, LocationName);

        // 5. Show "Game saved." popup
        if (_saveLabel != null)
        {
            _saveLabel.Visible = true;
            _saveLabel.Modulate = new Color(1, 1, 1, 1);
            _saveLabel.Position = new Vector2(0, -16);

            Tween tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(_saveLabel, "position", new Vector2(0, -48), 1.0f);
            tween.Parallel().TweenProperty(_saveLabel, "modulate", new Color(1, 1, 1, 0), 1.0f);
            tween.Chain().TweenCallback(Callable.From(() => _saveLabel.Visible = false));
        }

        // 6. Re-enable after brief cooldown
        GetTree().CreateTimer(0.5f).Timeout += () => _saving = false;
    }
}
