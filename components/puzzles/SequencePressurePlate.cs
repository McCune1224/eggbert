using Godot;

[GlobalClass]
[Tool]
public partial class SequencePressurePlate : Area2D
{
    [ExportGroup("Sequence")]
    [Export]
    /// Order index in the sequence (0-based). Plates must be pressed in ascending order.
    public int SequenceIndex { get; set; } = 0;
    internal SequencePuzzleController _controller;
    private Sprite2D _sprite;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer | CollisionConfig.InteractableLayer;
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player") && !body.IsInGroup("pushable")) return;

        if (_controller == null)
            _controller = GetParent()?.GetNodeOrNull<SequencePuzzleController>("SequenceController");

        GameLogger.Info("SequencePressurePlate", $"{Name}: pressed by '{body.Name}', sequenceIndex={SequenceIndex}");
        _controller?.StepPressed(SequenceIndex);
    }

    public void Flash(bool correct)
    {
        if (_sprite == null) return;
        _sprite.Modulate = correct ? new Color(0, 1, 0) : new Color(1, 0, 0);
        var tween = CreateTween();
        tween.TweenProperty(_sprite, "modulate", Colors.White, 0.5f);
    }
}
