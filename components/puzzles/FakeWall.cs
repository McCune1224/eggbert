using Godot;

/// <summary>
/// A wall that looks solid but can be walked through.
/// Toggles collision on proximity or interaction, revealing hidden rooms.
///
/// Usage: place in a level, configure RequireInteract.
/// - RequireInteract=true: player presses E to reveal
/// - RequireInteract=false: reveals automatically on touch
/// </summary>
[GlobalClass]
[Tool]
public partial class FakeWall : StaticBody2D
{
    [ExportGroup("Behavior")]
    [Export]
    /// If true, player must press E to reveal the wall. If false, walking into it reveals automatically.
    public bool RequireInteract { get; set; } = false;

    [ExportGroup("Dialog")]
    [Export]
    /// Optional lines shown when the wall is revealed.
    public string[] RevealDialogLines { get; set; }

    [Export]
    /// Voice style for reveal dialog.
    public DialogVoiceResource RevealVoice { get; set; }

    private CollisionShape2D _collision;
    private Sprite2D _sprite;
    private bool _revealed = false;
    private Area2D _triggerArea;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.WallsLayer;

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        _triggerArea = GetNodeOrNull<Area2D>("TriggerArea");

        if (!RequireInteract && _triggerArea != null)
            _triggerArea.BodyEntered += OnBodyEntered;

        if (RequireInteract && _triggerArea != null)
        {
            _triggerArea.BodyEntered += OnProximityEntered;
            _triggerArea.BodyExited += OnProximityExited;
        }

        UpdateInteractionPrompt();
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (_triggerArea == null)
            warnings.Add("FakeWall requires a TriggerArea child node for player detection.");
        return warnings.ToArray();
    }

    public override void _Input(InputEvent @event)
    {
        if (!RequireInteract || _revealed || !_playerNear) return;
        if (!@event.IsActionPressed("interact")) return;
        Reveal();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        Reveal();
    }

    private bool _playerNear = false;

    private void OnProximityEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerNear = true;
        UpdateInteractionPrompt();
    }

    private void OnProximityExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerNear = false;
        UpdateInteractionPrompt();
    }

    private void Reveal()
    {
        if (_revealed) return;
        _revealed = true;
        UpdateInteractionPrompt();

        if (_collision != null)
            _collision.Disabled = true;

        if (_sprite != null)
            _sprite.Modulate = new Color(1, 1, 1, 0.3f);

        if (RevealDialogLines != null && RevealDialogLines.Length > 0)
            CutsceneController.Instance.StartDialog(RevealDialogLines, RevealVoice);

        GameLogger.Debug("FakeWall", $"Fake wall '{Name}' revealed.");
    }
    private void UpdateInteractionPrompt()
    {
        Player.Instance?.InteractionPrompt?.SetInteractableAvailable(this, RequireInteract && _playerNear && !_revealed);
    }
}
