using Godot;

/// <summary>
/// Tile that pushes the player (and pushable objects) in a set direction.
/// Player can move against the conveyor with sprint.
/// </summary>
[GlobalClass]
[Tool]
public partial class ConveyorTile : Area2D
{
    /// <summary>Direction the conveyor pushes bodies (e.g. Vector2.Right, Vector2.Up).</summary>
    [ExportGroup("Conveyor")]
    [Export] public Vector2 ConveyorDirection { get; set; } = Vector2.Right;

    /// <summary>Push speed applied to bodies on the conveyor, in pixels per second.</summary>
    [Export(PropertyHint.Range, "10,300,10")]
    public float ConveyorSpeed { get; set; } = 80f;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer | CollisionConfig.InteractableLayer;
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (ConveyorDirection == Vector2.Zero)
            warnings.Add("ConveyorDirection is zero — conveyor will not push anything.");
        if (ConveyorSpeed <= 0f)
            warnings.Add("ConveyorSpeed is zero or negative — conveyor will not push anything.");
        return warnings.ToArray();
    }

    public override void _Draw()
    {
        if (!Engine.IsEditorHint()) return;
        // Gizmo: draw an arrow showing the push direction in the editor.
        Vector2 dir = ConveyorDirection.Normalized();
        if (dir == Vector2.Zero) return;
        var color = new Color(1, 0.8f, 0.2f, 0.7f);
        DrawLine(Vector2.Zero, dir * 24f, color, 3f);
        // Arrowhead
        Vector2 tip = dir * 24f;
        Vector2 left = tip + dir.Rotated(2.6f) * 8f;
        Vector2 right = tip + dir.Rotated(-2.6f) * 8f;
        DrawLine(tip, left, color, 3f);
        DrawLine(tip, right, color, 3f);
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) QueueRedraw();
    }

    private void OnBodyEntered(Node2D body)
    {
        string bodyName = body.Name;
        string bodyType = body is PushBlock ? "PushBlock" : body.IsInGroup("player") ? "Player" : "Unknown";
        GameLogger.Info("ConveyorTile", $"{Name}: {bodyType} '{bodyName}' entered — direction={ConveyorDirection}, speed={ConveyorSpeed}");
        if (body is PushBlock block)
            block.TryPush(ConveyorDirection);
        else if (body is Player player)
            player.RegisterConveyor(this);
    }

    private void OnBodyExited(Node2D body)
    {
        string bodyName = body.Name;
        string bodyType = body is PushBlock ? "PushBlock" : body.IsInGroup("player") ? "Player" : "Unknown";
        GameLogger.Info("ConveyorTile", $"{Name}: {bodyType} '{bodyName}' exited");
        if (body is Player player)
            player.UnregisterConveyor(this);
    }

    public Vector2 GetConveyorVelocity(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            var player = body as Player;
            if (player != null && Input.IsActionPressed("player_sprint"))
                return Vector2.Zero;
        }
        return ConveyorDirection.Normalized() * ConveyorSpeed;
    }
}
