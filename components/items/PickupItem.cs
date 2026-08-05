using Godot;

/// <summary>
/// Simple area-based pickup. On body_entered (collision layer 1/Player),
/// adds item to Inventory, optionally shows dialog, and queues free.
/// </summary>
public partial class PickupItem : Area2D
{
    /// <summary>ItemDatabase id of the item granted (e.g. "cell_key", "toast").</summary>
    [ExportGroup("Pickup")]
    [Export] public string ItemId = "";
    /// <summary>Stack count granted on pickup.</summary>
    [Export(PropertyHint.Range, "1,99,1")] public int Count = 1;
    /// <summary>Optional dialog shown when the item is picked up.</summary>
    [Export] public string[] DialogLines;
    /// <summary>World flags set to true on pickup (e.g. "has_cell_key").</summary>
    [Export] public string[] SetFlag = System.Array.Empty<string>();

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (string.IsNullOrEmpty(ItemId))
            warnings.Add("ItemId is empty — nothing will be granted. Set an ItemDatabase id (e.g. \"cell_key\").");
        return warnings.ToArray();
    }

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.ItemLayer;
        CollisionMask = CollisionConfig.PlayerLayer;
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;

        if (string.IsNullOrEmpty(ItemId))
        {
            GameLogger.Warn("PickupItem", $"'{Name}': ItemId is empty — nothing picked up");
            QueueFree();
            return;
        }

        Inventory.Instance.Add(ItemId, Count);
        GameLogger.Info("PickupItem", $"'{Name}': picked up (id={ItemId}, count={Count})");

        if (SetFlag != null)
        {
            foreach (string flag in SetFlag)
            {
                if (!string.IsNullOrEmpty(flag))
                {
                    WorldFlags.Instance.SetFlag(flag, true);
                    GameLogger.Info("PickupItem", $"'{Name}': set flag '{flag}'=true");
                }
            }
        }

        if (DialogLines != null && DialogLines.Length > 0)
        {
            GameLogger.Debug("PickupItem", $"'{Name}': showing dialog ({DialogLines.Length} lines)");
            DialogManager.Instance.StartDialog(new System.Collections.Generic.List<string>(DialogLines));
        }

        GameLogger.Info("PickupItem", $"'{Name}': destroyed after pickup");
        QueueFree();
    }
}
