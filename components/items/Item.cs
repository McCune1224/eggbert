using Godot;

public enum ItemCategory { Key, Consumable, Equipment }

public enum EquipSlot { None, Weapon, Armor, Accessory }

/// <summary>
/// Flat item definition. One resource covers all three categories —
/// consumable/equipment fields are simply ignored when not applicable.
/// ponytail: no subclasses, fields are 0/N/A by default.
/// </summary>
[GlobalClass]
public partial class Item : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string DisplayName { get; set; }
    [Export] public string Description { get; set; }
    [Export] public Texture2D Icon { get; set; }
    [Export] public string DescriptionUsed { get; set; }
    [Export] public ItemCategory Category { get; set; }

    // Consumable — used once HealthComponent exists.
    [Export] public int HealAmount { get; set; }

    // Equipment — applied by Equipment autoload when equipped.
    [Export] public EquipSlot Slot { get; set; }
    [Export] public int AttackBoost { get; set; }
    [Export] public int DefenseBoost { get; set; }
    [Export] public int SpeedBoost { get; set; }
    [Export] public int MaxHPBoost { get; set; }
    [Export] public float ParryRadiusBoost { get; set; }
    [Export] public int ParryDamageBoost { get; set; }

    // --- Combat behavior modifiers (docs/combat-ui-design.md §5.1) ---
    [Export] public float BulletSlowFactor { get; set; }        // 0.25 = enemy bullets 25% slower
    [Export] public float ParryCooldownReduction { get; set; }  // seconds off the 0.5s parry cooldown
    [Export] public float ReflectSpeedBoost { get; set; }       // 0.4 = reflected bullets +40% speed
    [Export] public float GrazeRadiusBoost { get; set; }        // px added to graze detection
    [Export] public float HomingResistance { get; set; }        // 0.5 = homing strength halved
    [Export] public int BlockCharges { get; set; }              // hits absorbed per combat
    [Export] public float RegenPerSecond { get; set; }          // combat HP regen
    [Export] public float EvadeChance { get; set; }             // 0.15 = 15% chance to ignore damage
    [Export] public float InvulnerabilityBoost { get; set; }    // seconds of iframes after a hit
    [Export] public float DashCooldownReduction { get; set; }   // seconds off dash cooldown
    [Export] public float TelegraphBoost { get; set; }          // 0.3 = enemy telegraphs 30% longer
}