using Godot;

/// <summary>
/// Floating combat numbers (docs/combat-ui-design.md §3.2 C7).
/// Spawns a world-anchored label that drifts up and fades, same visual pattern
/// as the PARRY! popup. Spawned into the current level (world space).
/// </summary>
public static class FloatingText
{
    public static void Show(Node2D anchor, string text, Color color)
    {
        if (anchor == null || !GodotObject.IsInstanceValid(anchor)) return;
        var level = GameController.Instance?.CurrentLevel as Node2D;
        if (level == null) return;

        var label = new Label
        {
            Text = text,
            ThemeTypeVariation = "HudLabel",
            Modulate = color,
            ZIndex = 20
        };
        label.AddThemeFontSizeOverride("font_size", 12);
        level.AddChild(label);
        label.GlobalPosition = anchor.GlobalPosition + new Vector2(-10f, -20f);

        var tween = label.CreateTween().SetParallel(true);
        tween.TweenProperty(label, "position:y", label.Position.Y - 24f, 0.7f);
        tween.TweenProperty(label, "modulate:a", 0f, 0.7f);
        tween.Chain().TweenCallback(Callable.From(label.QueueFree));
    }
}
