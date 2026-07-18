using Godot;

[GlobalClass]
[Tool]
public partial class KeyDoor : Door
{
    [ExportGroup("KeyDoor")]
    [Export] public string RequiredFlag;
    [Export] public string LockedMessage = "It's locked.";
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
