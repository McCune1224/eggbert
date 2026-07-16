using Godot;

/// <summary>
/// Shared static cache for commonly used fonts.
/// Prevents redundant ResourceLoader.Load calls across UI components.
/// </summary>
public static class FontCache
{
    private static Font _yoster;
    public static Font Yoster => _yoster ??= ResourceLoader.Load<Font>("res://assets/fonts/yoster.ttf");
}
