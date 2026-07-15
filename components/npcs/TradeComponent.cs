using Godot;

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
            return false;

        if (string.IsNullOrEmpty(RequiredItemId))
            return false;

        if (Inventory.Instance.Has(RequiredItemId))
        {
            Inventory.Instance.Remove(RequiredItemId, 1);
            if (!string.IsNullOrEmpty(RewardItemId))
                Inventory.Instance.Add(RewardItemId, 1);

            if (!string.IsNullOrEmpty(TradeCompleteFlag))
                WorldFlags.Instance.SetFlag(TradeCompleteFlag, true);

            return true;
        }

        return false;
    }
}
