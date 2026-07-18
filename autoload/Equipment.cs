using Godot;
using Godot.Collections;

public partial class Equipment : Node, ISavable
{
    private static Equipment _instance;
    public static Equipment Instance => _instance;

    private Dictionary<EquipSlot, string> _slots = new()
    {
        { EquipSlot.Weapon, "" },
        { EquipSlot.Armor, "" },
        { EquipSlot.Accessory, "" }
    };

    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = this;
            AddToGroup("persist");
        }
        else
        {
            QueueFree();
        }
    }

    public bool IsEquipped(string itemId)
    {
        foreach (var entry in _slots)
        {
            if (entry.Value == itemId) return true;
        }
        return false;
    }

    public string GetEquippedId(EquipSlot slot)
    {
        return _slots.TryGetValue(slot, out var id) ? id : "";
    }

    public Item GetEquipped(EquipSlot slot)
    {
        string id = GetEquippedId(slot);
        return !string.IsNullOrEmpty(id) ? ItemDatabase.Get(id) : null;
    }

    public void Equip(Item item)
    {
        if (item == null || item.Category != ItemCategory.Equipment) return;
        if (item.Slot == EquipSlot.None) return;

        // Unequip current item in same slot, returns it to inventory
        var current = GetEquipped(item.Slot);
        if (current != null)
            Unequip(item.Slot);

        // Verify item exists in inventory before equipping
        if (item.Id != GetEquippedId(item.Slot) && !Inventory.Instance.Remove(item.Id, 1))
            return;

        _slots[item.Slot] = item.Id;

        // Apply stat bonuses
        ApplyItemStats(item, 1);
        GameLogger.Info("Equipment", $"Equipped: {item.Id} \u2192 {item.Slot}");
    }

    public void Unequip(EquipSlot slot)
    {
        string id = GetEquippedId(slot);
        if (string.IsNullOrEmpty(id)) return;

        Item item = ItemDatabase.Get(id);
        if (item == null)
        {
            _slots[slot] = "";
            return;
        }

        // Remove stat bonuses
        ApplyItemStats(item, -1);

        _slots[slot] = "";
        Inventory.Instance.Add(id, 1);
        GameLogger.Info("Equipment", $"Unequipped: {id} from {slot}");
    }

    private void ApplyItemStats(Item item, int sign)
    {
        var player = Player.Instance;
        if (player == null) return;

        var hc = player.HealthComponent;
        if (hc != null)
        {
            hc.SetMaxHP(hc.MaxHP + item.MaxHPBoost * sign);
            hc.Defense = Mathf.Max(0, hc.Defense + item.DefenseBoost * sign);
        }

        var parry = player.Parry;
        if (parry != null)
            parry.UpdateStats(GetTotalParryRadius(), GetTotalParryDamage());
        GameLogger.Debug("Equipment", $"ApplyItemStats: '{item.Id}' sign={sign} — ATK={item.AttackBoost}, DEF={item.DefenseBoost}, SPD={item.SpeedBoost}, HP={item.MaxHPBoost}");
    }

    private float GetTotalParryRadius()
    {
        float total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.ParryRadiusBoost;
        }
        return total;
    }

    private int GetTotalParryDamage()
    {
        int total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null)
            {
                total += item.ParryDamageBoost;
                total += item.AttackBoost;
            }
        }
        return total;
    }

    public int TotalSpeedBoost => GetTotalSpeedBoost();

    private int GetTotalSpeedBoost()
    {
        int total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.SpeedBoost;
        }
        return total;
    }

    public int TotalAttackBoost => GetTotalAttackBoost();

    private int GetTotalAttackBoost()
    {
        int total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.AttackBoost;
        }
        return total;
    }

    /// <summary>
    /// Returns a stat-change preview string for equipping an item,
    /// showing current vs projected values with +/- deltas.
    /// </summary>
    public string PreviewDeltas(Item item)
    {
        if (item == null || item.Slot == EquipSlot.None) return "";

        var deltas = new System.Collections.Generic.List<string>();

        string currentId = GetEquippedId(item.Slot);
        Item current = string.IsNullOrEmpty(currentId) ? null : ItemDatabase.Get(currentId);

        int currentHp = current?.MaxHPBoost ?? 0;
        int currentAtk = current?.AttackBoost ?? 0;
        int currentDef = current?.DefenseBoost ?? 0;
        int currentSpd = current?.SpeedBoost ?? 0;

        void AddDelta(string label, int cur, int nxt)
        {
            int delta = nxt - cur;
            if (delta == 0) return;
            deltas.Add($"{label} {(delta > 0 ? "+" : "")}{delta}");
        }

        AddDelta("HP", currentHp, item.MaxHPBoost);
        AddDelta("ATK", currentAtk, item.AttackBoost);
        AddDelta("DEF", currentDef, item.DefenseBoost);
        AddDelta("SPD", currentSpd, item.SpeedBoost);

        return string.Join(", ", deltas);
    }

    public string SaveKey => "equipment";

    public Godot.Collections.Dictionary<string, Variant> Serialize()
    {
        string weapon = GetEquippedId(EquipSlot.Weapon);
        string armor = GetEquippedId(EquipSlot.Armor);
        string accessory = GetEquippedId(EquipSlot.Accessory);
        GameLogger.Debug("Equipment", $"Serialize: weapon='{weapon}', armor='{armor}', accessory='{accessory}'");

        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["weapon_id"] = weapon,
            ["armor_id"] = armor,
            ["accessory_id"] = accessory
        };
    }

    public void Deserialize(Godot.Collections.Dictionary<string, Variant> data)
    {
        // Clear all slots
        foreach (EquipSlot slot in System.Enum.GetValues<EquipSlot>())
            _slots[slot] = "";

        string w = data.TryGetValue("weapon_id", out var wv) ? wv.AsString() : "";
        string a = data.TryGetValue("armor_id", out var av) ? av.AsString() : "";
        string acc = data.TryGetValue("accessory_id", out var accv) ? accv.AsString() : "";
        GameLogger.Debug("Equipment", $"Deserialize: weapon='{w}', armor='{a}', accessory='{acc}'");

        int expected = (string.IsNullOrEmpty(w) ? 0 : 1) + (string.IsNullOrEmpty(a) ? 0 : 1) + (string.IsNullOrEmpty(acc) ? 0 : 1);
        int loaded = 0;
        if (!string.IsNullOrEmpty(w)) { EquipById(EquipSlot.Weapon, w); loaded++; }
        if (!string.IsNullOrEmpty(a)) { EquipById(EquipSlot.Armor, a); loaded++; }
        if (!string.IsNullOrEmpty(acc)) { EquipById(EquipSlot.Accessory, acc); loaded++; }
        GameLogger.Debug("Equipment", $"Deserialize: loaded {loaded}/{expected} slots — MATCH={loaded == expected}");
    }


    private void EquipById(EquipSlot slot, string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        Item item = ItemDatabase.Get(id);
        if (item == null || item.Category != ItemCategory.Equipment) return;

        _slots[slot] = id;

        ApplyItemStats(item, 1);
    }

    public int GetLoadPriority() => 5;
}
