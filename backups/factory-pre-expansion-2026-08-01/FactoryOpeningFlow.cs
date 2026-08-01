using Godot;

/// <summary>
/// Coordinates the factory tutorial's mandatory arrest handoff to Eggs Isle.
/// Scene-authored transitions own all room-to-room and post-arrest gating.
/// </summary>
public partial class FactoryOpeningFlow : Node
{
    private const string OpeningScenePath = "res://levels/factory/maps/OpeningZone.tscn";
    private const string ArrestScenePath = "res://levels/factory/maps/LoadingBay.tscn";
    private const string EggsileScenePath = "res://levels/eggsile/maps/area1.tscn";
    private const string ArrestedFlag = "arrested";
    private static readonly Rect2 OpeningBounds = new(-640, -352, 1280, 704);
    private static readonly string[] ArrestDialogLines =
    {
        "Officer Bacon: Stop right there! A witness says the murderer wore an egg costume.",
        "Officer Bacon: Wrong place. Wrong shell. You're coming with me.",
        "Officer Bacon: Eggs Isle has plenty of time for you to explain yourself."
    };

    private bool _awaitingArrestTransfer;

    public override void _Ready()
    {
        GameController.Instance.LevelLoaded += ConfigureCurrentLevel;
        CutsceneController.Instance.CutsceneFinished += TransferAfterArrest;
    }

    public override void _ExitTree()
    {
        if (GameController.Instance != null)
            GameController.Instance.LevelLoaded -= ConfigureCurrentLevel;
        if (CutsceneController.Instance != null)
            CutsceneController.Instance.CutsceneFinished -= TransferAfterArrest;
    }

    private void ConfigureCurrentLevel()
    {
        _awaitingArrestTransfer = false;

        var currentLevel = GameController.Instance.CurrentLevel;
        if (currentLevel?.SceneFilePath == OpeningScenePath)
        {
            if (!OpeningBounds.HasPoint(Player.Instance.Position))
            {
                Player.Instance.Position = Vector2.Zero;
                GameLogger.Info("FactoryOpening", "Reset legacy OpeningZone player position outside rebuilt-room bounds.");
            }

            return;
        }

        if (currentLevel?.SceneFilePath != ArrestScenePath)
            return;

        var arrest = currentLevel.GetNodeOrNull<CutsceneTrigger>("ArrestCutscene");
        if (arrest == null)
        {
            if (!WorldFlags.Instance.HasFlag(ArrestedFlag))
                GameLogger.Error("FactoryOpening", "LoadingBay is missing its ArrestCutscene trigger.");
            return;
        }

        if (arrest.Cutscene == null)
            arrest.DialogLines = ArrestDialogLines;

        _awaitingArrestTransfer = !WorldFlags.Instance.HasFlag(ArrestedFlag);
        GameLogger.Info("FactoryOpening", "Loading Bay configured — Eggs Isle exit gated until the arrest.");
    }

    private void TransferAfterArrest()
    {
        if (!_awaitingArrestTransfer || !WorldFlags.Instance.HasFlag(ArrestedFlag))
            return;

        _awaitingArrestTransfer = false;
        WarpDatabase.Unlock("eggsile_area1");
        GameLogger.Info("FactoryOpening", "Arrest completed — transferring player to Eggs Isle intake.");
        GameController.Instance.LoadLevel(EggsileScenePath, "HubArrival");
    }
}
