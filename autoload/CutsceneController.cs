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

    private CancellationTokenSource _cts;

    [Signal]
    public delegate void CutsceneFinishedEventHandler();

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

    public void Stop()
    {
        GameLogger.Info("Cutscene", "Cutscene stopped (cancelled).");
        if (!_isPlaying || _cts == null) return;
        _cts.Cancel();
        _isPlaying = false;
    }
}
