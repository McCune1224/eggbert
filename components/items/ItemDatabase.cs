using Godot;
using System.Collections.Generic;

/// <summary>
/// Static registry of all item definitions, keyed by Id.
/// ponytail: code-defined items (no .tres files), same pattern as WarpDatabase.
/// Add new items here.
/// </summary>
public static class ItemDatabase
{
    public static readonly Dictionary<string, Item> All = new()
    {
        // --- Key items ---
        {
            "rusty_key", new Item
            {
                Id = "rusty_key", DisplayName = "Rusty Key", Category = ItemCategory.Key,
                Description = "An old rusted key. Probably opens something nearby.",
            }
        },
        {
            "cell_key", new Item
            {
                Id = "cell_key", DisplayName = "Cell Key", Category = ItemCategory.Key,
                Description = "A heavy iron key marked with a 'C'.",
            }
        },
        // --- Consumables ---
        {
            "hardboiled_egg", new Item
            {
                Id = "hardboiled_egg", DisplayName = "Hardboiled Egg", Category = ItemCategory.Consumable,
                Description = "A perfectly boiled egg. Restores 30 HP.",
                HealAmount = 30,
            }
        },
        {
            "scrambled_egg", new Item
            {
                Id = "scrambled_egg", DisplayName = "Scrambled Egg", Category = ItemCategory.Consumable,
                Description = "Fluffy scrambled eggs. Restores 60 HP.",
                HealAmount = 60,
            }
        },
        // --- Equipment ---
        {
            "butter_knife", new Item
            {
                Id = "butter_knife", DisplayName = "Butter Knife", Slot = EquipSlot.Weapon,
                Category = ItemCategory.Equipment, AttackBoost = 3,
                Description = "Dull but dependable. +3 ATK",
            }
        },
        {
            "egg_shell", new Item
        {
                Id = "egg_shell", DisplayName = "Egg Shell", Slot = EquipSlot.Armor,
                Category = ItemCategory.Equipment, DefenseBoost = 5,
                Description = "Surprisingly sturdy. +5 DEF",
            }
        },
        {
            "lucky_yolk", new Item
            {
                Id = "lucky_yolk", DisplayName = "Lucky Yolk", Slot = EquipSlot.Accessory,
                Category = ItemCategory.Equipment, SpeedBoost = 2,
                Description = "A warm, golden yolk. +2 SPD",
            }
        },
        {
            "baseball_bat", new Item
            {
                Id = "baseball_bat", DisplayName = "Baseball Bat", Slot = EquipSlot.Weapon,
                Category = ItemCategory.Equipment, AttackBoost = 5,
                Description = "Crack! +5 ATK",
            }
        },
        {
            "soda_can_armor", new Item
            {
                Id = "soda_can_armor", DisplayName = "Soda Can Armor", Slot = EquipSlot.Armor,
                Category = ItemCategory.Equipment, DefenseBoost = 8,
                Description = "Fashionable and functional. +8 DEF",
            }
        },
        {
            "dice", new Item
            {
                Id = "dice", DisplayName = "Dice", Slot = EquipSlot.Accessory,
                Category = ItemCategory.Equipment, AttackBoost = 3, DefenseBoost = 3,
                Description = "Roll the bones. +3 ATK, +3 DEF",
            }
        },
        // --- Items referenced by scenes but previously missing (demo plan Step A1) ---
        {
            "eggshell_helm", new Item
            {
                Id = "eggshell_helm", DisplayName = "Eggshell Helm", Slot = EquipSlot.Armor,
                Category = ItemCategory.Equipment, DefenseBoost = 4, MaxHPBoost = 10,
                Description = "A helm fashioned from a cracked eggshell. +4 DEF, +10 Max HP",
            }
        },
        {
            "eggdrop_soup", new Item
            {
                Id = "eggdrop_soup", DisplayName = "Eggdrop Soup", Category = ItemCategory.Consumable,
                Description = "A warm bowl of eggdrop soup. Restores 25 HP.",
                HealAmount = 25,
            }
        },
        {
            "deviled_egg", new Item
            {
                Id = "deviled_egg", DisplayName = "Deviled Egg", Category = ItemCategory.Consumable,
                Description = "A spicy deviled egg. Restores 20 HP.",
                HealAmount = 20,
            }
        },
        {
            "egg_salad_sandwich", new Item
            {
                Id = "egg_salad_sandwich", DisplayName = "Egg Salad Sandwich", Category = ItemCategory.Consumable,
                Description = "A sturdy sandwich. Restores 45 HP.",
                HealAmount = 45,
            }
        },
        {
            "golden_yolk", new Item
            {
                Id = "golden_yolk", DisplayName = "Golden Yolk", Category = ItemCategory.Key,
                Description = "A radiant yolk that pulses with warmth. The heart of the Sunnyside shrine.",
            }
        },
        {
            "warden_key", new Item
            {
                Id = "warden_key", DisplayName = "Warden's Key", Category = ItemCategory.Key,
                Description = "A heavy brass key stamped with the warden's seal. Opens the way to the Warden's Quarters.",
            }
        },
    };

    public static Item Get(string id)
    {
        if (All.TryGetValue(id, out Item item))
            return item;

        GameLogger.Warn("ItemDatabase", $"Item not found: '{id}'");
        return null;
    }
}
