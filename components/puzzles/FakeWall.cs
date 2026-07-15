using Godot;

/// <summary>
/// A wall that looks solid but can be walked through.
/// Toggles collision on proximity or interaction, revealing hidden rooms.
/// </summary>
public partial class FakeWall : StaticBody2D
{
    [Export] public bool RequireInteract { get; set; } = false;
    [Export] public string[] RevealDialogLines { get; set; }
    [Export] public DialogVoiceResource RevealVoice { get; set; }

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
        {
            _triggerArea.BodyEntered += OnBodyEntered;
        }

        if (RequireInteract && _triggerArea != null)
        {
            // Wire up input handling via the trigger area
            _triggerArea.BodyEntered += OnProximityEntered;
            _triggerArea.BodyExited += OnProximityExited;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!RequireInteract || _revealed) return;
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
    }

    private void OnProximityExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerNear = false;
    }

    private void Reveal()
    {
        if (_revealed) return;
        _revealed = true;

        if (_collision != null)
            _collision.Disabled = true;

        if (_sprite != null)
            _sprite.Modulate = new Color(1, 1, 1, 0.3f);

        if (RevealDialogLines != null && RevealDialogLines.Length > 0)
        {
            CutsceneController.Instance.StartDialog(RevealDialogLines, RevealVoice);
        }

        GameLogger.Debug("FakeWall", $"Fake wall '{Name}' revealed.");
    }
}
