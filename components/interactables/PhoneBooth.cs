using Godot;

/// <summary>
/// Payphone that player can use to call home.
/// Dialog changes based on story progression (WorldFlags).
/// Uses InteractableArea base class for player detection + prompt.
/// </summary>
public partial class PhoneBooth : InteractableArea
{
    /// <summary>
    /// Dialog lines shown when no story progress has been made.
    /// </summary>
    [Export] public string[] IntroLines { get; set; }

    /// <summary>
    /// Dialog lines for mid-game call.
    /// </summary>
    [Export] public string[] MidgameLines { get; set; }

    /// <summary>
    /// WorldFlag that gates midgame dialog.
    /// </summary>
    [Export] public string MidgameFlag { get; set; } = "";

    /// <summary>
    /// Dialog lines for end-game call.
    /// </summary>
    [Export] public string[] EndgameLines { get; set; }

    /// <summary>
    /// WorldFlag that gates endgame dialog.
    /// </summary>
    [Export] public string EndgameFlag { get; set; } = "";

    /// <summary>
    /// WorldFlag set after the call is made.
    /// </summary>
    [Export] public string CallCompleteFlag { get; set; } = "";

    private bool _hasDialled = false;

    protected override void OnInteract()
    {
        if (_hasDialled) return;

        string[] lines = IntroLines;
        if (!string.IsNullOrEmpty(EndgameFlag) && WorldFlags.Instance.HasFlag(EndgameFlag))
            lines = EndgameLines;
        else if (!string.IsNullOrEmpty(MidgameFlag) && WorldFlags.Instance.HasFlag(MidgameFlag))
            lines = MidgameLines;

        if (lines == null || lines.Length == 0)
            lines = new[] { "*click*", "...", "No answer." };

        if (!string.IsNullOrEmpty(CallCompleteFlag))
        {
            WorldFlags.Instance.SetFlag(CallCompleteFlag, true);
            _hasDialled = true;
        }

        GameLogger.Info("PhoneBooth", $"'{Name}': called — flag='{CallCompleteFlag}', lines={(lines == IntroLines ? "intro" : lines == MidgameLines ? "midgame" : "endgame")}");
        ShowDialog(lines);
    }
}
