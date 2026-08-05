using Godot;
using Godot.Collections;

/// <summary>
/// Equipment autoload — holds equipped items in Weapon/Armor/Accessory slots.
/// Stat application (ApplyItemStats) modifies player stats using equipped items:
/// MaxHP and Defense are wired; ParryRadius and ParryDamage are wired.
/// Attack and Speed are computed but unused (per DESIGN.md: these stats
/// exist for future expansion, not current combat). Unequip reverses all
/// stat changes. ISavable — persisted with load priority 5 (after player).
/// </summary>
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

    /// <summary>
    /// Applies or reverses item stat modifiers. <paramref name="sign"/> is +1
    /// on equip and -1 on unequip. Only stat fields wired to player stats
    /// (MaxHP, Defense, ParryRadius, ParryDamage) are applied; Attack and
    /// Speed are computed but not wired to the player per DESIGN.md.
    /// </summary>
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
            parry.UpdateStats(GetTotalParryRadius(), GetTotalParryDamage(), GetTotalParryCooldownReduction());
        CombatStats.Refresh();
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

    // Public totals for UI (menu comparison, docs/combat-ui-design.md §4.2).
    public float TotalParryRadius => GetTotalParryRadius();
    public int TotalParryDamage => GetTotalParryDamage();
    public float TotalParryCooldownReduction => GetTotalParryCooldownReduction();
    public float TotalBulletSlow => GetTotalBulletSlow();

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

    // --- Combat behavior totals (docs/combat-ui-design.md §5.1) ---
    // All summed across the three equipped slots; consumed by CombatStats.Refresh().

    public float GetTotalBulletSlow()
    {
        float total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.BulletSlowFactor;
        }
        return total;
    }

    public float GetTotalParryCooldownReduction()
    {
        float total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.ParryCooldownReduction;
        }
        return total;
    }

    public float GetTotalReflectSpeedBoost()
    {
        float total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.ReflectSpeedBoost;
        }
        return total;
    }

    public float GetTotalGrazeRadiusBoost()
    {
        float total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.GrazeRadiusBoost;
        }
        return total;
    }

    public float GetTotalHomingResistance()
    {
        float total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.HomingResistance;
        }
        return total;
    }

    public int GetTotalBlockCharges()
    {
        int total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.BlockCharges;
        }
        return total;
    }

    public float GetTotalRegenPerSecond()
    {
        float total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.RegenPerSecond;
        }
        return total;
    }

    public float GetTotalEvadeChance()
    {
        float total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.EvadeChance;
        }
        return total;
    }

    public float GetTotalInvulnerabilityBoost()
    {
        float total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.InvulnerabilityBoost;
        }
        return total;
    }

    public float GetTotalDashCooldownReduction()
    {
        float total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.DashCooldownReduction;
        }
        return total;
    }

    public float GetTotalTelegraphBoost()
    {
        float total = 0;
        foreach (var id in _slots.Values)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var item = ItemDatabase.Get(id);
            if (item != null) total += item.TelegraphBoost;
        }
        return total;
    }

    /// <summary>
    /// Returns a stat-change preview string for equipping an item,
    /// showing current vs projected values with +/- deltas.
    /// Covers every combat-relevant field (docs/combat-ui-design.md §5.1).
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
        float currentParryR = current?.ParryRadiusBoost ?? 0;
        int currentParryDmg = current?.ParryDamageBoost ?? 0;
        float currentCd = current?.ParryCooldownReduction ?? 0;
        float currentSlow = current?.BulletSlowFactor ?? 0;
        float currentReflect = current?.ReflectSpeedBoost ?? 0;
        float currentGraze = current?.GrazeRadiusBoost ?? 0;
        float currentHoming = current?.HomingResistance ?? 0;
        int currentBlock = current?.BlockCharges ?? 0;
        float currentRegen = current?.RegenPerSecond ?? 0;
        float currentEvade = current?.EvadeChance ?? 0;
        float currentIframes = current?.InvulnerabilityBoost ?? 0;
        float currentDashCd = current?.DashCooldownReduction ?? 0;
        float currentTelegraph = current?.TelegraphBoost ?? 0;

        void AddDeltaInt(string label, int cur, int nxt)
        {
            int delta = nxt - cur;
            if (delta == 0) return;
            deltas.Add($"{label} {(delta > 0 ? "+" : "")}{delta}");
        }

        void AddDeltaFloat(string label, float cur, float nxt, string fmt = "0.#", string unit = "")
        {
            float delta = nxt - cur;
            if (Mathf.Abs(delta) < 0.0005f) return;
            deltas.Add($"{label} {(delta > 0 ? "+" : "")}{delta.ToString(fmt)}{unit}");
        }

        void AddPercent(string label, float cur, float nxt)
        {
            float delta = nxt - cur;
            if (Mathf.Abs(delta) < 0.0005f) return;
            deltas.Add($"{label} {(delta > 0 ? "+" : "")}{delta * 100f:0}%");
        }

        AddDeltaInt("HP", currentHp, item.MaxHPBoost);
        AddDeltaInt("ATK", currentAtk, item.AttackBoost);
        AddDeltaInt("DEF", currentDef, item.DefenseBoost);
        AddDeltaInt("SPD", currentSpd, item.SpeedBoost);
        AddDeltaFloat("PARRY R", currentParryR, item.ParryRadiusBoost);
        AddDeltaInt("PARRY DMG", currentParryDmg, item.ParryDamageBoost);
        AddDeltaFloat("PARRY CD", currentCd, item.ParryCooldownReduction, "0.0#", "s");
        AddPercent("SLOW", currentSlow, item.BulletSlowFactor);
        AddPercent("REFLECT", currentReflect, item.ReflectSpeedBoost);
        AddDeltaFloat("GRAZE", currentGraze, item.GrazeRadiusBoost);
        AddPercent("HOMING RES", currentHoming, item.HomingResistance);
        AddDeltaInt("BLOCK", currentBlock, item.BlockCharges);
        AddDeltaFloat("REGEN", currentRegen, item.RegenPerSecond, "0.#", "/s");
        AddPercent("EVADE", currentEvade, item.EvadeChance);
        AddDeltaFloat("IFRAMES", currentIframes, item.InvulnerabilityBoost, "0.0#", "s");
        AddDeltaFloat("DASH CD", currentDashCd, item.DashCooldownReduction, "0.0#", "s");
        AddPercent("TELEGRAPH", currentTelegraph, item.TelegraphBoost);

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
