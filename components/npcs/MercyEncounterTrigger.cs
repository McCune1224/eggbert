using Godot;
using System.Collections.Generic;

/// <summary>
/// Trigger that offers the player a Spare/Fight choice before a combat encounter.
/// Mirrors CerealEncounterTrigger flow but interleaves a dialog + PromptChoices
/// so mercy routes can set a "spared_<id>" flag without entering combat.
///
/// On win, the "BeatFlag" remains set in WorldFlags; on loss, SaveManager reloads
/// the last save and reverts it (Undertale-style death handling).
/// </summary>
[GlobalClass]
public partial class MercyEncounterTrigger : Area2D
{
    [ExportGroup("Combat")]
    [Export] public string ArenaPath = "res://combat/arena/OatmealArena.tscn";
    [Export] public Vector2 PlayerSpawn = Vector2.Zero;

    [ExportGroup("Flags")]
    /// <summary>Flag set to true when the player chooses mercy. e.g. "spared_oatmeal".</summary>
    [Export] public string SpareFlag = "";
    /// <summary>Flag set to true when the player chooses to fight. e.g. "fought_oatmeal".</summary>
    [Export] public string FightFlag = "";
    /// <summary>Flag set to true when the fight is chosen (set before combat so a win persists; a loss reverts via save reload). e.g. "beat_oatmeal".</summary>
    [Export] public string BeatFlag = "";
    /// <summary>If set, the trigger self-frees once this flag is true (one-shot encounters).</summary>
    [Export] public string OnceFlag = "";

    [ExportGroup("Dialog")]
    [Export] public string[] IntroLines = System.Array.Empty<string>();
    [Export] public string[] SpareLines = System.Array.Empty<string>();
    [Export] public string[] FightLines = System.Array.Empty<string>();
    [Export] public string[] SpareChoiceOptions = { "Spare it", "Fight it" };
    [Export] public DialogVoiceResource Voice;

    private bool _fired = false;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.TriggerAreaLayer;
        CollisionMask = CollisionConfig.PlayerLayer;
        BodyEntered += OnBodyEntered;

        if (!string.IsNullOrEmpty(OnceFlag) && WorldFlags.Instance.HasFlag(OnceFlag))
        {
            QueueFree();
            GameLogger.Debug("MercyEncounter", $"'{Name}': already resolved (OnceFlag='{OnceFlag}') — removed");
        }
    }

    private async void OnBodyEntered(Node body)
    {
        if (body != Player.Instance || _fired) return;
        if (CombatController.Instance == null) return;

        _fired = true;
        Player.Instance.InInteraction = true;

        try
        {
            if (IntroLines != null && IntroLines.Length > 0)
            {
                DialogManager.Instance.StartDialog(new List<string>(IntroLines), Voice);
                await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);
            }

            int choice = await DialogManager.Instance.PromptChoices(new List<string>(SpareChoiceOptions));
            GameLogger.Info("MercyEncounter", $"'{Name}': choice={choice}");

            if (choice <= 0)
            {
                // Mercy route
                if (!string.IsNullOrEmpty(SpareFlag))
                    WorldFlags.Instance.SetFlag(SpareFlag, true);
                if (!string.IsNullOrEmpty(OnceFlag))
                    WorldFlags.Instance.SetFlag(OnceFlag, true);
                if (SpareLines != null && SpareLines.Length > 0)
                {
                    DialogManager.Instance.StartDialog(new List<string>(SpareLines), Voice);
                    await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);
                }
            }
            else
            {
                // Fight route — set beat flag before combat so a win persists; loss reverts via save reload.
                if (!string.IsNullOrEmpty(FightFlag))
                    WorldFlags.Instance.SetFlag(FightFlag, true);
                if (!string.IsNullOrEmpty(BeatFlag))
                    WorldFlags.Instance.SetFlag(BeatFlag, true);
                if (!string.IsNullOrEmpty(OnceFlag))
                    WorldFlags.Instance.SetFlag(OnceFlag, true);
                if (FightLines != null && FightLines.Length > 0)
                {
                    DialogManager.Instance.StartDialog(new List<string>(FightLines), Voice);
                    await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);
                }
                CombatController.Instance.EnterCombat(ArenaPath, PlayerSpawn);
            }
        }
        finally
        {
            Player.Instance.InInteraction = false;
        }
    }
}