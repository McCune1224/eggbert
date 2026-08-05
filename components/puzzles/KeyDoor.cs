using Godot;

/// <summary>
/// A <see cref="Door"/> that is locked until the required WorldFlag is set. Displays a
/// locked message via <see cref="DialogManager"/> when the player tries to open it without
/// the flag. Once unlocked it stays unlocked permanently.
/// </summary>
/// <remarks>
/// <b>RequiredFlag + lockedMessage pattern:</b> <see cref="RequiredFlag"/> is checked each time
/// <see cref="Open"/> is called. If the flag is not present in <see cref="WorldFlags"/> the
/// <see cref="LockedMessage"/> is shown as a single-line dialog and the door stays closed.
/// When the flag is found, <see cref="_permanentlyUnlocked"/> is set to true and the door
/// opens. Subsequent calls to <see cref="Open"/> bypass the flag check entirely.
/// <see cref="Close"/> respects <see cref="_permanentlyUnlocked"/> — it will not re-close
/// an already-unlocked KeyDoor.
/// </remarks>

[GlobalClass]
[Tool]
public partial class KeyDoor : Door
{
    /// <summary>WorldFlag that unlocks this door (e.g. "has_cell_key"). Empty = door always opens.</summary>
    [ExportGroup("KeyDoor")]
    [Export] public string RequiredFlag;
    /// <summary>Dialog shown when the player tries to open the locked door without the flag.</summary>
    [Export] public string LockedMessage = "It's locked.";
    /// <summary>Jingle played when the door unlocks.</summary>
    [Export] public AudioStream UnlockJingle { get; set; }

    private bool _permanentlyUnlocked = false;

    public override void Open()
    {
        if (_permanentlyUnlocked)
        {
            base.Open();
            return;
        }

        if (string.IsNullOrEmpty(RequiredFlag))
        {
            base.Open();
            return;
        }

        if (WorldFlags.Instance.HasFlag(RequiredFlag))
        {
            _permanentlyUnlocked = true;
            if (UnlockJingle != null)
                AudioManager.Instance.PlaySfx(UnlockJingle);
            base.Open();
            GameLogger.Info("Puzzle", $"KeyDoor '{Name}': unlocked by flag '{RequiredFlag}'");
        }
        else
        {
            DialogManager.Instance.StartDialog(
                new System.Collections.Generic.List<string> { LockedMessage });
            GameLogger.Debug("Puzzle", $"KeyDoor '{Name}': locked — missing flag '{RequiredFlag}'");
        }
    }

    public override void Close()
    {
        if (!_permanentlyUnlocked)
            base.Close();
    }
}
