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

    public void TakeDamage(int rawDamage, Node source = null)
    {
        if (IsDead) return;
        int dmg = Mathf.Max(1, rawDamage - Defense);
        CurrentHP = Mathf.Max(0, CurrentHP - dmg);
        EmitSignal(SignalName.Damaged, dmg, source);
        if (CurrentHP <= 0)
            EmitSignal(SignalName.Died);
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        int before = CurrentHP;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        EmitSignal(SignalName.Healed, CurrentHP - before);
    }

    public void SetMaxHP(int newMax, bool refill = false)
    {
        MaxHP = newMax;
        if (refill)
            CurrentHP = MaxHP;
        else
            CurrentHP = Mathf.Min(CurrentHP, MaxHP);
    }

    public void Revive(int hpPercent = 50)
    {
        CurrentHP = Mathf.Max(1, MaxHP * hpPercent / 100);
        EmitSignal(SignalName.Revived);
    }
}
