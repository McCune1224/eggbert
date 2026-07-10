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
}