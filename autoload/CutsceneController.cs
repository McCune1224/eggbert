using Godot;
using System.Threading;

public partial class CutsceneController : Node
{
    private static CutsceneController _instance;
    public static CutsceneController Instance => _instance;

    private bool _isPlaying;
    public bool IsPlaying => _isPlaying;
    public bool Cancelled => _cts != null && _cts.IsCancellationRequested;
    public int LastChoiceIndex { get; set; } = -1;

    /// <summary>
    /// Lets AnimationPlayer-driven cutscene scenes (<see cref="CutsceneDirector"/>) claim or
    /// release the playing state so systems gating on <see cref="IsPlaying"/> (dev-save
    /// hotkeys, interaction prompts, cutscene double-fire protection) treat them like any
    /// other cutscene. The claiming Director owns cleanup (releases on finish or unload).
    /// </summary>
    public void ClaimExternalCutscene(bool active) => _isPlaying = active;

    /// <summary>
    /// Test hook: when set, RunDialogBranch uses this delegate instead of
    /// DialogManager.PromptChoices so headless tests can drive the choice
    /// index deterministically. Returns the choice index to take.
    /// </summary>
    public System.Func<System.Collections.Generic.List<string>, int> PromptChoicesOverride;
    private CancellationTokenSource _cts;

    [Signal]
    public delegate void CutsceneFinishedEventHandler();
    [Signal]
    public delegate void DialogBranchFinishedEventHandler();

    private System.Threading.Tasks.Task<int> PromptChoicesAsync(System.Collections.Generic.List<string> choices)
    {
        if (PromptChoicesOverride != null)
            return System.Threading.Tasks.Task.FromResult(PromptChoicesOverride(choices));
        return DialogManager.Instance.PromptChoices(choices);
    }

    public override void _Ready()
    {
        if (_instance == null)
            _instance = this;
        else
            QueueFree();
    }

    public async void StartCutscene(CutsceneResource resource)
    {
        if (_isPlaying || resource == null || resource.Steps == null) return;
        GameLogger.Info("Cutscene", $"Starting cutscene: {resource.ResourcePath} ({resource.Steps.Count} steps)");
        _isPlaying = true;
        _cts = new CancellationTokenSource();
        LastChoiceIndex = -1;

        int stepIndex = 0;
        foreach (var step in resource.Steps)
        {
            if (Cancelled) break;

            if (!step.ShouldExecute(WorldFlags.Instance, LastChoiceIndex))
            {
                GameLogger.Debug("Cutscene", $"Step {stepIndex}/{resource.Steps.Count} [{step.Type}]: skipped (condition not met)");
                stepIndex++;
                continue;
            }

            GameLogger.Debug("Cutscene", $"Step {stepIndex}/{resource.Steps.Count} [{step.Type}]: executing");
            await step.Execute(this);
            GameLogger.Debug("Cutscene", $"Step {stepIndex}/{resource.Steps.Count} [{step.Type}]: completed");

            if (step.Type == StepType.Stop || Cancelled)
                break;

            stepIndex++;
        }

        bool wasCancelled = Cancelled;
        _isPlaying = false;
        _cts = null;
        Player.Instance.InInteraction = false;
        if (!wasCancelled)
            EmitSignal(SignalName.CutsceneFinished);
        GameLogger.Info("Cutscene", $"Cutscene finished ({stepIndex}/{resource.Steps.Count} steps executed)");
    }

    public void StartDialog(string[] lines, DialogVoiceResource voice = null)
    {
        if (_isPlaying || lines == null || lines.Length == 0) return;
        GameLogger.Debug("Cutscene", $"Starting dialog-only ({lines.Length} lines)");
        _isPlaying = true;
        _cts = new CancellationTokenSource();

        DoDialog(lines, voice);
    }

    private async void DoDialog(string[] lines, DialogVoiceResource voice)
    {
        GameLogger.Debug("Cutscene", "DoDialog started");
        DialogManager.Instance.StartDialog(new System.Collections.Generic.List<string>(lines),
            voice ?? DialogManager.Instance.DefaultVoice);
        await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);
        GameLogger.Debug("Cutscene", "DoDialog finished");

        if (Cancelled)
        {
            DialogManager.Instance.Reset();
        }

