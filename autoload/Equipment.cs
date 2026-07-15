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

        _slots[item.Slot] = item.Id;

        // Apply stat bonuses
        ApplyItemStats(item, 1);
        Inventory.Instance.Remove(item.Id, 1);
        GameLogger.Info("Equipment", $"Equipped: {item.Id} → {item.Slot}");
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
        var hc = Player.Instance.HealthComponent;
        if (hc != null)
        {
            hc.SetMaxHP(hc.MaxHP + item.MaxHPBoost * sign);
            hc.Defense = Mathf.Max(0, hc.Defense + item.DefenseBoost * sign);
        }

        var parry = Player.Instance.Parry;
        if (parry != null)
            parry.UpdateStats(GetTotalParryRadius(), GetTotalParryDamage());
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

    public SaveResource Save(SaveResource newSave)
    {
        SaveDataEquipment data = new()
        {
            WeaponId = GetEquippedId(EquipSlot.Weapon),
            ArmorId = GetEquippedId(EquipSlot.Armor),
            AccessoryId = GetEquippedId(EquipSlot.Accessory)
        };
        newSave.EquipmentData = data;
        return newSave;
    }

    public void Load(SaveResource saveData)
    {
        if (saveData.EquipmentData == null) return;

        // Clear all slots first
        foreach (var slot in _slots.Keys)
            _slots[slot] = "";

        // Re-equip from save data
        EquipById(EquipSlot.Weapon, saveData.EquipmentData.WeaponId);
        EquipById(EquipSlot.Armor, saveData.EquipmentData.ArmorId);
        EquipById(EquipSlot.Accessory, saveData.EquipmentData.AccessoryId);
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
