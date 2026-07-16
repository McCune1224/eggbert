using Godot;

/// <summary>
/// Brief animated icon shown when the game autosaves.
/// Fades and slides up, then disappears.
/// </summary>
public partial class SaveIcon : Control
{
    private TextureRect _icon;
    private Tween _tween;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;

        _icon = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>("res://assets/ui/NPCPrompt.png"),
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
            Position = new Vector2(10, 10),
            Scale = new Vector2(0.5f, 0.5f)
        };
        AddChild(_icon);

        SaveLoadManager.Instance.SaveCompleted += OnSaveCompleted;
    }

    private void OnSaveCompleted()
    {
        if (_tween != null && _tween.IsValid())
            _tween.Kill();

        Visible = true;
        Modulate = new Color(1, 1, 1, 1);
        Position = new Vector2(10, 10);

        _tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(this, "position", new Vector2(10, 30), 1.5f);
        _tween.Parallel().TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 1.5f);
        _tween.TweenCallback(Callable.From(() => Visible = false));
    }
}
