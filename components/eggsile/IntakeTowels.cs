using Godot;

/// <summary>
/// Eggs Isle intake scavenger tracker. Watches the three towel-collection one-shot flags
/// and sets <c>intake_settled</c> once all are collected, gating the Kitchen exit and
/// Frank's handoff line. A 3-flag AND gate is not expressible with a single-RequiredFlag
/// component, so this small tracker exists (see docs/eggsile-intake.md).
/// </summary>
public partial class IntakeTowels : Node
{
    private static readonly string[] TowelFlags = { "cutscene_towel_1", "cutscene_towel_2", "cutscene_towel_3" };
    private const string SettledFlag = "intake_settled";

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        WorldFlags.Instance.StateChanged += OnStateChanged;
        CheckSettled();
    }

    public override void _ExitTree()
    {
        if (WorldFlags.Instance != null)
            WorldFlags.Instance.StateChanged -= OnStateChanged;
    }

    private void OnStateChanged()
    {
        CheckSettled();
    }

    private void CheckSettled()
    {
        if (WorldFlags.Instance.HasFlag(SettledFlag))
            return;

        bool allCollected = true;
        foreach (string flag in TowelFlags)
        {
            if (!WorldFlags.Instance.HasFlag(flag))
            {
                allCollected = false;
                break;
            }
        }

        if (allCollected)
        {
            WorldFlags.Instance.SetFlag(SettledFlag, true);
            GameLogger.Info("IntakeTowels", "All towels collected — intake settled, Kitchen exit unlocked.");
        }
    }
}
