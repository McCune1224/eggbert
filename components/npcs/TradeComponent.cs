using Godot;

[GlobalClass]
[Tool]
public partial class TradeComponent : Node
{
    [Export] public string RequiredItemId { get; set; } = "";
    [Export] public string RewardItemId { get; set; } = "";
    [Export] public string[] TradeDialogLines { get; set; }
    [Export] public string[] SuccessDialogLines { get; set; }
    [Export] public string[] FailDialogLines { get; set; }
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
