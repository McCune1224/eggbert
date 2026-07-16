using Godot;

/// <summary>
/// Interact-triggered dialog for signs, posters, books, scribbled notes.
/// Reuses InteractableArea base class for player detection + prompt.
/// </summary>
[GlobalClass]
[Tool]
public partial class ReadableObject : InteractableArea
{
    [Export] public string[] DialogLines { get; set; }
    [Export] public string[] AlternateLines { get; set; }

    /// <summary>
    /// If set, the WorldFlag to check before showing dialog.
    /// When true, AlternateLines are shown instead.
    /// </summary>
    [Export] public string GateFlag { get; set; } = "";

    /// <summary>
    /// If true, this readable can only be read once.
    /// </summary>
    [Export] public bool Once { get; set; } = false;

    private bool _hasBeenRead = false;

    public override void _Ready()
    {
        base._Ready();

        if (Once && !string.IsNullOrEmpty(GateFlag) && WorldFlags.Instance.HasFlag("read_" + GateFlag))
        {
            _hasBeenRead = true;
            QueueFree();
        }
    }

    protected override void OnInteract()
    {
        if (_hasBeenRead) return;

        if (Once)
        {
            string flag = "read_" + (string.IsNullOrEmpty(GateFlag) ? Name : GateFlag);
            WorldFlags.Instance.SetFlag(flag, true);
            _hasBeenRead = true;
        }

        string[] lines;
        if (!string.IsNullOrEmpty(GateFlag) && WorldFlags.Instance.HasFlag(GateFlag)
            && AlternateLines != null && AlternateLines.Length > 0)
            lines = AlternateLines;
        else
            lines = DialogLines;

        ShowDialog(lines);
    }
}
