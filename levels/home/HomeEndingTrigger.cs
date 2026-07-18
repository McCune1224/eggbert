using Godot;

/// <summary>
/// Auto-trigger at the Home entry point. Counts mercy flags and selects the
/// appropriate ending cutscene (Good/Mid/Bad). On normal cutscene completion,
/// returns to the main menu.
/// </summary>
public partial class HomeEndingTrigger : Area2D
{
    private bool _fired = false;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer;
        BodyEntered += OnBodyEntered;
    }

    private async void OnBodyEntered(Node body)
    {
        if (_fired) return;
        if (!body.IsInGroup("player")) return;
        if (CutsceneController.Instance.IsPlaying) return;

        _fired = true;

        int mercyCount = 0;
        if (WorldFlags.Instance.HasFlag("spared_oatmeal")) mercyCount++;
        if (WorldFlags.Instance.HasFlag("spared_yogurt")) mercyCount++;
        if (WorldFlags.Instance.HasFlag("spared_cereal")) mercyCount++;
        if (WorldFlags.Instance.HasFlag("waffles_spared")) mercyCount++;

        string resourcePath = mercyCount switch
        {
            4 => "res://levels/home/npcs/GoodEggEnding.tres",
            >= 2 and <= 3 => "res://levels/home/npcs/MidEggEnding.tres",
            _ => "res://levels/home/npcs/BadEggEnding.tres"
        };

        var resource = ResourceLoader.Load<CutsceneResource>(resourcePath);
        if (resource == null)
        {
            GameLogger.Error("HomeEnding", $"Failed to load ending cutscene: {resourcePath}");
            return;
        }

        CutsceneController.Instance.CutsceneFinished += OnCutsceneFinished;
        CutsceneController.Instance.StartCutscene(resource);
    }

    private void OnCutsceneFinished()
    {
        CutsceneController.Instance.CutsceneFinished -= OnCutsceneFinished;
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://ui/MainMenu.tscn");
    }
}
