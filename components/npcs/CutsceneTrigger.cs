using Godot;
using System.Collections.Generic;

public enum TriggerMode
{
    OnInteract,
    OnEnter
}

/// <summary>
/// Cutscene/dialog trigger for NPCs and world objects.
/// Inherits from InteractableArea for player detection + prompt.
/// Adds TriggerMode (OnInteract/OnEnter), Once/CutsceneId lifecycle,
/// and dispatch to CutsceneResource, DialogLines, or raw signal.
/// </summary>
[GlobalClass]
[Tool]
public partial class CutsceneTrigger : InteractableArea
{
    [ExportGroup("Trigger")]
    [Export] public TriggerMode Mode = TriggerMode.OnInteract;
    [Export] public bool Once = false;
    [Export] public string CutsceneId = "";
    [Export] public CutsceneResource Cutscene { get; set; }
    [Export] public string[] DialogLines { get; set; }
    /// <summary>World flags set to true when this trigger fires (e.g. "met_jamitor").</summary>
    [Export] public string[] SetFlagsOnFire { get; set; }
    [ExportGroup("Flavor Choice")]
    [Export] public string[] ChoiceOptions { get; set; }
    [Export] public string[] ChoiceResponses { get; set; }

    [Signal]
    public delegate void TriggeredEventHandler();

    private Sprite2D _npcSprite;
    private bool _hasFired = false;
    private bool _promptPositioned = false;

    public override void _Ready()
    {
        // Find NPC sprite sibling for prompt positioning
        foreach (Node sibling in GetParent().GetChildren())
        {
            if (sibling is Sprite2D s && sibling.Name != "Sprite2D")
            {
                _npcSprite = s;
                break;
            }
        }

        // Call base AFTER finding npcSprite so PositionPromptAboveNpc can use it
        base._Ready();

        if (Once && !string.IsNullOrEmpty(CutsceneId) && WorldFlags.Instance.HasFlag("cutscene_" + CutsceneId))
        {
            _hasFired = true;
            QueueFree();
            GameLogger.Debug("CutsceneTrigger", $"'{Name}': already seen (id='{CutsceneId}') — removed");
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
        PlayerInRange = true;

        if (Mode == TriggerMode.OnEnter)
        {
            GameLogger.Debug("CutsceneTrigger", $"'{Name}': player entered — OnEnter trigger mode");
            Fire();
            return;
        }

        if (PromptSprite != null && GodotObject.IsInstanceValid(PromptSprite))
        {
            if (!_promptPositioned && _npcSprite != null)
                PositionPromptAboveNpc();
            PromptSprite.Visible = true;
        }
    }

    protected override void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        PlayerInRange = false;

        if (PromptSprite != null && GodotObject.IsInstanceValid(PromptSprite))
            PromptSprite.Visible = false;

        if (!CutsceneController.Instance.IsPlaying)
            DialogManager.Instance.Reset();
    }

    protected override void OnInteract()
    {
        Fire();
    }

    private void Fire()
    {
        if (_hasFired)
        {
            GameLogger.Debug("CutsceneTrigger", $"'{Name}': Fire skipped — already fired (Once={Once})");
            return;
        }
        if (CutsceneController.Instance.IsPlaying)
        {
            GameLogger.Debug("CutsceneTrigger", $"'{Name}': Fire skipped — cutscene already playing");
            return;
        }
        if (Once)
            _hasFired = true;

        if (SetFlagsOnFire != null)
        {
            foreach (string flag in SetFlagsOnFire)
            {
                if (!string.IsNullOrEmpty(flag))
                {
                    WorldFlags.Instance.SetFlag(flag, true);
                    GameLogger.Info("CutsceneTrigger", $"'{Name}': set flag '{flag}'=true");
                }
            }
        }

        if (Cutscene != null)
        {
            GameLogger.Info("CutsceneTrigger", $"'{Name}': firing cutscene '{Cutscene.ResourcePath}', Once={Once}");
            CutsceneController.Instance.StartCutscene(Cutscene);
        }
        else if (DialogLines != null && DialogLines.Length > 0)
        {
            if (ChoiceOptions != null && ChoiceOptions.Length >= 2)
            {
                StartFlavorChoice();
                return;
            }

            GameLogger.Info("CutsceneTrigger", $"'{Name}': firing dialog ({DialogLines.Length} lines), Once={Once}");
            CutsceneController.Instance.StartDialog(DialogLines, Voice);
        }
        else
        {
            GameLogger.Debug("CutsceneTrigger", $"'{Name}': firing raw signal, Once={Once}");
            EmitSignal(SignalName.Triggered);
        }
    }

    private async void StartFlavorChoice()
    {
        DialogManager.Instance.StartDialog(new List<string>(DialogLines), Voice);
        await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);

        int choice = await DialogManager.Instance.PromptChoices(new List<string>(ChoiceOptions));
        GameLogger.Info("CutsceneTrigger", $"'{Name}': flavor choice={choice}");

        if (ChoiceResponses == null || choice < 0 || choice >= ChoiceResponses.Length ||
            string.IsNullOrWhiteSpace(ChoiceResponses[choice]))
            return;

        DialogManager.Instance.StartDialog(new List<string> { ChoiceResponses[choice] }, Voice);
    }

    private void PositionPromptAboveNpc()
    {
        float frameHeight = _npcSprite.GetRect().Size.Y;
        float topEdge = _npcSprite.Centered ? -frameHeight / 2f : 0f;
        float promptY = topEdge - 4f;
        PromptSprite.Position = new Vector2(0, promptY);
        _promptPositioned = true;
    }

    public bool IsPromptVisible() => PromptSprite?.Visible ?? false;

    public void HidePrompt()
    {
        if (PromptSprite != null)
            PromptSprite.Visible = false;
    }

    public void ShowPrompt()
    {
        if (PromptSprite != null)
            PromptSprite.Visible = true;
    }
}
