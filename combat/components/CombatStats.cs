using Godot;

/// <summary>
/// Static snapshot of the player's combat-relevant equipment modifiers,
/// refreshed by <see cref="Equipment"/> on every equip/unequip. Combat systems
/// (RedBullet, ParryComponent, HealthComponent, CombatArena, Dash) read this
/// instead of querying Equipment per-frame.
/// </summary>
public static class CombatStats
{
    public static float BulletSlowMultiplier { get; private set; } = 1f;   // ×speed for enemy bullets (0.75 = 25% slower)
    public static float HomingResistance { get; private set; } = 0f;       // 0..1 reduction applied to homing strength
    public static float ReflectSpeedMultiplier { get; private set; } = 1f; // ×speed for reflected bullets
    public static float GrazeRadiusBonus { get; private set; } = 0f;       // px added to graze radius
    public static int BlockCharges { get; private set; } = 0;              // hits absorbed per combat
    public static float RegenPerSecond { get; private set; } = 0f;         // combat HP regen
    public static float EvadeChance { get; private set; } = 0f;            // chance to ignore a hit
    public static float InvulnerabilityBoost { get; private set; } = 0f;   // seconds of iframes after a hit
    public static float ParryCooldownReduction { get; private set; } = 0f; // seconds off parry cooldown
    public static float DashCooldownReduction { get; private set; } = 0f;  // seconds off dash cooldown
    public static float TelegraphMultiplier { get; private set; } = 1f;    // ×windup duration (1.3 = 30% longer)
    public static float ReflectExplosionRadius { get; private set; } = 0f; // reflected bullets explode in this radius
    public static int ParryHealPerBullet { get; private set; } = 0;        // HP per reflected bullet
    public static int BounceCount { get; private set; } = 0;               // wall bounces for reflected bullets
    public static float BulletTimeZoneSeconds { get; private set; } = 0f;  // dash bullet-time zone duration
    public static float BulletTimeZoneSlow { get; private set; } = 0.5f;   // bullet speed inside the zone

    /// <summary>
    /// Recomputes every modifier from the currently equipped items.
    /// Called by <see cref="Equipment"/> after any equip/unequip.
    /// </summary>
    public static void Refresh()
    {
        var eq = Equipment.Instance;
        if (eq == null) return;

        BulletSlowMultiplier = Mathf.Max(0.1f, 1f - eq.GetTotalBulletSlow());
        HomingResistance = Mathf.Clamp(eq.GetTotalHomingResistance(), 0f, 0.95f);
        ReflectSpeedMultiplier = 1f + eq.GetTotalReflectSpeedBoost();
        GrazeRadiusBonus = Mathf.Max(0f, eq.GetTotalGrazeRadiusBoost());
        BlockCharges = Mathf.Max(0, eq.GetTotalBlockCharges());
        RegenPerSecond = Mathf.Max(0f, eq.GetTotalRegenPerSecond());
        EvadeChance = Mathf.Clamp(eq.GetTotalEvadeChance(), 0f, 0.8f);
        InvulnerabilityBoost = Mathf.Max(0f, eq.GetTotalInvulnerabilityBoost());
        ParryCooldownReduction = Mathf.Max(0f, eq.GetTotalParryCooldownReduction());
        DashCooldownReduction = Mathf.Max(0f, eq.GetTotalDashCooldownReduction());
        TelegraphMultiplier = Mathf.Max(1f, 1f + eq.GetTotalTelegraphBoost());
        ReflectExplosionRadius = Mathf.Max(0f, eq.GetTotalReflectExplosionRadius());
        ParryHealPerBullet = Mathf.Max(0, eq.GetTotalParryHeal());
        BounceCount = Mathf.Max(0, eq.GetTotalBounceCount());
        BulletTimeZoneSeconds = Mathf.Max(0f, eq.GetTotalBulletTimeZoneSeconds());
        BulletTimeZoneSlow = Mathf.Clamp(eq.GetTotalBulletTimeZoneSlow(), 0.2f, 0.95f);
    }
}
