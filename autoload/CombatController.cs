using Godot;

public partial class CombatController : Node
{
    private static CombatController _instance;
    public static CombatController Instance => _instance;

    private string _returnLevelPath;
    private Vector2 _returnPosition;
    private CombatArena _currentArena;

    public override void _Ready()
    {
        if (_instance == null)
            _instance = this;
        else
            QueueFree();
    }

    public async void EnterCombat(string arenaPath, Vector2 playerSpawn)
    {
        GameLogger.Info("Combat", $"Entering combat arena: {arenaPath}");
        // Block re-entry: if we're already in a combat arena, ignore the call so the
        // saved return position isn't overwritten and handlers don't pile up.
        if (_currentArena != null)
        {
            GameLogger.Warn("Combat", "EnterCombat called while already in combat — ignored.");
            return;
        }

        _returnLevelPath = GameController.Instance.CurrentLevel.SceneFilePath;
        _returnPosition = Player.Instance.Position;

        SaveLoadManager.Instance?.SaveGame();
        GameController.Instance.LoadLevel(arenaPath, playerSpawn, true);

        await ToSignal(GameController.Instance, GameController.SignalName.LevelLoaded);

        _currentArena = GameController.Instance.CurrentLevel as CombatArena;
        if (_currentArena != null)
        {
            // Method-group delegates so += and -= reference the same instance and actually unsubscribe.
            _currentArena.BattleWon += OnBattleWon;
            _currentArena.BattleLost += OnBattleLost;
        }
    }

    private void OnBattleWon()
    {
        UnhookArena();
        ReturnToOverworld();
    }

    private async void OnBattleLost()
    {
        UnhookArena();

        DialogManager.Instance.StartDialog(
            new System.Collections.Generic.List<string> { "You collapsed..." });
        await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);

        // Reload the pre-combat save to restore overworld state and full HP.
        SaveLoadManager.Instance?.LoadGame();
    }

    private void UnhookArena()
    {
        if (_currentArena == null)
            return;

        _currentArena.BattleWon -= OnBattleWon;
        _currentArena.BattleLost -= OnBattleLost;
        _currentArena = null;
    }

    public void ReturnToOverworld()
    {
        GameLogger.Info("Combat", $"Returning to overworld: {_returnLevelPath}");
        GameController.Instance.LoadLevel(_returnLevelPath, _returnPosition);
    }
}