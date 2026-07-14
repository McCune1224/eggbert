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
                Id = "rusty_key",
                DisplayName = "Rusty Key",
                Description = "A heavy iron key, orange with age. Opens something, probably.",
                Category = ItemCategory.Key,
            }
        },
        {
            "cell_key", new Item
            {
                Id = "cell_key",
                DisplayName = "Cell Key",
                Description = "A cold iron key stamped 'BLOCK C'. Feels important.",
                Category = ItemCategory.Key,
            }
        },
        // --- Consumables ---
        {
            "hardboiled_egg", new Item
            {
                Id = "hardboiled_egg",
                DisplayName = "Hardboiled Egg",
                Description = "Restores a bit of pep. (+10 HP)",
                Category = ItemCategory.Consumable,
                HealAmount = 10,
            }
        },
        {
            "scrambled_egg", new Item
            {
                Id = "scrambled_egg",
                DisplayName = "Scrambled Egg",
                Description = "A messy comfort. Heals more than it should. (+25 HP)",
                Category = ItemCategory.Consumable,
                HealAmount = 25,
            }
        },
        // --- Equipment ---
        {
            "eggshell_helm", new Item
            {
                Id = "eggshell_helm",
                DisplayName = "Eggshell Helm",
                Description = "Cracked but sturdy. Worn on the head, mostly. (+10 HP, +3 DEF)",
                Category = ItemCategory.Equipment,
                Slot = EquipSlot.Armor,
                DefenseBoost = 3,
                MaxHPBoost = 10,
            }
        },
    };

    public static Item Get(string id) =>
        All.TryGetValue(id, out Item item) ? item : null;
}