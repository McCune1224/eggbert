using Godot;
using System.Threading;
using System.Threading.Tasks;

public partial class CutsceneController : Node
{
    private static CutsceneController _instance;
    public static CutsceneController Instance => _instance;

    private bool _isPlaying;
    public bool IsPlaying => _isPlaying;
    public bool Cancelled => _cts != null && _cts.IsCancellationRequested;
    public int LastChoiceIndex { get; set; } = -1;

    private CancellationTokenSource _cts;

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
        _isPlaying = true;
        _cts = new CancellationTokenSource();

        foreach (var step in resource.Steps)
        {
            if (Cancelled) break;
            if (!step.ShouldExecute(WorldFlags.Instance, LastChoiceIndex))
                continue;

            await step.Execute(this);

            if (step.Type == StepType.Stop || Cancelled)
                break;
        }

        _isPlaying = false;
        _cts = null;
        Player.Instance.InInteraction = false;
    }

    public void StartDialog(string[] lines, DialogVoiceResource voice = null)
    {
        if (_isPlaying || lines == null || lines.Length == 0) return;
        _isPlaying = true;
        _cts = new CancellationTokenSource();

        DoDialog(lines, voice);
    }

    private async void DoDialog(string[] lines, DialogVoiceResource voice)
    {
        DialogManager.Instance.StartDialog(new System.Collections.Generic.List<string>(lines),
            voice ?? DialogManager.Instance.DefaultVoice);
        await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);

        _isPlaying = false;
        _cts = null;
        Player.Instance.InInteraction = false;
    }

    public void Stop()
    {
        if (!_isPlaying || _cts == null) return;
        _cts.Cancel();
    }
}
