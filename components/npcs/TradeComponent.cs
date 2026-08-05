using Godot;
/// <summary>
/// Enables a trade interaction on an NPC: the player gives an item
/// from Inventory and receives a reward item on success.
/// </summary>

[GlobalClass]
[Tool]
public partial class TradeComponent : Node
{
    /// <summary>Item id the player must have in inventory to complete the trade (e.g. "cell_key").</summary>
    [ExportGroup("Trade")]
    [Export] public string RequiredItemId { get; set; } = "";
    /// <summary>Item id granted to the player on a successful trade. Empty = no item.</summary>
    [Export] public string RewardItemId { get; set; } = "";
    /// <summary>Lines shown when the player initiates the trade.</summary>
    [Export] public string[] TradeDialogLines { get; set; }
    /// <summary>Lines shown after a successful trade.</summary>
    [Export] public string[] SuccessDialogLines { get; set; }
    /// <summary>Lines shown when the player lacks the required item.</summary>
    [Export] public string[] FailDialogLines { get; set; }
    /// <summary>WorldFlag set after a successful trade, so it can only happen once.</summary>
    [Export] public string TradeCompleteFlag { get; set; } = "";

    public bool TryTrade()
    {
        if (!string.IsNullOrEmpty(TradeCompleteFlag) && WorldFlags.Instance.HasFlag(TradeCompleteFlag))
        {
            GameLogger.Debug("TradeComponent", $"'{Name}': trade already completed (flag='{TradeCompleteFlag}')");
            return false;
        }

        if (string.IsNullOrEmpty(RequiredItemId))
        {
            GameLogger.Warn("TradeComponent", $"'{Name}': RequiredItemId is empty");
            return false;
        }

        if (Inventory.Instance.Has(RequiredItemId))
        {
            Inventory.Instance.Remove(RequiredItemId, 1);
            if (!string.IsNullOrEmpty(RewardItemId))
                Inventory.Instance.Add(RewardItemId, 1);

            if (!string.IsNullOrEmpty(TradeCompleteFlag))
                WorldFlags.Instance.SetFlag(TradeCompleteFlag, true);

            GameLogger.Info("TradeComponent", $"'{Name}': traded '{RequiredItemId}' → '{RewardItemId}', flag='{TradeCompleteFlag}'");
            return true;
        }

        GameLogger.Debug("TradeComponent", $"'{Name}': missing required item '{RequiredItemId}'");
        return false;
    }
}
