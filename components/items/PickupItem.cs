using Godot;

/// <summary>
/// Simple area-based pickup. On body_entered (collision layer 1/Player),
/// adds item to Inventory, optionally shows dialog, and queues free.
/// </summary>
public partial class PickupItem : Area2D
{
    [Export] public string ItemId = "";
    [Export] public int Count = 1;
    [Export] public string[] DialogLines;
    [Export] public string SetFlag = "";

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.ItemLayer;
        CollisionMask = CollisionConfig.PlayerLayer;
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;

        Inventory.Instance.Add(ItemId, Count);

        if (!string.IsNullOrEmpty(SetFlag))
            WorldFlags.Instance.SetFlag(SetFlag, true);

        if (DialogLines != null && DialogLines.Length > 0)
            DialogManager.Instance.StartDialog(new System.Collections.Generic.List<string>(DialogLines));

        QueueFree();
    }
}
