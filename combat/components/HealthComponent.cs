using Godot;

public partial class HealthComponent : Node
{
    [Signal]
    public delegate void DamagedEventHandler(int amount, Node source);
    [Signal]
    public delegate void HealedEventHandler(int amount);
    [Signal]
    public delegate void DiedEventHandler();
    [Signal]
    public delegate void RevivedEventHandler();
    [Signal]
    public delegate void BlockedEventHandler();

    [Export] public int MaxHP { get; set; } = 100;
    [Export] public int CurrentHP { get; set; }
    [Export] public int Defense { get; set; }
    /// <summary>Base invulnerability seconds after taking damage (0 = none). Item boost via CombatStats adds on top.</summary>
    [Export] public float IframeSeconds { get; set; }

    public bool IsDead => CurrentHP <= 0;

    /// <summary>Hits remaining that are absorbed for free this combat (Bubble Wrap). Reset by CombatArena.</summary>
    public int BlockChargesRemaining { get; set; }

    private float _iframesRemaining = 0f;

    public override void _Ready()
    {
        if (CurrentHP <= 0)
            CurrentHP = MaxHP;
    }

    public override void _Process(double delta)
    {
        if (_iframesRemaining > 0f)
            _iframesRemaining -= (float)delta;
    }

    /// <summary>
    /// Applies damage after defense mitigation (min 1). Returns false when the hit was
    /// negated by iframes, evade, or an active block charge (no damage applied).
    /// </summary>
    public bool TakeDamage(int rawDamage, Node source = null)
    {
        if (IsDead) return false;

        if (_iframesRemaining > 0f)
        {
            GameLogger.Debug("Health", $"{GetParent()?.Name ?? "?"} ignored {rawDamage} dmg — iframes active");
            return false;
        }

        if (CombatStats.EvadeChance > 0f && GD.Randf() < CombatStats.EvadeChance)
        {
            GameLogger.Debug("Health", $"{GetParent()?.Name ?? "?"} evaded {rawDamage} dmg (evade {CombatStats.EvadeChance:P0})");
            return false;
        }

        if (BlockChargesRemaining > 0)
        {
            BlockChargesRemaining--;
            EmitSignal(SignalName.Blocked);
            GameLogger.Debug("Health", $"{GetParent()?.Name ?? "?"} blocked {rawDamage} dmg — {BlockChargesRemaining} charges left");
            return false;
        }

        int dmg = Mathf.Max(1, rawDamage - Defense);
        string ownerName = GetParent()?.Name ?? "?";
        string srcName = source?.Name ?? "?";
        GameLogger.Debug("Health", $"TakeDamage: {ownerName} took {dmg} (raw={rawDamage}, def={Defense}) from '{srcName}' → HP={CurrentHP}");
        CurrentHP = Mathf.Max(0, CurrentHP - dmg);
        EmitSignal(SignalName.Damaged, dmg, source);

        float iframes = IframeSeconds + CombatStats.InvulnerabilityBoost;
        if (iframes > 0f)
            _iframesRemaining = iframes;

        if (CurrentHP <= 0)
        {
            EmitSignal(SignalName.Died);
            GameLogger.Info("Health", $"'{ownerName}' died — {CurrentHP}/{MaxHP}");
        }
        return true;
    }

    /// <summary>
    /// Restores HP by <paramref name="amount"/> capped at <see cref="MaxHP"/>. Emits <see cref="Healed"/> with the actual amount healed.
    /// Does nothing if the component is dead (<see cref="IsDead"/>).
    /// </summary>
    /// <param name="amount">HP to restore.</param>
    public void Heal(int amount)
    {
        if (IsDead) return;
        int before = CurrentHP;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        GameLogger.Debug("Health", $"Heal: +{CurrentHP - before} → HP={CurrentHP}/{MaxHP}");
        EmitSignal(SignalName.Healed, CurrentHP - before);
    }

    /// <summary>
    /// Updates <see cref="MaxHP"/> and optionally refills HP. If not refilling, current HP is clamped to the new maximum
    /// so it does not exceed the new cap.
    /// </summary>
    /// <param name="newMax">The new maximum HP value.</param>
    /// <param name="refill">If true, sets CurrentHP to newMax. Otherwise clamps CurrentHP to newMax.</param>
    public void SetMaxHP(int newMax, bool refill = false)
    {
        MaxHP = newMax;
        if (refill)
            CurrentHP = MaxHP;
        else
            CurrentHP = Mathf.Min(CurrentHP, MaxHP);
    }

    /// <summary>
    /// Resurrects the entity with HP equal to the specified percentage of <see cref="MaxHP"/> (minimum 1).
    /// Emits <see cref="Revived"/> on success.
    /// </summary>
    /// <param name="hpPercent">Percentage of MaxHP to restore on revive (default 50).</param>
    public void Revive(int hpPercent = 50)
    {
        CurrentHP = Mathf.Max(1, MaxHP * hpPercent / 100);
        GameLogger.Info("Health", $"Revived → HP={CurrentHP}/{MaxHP}");
        EmitSignal(SignalName.Revived);
    }
}
