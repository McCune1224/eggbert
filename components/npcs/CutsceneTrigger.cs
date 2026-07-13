using Godot;

public enum TriggerMode
{
    OnInteract,
    OnEnter
}

public partial class CutsceneTrigger : Area2D
{
    [Export] public TriggerMode Mode = TriggerMode.OnInteract;
    [Export] public bool Once = false;
    [Export] public string CutsceneId = "";
    [Export] public CutsceneResource Cutscene { get; set; }

    [Signal]
    public delegate void TriggeredEventHandler();

    private Sprite2D _promptSprite;
    private Sprite2D _npcSprite;
    private bool _playerInRange = false;
    private bool _hasFired = false;
    private bool _promptPositioned = false;

    public override void _Ready()
    {
        _promptSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_promptSprite != null)
            _promptSprite.Visible = false;

        foreach (Node sibling in GetParent().GetChildren())
        {
            if (sibling is Sprite2D s)
            {
                _npcSprite = s;
                break;
            }
        }

        if (_npcSprite == null)
            GD.PrintErr($"CutsceneTrigger ({GetParent().Name}): no Sprite2D sibling found for prompt positioning.");

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        if (Once && !string.IsNullOrEmpty(CutsceneId) && WorldFlags.Instance.HasFlag("cutscene_" + CutsceneId))
        {
            _hasFired = true;
            QueueFree();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Mode != TriggerMode.OnInteract) return;
        if (!_playerInRange || _hasFired) return;
        if (!@event.IsActionPressed("interact")) return;

        Fire();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInRange = true;

        if (Mode == TriggerMode.OnEnter)
        {
            Fire();
            return;
        }

        if (_promptSprite != null)
        {
            if (!_promptPositioned && _npcSprite != null)
                PositionPromptAboveNpc();

            _promptSprite.Visible = true;
        }
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

    private void PositionPromptAboveNpc()
    {
        float frameHeight = _npcSprite.GetRect().Size.Y;
        float topEdge = _npcSprite.Centered ? -frameHeight / 2f : 0f;
        float promptY = topEdge - 4f;
        _promptSprite.Position = new Vector2(0, promptY);
        _promptPositioned = true;
    }

    private void Fire()
    {
        if (_hasFired) return;

        if (Once && !string.IsNullOrEmpty(CutsceneId))
            WorldFlags.Instance.SetFlag("cutscene_" + CutsceneId, true);

        _hasFired = true;

        if (Cutscene != null)
            CutsceneController.Instance.StartCutscene(Cutscene);
        else
            EmitSignal(SignalName.Triggered);
    }

    public bool IsPromptVisible() => _promptSprite?.Visible ?? false;

    public void HidePrompt()
    {
        if (_promptSprite != null)
            _promptSprite.Visible = false;
    }

    public void ShowPrompt()
    {
        if (_promptSprite != null)
            _promptSprite.Visible = true;
    }
}
