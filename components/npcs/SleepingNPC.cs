using Godot;

/// <summary>
/// An NPC that starts asleep. On interact, wakes with grumpy dialog.
/// Tracks wake state via WorldFlag for subsequent interactions.
/// </summary>
public partial class SleepingNPC : Area2D
{
    [Export] public string[] WakeLines { get; set; }
    [Export] public string[] AwakeLines { get; set; }
    [Export] public DialogVoiceResource Voice { get; set; }
    [Export] public string NpcId { get; set; } = "";

    private Sprite2D _promptSprite;
    private AnimatedSprite2D _zzzSprite;
    private bool _playerInRange = false;
    private bool _isAwake = false;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer;

        if (string.IsNullOrEmpty(NpcId))
            NpcId = Name;

        _isAwake = WorldFlags.Instance.HasFlag("woke_" + NpcId);

        _promptSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_promptSprite != null)
            _promptSprite.Visible = false;

        _zzzSprite = GetNodeOrNull<AnimatedSprite2D>("Zzz");
        if (_zzzSprite != null)
            _zzzSprite.Visible = !_isAwake;

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _Input(InputEvent @event)
    {
        if (!_playerInRange) return;
        if (!@event.IsActionPressed("interact")) return;

        Interact();
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

    private void Interact()
    {
        if (!_isAwake)
        {
            _isAwake = true;
            WorldFlags.Instance.SetFlag("woke_" + NpcId, true);

            if (_zzzSprite != null)
                _zzzSprite.Visible = false;

            if (WakeLines != null && WakeLines.Length > 0)
            {
                CutsceneController.Instance.StartDialog(WakeLines, Voice);
                return;
            }
        }

        if (AwakeLines != null && AwakeLines.Length > 0)
        {
            CutsceneController.Instance.StartDialog(AwakeLines, Voice);
        }
    }
}
