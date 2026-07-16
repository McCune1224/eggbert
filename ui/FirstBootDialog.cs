using Godot;

/// <summary>
/// Text speed picker shown on first launch.
/// Lets the player choose Fast / Medium / Slow text speed,
/// saves the preference, and sets a flag so it only shows once.
/// </summary>
public partial class FirstBootDialog : CanvasLayer
{
    [Signal]
    public delegate void CompletedEventHandler();

    private static bool _wasShownThisSession = false;

    public override void _Ready()
    {
        // Prevent showing again this session
        if (_wasShownThisSession ||
            WorldFlags.Instance.HasFlag("first_boot_speed_chosen"))
        {
            EmitSignal(SignalName.Completed);
            QueueFree();
            return;
        }

        _wasShownThisSession = true;
        Layer = 200;

        BuildUI();
    }

    private void BuildUI()
    {
        // Dark backdrop
        var backdrop = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.7f),
        MouseFilter = Control.MouseFilterEnum.Ignore
        };
        backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(backdrop);

        // Container
        var container = new VBoxContainer
        {
            AnchorLeft = 0.3f,
            AnchorTop = 0.3f,
            AnchorRight = 0.7f,
            AnchorBottom = 0.7f
        };

        // Title
        var title = new Label
        {
            Text = "Text Speed",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var yosterFont = FontCache.Yoster;
        if (yosterFont != null)
            title.AddThemeFontOverride("font", yosterFont);
        title.AddThemeFontSizeOverride("font_size", 16);
        container.AddChild(title);

        // Description
        var desc = new Label
        {
            Text = "How fast should dialog text appear?",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        if (yosterFont != null)
            desc.AddThemeFontOverride("font", yosterFont);
        desc.AddThemeFontSizeOverride("font_size", 12);
        container.AddChild(desc);

        container.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.Expand });

        // Buttons
        AddSpeedButton(container, "Fast", DialogManager.TextSpeed.Fast, "Smooth and snappy. Recommended.");
        AddSpeedButton(container, "Medium", DialogManager.TextSpeed.Normal, "A relaxed pace.");
        AddSpeedButton(container, "Instant", DialogManager.TextSpeed.Instant, "Text appears all at once.");

        container.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.Expand });
    }

    private void AddSpeedButton(VBoxContainer parent, string label, DialogManager.TextSpeed speed, string hint)
    {
        var btn = new Button
        {
            Text = $"{label}\n{hint}",
            Flat = false
        };
        btn.Pressed += () => OnSpeedChosen(speed);
        parent.AddChild(btn);
    }

    private void OnSpeedChosen(DialogManager.TextSpeed speed)
    {
        DialogManager.CurrentTextSpeed = speed;
        WorldFlags.Instance.SetFlag("first_boot_speed_chosen", true);

        GameLogger.Info("FirstBoot", $"Text speed set to {speed}");
        EmitSignal(SignalName.Completed);
        QueueFree();
    }
}
