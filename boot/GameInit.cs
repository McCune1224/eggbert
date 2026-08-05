using Godot;
/// <summary>
/// Boot autoload that initializes the logger, loads settings,
/// and either skips to the main menu or shows the MainMenu scene.
/// </summary>

public partial class GameInit : Node
{
    public override void _Ready()
    {
        // Deferring avoids 'Parent node is busy setting up children' error when
        // GameInit runs its _Ready during the root's _Ready cascade.
        Callable.From(BootDeferred).CallDeferred();
    }

    private async void BootDeferred()
    {
        GameLogger.InitializeFromEnv();
        Settings.Load();
        GameLogger.Info("GameInit", "BootDeferred: logger initialized, settings loaded.");

        // Debug auto-start: skip the main menu and load the last save directly.
        bool skipMenu = System.Environment.GetEnvironmentVariable("EGGBERT_SKIP_MENU") == "1";
        // Debug auto-start variant: boot straight into a named dev save state (EGGBERT_LOAD_STATE=<slot>).
        // Takes precedence over EGGBERT_SKIP_MENU; works even when no default save exists.
        string loadState = System.Environment.GetEnvironmentVariable("EGGBERT_LOAD_STATE");
        GameLogger.Info("GameInit", $"BootDeferred: EGGBERT_SKIP_MENU={skipMenu}, EGGBERT_LOAD_STATE='{loadState}', HasSave={SaveManager.Instance.HasSave()}, SaveManager.Instance is null? {SaveManager.Instance == null}");

        if (!string.IsNullOrEmpty(loadState))
        {
            GameLogger.Info("GameInit", $"EGGBERT_LOAD_STATE set — loading dev state '{loadState}'.");
            if (!SaveManager.Instance.HasSave(loadState))
            {
                GameLogger.Warn("GameInit", $"EGGBERT_LOAD_STATE='{loadState}' has no matching slot or fixture — falling through to normal boot.");
            }
            else
            {
                bool loaded = SaveManager.Instance.LoadGame(loadState);
                GameLogger.Info("GameInit", $"LoadGame('{loadState}') returned {loaded}. Waiting for level load...");
                if (loaded)
                {
                    await ToSignal(GameController.Instance, GameController.SignalName.LevelLoaded);
                    GameLogger.Info("GameInit", "Load-state: level loaded, returning (no MainMenu).");
                    return;
                }
                GameLogger.Warn("GameInit", "LoadGame returned false — falling through to normal boot.");
            }
        }

        if (skipMenu && SaveManager.Instance.HasSave())
        {
            GameLogger.Info("GameInit", "SKIP_MENU set + save exists — loading last save.");
            bool loaded = SaveManager.Instance.LoadGame();
            GameLogger.Info("GameInit", $"LoadGame returned {loaded}. Waiting for level load...");
            if (loaded)
            {
                // Wait for the level to finish loading (Player.Deserialize calls LoadLevel which emits LevelLoaded)
                await ToSignal(GameController.Instance, GameController.SignalName.LevelLoaded);
                GameLogger.Info("GameInit", "Skip-menu: level loaded, returning (no MainMenu).");
                return;
            }
            GameLogger.Warn("GameInit", "LoadGame returned false — old/corrupt save was deleted. Falling through to main menu.");
        }

        GameLogger.Info("GameInit", "Proceeding to load main menu.");
        // Add persistent dialog log
        var dialogLog = new DialogLog();
        GetTree().Root.AddChild(dialogLog);
        GameLogger.Info("GameInit", "DialogLog added to root.");
        var menuPacked = ResourceLoader.Load<PackedScene>("res://ui/MainMenu.tscn");
        if (menuPacked == null)
        {
            GameLogger.Error("GameInit", "Failed to load MainMenu.tscn");
            return;
        }
        GetTree().Root.AddChild(menuPacked.Instantiate());
        GameLogger.Info("GameInit", "MainMenu instantiated and added to root.");
    }
}