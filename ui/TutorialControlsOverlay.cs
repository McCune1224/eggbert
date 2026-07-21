using Godot;

public partial class TutorialControlsOverlay : Node2D
{
    private const float FadeDelaySeconds = 8.0f;
    private PanelContainer _panel;

    public override void _Ready()
    {
        ZIndex = 100;

        _panel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(280f, 104f),
            Size = new Vector2(280f, 104f),
            Position = Vector2.Zero
        };

        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.12f, 0.96f),
            BorderColor = new Color(0.3f, 0.6f, 0.9f, 0.65f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ContentMarginLeft = 18,
            ContentMarginTop = 14,
            ContentMarginRight = 18,
            ContentMarginBottom = 14
        };
        _panel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(_panel);

        var label = new Label
        {
            Text = "W A S D  MOVE\nSHIFT    SPRINT\nSPACE    DASH\nE        INTERACT\nESC      MENU",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.Off,
            ClipText = false
        };

        var font = FontCache.Yoster;
        if (font != null)
            label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", 15);
        label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 1f));
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _panel.AddChild(label);

        var timer = GetTree().CreateTimer(FadeDelaySeconds);
        timer.Timeout += OnFadeTimerTimeout;
    }

    private void OnFadeTimerTimeout()
    {
        if (!IsInsideTree() || _panel == null || !GodotObject.IsInstanceValid(_panel))
            return;

        var tween = CreateTween();
        tween.TweenProperty(_panel, "modulate", new Color(1f, 1f, 1f, 0f), 0.4f);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
