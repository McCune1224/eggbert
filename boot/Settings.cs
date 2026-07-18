using Godot;

/// <summary>
/// Central game settings, persisted via ConfigFile.
/// </summary>
public static class Settings
{
    private const string ConfigPath = "user://settings.cfg";

    public static bool ShowInteractionPrompt
    {
        get => _showInteractionPrompt;
        set
        {
            _showInteractionPrompt = value;
            GameLogger.Debug("Settings", $"ShowInteractionPrompt set to {value}");
            Save();
        }
    }

    private static bool _showInteractionPrompt = true;
    private static bool _loaded = false;

    public static void Load()
    {
        if (_loaded) return;
        _loaded = true;

        var config = new ConfigFile();
        if (config.Load(ConfigPath) != Error.Ok) return;

        _showInteractionPrompt = config.GetValue("general", "show_interaction_prompt", true).AsBool();
        GameLogger.Debug("Settings", $"Loaded: showInteractionPrompt={_showInteractionPrompt}");
    }

    private static void Save()
    {
        var config = new ConfigFile();
        config.SetValue("general", "show_interaction_prompt", _showInteractionPrompt);
        config.Save(ConfigPath);
        GameLogger.Debug("Settings", $"Saved: showInteractionPrompt={_showInteractionPrompt}");
    }
}
