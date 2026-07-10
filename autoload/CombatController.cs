using Godot;
using System.Threading.Tasks;

public partial class CombatController : Node
{
    private static CombatController _instance;
    public static CombatController Instance => _instance;

    private string _returnLevelPath;
    private Vector2 _returnPosition;

    public override void _Ready()
    {
        if (_instance == null)
            _instance = this;
        else
            QueueFree();
    }

    public async void EnterCombat(string arenaPath, Vector2 playerSpawn)
    {
        _returnLevelPath = GameController.Instance.CurrentLevel.SceneFilePath;
        _returnPosition = Player.Instance.Position;

        SaveLoadManager.Instance?.SaveGame();
        GameController.Instance.LoadLevel(arenaPath, playerSpawn, true);

        await ToSignal(GameController.Instance, GameController.SignalName.LevelLoaded);

        var arena = GameController.Instance.CurrentLevel as CombatArena;
        if (arena != null)
        {
            arena.BattleWon += () => OnBattleWon(arena);
            arena.BattleLost += () => OnBattleLost(arena);
        }
    }

    private void OnBattleWon(CombatArena arena)
    {
        arena.BattleWon -= () => OnBattleWon(arena);
        arena.BattleLost -= () => OnBattleLost(arena);
        ReturnToOverworld();
    }

    private async void OnBattleLost(CombatArena arena)
    {
        arena.BattleWon -= () => OnBattleWon(arena);
        arena.BattleLost -= () => OnBattleLost(arena);

        DialogManager.Instance.StartDialog(
            new System.Collections.Generic.List<string> { "You collapsed..." }, new DialogVoice());
        await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);

        ReturnToOverworld();
        await ToSignal(GameController.Instance, GameController.SignalName.LevelLoaded);

        Player.Instance.HealthComponent.Revive(50);
    }

    public void ReturnToOverworld()
    {
        GameController.Instance.LoadLevel(_returnLevelPath, _returnPosition);
    }
}
