using Godot;

/// <summary>
/// Shared static cache for commonly used fonts.
/// Prevents redundant ResourceLoader.Load calls across UI components.
/// </summary>
public static class FontCache
{
    private static Font _yoster;
    public static Font Yoster
    {
        get
        {
            if (_yoster == null)
            {
                _yoster = ResourceLoader.Load<Font>("res://assets/fonts/yoster.ttf");
                if (_yoster == null)
                    GameLogger.Error("FontCache", "Failed to load yoster.ttf from res://assets/fonts/");
            }
            return _yoster;
        }
    }
}
