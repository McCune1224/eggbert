using Godot;

public partial class KeyDoor : Door
{
    [Export] public string RequiredFlag;
    [Export] public string LockedMessage = "It's locked.";

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
            base.Open();
            GD.Print($"KeyDoor: unlocked by flag '{RequiredFlag}'");
        }
        else
        {
            DialogManager.Instance.StartDialog(
                new System.Collections.Generic.List<string> { LockedMessage });
            GD.Print($"KeyDoor: locked — missing flag '{RequiredFlag}'");
        }
    }

    public override void Close()
    {
        if (!_permanentlyUnlocked)
            base.Close();
    }
}
