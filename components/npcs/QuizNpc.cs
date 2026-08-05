using Godot;
using System.Collections.Generic;

/// <summary>
/// NPC that runs a multi-question quiz. On all-correct, grants an item and sets
/// a pass flag. On any wrong answer, plays a fail line and stops (retryable).
///
/// Question data is authored as QuizQuestion sub-resources in the scene.
/// Mirrors the CutsceneTrigger interaction shape (InteractableLayer, player prompt).
/// </summary>
[GlobalClass]
public partial class QuizNpc : Area2D
{
    /// <summary>The quiz questions, asked in order. Each grants its own flag on a correct answer.</summary>
    [ExportGroup("Quiz")]
    [Export] public QuizQuestion[] Questions { get; set; }
    /// <summary>WorldFlag set when the whole quiz is passed.</summary>
    [Export] public string PassFlag { get; set; } = "";
    /// <summary>World flags set to true when the quiz is passed (in addition to PassFlag).</summary>
    [Export] public string[] GrantFlags { get; set; }
    /// <summary>Lines spoken when the player first interacts, before the first question.</summary>
    [Export] public string[] IntroLines { get; set; }
    /// <summary>Lines spoken when the quiz is fully passed.</summary>
    [Export] public string[] PassLines { get; set; }
    /// <summary>Lines spoken when any question is answered wrong (retryable).</summary>
    [Export] public string[] FailLines { get; set; }
    /// <summary>Item granted on passing (use an ItemDatabase id, e.g. "cell_key"). Empty = no item.</summary>
    [Export] public string GrantItemId { get; set; } = "";
    /// <summary>Stack count granted with GrantItemId.</summary>
    [Export(PropertyHint.Range, "1,99,1")] public int GrantCount { get; set; } = 1;
    /// <summary>Voice style for all quiz dialog.</summary>
    [Export] public DialogVoiceResource Voice { get; set; }

    /// <summary>When the quiz fires: OnInteract (press E) or OnEnter (step into the area).</summary>
    [ExportGroup("Trigger")]
    [Export] public TriggerMode Mode { get; set; } = TriggerMode.OnInteract;
    /// <summary>If set, the NPC self-frees once this flag is true (one-shot quiz gates).</summary>
    [Export] public string OnceFlag { get; set; } = "";

    private bool _playerInRange = false;
    private bool _busy = false;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.InteractableLayer;
        CollisionMask = CollisionConfig.PlayerLayer;
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        if (!string.IsNullOrEmpty(OnceFlag) && WorldFlags.Instance.HasFlag(OnceFlag))
        {
            QueueFree();
            GameLogger.Debug("QuizNpc", $"'{Name}': already passed (OnceFlag='{OnceFlag}') — removed");
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInRange = true;

        if (Mode == TriggerMode.OnEnter)
            _ = RunQuiz();
        else
            UpdateInteractionPrompt();
    }

    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInRange = false;
        UpdateInteractionPrompt();
    }

    public override void _Input(InputEvent @event)
    {
        if (Mode != TriggerMode.OnInteract) return;
        if (_busy || !_playerInRange) return;
        if (!@event.IsActionPressed("interact")) return;
        _ = RunQuiz();
        GetViewport().SetInputAsHandled();
    }

    private async System.Threading.Tasks.Task RunQuiz()
    {
        if (_busy) return;
        if (CutsceneController.Instance.IsPlaying) return;
        _busy = true;
        Player.Instance.InInteraction = true;
        UpdateInteractionPrompt();

        try
        {
            if (IntroLines != null && IntroLines.Length > 0)
                await Say(IntroLines);

            if (Questions == null || Questions.Length == 0)
            {
                GameLogger.Warn("QuizNpc", $"'{Name}': no questions configured");
                return;
            }

            for (int i = 0; i < Questions.Length; i++)
            {
                var q = Questions[i];
                if (q == null) continue;

                if (q.PromptLines != null && q.PromptLines.Length > 0)
                    await Say(q.PromptLines);

                int choice = await DialogManager.Instance.PromptChoices(new List<string>(q.Options));
                GameLogger.Info("QuizNpc", $"'{Name}': Q{i + 1} choice={choice} (correct={q.CorrectIndex})");

                if (choice != q.CorrectIndex)
                {
                    if (q.WrongResponseLines != null && q.WrongResponseLines.Length > 0)
                        await Say(q.WrongResponseLines);
                    if (FailLines != null && FailLines.Length > 0)
                        await Say(FailLines);
                    GameLogger.Info("QuizNpc", $"'{Name}': quiz failed at Q{i + 1}");
                    return;
                }

                if (!string.IsNullOrEmpty(q.CorrectFlag))
                    WorldFlags.Instance.SetFlag(q.CorrectFlag, true);
                if (q.CorrectResponseLines != null && q.CorrectResponseLines.Length > 0)
                    await Say(q.CorrectResponseLines);
            }

            if (!string.IsNullOrEmpty(PassFlag))
                WorldFlags.Instance.SetFlag(PassFlag, true);
            if (GrantFlags != null)
            {
                foreach (string flag in GrantFlags)
                {
                    if (!string.IsNullOrEmpty(flag))
                        WorldFlags.Instance.SetFlag(flag, true);
                }
            }
            if (!string.IsNullOrEmpty(GrantItemId))
                Inventory.Instance.Add(GrantItemId, GrantCount);
            if (!string.IsNullOrEmpty(OnceFlag))
                WorldFlags.Instance.SetFlag(OnceFlag, true);
            if (PassLines != null && PassLines.Length > 0)
                await Say(PassLines);
            GameLogger.Info("QuizNpc", $"'{Name}': quiz passed — PassFlag='{PassFlag}', granted '{GrantItemId}'x{GrantCount}");
        }
        finally
        {
            _busy = false;
            Player.Instance.InInteraction = false;
            UpdateInteractionPrompt();
        }
    }
    private bool ShouldShowInteractionPrompt()
    {
        return Mode == TriggerMode.OnInteract &&
               _playerInRange &&
               !_busy &&
               (string.IsNullOrEmpty(OnceFlag) || !WorldFlags.Instance.HasFlag(OnceFlag));
    }

    private void UpdateInteractionPrompt()
    {
        Player.Instance?.InteractionPrompt?.SetInteractableAvailable(this, ShouldShowInteractionPrompt());
    }

    private async System.Threading.Tasks.Task Say(string[] lines)
    {
        DialogManager.Instance.StartDialog(new List<string>(lines), Voice);
        await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);
    }
}