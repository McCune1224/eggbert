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
    [Export] public float ReflectExplosionRadius { get; set; }  // 0 = off; reflected bullets explode in this radius (Frying Pan)
    [Export] public int ParryHeal { get; set; }                 // HP restored per reflected bullet (Ladle)
    [Export] public int BounceCount { get; set; }               // wall bounces for reflected bullets (Rubber Band)
    [Export] public float BulletTimeZoneSeconds { get; set; }   // dash drops a bullet-time zone for this long (Hourglass)
    [Export] public float BulletTimeZoneSlow { get; set; }      // bullet speed multiplier inside the zone (0.5 = half speed)

    /// <summary>
    /// Plain-language effect lines for UI (docs/combat-ui-design.md §4.3).
    /// Describes every non-zero combat behavior modifier.
    /// </summary>
    public string EffectSummary()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (BulletSlowFactor > 0.0005f) parts.Add($"Slows enemy bullets {BulletSlowFactor * 100f:0}%");
        if (ParryCooldownReduction > 0.0005f) parts.Add($"Parry cooldown -{ParryCooldownReduction:0.0#}s");
        if (ReflectSpeedBoost > 0.0005f) parts.Add($"Reflects bullets {ReflectSpeedBoost * 100f:0}% faster");
        if (GrazeRadiusBoost > 0.0005f) parts.Add($"+{GrazeRadiusBoost:0} graze radius");
        if (HomingResistance > 0.0005f) parts.Add($"Homing bullets {HomingResistance * 100f:0}% weaker");
        if (BlockCharges > 0) parts.Add($"Blocks the first {BlockCharges} hits of each battle");
        if (RegenPerSecond > 0.0005f) parts.Add($"Regenerates {RegenPerSecond:0.#} HP/s in combat");
        if (EvadeChance > 0.0005f) parts.Add($"{EvadeChance * 100f:0}% chance to ignore damage");
        if (InvulnerabilityBoost > 0.0005f) parts.Add($"+{InvulnerabilityBoost:0.0#}s invulnerability after a hit");
        if (DashCooldownReduction > 0.0005f) parts.Add($"Dash cooldown -{DashCooldownReduction:0.0#}s");
        if (TelegraphBoost > 0.0005f) parts.Add($"Enemy windups last {TelegraphBoost * 100f:0}% longer");
        if (ParryRadiusBoost > 0.0005f) parts.Add($"+{ParryRadiusBoost:0} parry radius");
        if (ParryDamageBoost != 0) parts.Add($"{(ParryDamageBoost > 0 ? "+" : "")}{ParryDamageBoost} parry damage");
        if (ReflectExplosionRadius > 0.0005f) parts.Add($"Reflected bullets explode ({ReflectExplosionRadius:0}px splash)");
        if (ParryHeal > 0) parts.Add($"Parry restores {ParryHeal} HP per reflected bullet");
        if (BounceCount > 0) parts.Add($"Reflected bullets bounce {BounceCount} time(s) off walls");
        if (BulletTimeZoneSeconds > 0.0005f) parts.Add($"Dash drops a {BulletTimeZoneSeconds:0.#}s bullet-time zone (bullets at {BulletTimeZoneSlow * 100f:0}%)");
        return string.Join("\n", parts);
    }
}