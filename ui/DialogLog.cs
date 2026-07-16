using Godot;
using System.Collections.Generic;

/// <summary>
/// Persistent scrollable dialog log shown during conversations.
/// Holds the last N lines and toggles via Tab/Esc.
/// </summary>
public partial class DialogLog : CanvasLayer
{
    private const int MaxLines = 50;

    private static List<string> _buffer = new(MaxLines);
    private static bool _visible = false;
    private Control _container;
    private RichTextLabel _logLabel;

    public override void _Ready()
    {
        Layer = 150;
        _container = new Control();
        _container.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _container.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_container);

        var bg = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.8f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _container.AddChild(bg);

        _logLabel = new RichTextLabel
        {
            Position = new Vector2(20, 20),
            Size = new Vector2(600, 320),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            BbcodeEnabled = true
        };
        var font = FontCache.Yoster;
        if (font != null)
            _logLabel.AddThemeFontOverride("normal_font", font);
        _logLabel.AddThemeFontSizeOverride("normal_font_size", 10);
        _logLabel.AddThemeColorOverride("default_color", new Color(0.8f, 0.8f, 0.8f));
        _container.AddChild(_logLabel);

        _container.Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (!DialogManager.Instance.IsDialogActive) return;
        if (@event.IsActionPressed("ui_focus_next") || @event.IsActionPressed("menu_pause"))
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }

    public static void AppendLine(string speaker, string text)
    {
        string entry;
        if (!string.IsNullOrEmpty(speaker))
            entry = $"[b]{speaker}:[/b] {text}";
        else
            entry = text;

        _buffer.Add(entry);
        if (_buffer.Count > MaxLines)
            _buffer.RemoveAt(0);
    }

    private void Toggle()
    {
        _visible = !_visible;
        _container.Visible = _visible;

        if (_visible)
        {
            _logLabel.Text = string.Join("\n", _buffer);
            _logLabel.ScrollToLine(_logLabel.GetLineCount() - 1);
        }
    }
}
