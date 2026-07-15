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
                Icon = ResourceLoader.Load<Texture2D>("res://assets/items/sprites/item_sprite_0010.png"),
            }
        },
        {
            "cell_key", new Item
            {
                Id = "cell_key",
                DisplayName = "Cell Key",
                Description = "A cold iron key stamped 'BLOCK C'. Feels important.",
                Category = ItemCategory.Key,
                Icon = ResourceLoader.Load<Texture2D>("res://assets/items/sprites/item_sprite_0009.png"),
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
                Icon = ResourceLoader.Load<Texture2D>("res://assets/items/sprites/item_sprite_0019.png"),
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
                Icon = ResourceLoader.Load<Texture2D>("res://assets/items/sprites/item_sprite_0020.png"),
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
                Icon = ResourceLoader.Load<Texture2D>("res://assets/items/icons/icon_0192.png"),
            }
        },
        {
            "egg_salad_sandwich", new Item
            {
                Id = "egg_salad_sandwich",
                DisplayName = "Egg Salad Sandwich",
                Description = "A hearty sandwich. Fills you right up. (+40 HP)",
                Category = ItemCategory.Consumable,
                HealAmount = 40,
                Icon = ResourceLoader.Load<Texture2D>("res://assets/items/sprites/item_sprite_0018.png"),
            }
        },
        {
            "eggdrop_soup", new Item
            {
                Id = "eggdrop_soup",
                DisplayName = "Egg Drop Soup",
                Description = "Steamy and restorative. Like a hug in a bowl. (+60 HP)",
                Category = ItemCategory.Consumable,
                HealAmount = 60,
                Icon = ResourceLoader.Load<Texture2D>("res://assets/items/sprites/item_sprite_0017.png"),
            }
        },
        {
            "golden_yolk", new Item
            {
                Id = "golden_yolk",
                DisplayName = "Golden Yolk",
                Description = "Radiant and warm. Tastes like victory. (Full HP)",
                Category = ItemCategory.Consumable,
                HealAmount = 9999,
                Icon = ResourceLoader.Load<Texture2D>("res://assets/items/sprites/item_sprite_0016.png"),
            }
        },
        {
            "deviled_egg", new Item
            {
                Id = "deviled_egg",
                DisplayName = "Deviled Egg",
                Description = "Spicy little number. Two bites. (+20 HP)",
                Category = ItemCategory.Consumable,
                HealAmount = 20,
                Icon = ResourceLoader.Load<Texture2D>("res://assets/items/sprites/item_sprite_0021.png"),
            }
        },
        {
            "egg_tea", new Item
            {
                Id = "egg_tea",
                DisplayName = "Egg Tea",
                Description = "A calm, quiet sip. Clears the head. (+15 HP)",
                Category = ItemCategory.Consumable,
                HealAmount = 15,
                Icon = ResourceLoader.Load<Texture2D>("res://assets/items/sprites/item_sprite_0022.png"),
            }
        },
    };

    public static Item Get(string id) =>
        All.TryGetValue(id, out Item item) ? item : null;
}