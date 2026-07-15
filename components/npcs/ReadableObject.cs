using Godot;

/// <summary>
/// Interact-triggered dialog for signs, posters, books, scribbled notes.
/// Reuses DialogManager to show lines on interaction.
/// WorldFlags can gate which line shows via optional flag checks.
/// </summary>
public partial class ReadableObject : Area2D
{
    [Export] public string[] DialogLines { get; set; }
    [Export] public DialogVoiceResource Voice { get; set; }

    /// <summary>
    /// If set, the WorldFlag to check before showing dialog.
    /// When the flag is true, the alternate lines (if provided) are shown instead.
    /// </summary>
    [Export] public string GateFlag { get; set; } = "";

    /// <summary>
    /// Dialog lines to show when GateFlag is true. If empty, falls back to DialogLines.
    /// </summary>
    [Export] public string[] GateDialogLines { get; set; }

    /// <summary>
    /// If true, this readable can only be read once (sets a flag after reading).
    /// </summary>
    [Export] public bool Once { get; set; } = false;

    private Sprite2D _promptSprite;
    private bool _playerInRange = false;
    private bool _hasBeenRead = false;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer;

        _promptSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_promptSprite != null)
            _promptSprite.Visible = false;

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        if (Once && !string.IsNullOrEmpty(GateFlag) && WorldFlags.Instance.HasFlag("read_" + GateFlag))
        {
            _hasBeenRead = true;
            QueueFree();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_playerInRange || _hasBeenRead) return;
        if (!@event.IsActionPressed("interact")) return;

        Read();
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

    private void Read()
    {
        if (Once)
        {
            string flag = "read_" + (string.IsNullOrEmpty(GateFlag) ? Name : GateFlag);
            WorldFlags.Instance.SetFlag(flag, true);
            _hasBeenRead = true;
        }

        string[] lines = DialogLines;

        if (lines != null && lines.Length > 0)
        {
            CutsceneController.Instance.StartDialog(lines, Voice);
        }
    }
}
