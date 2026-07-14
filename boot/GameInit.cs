using Godot;

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
        // Debug auto-start: skip the main menu and load the last save directly.
        // Set the EGGBERT_SKIP_MENU environment variable to "1" to activate.
        //   - In the MCP godot_run_project environment (.opencode/opencode.json).
        //   - Via CLI: EGGBERT_SKIP_MENU=1 godot --path .
        // Exported/normal builds won't have the var set, so the flow is export-safe.
        bool skipMenu = System.Environment.GetEnvironmentVariable("EGGBERT_SKIP_MENU") == "1";

        if (skipMenu
            && SaveLoadManager.Instance != null
            && SaveLoadManager.Instance.HasSave())
        {
            SaveLoadManager.Instance.LoadGame();
            await ToSignal(GameController.Instance, GameController.SignalName.LevelLoaded);
            return;
        }

        var menuPacked = ResourceLoader.Load<PackedScene>("res://ui/MainMenu.tscn");
        if (menuPacked == null)
            GameLogger.Error("GameInit", "Failed to load MainMenu.tscn");
            GD.PrintErr("Failed to load MainMenu.tscn");
            return;
        }
        GetTree().Root.AddChild(menuPacked.Instantiate());
    }
}