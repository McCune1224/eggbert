using Godot;

/// <summary>
/// Coordinates the story-specific handoff at the end of the factory tutorial.
/// Scene authoring remains in OpeningZone; this node only gates its exit and
/// turns the arrest dialog into the mandatory transfer to Eggs Isle.
/// </summary>
public partial class FactoryOpeningFlow : Node
{
    private const string OpeningScenePath = "res://levels/factory/maps/OpeningZone.tscn";
    private const string EggsileScenePath = "res://levels/eggsile/maps/area1.tscn";
    private const string ArrestedFlag = "arrested";
    private static readonly string[] ArrestDialogLines =
    {
        "Officer Bacon: Stop right there! A witness says the murderer wore an egg costume.",
        "Officer Bacon: Wrong place. Wrong shell. You're coming with me.",
        "Officer Bacon: Eggs Isle has plenty of time for you to explain yourself."
    };
    private static readonly string[] OpeningExitNames =
    {
        "OverworldFactoryGate",
        "Zone1Entrance",
        "EggsileTransition"
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

        if (GameController.Instance.CurrentLevel?.SceneFilePath != OpeningScenePath)
            return;

        foreach (var exitName in OpeningExitNames)
        {
            var exit = GameController.Instance.CurrentLevel.GetNodeOrNull<LevelTransition>(exitName);
            if (exit == null)
            {
                GameLogger.Error("FactoryOpening", $"OpeningZone is missing its {exitName} LevelTransition.");
                continue;
            }

            exit.RequiredFlag = ArrestedFlag;
        }

        var arrest = GameController.Instance.CurrentLevel.GetNodeOrNull<CutsceneTrigger>("ArrestCutscene");
        if (arrest == null)
        {
            if (WorldFlags.Instance.HasFlag(ArrestedFlag))
            {
                GameLogger.Debug("FactoryOpening", "Factory arrest already completed — no trigger setup needed.");
                return;
            }

            GameLogger.Error("FactoryOpening", "OpeningZone is missing its ArrestCutscene trigger.");
            return;
        }

        if (arrest.Cutscene == null)
            arrest.DialogLines = ArrestDialogLines;

        _awaitingArrestTransfer = !WorldFlags.Instance.HasFlag(ArrestedFlag);
        GameLogger.Info("FactoryOpening", "Factory tutorial configured — Eggs Isle exit gated until the arrest.");
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
