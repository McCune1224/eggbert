using Godot;

/// <summary>
/// An item pickup that only appears when a WorldFlag condition is met.
/// Useful for hidden items that reveal after certain game events.
/// </summary>
public partial class ConditionalItem : Area2D
{
    /// <summary>ItemDatabase id of the item granted (e.g. "cell_key").</summary>
    [ExportGroup("Pickup")]
    [Export] public string ItemId { get; set; } = "";
    /// <summary>Stack count granted on pickup.</summary>
    [Export(PropertyHint.Range, "1,99,1")] public int Count { get; set; } = 1;
    /// <summary>
    /// If non-empty, the item is only visible and collidable when this WorldFlag is set (or not set, when RequiresNotSet is true).
    /// </summary>
    [ExportGroup("Condition")]
     [Export] public string RequiredFlag { get; set; } = "";
    /// <summary>If true, the item appears when RequiredFlag is NOT set (inverts the condition).</summary>
    [Export] public bool RequiresNotSet { get; set; } = false;
    /// <summary>Dialog shown when the item is picked up.</summary>
    [Export] public string[] PickupDialogLines { get; set; }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (string.IsNullOrEmpty(ItemId))
            warnings.Add("ItemId is empty — nothing will be granted. Set an ItemDatabase id (e.g. \"cell_key\").");
        return warnings.ToArray();
    }

    private CollisionShape2D _collision;
    private Sprite2D _sprite;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.ItemLayer;
        CollisionMask = CollisionConfig.PlayerLayer;

        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");

        bool conditionMet;
        if (string.IsNullOrEmpty(RequiredFlag))
            conditionMet = true;
        else if (RequiresNotSet)
            conditionMet = !WorldFlags.Instance.HasFlag(RequiredFlag);
        else
            conditionMet = WorldFlags.Instance.HasFlag(RequiredFlag);

        Visible = conditionMet;
        if (_collision != null)
            _collision.Disabled = !conditionMet;

        GameLogger.Debug("ConditionalItem", $"'{Name}': condition (flag='{RequiredFlag}', notSet={RequiresNotSet}) = {conditionMet}");
        if (!conditionMet)
            GameLogger.Debug("ConditionalItem", $"'{Name}': hidden — condition not met");

        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;

        if (string.IsNullOrEmpty(ItemId))
        {
            GameLogger.Warn("ConditionalItem", $"'{Name}': ItemId is empty — nothing picked up");
            QueueFree();
            return;
        }

        Inventory.Instance.Add(ItemId, Count);
        GameLogger.Info("ConditionalItem", $"'{Name}': picked up (id={ItemId}, count={Count})");

        if (PickupDialogLines != null && PickupDialogLines.Length > 0)
        {
            GameLogger.Debug("ConditionalItem", $"'{Name}': showing pickup dialog ({PickupDialogLines.Length} lines)");
            CutsceneController.Instance.StartDialog(PickupDialogLines);
        }

        GameLogger.Info("ConditionalItem", $"'{Name}': destroyed after pickup");
        QueueFree();
    }
}