        bool wasCancelled = Cancelled;
        _isPlaying = false;
        _cts = null;
        Player.Instance.InInteraction = false;
        if (!wasCancelled)
            EmitSignal(SignalName.CutsceneFinished);
    }

    /// <summary>
    /// Plays an authored dialog tree. When called outside a cutscene, this method owns
    /// the interaction lifecycle; cutscene steps reuse their enclosing lifecycle.
    /// </summary>
    public async System.Threading.Tasks.Task RunDialogBranch(DialogBranch branch, string startNodeId = null)
    {
        if (branch == null || branch.Nodes == null || branch.Nodes.Count == 0)
        {
            GameLogger.Warn("Cutscene", "DialogBranch: no nodes to run.");
            return;
        }

        bool ownsSession = !_isPlaying;
        if (ownsSession)
        {
            _isPlaying = true;
            _cts = new CancellationTokenSource();
            LastChoiceIndex = -1;
        }

        try
        {
            int nodeIndex = FindDialogNodeIndex(branch, startNodeId);
            if (nodeIndex < 0)
            {
                GameLogger.Warn("Cutscene", $"DialogBranch: start node '{startNodeId}' was not found.");
                return;
            }

            // A StartNodeId is the entry point — the first node enters via normal
            // linear walk. A node entered via a response's NextNodeId jump is
            // terminal: processing it ends the branch so the walker never
            // reaches sibling nodes past the chosen path.
            bool jumpedToCurrent = false;
            while (!Cancelled && nodeIndex >= 0 && nodeIndex < branch.Nodes.Count)
            {
                DialogNode node = branch.Nodes[nodeIndex];
                if (node == null)
                {
                    if (jumpedToCurrent) { jumpedToCurrent = false; break; }
                    nodeIndex++;
                    continue;
                }

                if (node.Condition != null && !node.Condition.IsMet(WorldFlags.Instance, LastChoiceIndex))
                {
                    GameLogger.Debug("Cutscene", $"DialogBranch: node '{node.Id}' skipped (condition not met).");
                    if (jumpedToCurrent) { jumpedToCurrent = false; break; }
                    nodeIndex++;
                    continue;
                }


                SetDialogBranchFlags(node.SetFlagsOnEnter, $"node '{node.Id}'");
                if (node.Lines != null && node.Lines.Length > 0)
                {
                    DialogManager.Instance.StartDialog(
                        new System.Collections.Generic.List<string>(node.Lines),
                        node.Voice ?? DialogManager.Instance.DefaultVoice);
                    await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);
                    if (Cancelled)
                    {
                        DialogManager.Instance.Reset();
                        break;
                    }
                }

                var availableResponses = new System.Collections.Generic.List<DialogResponse>();
                if (node.Responses != null)
                {
                    foreach (DialogResponse response in node.Responses)
                    {
                        if (response != null && (response.Condition == null ||
                                                 response.Condition.IsMet(WorldFlags.Instance, LastChoiceIndex)))
                            availableResponses.Add(response);
                    }
                }

                if (availableResponses.Count == 0)
                {
                    // No responses: a jumped-to target ends the branch here.
                    if (jumpedToCurrent) { jumpedToCurrent = false; break; }
                    nodeIndex++;
                    continue;
                }

                var choices = new System.Collections.Generic.List<string>(availableResponses.Count);
                foreach (DialogResponse response in availableResponses)
                    choices.Add(response.Text);

                int choiceIndex = await PromptChoicesAsync(choices);
                LastChoiceIndex = choiceIndex;
                if (choiceIndex < 0 || choiceIndex >= availableResponses.Count)
                    break;

                DialogResponse selectedResponse = availableResponses[choiceIndex];
                SetDialogBranchFlags(
                    string.IsNullOrWhiteSpace(selectedResponse.SetFlagOnSelect)
                        ? System.Array.Empty<string>()
                        : new[] { selectedResponse.SetFlagOnSelect },
                    $"response '{selectedResponse.Text}'");

                if (string.IsNullOrWhiteSpace(selectedResponse.NextNodeId))
                    break;
                nodeIndex = FindDialogNodeIndex(branch, selectedResponse.NextNodeId);
                if (nodeIndex < 0)
                {
                    GameLogger.Warn("Cutscene", $"DialogBranch: target node '{selectedResponse.NextNodeId}' was not found.");
                    break;
                }
                // Next iteration enters this node as a jump target (terminal).
                jumpedToCurrent = true;
            }
        }
        finally
        {
            bool wasCancelled = Cancelled;
            EmitSignal(SignalName.DialogBranchFinished);

            if (ownsSession)
            {
                _isPlaying = false;
                _cts = null;
                if (Player.Instance != null)
                    Player.Instance.InInteraction = false;
                if (!wasCancelled)
                    EmitSignal(SignalName.CutsceneFinished);
            }
        }
    }

    private static int FindDialogNodeIndex(DialogBranch branch, string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            return 0;

        for (int index = 0; index < branch.Nodes.Count; index++)
        {
            if (branch.Nodes[index]?.Id == nodeId)
                return index;
        }

        return -1;
    }

    private static void SetDialogBranchFlags(string[] flags, string source)
    {
        if (flags == null)
            return;

        foreach (string flag in flags)
        {
            if (string.IsNullOrWhiteSpace(flag))
                continue;

            WorldFlags.Instance.SetFlag(flag, true);
            GameLogger.Debug("Cutscene", $"DialogBranch: set flag '{flag}' from {source}.");
        }
    }

    public void Stop()
    {
        GameLogger.Info("Cutscene", "Cutscene stopped (cancelled).");
        if (!_isPlaying || _cts == null) return;
        _cts.Cancel();
        _isPlaying = false;
    }
}
