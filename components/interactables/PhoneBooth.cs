using Godot;

/// <summary>
/// Payphone that player can use to call home.
/// Dialog changes based on story progression (WorldFlags).
/// </summary>
public partial class PhoneBooth : Area2D
{
    [Export] public DialogVoiceResource PhoneVoice { get; set; }

    /// <summary>
    /// Dialog lines shown when no story progress has been made.
    /// </summary>
    [Export] public string[] IntroLines { get; set; }

    /// <summary>
    /// Dialog lines for mid-game call (after some progress).
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
    /// WorldFlag set after the call is made (to prevent repeat calls or for state tracking).
    /// </summary>
    [Export] public string CallCompleteFlag { get; set; } = "";

    private Sprite2D _promptSprite;
    private bool _playerInRange = false;
    private bool _hasDialled = false;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer;

        _promptSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_promptSprite != null)
            _promptSprite.Visible = false;

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _Input(InputEvent @event)
    {
        if (!_playerInRange || _hasDialled) return;
        if (!@event.IsActionPressed("interact")) return;

        PerformCall();
        GetViewport().SetInputAsHandled();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInRange = true;

        if (_promptSprite != null)
            _promptSprite.Visible = true;
    }

    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInRange = false;

        if (_promptSprite != null)
            _promptSprite.Visible = false;

        if (!CutsceneController.Instance.IsPlaying)
            DialogManager.Instance.Reset();
    }

    private void PerformCall()
    {
        string[] lines = IntroLines;

        if (!string.IsNullOrEmpty(EndgameFlag) && WorldFlags.Instance.HasFlag(EndgameFlag))
            lines = EndgameLines;
        else if (!string.IsNullOrEmpty(MidgameFlag) && WorldFlags.Instance.HasFlag(MidgameFlag))
            lines = MidgameLines;

        if (lines == null || lines.Length == 0)
        {
            lines = new[] { "*click*", "...", "No answer." };
        }

        if (!string.IsNullOrEmpty(CallCompleteFlag))
        {
            WorldFlags.Instance.SetFlag(CallCompleteFlag, true);
            _hasDialled = true;
        }


        CutsceneController.Instance.StartDialog(lines, PhoneVoice);
    }
}
