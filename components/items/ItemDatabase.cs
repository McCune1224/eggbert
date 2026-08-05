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
        // --- Additional consumables (demo plan Step A2) ---
        {
            "omelette", new Item
            {
                Id = "omelette", DisplayName = "Omelette", Category = ItemCategory.Consumable,
                Description = "A fluffy omelette. Restores 80 HP.",
                HealAmount = 80,
            }
        },
        {
            "poached_egg", new Item
            {
                Id = "poached_egg", DisplayName = "Poached Egg", Category = ItemCategory.Consumable,
                Description = "A delicately poached egg. Restores 50 HP.",
                HealAmount = 50,
            }
        },
        {
            "egg_nog", new Item
            {
                Id = "egg_nog", DisplayName = "Egg Nog", Category = ItemCategory.Consumable,
                Description = "A spiced mug of egg nog. Restores 15 HP.",
                HealAmount = 15,
            }
        },
        {
            "century_egg", new Item
            {
                Id = "century_egg", DisplayName = "Century Egg", Category = ItemCategory.Consumable,
                Description = "A preserved delicacy that restores all HP.",
                HealAmount = 999,
            }
        },
        // --- Combat gear (docs/combat-ui-design.md §5.2) ---
        // Weapons — change how parry works
        {
            "whisk", new Item
            {
                Id = "whisk", DisplayName = "Whisk", Slot = EquipSlot.Weapon,
                Category = ItemCategory.Equipment, AttackBoost = 2, ReflectSpeedBoost = 0.4f,
                Description = "Whisk it good. Reflected bullets fly 40% faster. +2 ATK",
            }
        },
        {
            "spatula", new Item
            {
                Id = "spatula", DisplayName = "Spatula", Slot = EquipSlot.Weapon,
                Category = ItemCategory.Equipment, ParryRadiusBoost = 22, ParryDamageBoost = -1,
                Description = "Flip more, hit softer. +22 parry radius, -1 parry damage",
            }
        },
        {
            "slotted_spoon", new Item
            {
                Id = "slotted_spoon", DisplayName = "Slotted Spoon", Slot = EquipSlot.Weapon,
                Category = ItemCategory.Equipment, ParryCooldownReduction = 0.2f,
                Description = "Packed with holes, ready for rapid parries. -0.2s parry cooldown",
            }
        },
        {
            "frying_pan", new Item
            {
                Id = "frying_pan", DisplayName = "Cast Iron Frying Pan", Slot = EquipSlot.Weapon,
                Category = ItemCategory.Equipment, ParryDamageBoost = 4, ReflectExplosionRadius = 30f,
                Description = "Cast iron. Reflected bullets explode for 30px splash. +4 parry damage",
            }
        },
        {
            "ladle", new Item
            {
                Id = "ladle", DisplayName = "Ladle", Slot = EquipSlot.Weapon,
                Category = ItemCategory.Equipment, ParryHeal = 3,
                Description = "Serves up healing soup. Parry restores 3 HP per reflected bullet",
            }
        },
        {
            "egg_timer", new Item
            {
                Id = "egg_timer", DisplayName = "Egg Timer", Slot = EquipSlot.Weapon,
                Category = ItemCategory.Equipment, BulletSlowFactor = 0.25f,
                Description = "Time waits for no one. Slows enemy bullets 25%",
            }
        },
        // Armor — survivability with trade-offs
        {
            "pot_lid", new Item
            {
                Id = "pot_lid", DisplayName = "Pot Lid", Slot = EquipSlot.Armor,
                Category = ItemCategory.Equipment, DefenseBoost = 6, ParryRadiusBoost = 15,
                Description = "A sturdy lid. +6 DEF, +15 parry radius",
            }
        },
        {
            "bubble_wrap", new Item
            {
                Id = "bubble_wrap", DisplayName = "Bubble Wrap", Slot = EquipSlot.Armor,
                Category = ItemCategory.Equipment, BlockCharges = 3,
                Description = "Pop! Blocks the first 3 hits of each battle",
            }
        },
        {
            "stained_apron", new Item
            {
                Id = "stained_apron", DisplayName = "Stained Apron", Slot = EquipSlot.Armor,
                Category = ItemCategory.Equipment, DefenseBoost = 6, RegenPerSecond = 2,
                Description = "Worn from years of service. +6 DEF, regen 2 HP/s in combat",
            }
        },
        {
            "tin_foil_hat", new Item
            {
                Id = "tin_foil_hat", DisplayName = "Tin Foil Hat", Slot = EquipSlot.Armor,
                Category = ItemCategory.Equipment, DefenseBoost = 3, HomingResistance = 0.5f,
                Description = "Blocks the mind-reading rays. +3 DEF, homing bullets 50% weaker",
            }
        },
        {
            "silicone_mat", new Item
            {
                Id = "silicone_mat", DisplayName = "Silicone Baking Mat", Slot = EquipSlot.Armor,
                Category = ItemCategory.Equipment, DefenseBoost = 4, DashCooldownReduction = 0.15f,
                Description = "Non-stick, extra dashy. +4 DEF, dash cooldown -0.15s",
            }
        },
        {
            "cracked_carton", new Item
            {
                Id = "cracked_carton", DisplayName = "Cracked Carton", Slot = EquipSlot.Armor,
                Category = ItemCategory.Equipment, DefenseBoost = 12, SpeedBoost = -15,
                Description = "Heavy but tough. +12 DEF, -15% move speed",
            }
        },
        {
            "hardboiled_shell", new Item
            {
                Id = "hardboiled_shell", DisplayName = "Hardboiled Shell", Slot = EquipSlot.Armor,
                Category = ItemCategory.Equipment, DefenseBoost = 20, SpeedBoost = -20,
                Description = "Rock solid. +20 DEF, -20% move speed",
            }
        },
        // Accessories — weird, build-defining
        {
            "butter", new Item
            {
                Id = "butter", DisplayName = "Butter", Slot = EquipSlot.Accessory,
                Category = ItemCategory.Equipment, BulletSlowFactor = 0.2f,
                Description = "Everything's better with butter. Slows enemy bullets 20%",
            }
        },
        {
            "molasses", new Item
            {
                Id = "molasses", DisplayName = "Molasses", Slot = EquipSlot.Accessory,
                Category = ItemCategory.Equipment, BulletSlowFactor = 0.35f, SpeedBoost = -10,
                Description = "Thick and slow. Bullets 35% slower, but you move 10% slower too",
            }
        },
        {
            "hourglass", new Item
            {
                Id = "hourglass", DisplayName = "Hourglass", Slot = EquipSlot.Accessory,
                Category = ItemCategory.Equipment, BulletTimeZoneSeconds = 2f, BulletTimeZoneSlow = 0.5f,
                Description = "Dash drops a 2s bullet-time zone. Bullets inside move at 50% speed",
            }
        },
        {
            "graze_charm", new Item
            {
                Id = "graze_charm", DisplayName = "Graze Charm", Slot = EquipSlot.Accessory,
                Category = ItemCategory.Equipment, GrazeRadiusBoost = 40,
                Description = "Dance closer. +40 graze radius",
            }
        },
        {
            "lucky_horseshoe", new Item
            {
                Id = "lucky_horseshoe", DisplayName = "Lucky Horseshoe", Slot = EquipSlot.Accessory,
                Category = ItemCategory.Equipment, EvadeChance = 0.15f,
                Description = "Knock on wood. 15% chance to ignore damage",
            }
        },
        {
            "rubber_band", new Item
            {
                Id = "rubber_band", DisplayName = "Rubber Band", Slot = EquipSlot.Accessory,
                Category = ItemCategory.Equipment, BounceCount = 1,
                Description = "Snappy. Reflected bullets bounce once off walls",
            }
        },
        {
            "stopwatch", new Item
            {
                Id = "stopwatch", DisplayName = "Stopwatch", Slot = EquipSlot.Accessory,
                Category = ItemCategory.Equipment, TelegraphBoost = 0.3f,
                Description = "The whole kitchen slows down. Enemy windups last 30% longer",
            }
        },
        {
            "wedding_ring", new Item
            {
                Id = "wedding_ring", DisplayName = "Wedding Ring", Slot = EquipSlot.Accessory,
                Category = ItemCategory.Equipment, ParryDamageBoost = 5,
                Description = "Til death do us part. +5 parry damage",
            }
        },
        {
            "sunglasses", new Item
            {
                Id = "sunglasses", DisplayName = "Sunglasses", Slot = EquipSlot.Accessory,
                Category = ItemCategory.Equipment, ParryCooldownReduction = 0.1f, InvulnerabilityBoost = 0.5f,
                Description = "Too cool to get hit twice. -0.1s parry cooldown, +0.5s invulnerability after a hit",
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
