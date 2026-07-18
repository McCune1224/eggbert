using Godot;

/// <summary>
/// Tile that pushes the player (and pushable objects) in a set direction.
/// Player can move against the conveyor with sprint.
/// </summary>
[GlobalClass]
[Tool]
public partial class ConveyorTile : Area2D
{
    [ExportGroup("Conveyor")]
    [Export]
    /// Direction the conveyor pushes bodies (e.g. Vector2.Right, Vector2.Up).
    public Vector2 ConveyorDirection { get; set; } = Vector2.Right;

    [Export]
    /// Push speed applied to bodies on the conveyor.
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

    private void OnBodyEntered(Node2D body)
    {
        string bodyName = body.Name;
        string bodyType = body is PushBlock ? "PushBlock" : body.IsInGroup("player") ? "Player" : "Unknown";
        GameLogger.Info("ConveyorTile", $"{Name}: {bodyType} '{bodyName}' entered — direction={ConveyorDirection}, speed={ConveyorSpeed}");
        if (body is PushBlock block)
            block.TryPush(ConveyorDirection);
    }

    private void OnBodyExited(Node2D body)
    {
        string bodyName = body.Name;
        string bodyType = body is PushBlock ? "PushBlock" : body.IsInGroup("player") ? "Player" : "Unknown";
        GameLogger.Info("ConveyorTile", $"{Name}: {bodyType} '{bodyName}' exited");
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
