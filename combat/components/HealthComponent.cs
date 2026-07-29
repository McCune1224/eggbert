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

    [Export] public int MaxHP { get; set; } = 100;
    [Export] public int CurrentHP { get; set; }
    [Export] public int Defense { get; set; }

    public bool IsDead => CurrentHP <= 0;

    public override void _Ready()
    {
        if (CurrentHP <= 0)
            CurrentHP = MaxHP;
    }

    /// <summary>
    /// Applies damage after reducing it by <see cref="Defense"/>. The minimum dealt damage is 1 (damage formula: max(1, raw - def)).
    /// Emits <see cref="Damaged"/> and, if HP reaches zero, emits <see cref="Died"/>.
    /// </summary>
    /// <param name="rawDamage">Unreduced damage amount before defense mitigation.</param>
    /// <param name="source">Optional node that originated the damage (used for logging).</param>
    public void TakeDamage(int rawDamage, Node source = null)
    {
        if (IsDead) return;
        int dmg = Mathf.Max(1, rawDamage - Defense);
        string ownerName = GetParent()?.Name ?? "?";
        string srcName = source?.Name ?? "?";
        GameLogger.Debug("Health", $"TakeDamage: {ownerName} took {dmg} (raw={rawDamage}, def={Defense}) from '{srcName}' → HP={CurrentHP}");
        CurrentHP = Mathf.Max(0, CurrentHP - dmg);
        EmitSignal(SignalName.Damaged, dmg, source);
        if (CurrentHP <= 0)
        {
            EmitSignal(SignalName.Died);
            GameLogger.Info("Health", $"'{ownerName}' died — {CurrentHP}/{MaxHP}");
        }
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
