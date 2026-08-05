using Godot;
using System.Collections.Generic;

/// <summary>
/// Triggers an authored dialog tree (DialogBranch) when the player interacts or
/// enters the area. Mirrors CutsceneTrigger's Once + CutsceneId lifecycle, but
/// the payload is a DialogBranch resource rather than a CutsceneResource.
/// </summary>
[GlobalClass]
[Tool]
public partial class DialogBranchTrigger : InteractableArea
{
    /// <summary>When the trigger fires: OnInteract (press E) or OnEnter (step into the area).</summary>
    [ExportGroup("Trigger")]
    [Export] public TriggerMode Mode = TriggerMode.OnInteract;
    /// <summary>If true, the branch runs only once and sets "branch_&lt;DialogBranchId&gt;" on WorldFlags.</summary>
    [Export] public bool Once = false;
    /// <summary>Stable id used with Once for the "branch_&lt;id&gt;" dedup flag.</summary>
    [Export] public string DialogBranchId = "";
    /// <summary>The authored dialog tree to run. Create one via Resource > New > DialogBranch.</summary>
    [Export] public DialogBranch DialogBranch { get; set; }
    /// <summary>Node id in the branch to start from. Empty = the branch's default start node.</summary>
    [Export] public string StartNodeId { get; set; } = "";
    /// <summary>WorldFlags set to true when the branch fires (e.g. "met_frank").</summary>
    [Export] public string[] SetFlagsOnFire { get; set; }

    [Signal]
    public delegate void TriggeredEventHandler();
    private bool _hasFired = false;

    public override void _Ready()
    {
        base._Ready();

        if (Engine.IsEditorHint())
            return;

        if (Once && !string.IsNullOrEmpty(DialogBranchId) && WorldFlags.Instance.HasFlag("branch_" + DialogBranchId))
        {
            _hasFired = true;
            QueueFree();
            GameLogger.Debug("DialogBranchTrigger", $"'{Name}': already seen (id='{DialogBranchId}') — removed");
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Mode != TriggerMode.OnInteract) return;
        if (_hasFired) return;
        if (!PlayerInRange) return;
        if (!@event.IsActionPressed("interact")) return;
        OnInteract();
        GetViewport().SetInputAsHandled();
    }

    protected override void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;

        if (Mode == TriggerMode.OnEnter)
        {
            PlayerInRange = true;
            GameLogger.Debug("DialogBranchTrigger", $"'{Name}': player entered — OnEnter trigger mode");
            Fire();
            return;
        }

        base.OnBodyEntered(body);
    }

    protected override void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;

        if (Mode == TriggerMode.OnEnter)
        {
            PlayerInRange = false;
            if (!CutsceneController.Instance.IsPlaying)
                DialogManager.Instance.Reset();
            return;
        }

        base.OnBodyExited(body);
    }

    protected override void OnInteract()
    {
        Fire();
    }

    private void Fire()
    {
        if (_hasFired)
        {
            GameLogger.Debug("DialogBranchTrigger", $"'{Name}': Fire skipped — already fired (Once={Once})");
            return;
        }
        if (CutsceneController.Instance.IsPlaying)
        {
            GameLogger.Debug("DialogBranchTrigger", $"'{Name}': Fire skipped — cutscene/dialog already playing");
            return;
        }

        if (Once)
        {
            _hasFired = true;
            if (!string.IsNullOrEmpty(DialogBranchId))
                WorldFlags.Instance.SetFlag("branch_" + DialogBranchId, true);
        }

        if (SetFlagsOnFire != null)
        {
            foreach (string flag in SetFlagsOnFire)
            {
                if (!string.IsNullOrEmpty(flag))
                {
                    WorldFlags.Instance.SetFlag(flag, true);
                    GameLogger.Info("DialogBranchTrigger", $"'{Name}': set flag '{flag}'=true");
                }
            }
        }

        if (DialogBranch == null)
        {
            GameLogger.Warn("DialogBranchTrigger", $"'{Name}': no DialogBranch assigned — emitting signal only");
            EmitSignal(SignalName.Triggered);
            return;
        }

        GameLogger.Info("DialogBranchTrigger", $"'{Name}': running branch '{DialogBranch.ResourcePath}', Once={Once}");
        _ = RunBranchAsync();
    }

    private async System.Threading.Tasks.Task RunBranchAsync()
    {
        await CutsceneController.Instance.RunDialogBranch(DialogBranch, StartNodeId);
    }
}
