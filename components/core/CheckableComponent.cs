using Godot;

/// <summary>
/// Attach to any entity (NPC, object) to make it inspectable via the Check/Tattle action.
/// Shows a dialog bubble with the CheckLine when the player presses the check key while facing it.
/// </summary>
public partial class CheckableComponent : Area2D
{
    [Export] public string CheckLine { get; set; } = "";

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.InteractableLayer;

        // Small default collision shape if none assigned
        if (GetChildCount() == 0 || GetNodeOrNull<CollisionShape2D>("CollisionShape2D") == null)
        {
            var shape = new CollisionShape2D
            {
                Shape = new CircleShape2D { Radius = 48f }
            };
            AddChild(shape);
        }
    }
}
